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
        public Unity3DTilesetSceneOptions SceneOptions = new Unity3DTilesetSceneOptions();

        public Unity3DTilesetStatistics Stats;

        public LRUCache<Unity3DTile> LRUCache = new LRUCache<Unity3DTile>();
        public Queue<Unity3DTile> ProcessingQueue = new Queue<Unity3DTile>();

        private RequestManager _requestManager;
        public RequestManager RequestManager
        {
            get
            {
                if (_requestManager == null)
                {
                    _requestManager = new RequestManager(MaxConcurrentRequests);
                }
                return _requestManager;
            }
        }

        public int MaxConcurrentRequests //for legacy api
        {
            get { return SceneOptions.MaxConcurrentRequests; } 
            set { SceneOptions.MaxConcurrentRequests = value; } 
        }

        public abstract bool Ready();

        public abstract BoundingSphere BoundingSphere(Func<Unity3DTileset, bool> filter = null);

        public abstract int DeepestDepth();

        public abstract void ClearForcedTiles();

        public void Update()
        {
            this._update();
        }

        public void LateUpdate()
        {
            LRUCache.MaxSize = SceneOptions.LRUCacheMaxSize;
            LRUCache.MarkAllUnused();
            this._lateUpdate();
            this._requestManager?.Process();
            // Move any tiles with downloaded content to the ready state
            int processed = 0;
            while (processed < this.SceneOptions.MaximumTilesToProcessPerFrame && this.ProcessingQueue.Count != 0)
            {
                var tile = this.ProcessingQueue.Dequeue();
                // We allow requests to terminate early if the (would be) tile goes out of view, so check if a tile is actually processed
                if (tile.Process())
                {
                    processed++;
                }
            }

            LRUCache.UnloadUnusedContent(SceneOptions.LRUCacheTargetSize, SceneOptions.LRUMaxFrameUnloadRatio, n => -n.Depth, t => t.UnloadContent());

            this.UpdateStats();
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
