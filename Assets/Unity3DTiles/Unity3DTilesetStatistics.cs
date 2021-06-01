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
        public int UsedSet;
        public int FrustumSet;
        public int ColliderSet;

        public int VisibleTiles;
        public int VisibleFaces;
        public int VisibleTextures;
        public int VisiblePixels;

        public int MinVisibleTileDepth = -1;
        public int MaxVisibleTileDepth = -1;
        
        public int RequestsThisFrame;  // Number of download requests processed this frame
        public int NetworkErrorsThisFrame;  // Number of network errors this frame

        [Header("Dataset statistics")]
        public int NumberOfTilesTotal; // #tiles in tileset(s)

        public int RequestQueueLength; // #tiles waiting to be downloaded
        public int ActiveDownloads; // #tiles being downloaded
        public int ProcessingQueueLength; // #tiles waiting to be processed

        public int DownloadedTiles; // #tiles with downloaded content (may still be processing)
        public int ReadyTiles;   // #tiles loaded and processed

        public static int MaxLoadedTiles; // maximum #tiles that can be loaded at a time

        //was TilesLeftToLoad
        public int PendingTiles // #tiles waiting to be downloaded, being downloaded, or waiting to process
        {
            get { return RequestQueueLength + ActiveDownloads + ProcessingQueueLength; }
        }

        public float LoadProgress
        {
            get { return Mathf.Clamp01( ReadyTiles / (float)Mathf.Min(UsedSet, MaxLoadedTiles)); }
        }

        public void TallyVisibleTile(Unity3DTile tile)
        {
            VisibleTiles += 1;
            if (tile.Content != null)
            {
                VisibleFaces += tile.Content.FaceCount;
                VisibleTextures += tile.Content.TextureCount;
                VisiblePixels += tile.Content.PixelCount;
                MinVisibleTileDepth = MinPositive(MinVisibleTileDepth, tile.Depth);
                MaxVisibleTileDepth = MaxPositive(MaxVisibleTileDepth, tile.Depth);
            }
        }

        public static Unity3DTilesetStatistics Aggregate(params Unity3DTilesetStatistics[] stats)
        {
            Unity3DTilesetStatistics ret = new Unity3DTilesetStatistics();
            ret.Clear();

            if (stats.Length == 0)
            {
                return ret;
            }

            foreach (var stat in stats)
            {
                ret.UsedSet += stat.UsedSet;
                ret.FrustumSet += stat.FrustumSet;
                ret.VisibleTiles += stat.VisibleTiles;
                ret.ColliderSet += stat.ColliderSet;

                ret.VisibleFaces += stat.VisibleFaces;
                ret.VisibleTextures += stat.VisibleTextures;
                ret.VisiblePixels += stat.VisiblePixels;

                ret.MinVisibleTileDepth = MinPositive(ret.MinVisibleTileDepth, stat.MinVisibleTileDepth);
                ret.MaxVisibleTileDepth = MaxPositive(ret.MaxVisibleTileDepth, stat.MaxVisibleTileDepth);

                ret.RequestsThisFrame += stat.RequestsThisFrame;
                ret.NetworkErrorsThisFrame += stat.NetworkErrorsThisFrame;

                ret.NumberOfTilesTotal += stat.NumberOfTilesTotal;

                ret.RequestQueueLength += stat.RequestQueueLength;
                ret.ActiveDownloads += stat.ActiveDownloads;
                ret.ProcessingQueueLength += stat.ProcessingQueueLength;

                ret.DownloadedTiles += stat.DownloadedTiles;
                ret.ReadyTiles += stat.ReadyTiles;
            }

            return ret;
        }

        public void Clear()
        {
            UsedSet = 0;
            FrustumSet = 0;
            ColliderSet = 0;

            VisibleTiles = 0;
            VisibleFaces = 0;
            VisibleTextures = 0;
            VisiblePixels = 0;

            MinVisibleTileDepth = -1;
            MaxVisibleTileDepth = -1;

            RequestsThisFrame = 0;
            NetworkErrorsThisFrame = 0;

            //NumberOfTilesTotal = 0;

            RequestQueueLength = 0;
            ActiveDownloads = 0;
            ProcessingQueueLength = 0;

            DownloadedTiles = 0;
            ReadyTiles = 0;
        }

        private static int MinPositive(int a, int b)
        {
            if (a > 0 && b < 0)
            {
                return a;
            }
            if (a < 0 && b > 0)
            {
                return b;
            }
            if (a <= b)
            {
                return a;
            }
            return b;
        }
    
        private static int MaxPositive(int a, int b)
        {
            if (a > 0 && b < 0)
            {
                return a;
            }
            if (a < 0 && b > 0)
            {
                return b;
            }
            if (a >= b)
            {
                return a;
            }
            return b;
        }
    }
}
