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
    public class Unity3DTilesetOptions
    {
        //Options unique to a single tileset 

        [Tooltip("Full path URL to the tileset. Can be a local file or url as long as it is a full path")]
        public string Name = null;
        public string Url = null;
        public bool Show = true;
        public UnityEngine.Rendering.ShadowCastingMode ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        public bool RecieveShadows = true;
        public bool CreateColliders = true;

        [Tooltip("Controls how many colliders can be created per frame, this can be an expensive operation on some platforms.  Increasing this number will decrese load time but may increase frame lurches when loading tiles.")]
        public int MaximumTilesToProcessPerFrame = 1;

        [Tooltip("Controls the level of detail the tileset will be loaded to by specifying the allowed amount of on screen geometric error allowed in pixels")]
        public double MaximumScreenSpaceError = 16;

        [Tooltip("Controls what parent tiles will be skipped when loading a tileset.  This number will be multipled by MaximumScreenSpaceError and any tile with an on screen error larger than this will be skipped by the loading and rendering algorithm")]
        public double SkipScreenSpaceErrorMultiplier = 16;

        public bool LoadSiblings = true;

        public Matrix4x4 Transform = Matrix4x4.identity;

        [Tooltip("Max child depth that we should render. If this is zero, disregard")]
        /// <summary>
        /// "Max child depth that we should render. If this is zero, disregard"
        /// </summary>
        public int MaxDepth = 0;

        [Header("GLTF Loader Settings")]
        // Options for B3DM files
        public bool GLTFMultithreadedLoad = true;
        public int GLTFMaximumLOD = 300;
        public Shader GLTFShaderOverride;

        [Header("Debug Settings")]
        public bool DebugDrawBounds = false;

        public System.Func<Unity3DTile, float> TilePriority = new System.Func<Unity3DTile, float>(tile =>
        {
            return (float)(tile.Depth - 1.0 / tile.FrameState.DistanceToCamera);
        });
    }

    [System.Serializable]
    public class Unity3DTilesetSceneOptions
    {
        //Options shared between tilesets in a scene, extended below

        public List<Camera> ClippingCameras;

        [Tooltip("Sets the target maximum number of tiles that can be loaded into memory at any given time.  Beyond this limit, unused tiles will be unloaded as new requests are made.")]
        /// <summary>
        /// Target max number of items in LRU cache
        /// </summary>
        public int LRUCacheTargetSize = 600;

        [Tooltip("Sets the maximum number of tiles (hard limit) that can be loaded into memory at any given time. Requests that would exceed this limit fail.")]
        public int LRUCacheMaxSize = 700;

        /// <summary>
        /// Controls the maximum number of unused tiles that will be unloaded at a time
        /// When the cache is full.  This is specified as a ratio of the LRUMaxCacheSize.
        /// For example, if this is set to 0.2 and LRUMaxCacheSize is 600 then at most we will
        /// unload 120 (0.2*600) tiles in a single frame.
        /// </summary>
        public float LRUMaxFrameUnloadRatio = 0.2f;
    }

    public class LegacyTilesetOptions 
    {
        //includes both tileset and scene options

        [Tooltip("Full path URL to the tileset. Can be a local file or url as long as it is a full path")]
        public string Url = null;
        public bool Show = true;
        public UnityEngine.Rendering.ShadowCastingMode ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        public bool RecieveShadows = true;
        public bool CreateColliders = true;

        [Tooltip("Controls how many colliders can be created per frame, this can be an expensive operation on some platforms.  Increasing this number will decrese load time but may increase frame lurches when loading tiles.")]
        public int MaximumTilesToProcessPerFrame = 1;

        [Tooltip("Controls the level of detail the tileset will be loaded to by specifying the allowed amount of on screen geometric error allowed in pixels")]
        public double MaximumScreenSpaceError = 16;

        [Tooltip("Controls what parent tiles will be skipped when loading a tileset.  This number will be multipled by MaximumScreenSpaceError and any tile with an on screen error larger than this will be skipped by the loading and rendering algorithm")]
        public double SkipScreenSpaceErrorMultiplier = 16;

        public bool LoadSiblings = true;

        public List<Camera> ClippingCameras;

        [Tooltip("Max child depth that we should render. If this is zero, disregard")]
        /// <summary>
        /// "Max child depth that we should render. If this is zero, disregard"
        /// </summary>
        public int MaxDepth = 0;

        [Tooltip("Sets the maximum number of tiles (hard limit) that can be loaded into memory at any given time. Requests that would exceed this limit fail.")]
        public int LRUCacheMaxSize = 600;

        /// <summary>
        /// Controls the maximum number of unused tiles that will be unloaded at a time
        /// When the cache is full.  This is specified as a ratio of the LRUMaxCacheSize.
        /// For example, if this is set to 0.2 and LRUMaxCacheSize is 600 then at most we will
        /// unload 120 (0.2*600) tiles in a single frame.
        /// </summary>
        public float LRUMaxFrameUnloadRatio = 0.2f;

        //Options unique to a single tileset 

        [Header("GLTF Loader Settings")]
        // Options for B3DM files
        public bool GLTFMultithreadedLoad = true;
        public int GLTFMaximumLOD = 300;
        public Shader GLTFShaderOverride;

        [Header("Debug Settings")]
        public bool DebugDrawBounds = false;

        public Unity3DTilesetSceneOptions GetSceneOptions()
        {
            Unity3DTilesetSceneOptions opts = new Unity3DTilesetSceneOptions();
            opts.ClippingCameras = this.ClippingCameras;
            opts.LRUCacheMaxSize = this.LRUCacheMaxSize;
            opts.LRUCacheTargetSize = (int)(0.9 * this.LRUCacheMaxSize);
            opts.LRUMaxFrameUnloadRatio = this.LRUMaxFrameUnloadRatio;
            return opts;
        }

        public Unity3DTilesetOptions GetTilesetOptions()
        {
            Unity3DTilesetOptions opts = new Unity3DTilesetOptions();     
            opts.Name = this.Url;
            opts.Url = this.Url;
            opts.Show = this.Show;
            opts.ShadowCastingMode = this.ShadowCastingMode;
            opts.RecieveShadows = this.RecieveShadows;
            opts.CreateColliders = this.CreateColliders;     
            opts.MaximumTilesToProcessPerFrame = this.MaximumTilesToProcessPerFrame;
            opts.MaximumScreenSpaceError = this.MaximumScreenSpaceError;
            opts.SkipScreenSpaceErrorMultiplier = this.SkipScreenSpaceErrorMultiplier;
            opts.LoadSiblings = this.LoadSiblings;
            opts.MaxDepth = 0;
            opts.GLTFMultithreadedLoad = this.GLTFMultithreadedLoad;
            opts.GLTFMaximumLOD = this.GLTFMaximumLOD;
            opts.GLTFShaderOverride = this.GLTFShaderOverride;
            opts.DebugDrawBounds = false;
            return opts;
        }
    }

}