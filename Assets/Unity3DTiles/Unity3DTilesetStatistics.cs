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
    [System.Serializable]
    public class Unity3DTilesetStatistics
    {
        [Header("Frame statistics")]
        public int FrustumSetCount;
        public int UsedSetCount;
        public int VisibleTileCount;
        public int ColliderTileCount;
        public int VisibleFaces;
        public int VisibleTextures;
        public int VisiblePixels;
        
        [Header("Dataset statistics")]
        public int NumberOfTilesTotal; // Number of tiles in tileset.json (and other tileset.json files as they are loaded)
        public int LoadedContentCount; // Number of tiles with loaded content
        public int ProcessingTiles;    // Number of tiles still being processed
        public int RequestQueueLength; // Number of tiles waiting to be downloaded
        public int ConcurrentRequests; // Number of tiles being downloaded
        public int TotalTilesLoaded;   // Number of tiles downloaded since start
        public int TilesLeftToLoad;    // Number of tiles left to load for the current viewpoint
        public int LeafContentRequired;// Number of leaf tiles with content (loaded or not loaded)
        public int LeafContentLoaded;  // Number of leaf tiles with content that has been loaded
        public int RequestsThisFrame;  // Number of requests processed this frame
        public bool NetworkError;      // Indicates if the most recent network request succeeded or failed

        public float LeafLoadProgress
        {
            get
            {
                if(LeafContentRequired == 0)
                {
                    return 0;
                }
                return LeafContentLoaded / (float)LeafContentRequired;
            }
        }

        public static Unity3DTilesetStatistics aggregate(params Unity3DTilesetStatistics[] stats)
        {
            Unity3DTilesetStatistics ret = new Unity3DTilesetStatistics();
            ret.Clear();

            if(stats.Length == 0)
            {
                return ret;
            }

            ret.NumberOfTilesTotal = 0;
            ret.LoadedContentCount = 0;
            ret.ProcessingTiles = 0;
            ret.RequestQueueLength = stats[0].RequestQueueLength; //Cache informed statistics shared between tilesets, do not sum
            ret.ConcurrentRequests = stats[0].ConcurrentRequests;
            ret.TotalTilesLoaded = 0;

            foreach (var stat in stats)
            {
                ret.FrustumSetCount += stat.FrustumSetCount;
                ret.UsedSetCount += stat.UsedSetCount;
                ret.VisibleTileCount += stat.VisibleTileCount;
                ret.ColliderTileCount += stat.ColliderTileCount;
                ret.VisibleFaces += stat.VisibleFaces;
                ret.VisibleTextures += stat.VisibleTextures;
                ret.VisiblePixels += stat.VisiblePixels;
                         
                ret.NumberOfTilesTotal += stat.NumberOfTilesTotal;
                ret.LoadedContentCount += stat.LoadedContentCount;
                ret.ProcessingTiles += stat.ProcessingTiles;
                ret.TotalTilesLoaded += stat.TotalTilesLoaded;
                ret.LeafContentRequired += stat.LeafContentRequired;
                ret.LeafContentLoaded += stat.LeafContentLoaded;
                ret.RequestsThisFrame += stat.RequestsThisFrame;
                ret.NetworkError = ret.NetworkError || stat.NetworkError;
            }
            ret.TilesLeftToLoad = ret.RequestQueueLength + ret.ConcurrentRequests + ret.ProcessingTiles; //Combination of tileset and cache stats
            return ret;
        }

        public void Clear()
        {
            FrustumSetCount = 0;
            UsedSetCount = 0;
            VisibleTileCount = 0;
            ColliderTileCount = 0;
            VisibleFaces = 0;
            VisibleTextures = 0;
            VisiblePixels = 0;
            LeafContentRequired = 0;
            LeafContentLoaded = 0;
            RequestsThisFrame = 0;
            NetworkError = false;
        }
    }
}
