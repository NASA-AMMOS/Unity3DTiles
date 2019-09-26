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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unity3DTiles
{
    public class AbstractTilesetBehaviour : MonoBehaviour
    {
        public Unity3DTilesetSceneOptions SceneOptions = new Unity3DTilesetSceneOptions();        
        public Unity3DTilesetStatistics Stats;
        public LRUCache<Unity3DTile> LRUCache = new LRUCache<Unity3DTile>();

        public int MaxConcurrentRequests = 6;

        public void LateUpdate()
        {
            LRUCache.MaxSize = SceneOptions.LRUCacheMaxSize;
            LRUCache.MarkAllUnused();
            this._lateUpdate();
            LRUCache.UnloadUnusedContent(SceneOptions.LRUCacheTargetSize, SceneOptions.LRUMaxFrameUnloadRatio, n => -n.Depth, t => t.UnloadContent());
        }

        protected virtual void _lateUpdate()
        {
            //override in subclass
        }
    }

    public class TilesetBehaviour : AbstractTilesetBehaviour
    {
        public Unity3DTilesetOptions TilesetOptions = new Unity3DTilesetOptions();
        public Unity3DTileset Tileset;

        public void MakeTileset()
        {
            RequestManager rm = new RequestManager(MaxConcurrentRequests);
            Tileset = new Unity3DTileset(TilesetOptions, this, rm, this.LRUCache);
            Stats = Tileset.Statistics;
        }

        public void Start()
        {
            _start();
        }

        protected virtual void _start()
        {
            MakeTileset();
        }

        protected override void _lateUpdate()
        {
            if (Tileset != null)
            {
                Tileset.Update();
            }
        }
    }
}