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
using System.IO;
using UnityEngine;
using RSG;
using Unity3DTiles.Schema;

namespace Unity3DTiles
{
    public enum Unity3DTileContentState  {
        UNLOADED = 0,   // Has never been requested
        LOADING = 1,    // Is waiting on a pending request
        PROCESSING = 2, // Request received.  Contents are being processed for rendering.  Depending on the content, it might make its own requests for external data.
        READY = 3      // Ready to render.
    };

    public enum Unity3DTileContentType
    {
        B3DM,
        PNTS,
        JSON,
        Unknown
    }

    [Serializable]
    public class Unity3DTileFrameState
    {
        public int LastVisitedFrame = -1;
        
        public bool InFrustumSet = false;  // Currently in view of a camera (And in this frames "Used set")
        public bool InUsedSet = false;     // should be checked with IsUsedThisFrame
        public bool IsUsedSetLeaf = false; 
        public bool InRenderSet = false;   // This tile should be rendered this frame
        public bool InColliderSet = false; // This tile should have its collider enabled this frame
        public bool UsedLastFrame = false; // This tile was in the used set last frame
        public float DistanceToCamera = float.MaxValue;
        public float PixelsToCameraCenter = float.MaxValue;
        public float ScreenSpaceError = 0;
        public float Priority = float.MaxValue; // lower value means higher priority
        
        public void MarkUsed()
        {
            InUsedSet = true;
        }
        
        public bool IsUsedThisFrame
        {
            get
            {
                return InUsedSet && LastVisitedFrame == Time.frameCount;
            }
        }
        
        public void Reset()
        {
            if (LastVisitedFrame == Time.frameCount)
            {
                return;
            }
            LastVisitedFrame = Time.frameCount;
            InUsedSet = false;
            InFrustumSet = false;
            IsUsedSetLeaf = false;
            InRenderSet = false;
            InColliderSet = false;
            DistanceToCamera = float.MaxValue;
            PixelsToCameraCenter = float.MaxValue;
            ScreenSpaceError = 0;
            Priority = float.MaxValue;
        }
    }

    public class Unity3DTileInfo : MonoBehaviour
    {
        public Unity3DTileFrameState FrameState;
        public Unity3DTile Tile;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(Unity3DTileInfo))]
    public class Unity3DTileInfoEditor : UnityEditor.Editor
    {
        private UnityEditor.SerializedProperty frameState;

        public void OnEnable()
        {
            frameState = serializedObject.FindProperty("FrameState");
        }

        public override void OnInspectorGUI()
        {
            var ti = target as Unity3DTileInfo;
            //DrawDefaultInspector();
            UnityEditor.EditorGUILayout.LabelField("Used This Frame", ti.FrameState.IsUsedThisFrame.ToString());
            UnityEditor.EditorGUILayout.LabelField("Id", ti.Tile.Id);
            UnityEditor.EditorGUILayout.LabelField("Parent", ti.Tile.Parent != null ? ti.Tile.Parent.Id : "null");
            UnityEditor.EditorGUILayout.LabelField("Depth", ti.Tile.Depth.ToString());
            UnityEditor.EditorGUILayout.LabelField("Geometric Error", ti.Tile.GeometricError.ToString());
            UnityEditor.EditorGUILayout.LabelField("Refine", ti.Tile.Refine.ToString());
            UnityEditor.EditorGUILayout.LabelField("Has Empty Content", ti.Tile.HasEmptyContent.ToString());
            UnityEditor.EditorGUILayout.LabelField("Content Url", ti.Tile.ContentUrl);
            UnityEditor.EditorGUILayout.LabelField("Content Type", ti.Tile.ContentType.ToString());
            UnityEditor.EditorGUILayout.LabelField("Content State", ti.Tile.ContentState.ToString());
            UnityEditor.EditorGUILayout.PropertyField(frameState);
        }
    }
