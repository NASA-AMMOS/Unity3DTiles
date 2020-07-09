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

namespace Unity3DTiles
{
    public enum Unity3DTileContentState  {
        UNLOADED = 0,   // Has never been requested
        LOADING = 1,    // Is waiting on a pending request
        PROCESSING = 2, // Request received.  Contents are being processed for rendering.  Depending on the content, it might make its own requests for external data.
        READY = 3,      // Ready to render.
        EXPIRED = 4,    // Is expired and will be unloaded once new content is loaded.
        FAILED = 5      // Request failed.
    };

    public enum Unity3DTileContentType
    {
        B3DM,
        PNTS,
        JSON,
        Unknown
    }

    public class TileInfo : MonoBehaviour
    {
        public Unity3DTile Tile;
    }

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
        public Schema.Tile tile;

        public Unity3DTilesetStyle Style;

        public float GeometricError
        {
            get { return (float)tile.GeometricError; }
        }

        public Schema.TileRefine Refine
        {
            get { return tile.Refine.Value; }
        }
        
        public bool HasEmptyContent
        {
            get
            {
                return tile.Content == null;
            }
        }

        public Unity3DTileContentType ContentType
        {
            get
            {
                string ext = Path.GetExtension(UrlUtils.RemoveQuery(this.ContentUrl)).ToLower();
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

        public Unity3DTileContentState ContentState { get; private set; }

        public bool ContentActive { get { return Content != null && Content.IsActive; } }

        public string ContentUrl { get; private set; }

        public Promise ContentReadyToProcessPromise { get; private set; }

        public Promise ContentReadyPromise { get; private set; }

        public Unity3DTileContent ExpiredContent { get; private set; }

        public bool HasRenderableContent { get; private set; }

        public bool HasTilesetContent { get; private set; }

        public TileFrameState FrameState { get; private set; }

        private int hashCode;

        public class TileFrameState
        {
            public int LastVisitedFrame = -1;
         
            public bool InFrustumSet = false;       // Currently in view of a camera (And in this frames "Used set")
            private bool InUsedSet = false;         // Relevant to rendering or collisons this frame, private because this should be checked with IsUsedThisFrame to void reading stale values
            public bool IsUsedSetLeaf = false;      // This tile is at the maximum LOD this frame given our screen space error requirements
            public bool InRenderSet = false;        // This tile should be rendered this frame
            public bool InColliderSet = false;      // This tile should have its collider enabled this frame
            public bool UsedLastFrame = false;      // This tile was in the used set last frame and may need to be deactivated next frame
            public float DistanceToCamera = float.MaxValue;
            public float PixelsToCameraCenter = float.MaxValue;
            public float ScreenSpaceError = 0;

            public void MarkUsed()
            {
                InUsedSet = true;
            }

            public bool IsUsedThisFrame(int frameCount)
            {
                return InUsedSet && LastVisitedFrame == frameCount;
            }

            public void Reset(int frameCount)
            {
                if(LastVisitedFrame == frameCount)
                {
                    return;
                }
                LastVisitedFrame = frameCount;
                InUsedSet = false;
                InFrustumSet = false;
                IsUsedSetLeaf = false;
                InRenderSet = false;
                InColliderSet = false;
                DistanceToCamera = float.MaxValue;
                PixelsToCameraCenter = float.MaxValue;
            }
        }

        public void MarkUsed()
        {
            // Mark as used in frame state
            this.FrameState.MarkUsed();
            // If this node has content, it will also be tracked in the LRUContent datastructure.  Mark it as used
            // so it's content won't be selected for unloading
            this.Tileset.LRUContent.MarkUsed(this);
        }

        public Unity3DTile(Unity3DTileset tileset, string basePath, Schema.Tile tile, Unity3DTile parent)
        {
            this.hashCode = (int)UnityEngine.Random.Range(0, int.MaxValue);
            this.Tileset = tileset;
            this.tile = tile;
            this.FrameState = new TileFrameState();
            if (tile.Content != null)
            {
                this.Id = Path.GetFileNameWithoutExtension(tile.Content.GetUri());
            }
            if (parent != null)
            {
                parent.Children.Add(this);
                this.Depth = parent.Depth + 1;
            }

            // TODO: Consider using a double percision Matrix library for doing 3d tiles root transform calculations
            // Set the local transform for this tile, default to identity matrix
            this.transform = this.tile.UnityTransform();
            var parentTransform = (parent != null) ? parent.computedTransform : tileset.GetRootTransform();
            this.computedTransform = parentTransform * this.transform;

            this.BoundingVolume = CreateBoundingVolume(tile.BoundingVolume, this.computedTransform);

            // TODO: Add 2D bounding volumes

            if (tile.Content != null && tile.Content.BoundingVolume.IsDefined())
            {
                // Non-leaf tiles may have a content bounding-volume, which is a tight-fit bounding volume
                // around only the features in the tile.  This box is useful for culling for rendering,
                // but not for culling for traversing the tree since it does not guarantee spatial coherence, i.e.,
                // since it only bounds features in the tile, not the entire tile, children may be
                // outside of this box.
                this.ContentBoundingVolume = CreateBoundingVolume(tile.Content.BoundingVolume, this.computedTransform);
            }
            else
            {
                // Default to tile bounding volume
                this.ContentBoundingVolume = CreateBoundingVolume(tile.BoundingVolume, this.computedTransform);
            }
            // TODO: Add viewer request volume support
            //if(tile.ViewerRequestVolume != null && tile.ViewerRequestVolume.IsDefined())
            //{
            //    this.viewerRequestVolume = CreateBoundingVolume(tile.ViewerRequestVolume, transform);
            //}

            if(!tile.Refine.HasValue)
            {
                tile.Refine = (parent == null) ? Schema.TileRefine.REPLACE : parent.tile.Refine.Value;
            }

            this.Parent = parent;
            
            if(this.HasEmptyContent)
            {
                this.ContentState = Unity3DTileContentState.READY;
            }
            else
            {
                ContentState = Unity3DTileContentState.UNLOADED;
                this.ContentUrl = UrlUtils.JoinUrls(basePath, tile.Content.GetUri());
            }

            this.HasRenderableContent = false;
            this.HasTilesetContent = false;
        }

        public bool Process()
        {
            if (this.ContentState == Unity3DTileContentState.PROCESSING)
            {
                this.ContentState = Unity3DTileContentState.READY;
                this.Content.Initialize(this.Tileset.TilesetOptions.CreateColliders);

                var indexMode = this.Tileset.TilesetOptions.LoadIndices;
                if (indexMode != IndexMode.Default && indexMode != IndexMode.None)
                {
                    Action<IndexMode, string, string> fail = (mode, url, msg) =>
                    {
                        //we could log a warning here, but if indices are expected but not available
                        //that might not actually be a true error condition
                        //and this would spam the log
                        //Debug.LogWarning("failed to load " + mode + " index for " + this.ContentUrl + ": " + msg);
                    };
                    Action<Unity3DTileIndex> success = index => { this.Content.Index = index; };
                    this.Tileset.Behaviour
                        .StartCoroutine(Unity3DTileIndex.Load(indexMode, this.ContentUrl, success, fail));
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
            if (this.Tileset.RequestManager.Full())
            {
                return;
            }
            if(this.HasEmptyContent)
            {
                return;
            }
            if (this.ContentState == Unity3DTileContentState.UNLOADED ||
               this.ContentState == Unity3DTileContentState.EXPIRED)
            {
                this.ContentState = Unity3DTileContentState.LOADING;
                
                Promise<bool> finished = new Promise<bool>();
                finished.Then((success) =>
                {
                    this.Tileset.Statistics.NetworkError = !success;
                    this.Tileset.Statistics.RequestsThisFrame += 1;
                    if (success)
                    {
                        this.ContentState = Unity3DTileContentState.PROCESSING;
                        this.Content.SetShadowMode(this.Tileset.TilesetOptions.ShadowCastingMode, this.Tileset.TilesetOptions.RecieveShadows);
                        this.Tileset.Statistics.LoadedContentCount += 1;
                        this.Tileset.Statistics.TotalTilesLoaded += 1;
                        // Track tile in cache as soon as it has downloaded content, but still queue it for processing
                        CacheRequestStatus status = this.Tileset.LRUContent.Add(this);
                        if (status == CacheRequestStatus.ADDED)
                        {
                            this.Tileset.ProcessingQueue.Enqueue(this);
                        }
                        else
                        {
                            UnloadContent();
                        }                                             
                    }
                    else
                    {
                        UnloadContent();
                    }
                });

                Promise started = new Promise();
                started.Then(() =>
                {
                    GameObject go = new GameObject(Id);
                    go.transform.parent = this.Tileset.Behaviour.transform;
                    go.transform.localPosition = new Vector3(this.computedTransform.m03, this.computedTransform.m13, this.computedTransform.m23);
                    go.transform.localRotation = this.computedTransform.rotation;
                    go.transform.localScale = this.computedTransform.lossyScale;
                    go.layer = this.Tileset.Behaviour.gameObject.layer;
                    go.SetActive(false);
                    var info = go.AddComponent<TileInfo>();
                    info.Tile = this;
                    this.Content = new Unity3DTileContent(go);

                    if (ContentType == Unity3DTileContentType.B3DM)
                    {
                        B3DMComponent b3dmCo = go.AddComponent<B3DMComponent>();
                        b3dmCo.Url = this.ContentUrl;
                        b3dmCo.Multithreaded = this.Tileset.TilesetOptions.GLTFMultithreadedLoad;
                        b3dmCo.MaximumLod = this.Tileset.TilesetOptions.GLTFMaximumLOD;
                        if (!string.IsNullOrEmpty(this.Tileset.TilesetOptions.ShaderOverride))
                        {
                            b3dmCo.ShaderOverride = Shader.Find(this.Tileset.TilesetOptions.ShaderOverride);
                        }
                        b3dmCo.AddColliders = false;
                        b3dmCo.DownloadOnStart = false;
                        this.Tileset.Behaviour.StartCoroutine(b3dmCo.Download(finished));
                    }
                    else if (ContentType == Unity3DTileContentType.PNTS)
                    {
                        PNTSComponent pntsCo = go.AddComponent<PNTSComponent>();
                        pntsCo.Url = UrlUtils.RemoveQuery(this.ContentUrl);
                        pntsCo.ShaderOverride = Shader.Find("Point Cloud/Point");
                        pntsCo.DownloadOnStart = false;
                        this.Tileset.Behaviour.StartCoroutine(pntsCo.Download(finished));
                    }

                });
                Request request = new Request(this, priority, started, finished);
                this.Tileset.RequestManager.EnqueRequest(request);
            }
        }

        public void UnloadContent()
        {
            if (this.HasEmptyContent)
            {
                return;
            }
            this.ContentState = Unity3DTileContentState.UNLOADED;
            if (this.Content != null && this.Content.Go != null)
            {
                this.Tileset.Statistics.LoadedContentCount -= 1;
                GameObject.Destroy(this.Content.Go);
                this.Content = null;
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
            return this.hashCode;
        }
    }
}
