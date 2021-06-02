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
using Newtonsoft.Json;

namespace Unity3DTiles
{
    [Serializable] //Serializable so it will show up in Unity editor inspector
    public class Unity3DTilesetOptions
    {
        //Options unique to a single tileset 

        [Tooltip("Unique name of tileset. Defaults to Url if null or empty.")]
        public string Name = null;

        [Tooltip("Full path URL to the tileset. Can be a local file or url as long as it is a full path, or can start with StreamingAssets.")]
        public string Url = null;

        [Tooltip("Whether tileset is initially visible.")]
        public bool Show = true;

        [Tooltip("Controls the level of detail the tileset will be loaded to by specifying the allowed amount of on screen geometric error allowed in pixels")]
        public double MaximumScreenSpaceError = 8;

        [Tooltip("Controls what parent tiles will be skipped when loading a tileset.  This number will be multipled by MaximumScreenSpaceError and any tile with an on screen error larger than this will be skipped by the loading and rendering algorithm")]
        public double SkipScreenSpaceErrorMultiplier = 64;

        [Tooltip("If a tile is in view and needs to be rendered, also load its siblings even if they are not visible.  Especially useful when using colliders so that raycasts outside the users field of view can succeed.  Increases load time and number of tiles that need to be stored in memory.")]
        public bool LoadSiblings = true;

        [Header("Root Transform")]

        [Tooltip("Tileset translation in right-handed tileset coordinates.")]
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 Translation = Vector3.zero;

        [Tooltip("Tileset rotation in right-handed tileset coordinates.")] 
        [JsonConverter(typeof(QuaternionConverter))]
#if UNITY_EDITOR
        [EulerAngles]
#endif
        public Quaternion Rotation = Quaternion.identity;

        [Tooltip("Tileset scale in right-handed tileset coordinates.")]
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 Scale = Vector3.one;

        [Tooltip("Max child depth that we should render. If this is zero, disregard")]
        public int MaxDepth = 0;

        [Header("GLTF Loader Settings")]
        public bool GLTFMultithreadedLoad = true;

        public int GLTFMaximumLOD = 300;

        [Tooltip("Overrides shader override of individual tilesets (e.g. \"Unlit/Texture\").  If set to null UnityGLTF will use the StandardShader for GLTF assets.  This can have dramatic performance impacts on resource constrained devices.  This allows a different shader to be used when instantiating GLTF assets.  Also see Style.")]
        public string ShaderOverride = null;

        [Tooltip("A flexible way to change style properties such as shaders at runtime on a tile by tile basis.")]
        [JsonIgnore]
        public Unity3DTilesetStyle Style = null;
        
        public IndexMode LoadIndices = IndexMode.Default;

        public UnityEngine.Rendering.ShadowCastingMode ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        public bool RecieveShadows = true;

        public bool CreateColliders = true;

        public bool DebugDrawBounds = false;

        //tiles are loaded in order or priority, so *lower* priority values load before higher
        //if null a default prioritization is used, see Unity3DTilesetTraversal.TilePriority()
        [JsonIgnore]
        public Func<Unity3DTile, float> TilePriority = null;
    }

    [Serializable] //Serializable so it will show up in Unity editor inspector
    public class Unity3DTilesetSceneOptions
    {
        //Options shared between tilesets in a scene

        [Tooltip("Pause new tile requests if the used memory size grows beyond this size in bytes.")]
        public long PauseMemThreshold = -1;
        
        [Tooltip("Periodically request garbage collection if the used memory size grows beyond this size in bytes.")]
        public long GCMemThreshold = -1;

        [Tooltip("Controls how many colliders can be created per frame, this can be an expensive operation on some platforms.  Increasing this number will decrease load time but may increase frame lurches when loading tiles.")]
        public int MaximumTilesToProcessPerFrame = 1;

        [Tooltip("Sets the target maximum number of tiles that can be loaded into memory at any given time.  Beyond this limit, unused tiles will be unloaded as new requests are made.")]
        public int CacheTargetSize = 600;

        [Tooltip("Sets the maximum number of tiles (hard limit) that can be loaded into memory at any given time. Requests that would exceed this limit fail.")]
        public int CacheMaxSize = 1000;

        [Tooltip("Controls the maximum number of unused tiles that will be unloaded at a time when the cache is full.  This is specified as a ratio of the MaxCacheSize. For example, if this is set to 0.2 and MaxCacheSize is 600 then at most we will unload 120 (0.2*600) tiles in a single frame.")]
        public float MaxCacheUnloadRatio = 0.2f;

        [Tooltip("Manages how many downloads can occurs simultaneously.  Larger results in faster load times but this should be tuned for the particular platform you are deploying to.")]
        public int MaxConcurrentRequests = 6;

        [Tooltip("Overrides shader override of individual tilesets (e.g. \"Unlit/Texture\").")]
        public string ShaderOverride = null;

        [Tooltip("Optional style, if set it is used as the default style for tilesets that don't have their own.")]
        [JsonIgnore]
        public Unity3DTilesetStyle Style = null;

        public IndexMode LoadIndices = IndexMode.Default;

        [Tooltip("The set of cameras that should be used to determine which tiles to load.  Typically this will just be the main camera (and that is the default if not specified).  Adding more cameras will decrease performance.")]
        [JsonIgnore]
        public List<Camera> ClippingCameras = new List<Camera>();

        [Header("Default Camera Pose")]

        [Tooltip("Camera translation in right-handed tileset coordinates.")]
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 DefaultCameraTranslation = new Vector3(0, 0, -10);

        [Tooltip("Camera rotation in right-handed tileset coordinates.")]
        [JsonConverter(typeof(QuaternionConverter))]
#if UNITY_EDITOR
        [EulerAngles]
#endif
        public Quaternion DefaultCameraRotation = Quaternion.identity;
    }
}