#endif

    public class Unity3DTile
    {

        public Unity3DTileset Tileset;
        private Matrix4x4 transform;                                                // In parent tile frame
        private Matrix4x4 computedTransform;                                        // In tileset coordinate frame
        public Unity3DTileBoundingVolume BoundingVolume { get; private set; }       // In tileset coordinate frame (encloses all children)
        public Unity3DTileBoundingVolume ContentBoundingVolume { get; private set; }// In tileset coordinate frame (optional, encloses just content)
        // private Unity3DTileBoundingVolume viewerRequestVolume;                      // In tileset coordinate frame (optional, volume that user must be inside before this tile is requested or refined)

        public List<Unity3DTile> Children = new List<Unity3DTile>();
        public Unity3DTile Parent;
        public int Depth = 0;
        public string Id { get; private set; }
        public Schema.Tile schemaTile;

        public Unity3DTilesetStyle Style;

        public float GeometricError
        {
            get { return (float)schemaTile.GeometricError; }
        }

        public Schema.TileRefine Refine
        {
            get { return schemaTile.Refine.Value; }
        }
        
        public bool HasEmptyContent
        {
            get
            {
                return schemaTile.Content == null;
            }
        }

        public string ContentUrl { get; private set; }

        public Unity3DTileContentType ContentType
        {
            get
            {
                string ext = Path.GetExtension(UrlUtils.RemoveQuery(ContentUrl)).ToLower();
                if(ext.Equals(".b3dm"))
                {
                    return Unity3DTileContentType.B3DM;
                }
                if (ext.Equals(".pnts"))
                {
                    return Unity3DTileContentType.PNTS;
                }
                if (ext.Equals(".json"))
                {
                    return Unity3DTileContentType.JSON;
                }
                return Unity3DTileContentType.Unknown;
            }
        }

        public Unity3DTileContent Content { get; private set; }

        public Unity3DTileContentState ContentState;

        private int hashCode;

        public Unity3DTileFrameState FrameState = new Unity3DTileFrameState();

        public void MarkUsed()
        {
            FrameState.MarkUsed();
            Tileset.TileCache.MarkUsed(this);
        }

        public Unity3DTile(Unity3DTileset tileset, string basePath, Schema.Tile schemaTile, Unity3DTile parent)
        {
            hashCode = (int)UnityEngine.Random.Range(0, int.MaxValue);
            Tileset = tileset;
            this.schemaTile = schemaTile;
            if (schemaTile.Content != null)
            {
                Id = Path.GetFileNameWithoutExtension(schemaTile.Content.GetUri());
            }
            if (parent != null)
            {
                parent.Children.Add(this);
                Depth = parent.Depth + 1;
            }

            // TODO: Consider using a double percision Matrix library for doing 3d tiles root transform calculations
            // Set the local transform for this tile, default to identity matrix
            transform = schemaTile.UnityTransform();
            var parentTransform = (parent != null) ? parent.computedTransform : tileset.GetRootTransform();
            computedTransform = parentTransform * transform;

            BoundingVolume = CreateBoundingVolume(schemaTile.BoundingVolume, computedTransform);

            // TODO: Add 2D bounding volumes

            if (schemaTile.Content != null && schemaTile.Content.BoundingVolume.IsDefined())
            {
                // Non-leaf tiles may have a content bounding-volume, which is a tight-fit bounding volume
                // around only the features in the tile.  This box is useful for culling for rendering,
                // but not for culling for traversing the tree since it does not guarantee spatial coherence, i.e.,
                // since it only bounds features in the tile, not the entire tile, children may be
                // outside of this box.
                ContentBoundingVolume = CreateBoundingVolume(schemaTile.Content.BoundingVolume, computedTransform);
            }
            else
            {
                // Default to tile bounding volume
                ContentBoundingVolume = CreateBoundingVolume(schemaTile.BoundingVolume, computedTransform);
            }
            // TODO: Add viewer request volume support
            //if(schemaTile.ViewerRequestVolume != null && schemaTile.ViewerRequestVolume.IsDefined())
            //{
            //    viewerRequestVolume = CreateBoundingVolume(schemaTile.ViewerRequestVolume, transform);
            //}

            if (!schemaTile.Refine.HasValue)
            {
                schemaTile.Refine = (parent == null) ? Schema.TileRefine.REPLACE : parent.schemaTile.Refine.Value;
            }

            Parent = parent;
            
            if (HasEmptyContent)
            {
                ContentState = Unity3DTileContentState.READY;
            }
            else
            {
                ContentState = Unity3DTileContentState.UNLOADED;
                ContentUrl = UrlUtils.JoinUrls(basePath, schemaTile.Content.GetUri());
            }
        }

        public bool Process()
        {
            if (ContentState == Unity3DTileContentState.PROCESSING)
            {
                ContentState = Unity3DTileContentState.READY;

                Content.SetShadowMode(Tileset.TilesetOptions.ShadowCastingMode,
                                      Tileset.TilesetOptions.RecieveShadows);

                Content.Initialize(Tileset.TilesetOptions.CreateColliders);

                var indexMode = Tileset.TilesetOptions.LoadIndices;
                if (indexMode != IndexMode.Default && indexMode != IndexMode.None)
                {
                    Action<IndexMode, string, string> fail = (mode, url, msg) =>
                    {
                        //we could log a warning here, but if indices are expected but not available
                        //that might not actually be a true error condition
                        //and this would spam the log
                        if (Unity3DTileIndex.EnableLoadWarnings)
                        {
#pragma warning disable 0162
                            Debug.LogWarning("failed to load " + mode + " index for " + ContentUrl + ": " + msg);
#pragma warning restore 0162

                        }
                    };
                    Action<Unity3DTileIndex> success = index => { Content.Index = index; };
                    Tileset.Behaviour.StartCoroutine(Unity3DTileIndex.Load(indexMode, ContentUrl, success, fail));
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Lower priority will be loaded sooner
        /// </summary>
        /// <param name="priority"></param>
        public void RequestContent(float priority)
        {
            if (HasEmptyContent || ContentState != Unity3DTileContentState.UNLOADED)
            {
                return;
            }
                
            Promise<bool> finished = new Promise<bool>();
            finished.Then((success) =>
            {
                Tileset.Statistics.RequestsThisFrame++;
                Tileset.Statistics.NetworkErrorsThisFrame += success ? 0 : 1;
                bool duplicate = false;
                if (success && Tileset.TileCache.Add(this, out duplicate))
                {
                    ContentState = Unity3DTileContentState.PROCESSING;
                    Tileset.ProcessingQueue.Enqueue(this);
                }
                else if (!duplicate)
                {
                    UnloadContent();
                }                                             
            });
            
            Promise started = new Promise();
            started.Then(() =>
            {
                GameObject go = new GameObject(Id);
                go.transform.parent = Tileset.Behaviour.transform;
                go.transform.localPosition =
                    new Vector3(computedTransform.m03, computedTransform.m13, computedTransform.m23);
                go.transform.localRotation = computedTransform.rotation;
                go.transform.localScale = computedTransform.lossyScale;
                go.layer = Tileset.Behaviour.gameObject.layer;
                go.SetActive(false);
                var info = go.AddComponent<Unity3DTileInfo>();
                info.Tile = this;
                info.FrameState = FrameState;
                Content = new Unity3DTileContent(go);
                
                if (ContentType == Unity3DTileContentType.B3DM)
                {
                    B3DMComponent b3dmCo = go.AddComponent<B3DMComponent>();
                    b3dmCo.Url = ContentUrl;
                    b3dmCo.Multithreaded = Tileset.TilesetOptions.GLTFMultithreadedLoad;
                    b3dmCo.MaximumLod = Tileset.TilesetOptions.GLTFMaximumLOD;
                    if (!string.IsNullOrEmpty(Tileset.TilesetOptions.ShaderOverride))
                    {
                        b3dmCo.ShaderOverride = Shader.Find(Tileset.TilesetOptions.ShaderOverride);
                    }
                    b3dmCo.AddColliders = false;
                    b3dmCo.DownloadOnStart = false;
                    Tileset.Behaviour.StartCoroutine(b3dmCo.Download(finished));
                }
                else if (ContentType == Unity3DTileContentType.PNTS)
                {
                    PNTSComponent pntsCo = go.AddComponent<PNTSComponent>();
                    pntsCo.Url = UrlUtils.RemoveQuery(ContentUrl);
                    pntsCo.ShaderOverride = Shader.Find("Point Cloud/Point");
                    pntsCo.DownloadOnStart = false;
                    Tileset.Behaviour.StartCoroutine(pntsCo.Download(finished));
                }
                
            });

            Tileset.RequestManager.EnqueRequest(new Request(this, started, finished));
        }

        public void UnloadContent()
        {
            if (HasEmptyContent)
            {
                return;
            }
            ContentState = Unity3DTileContentState.UNLOADED;
            if (Content != null && Content.Go != null)
            {
                GameObject.Destroy(Content.Go);
                Content = null;
                Tileset.Behaviour.RequestUnloadUnusedAssets();
            }
        }

        Unity3DTileBoundingVolume CreateBoundingVolume(Schema.BoundingVolume boundingVolume, Matrix4x4 transform)
        {
            if (boundingVolume.Box.Count == 12)
            {
                var box = boundingVolume.Box;
                Vector3 center = new Vector3((float)box[0], (float)box[1], (float)box[2]);
                Vector3 halfAxesX = new Vector3((float)box[3], (float)box[4], (float)box[5]);
                Vector3 halfAxesY = new Vector3((float)box[6], (float)box[7], (float)box[8]);
                Vector3 halfAxesZ = new Vector3((float)box[9], (float)box[10], (float)box[11]);

                // TODO: Review this coordinate frame change
                // This does not take into account the coodinate frame of the glTF files and gltfUpAxis
                // https://github.com/AnalyticalGraphicsInc/3d-tiles/issues/280#issuecomment-359980111
                center.x *= -1;
                halfAxesX.x *= - 1;
                halfAxesY.x *= -1;
                halfAxesZ.x *= -1;

                var result = new TileOrientedBoundingBox(center, halfAxesX, halfAxesY, halfAxesZ);
                result.Transform(transform);
                return result;
            }
            if (boundingVolume.Sphere.Count == 4)
            {
                var sphere = boundingVolume.Sphere;
                Vector3 center = new Vector3((float)sphere[0], (float)sphere[1], (float)sphere[2]);
                float radius = (float)sphere[3];
                var result = new TileBoundingSphere(center, radius);
                result.Transform(transform);
                return result;
            }
            if (boundingVolume.Region.Count == 6)
            {
                // TODO: Implement support for regions
                Debug.LogError("Regions not supported");
                return null;
            }
            Debug.LogError("boundingVolume must contain a box, sphere, or region");
            return null;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }
    }
}
