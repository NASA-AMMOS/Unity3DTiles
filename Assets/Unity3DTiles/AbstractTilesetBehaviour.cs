/*
 * Copyright 2018, by the California Institute of Technology. ALL RIGHTS 
 * RESERVED. United States Government Sponsorship acknowledged. Any 
 * commercial use must be negotiated with the Office of Technology 
 * Transfer at the California Institute of Technology.
 * 
 * This software may be subject to U.S.export control laws.By accepting 
 * this software, the user agrees to comply with all applicable 
 * U.S.export laws and regulations. User has the responsibility to 
 * obtain export licenses, or other export authority as may be required 
 * before exporting such information to foreign countries or providing 
 * access to foreign persons.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unity3DTiles
{
    public abstract class AbstractTilesetBehaviour : MonoBehaviour
    {
        //not readonly to show up in inspector
        public Unity3DTilesetSceneOptions SceneOptions = new Unity3DTilesetSceneOptions();

        public Unity3DTilesetStatistics Stats; //not readonly to show up in inspector

        public RequestManager RequestManager { get; private set; }

        public TileCache TileCache { get; private set; }

        public readonly Queue<Unity3DTile> ProcessingQueue = new Queue<Unity3DTile>();

        private bool unloadAssetsPending;
        private AsyncOperation lastUnloadAssets;

        public abstract bool Ready();

        public abstract BoundingSphere BoundingSphere(Func<Unity3DTileset, bool> filter = null);

        public abstract int DeepestDepth();

        public abstract void ClearForcedTiles();

        public void RequestUnloadUnusedAssets()
        {
            unloadAssetsPending = true;
        }

        public void Update()
        {
            _update();
        }

        public void LateUpdate()
        {
            TileCache.MarkAllUnused();

            _lateUpdate();

            RequestManager.ForEachQueuedDownload(DerateUnusedTilePriority);
            RequestManager.ForEachActiveDownload(DerateUnusedTilePriority);
            TileCache.ForEach(DerateUnusedTilePriority);

            int maxNewRequests = TileCache.HasMaxSize ? TileCache.MaxSize - TileCache.Count() : -1;
            RequestManager.Process(maxNewRequests);

            int processed = 0;
            while (processed < SceneOptions.MaximumTilesToProcessPerFrame && ProcessingQueue.Count != 0)
            {
                var tile = ProcessingQueue.Dequeue();
                if (tile.Process()) //bake colliders, load indices, etc...
                {
                    // We allow requests to terminate early if the (would be) tile goes out of view, so check if a tile
                    // is actually processed
                    processed++;
                }
            }

            //if there are queued requests with higher priority than existing tiles but the TileCache cache is too full
            //to allow them to load, unload a corresponding number of lowest-priority tiles
            int ejected = 0;
            if (maxNewRequests >= 0 && maxNewRequests < SceneOptions.MaxConcurrentRequests &&
                RequestManager.Count() > maxNewRequests)
            {
                var re = RequestManager.GetEnumerator(); //high priority (low value) first
                ejected = TileCache.MarkLowPriorityUnused(t => //low priority (high value) first
                                                          re.MoveNext() &&
                                                          t.FrameState.Priority > re.Current.FrameState.Priority,
                                                          SceneOptions.MaxConcurrentRequests - maxNewRequests);
            }

            if (ejected > 0 || (TileCache.TargetSize > 0 && TileCache.Count() > TileCache.TargetSize))
            {
                //this will trigger Resources.UnloadUnusedAssets() as needed
                TileCache.UnloadUnusedContent(ejected);
            }

            if (unloadAssetsPending)
            {
                if (lastUnloadAssets == null || lastUnloadAssets.isDone)
                {
                    unloadAssetsPending = false;
                    lastUnloadAssets = Resources.UnloadUnusedAssets();
                }
            }
                
            UpdateStats();
        }

        private void DerateUnusedTilePriority(Unity3DTile tile)
        {
            if (!tile.FrameState.IsUsedThisFrame && !tile.FrameState.UsedLastFrame)
            {
                if (tile.FrameState.LastVisitedFrame < 0)
                {
                    tile.FrameState.Priority = float.MaxValue;
                }
                else if (!float.IsNaN(tile.FrameState.Priority) && tile.FrameState.Priority > 0 &&
                         tile.FrameState.Priority < float.MaxValue)
                {
                    float maxFrames = 1000;
                    float framesSinceUsed = Time.frameCount - tile.FrameState.LastVisitedFrame;
                    if (framesSinceUsed > 0 && framesSinceUsed <= maxFrames)
                    {
                        tile.FrameState.Priority *= (maxFrames + framesSinceUsed) / (maxFrames + framesSinceUsed - 1);
                    }
                }
            }
        }

        protected virtual void UpdateStats()
        {
            //override in subclass
        }

        protected virtual void _update()
        {
            //override in subclass
        }

        protected virtual void _lateUpdate()
        {
            //override in subclass
        }

        public void Start()
        {
            RequestManager = new RequestManager(SceneOptions);
            TileCache = new TileCache(SceneOptions);

            if (SceneOptions.ClippingCameras.Count == 0)
            {
                SceneOptions.ClippingCameras.Add(Camera.main);
            }

            _start();
        }

        protected virtual void _start()
        {
            //override in subclass
        }
    }
}
