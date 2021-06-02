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
using UnityEngine.Rendering;
using System.Linq;
using System;

namespace Unity3DTiles
{

    public class Unity3DTilesetTraversal
    {
        public HashSet<Unity3DTile> ForceTiles = new HashSet<Unity3DTile>();

        private Unity3DTileset tileset;
        private Unity3DTilesetSceneOptions sceneOptions;

        private Unity3DTilesetOptions tilesetOptions
        {
            get { return tileset.TilesetOptions; }
        }

        public Unity3DTilesetTraversal(Unity3DTileset tileset, Unity3DTilesetSceneOptions sceneOptions)
        {
            this.tileset = tileset;
            this.sceneOptions = sceneOptions;
        }

        public void Run()
        {
            if (!tileset.TilesetOptions.Show)
            {
                ToggleTiles(tileset.Root);
                return;
            }

            SSECalculator sse = new SSECalculator(tileset);
            foreach (Camera cam in sceneOptions.ClippingCameras)
            {
                if (cam == null)
                {
                    continue;
                }

                // All of our bounding boxes and tiles are using tileset coordinate frame so lets get our frustrum
                // planes in tileset frame.  This way we only need to transform our planes, not every bounding box we
                // need to check against
                Matrix4x4 cameraMatrix =
                    cam.projectionMatrix * cam.worldToCameraMatrix * tileset.Behaviour.transform.localToWorldMatrix;
                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cameraMatrix);

                sse.Configure(cam);
                Vector3 cameraPositionInTileset =
                    tileset.Behaviour.transform.InverseTransformPoint(cam.transform.position);
                Vector3 cameraForwardInTileset =
                    tileset.Behaviour.transform.InverseTransformDirection(cam.transform.forward);

                DetermineFrustumSet(tileset.Root, planes, sse, cameraPositionInTileset, cameraForwardInTileset,
                                    PlaneClipMask.GetDefaultMask());
            }

            foreach (var ft in ForceTiles)
            {
                for (var tile = ft; tile != null; tile = tile.Parent)
                {
                    if (!tile.FrameState.IsUsedThisFrame)
                    {
                        tile.FrameState.Reset();
                        tile.MarkUsed();
                        tile.FrameState.InFrustumSet = true;
                        tileset.Statistics.FrustumSet++;
                    }
                }
            }
            
            MarkUsedSetLeaves(tileset.Root);

            AssignPrioritiesRecursively(tileset.Root);

            SkipTraversal(tileset.Root);      

            ToggleTiles(tileset.Root);

            //RequestManager.Process() and UnloadUnusedContent()
            //are called once for all tilesets at end of AbstractTilesetBehaviour.LateUpdate()
        }

        class SSECalculator
        {
            private Camera cam;
            private float sseDenominator;    // used for perspective
            private float pixelSize;         // used for orthographic      
            private Unity3DTileset tileset;

            public SSECalculator(Unity3DTileset tileset)
            {
                this.tileset = tileset;
            }

            public void Configure(Camera cam)
            {
                this.cam = cam;
                if (cam.orthographic)
                {
                    pixelSize = Mathf.Max(cam.orthographicSize * 2, cam.orthographicSize * 2 * cam.aspect) /
                        Mathf.Max(cam.pixelHeight, cam.pixelWidth);
                }
                else
                {
                    sseDenominator = 2 * Mathf.Tan(0.5f * cam.fieldOfView * Mathf.Deg2Rad);
                }                
            }

            public float PixelError(float tileError, float distFromCamera)
            {
                if (tileError == 0)
                {
                    return 0; // Leaf tiles have no screenspace error
                }
                return ProjectDistanceOnTileToScreen(tileError, distFromCamera);
            }

            public float ProjectDistanceOnTileToScreen(float distOnTile, float distFromCamera)
            {
                if (cam.orthographic)
                {
                    return distOnTile / pixelSize;
                }
                else
                {
                    distFromCamera = Mathf.Max(distFromCamera, 0.00001f);
                    return (distOnTile * cam.pixelHeight) / (distFromCamera * sseDenominator);
                }
            }

            private float Fog(float distFromCamera, float density)
            {  
                float scalar = distFromCamera * density;
                return 1.0f - Mathf.Exp(-(scalar * scalar));
            }
        }
        
        /// <summary>
        /// After calling this method all tiles within a camera frustum will be marked as being in the frustum set and
        /// as being used starting at the root and stopping when:
        ///
        /// 1) A tile is found that has a screen space error less than or equal to our target SSE
        /// 2) MaxDepth is reached (Optional)
        ///
        /// Tiles with no content are ignored (i.e. we recurse to the nearest complete set of descendents with content)
        /// 
        /// If the LoadSiblings criteria is enabled we add additional tiles to the used set. Specificlly, if a tile is
        /// in the Frustum set, we gurantee that all of its siblings are marked as used.  If the siblings have empty
        /// conetent, we mark the first set of decendents that have content as used.  This is useful for tree traversals
        /// where we want to load content or do computation on tiles that are outside the users current view but results
        /// in a slower traversal.
        ///  
        /// After this method is run, only tiles that are in the used set are considered by the rest of the traversal
        /// algorithm for this frame.  Unused tiles may be subject to being unloaded.
        /// </summary>
        private bool DetermineFrustumSet(Unity3DTile tile, Plane[] planes, SSECalculator sse,
                                         Vector3 cameraPosInTilesetFrame, Vector3 cameraFwdInTilesetFrame,
                                         PlaneClipMask mask)
        {
            // Reset frame state if needed
            tile.FrameState.Reset();
            // Check to see if we are in the fustrum
            mask = tile.BoundingVolume.IntersectPlanes(planes, mask);
            if (mask.Intersection == IntersectionType.OUTSIDE)
            {
                return false;
            }
            // We are in frustum and at a rendereable level of detail, mark as used and as visible
            tile.MarkUsed();  //  mark content as used in LRUContent so it won't be unloaded
            tile.FrameState.InFrustumSet = true;
            tileset.Statistics.FrustumSet++;
            // Skip screen space error check if this node has empty content, 
            // we need to keep recursing until we find a node with content regardless of error
            if (!tile.HasEmptyContent)
            {
                // Check to see if this tile meets the on screen error level of detail requirement
                float distance = tile.BoundingVolume.MinDistanceTo(cameraPosInTilesetFrame);

                // We take the min in case multiple cameras, reset dist to max float on frame reset
                tile.FrameState.DistanceToCamera = Mathf.Min(distance, tile.FrameState.DistanceToCamera);
                tile.FrameState.ScreenSpaceError =
                    sse.PixelError(tile.GeometricError, tile.FrameState.DistanceToCamera);

                Ray cameraRay = new Ray(cameraPosInTilesetFrame, cameraFwdInTilesetFrame);
                float distToAxis = tile.BoundingVolume.CenterDistanceTo(cameraRay);
                float pixelsToCamCtr = sse.ProjectDistanceOnTileToScreen(distToAxis, distance);
                tile.FrameState.PixelsToCameraCenter = Mathf.Min(pixelsToCamCtr, tile.FrameState.PixelsToCameraCenter);

                //prune traversal when we hit a tile that meets SSE
                if (tile.FrameState.ScreenSpaceError <= tileset.TilesetOptions.MaximumScreenSpaceError)
                {
                    return true;
                }
            }
            if (tileset.TilesetOptions.MaxDepth > 0 && tile.Depth >= tileset.TilesetOptions.MaxDepth)
            {
                return true;
            }
            // Recurse on children
            bool anyChildUsed = false;
            for (int i = 0; i < tile.Children.Count; i++)
            {
                bool r = DetermineFrustumSet(tile.Children[i], planes, sse, cameraPosInTilesetFrame,
                                             cameraFwdInTilesetFrame, mask);
                anyChildUsed = anyChildUsed || r;
            }
            // If any children are in the working set, mark all of them as being used
            if (anyChildUsed && tileset.TilesetOptions.LoadSiblings)
            {
                for (int i = 0; i < tile.Children.Count; i++)
                {
                    MarkUsed(tile.Children[i]);
                }
            }
            return true;
        }

        /// <summary>
        /// Mark this tile as used.  In the case that this tile has empty content,
        /// recurse until a complete set of leaf nodes with content are found
        /// This is only needed to handle the case of empty tiles
        /// </summary>
        private void MarkUsed(Unity3DTile tile)
        {
            // We need to reset as we go in case we find tiles that weren't previously explored
            // If they have already been reset this frame this has no effect
            tile.FrameState.Reset();
            tile.MarkUsed();
            if (tile.HasEmptyContent)
            {
                for(int i = 0; i < tile.Children.Count; i++)
                {
                    MarkUsed(tile.Children[i]);
                }
            }
        }

        /// <summary>
        /// Identify the deepest set of tiles that are in the used set this frame and mark them as "used set leaves"
        /// After this point we will not consider any tiles that are beyond a used set leaf.
        /// Leafs are the content we ideally want to show this frame
        /// </summary>
        void MarkUsedSetLeaves(Unity3DTile tile)
        {
            // A used leaf is a node that is used but has no children in the used set
            if (!tile.FrameState.IsUsedThisFrame)
            {
                // Not used this frame, can't be a used leaf and neither can anything beneath us
                return;
            }
            tileset.Statistics.UsedSet++;
            // If any child is used, then we are not a leaf
            bool anyChildrenUsed = false;
            for (int i = 0; i < tile.Children.Count; i++)
            {
                anyChildrenUsed = anyChildrenUsed || tile.Children[i].FrameState.IsUsedThisFrame;
            }
            if (!anyChildrenUsed || ForceTiles.Contains(tile))
            {
                tile.FrameState.IsUsedSetLeaf = true;
            }
            else
            {
                for (int i = 0; i < tile.Children.Count; i++)
                {
                    MarkUsedSetLeaves(tile.Children[i]);
                }
            }
        }

        /// <summary>
        /// Traverse the tree, request tiles, and enable visible tiles
        /// Skip parent tiles that have a screen space error larger than
        /// MaximumScreenSpaceError*SkipScreenSpaceErrorMultiplier
        /// </summary>
        /// <param name="tile"></param>
        void SkipTraversal(Unity3DTile tile)
        {
            if (!tile.FrameState.IsUsedThisFrame)
            {
                return;
            }

            if (tile.FrameState.IsUsedSetLeaf)
            {
                if (tile.ContentState == Unity3DTileContentState.READY)
                {
                    if (tile.FrameState.InFrustumSet)
                    {
                        tile.FrameState.InRenderSet = true;
                        tileset.Statistics.TallyVisibleTile(tile);
                    }
                    tile.FrameState.InColliderSet = true;
                    tileset.Statistics.ColliderSet++;
                }
                else
                {
                    RequestTile(tile);
                }
                return;
            }

            // Draw a parent tile iff
            // 1) meets SSE cuttoff
            // 2) has content and is not empty
            // 3) one or more of its chidlren don't have content
            bool meetsSSE = tile.FrameState.ScreenSpaceError <
                (tilesetOptions.MaximumScreenSpaceError * tilesetOptions.SkipScreenSpaceErrorMultiplier);
            bool hasContent = tile.ContentState == Unity3DTileContentState.READY && !tile.HasEmptyContent;
            bool allChildrenHaveContent = true;
            for (int i = 0; i < tile.Children.Count; i++)
            {
                if (tile.Children[i].FrameState.IsUsedThisFrame)
                {
                    bool childContent = tile.Children[i].ContentState == Unity3DTileContentState.READY ||
                        tile.HasEmptyContent;
                    allChildrenHaveContent = allChildrenHaveContent && childContent;
                }
            }

            if (meetsSSE && !hasContent)
            {
                RequestTile(tile);
            }

            if (meetsSSE && hasContent && !allChildrenHaveContent)
            {
                if (tile.FrameState.InFrustumSet)
                {
                    tile.FrameState.InRenderSet = true;
                    tileset.Statistics.TallyVisibleTile(tile);
                }
                tile.FrameState.InColliderSet = true;
                tileset.Statistics.ColliderSet++;
                // Request children
                for (int i = 0; i < tile.Children.Count; i++)
                {
                    if (tile.Children[i].FrameState.IsUsedThisFrame)
                    {
                        RequestTile(tile.Children[i]);
                    }
                }
                return;
            }

            // Otherwise keep decending
            for (int i = 0; i < tile.Children.Count; i++)
            {
                if (tile.Children[i].FrameState.IsUsedThisFrame)
                {
                    SkipTraversal(tile.Children[i]);
                }
            }
        }

        void ToggleTiles(Unity3DTile tile)
        {
            // Only consider tiles that were used this frame or the previous frame
            if (tile.FrameState.IsUsedThisFrame || tile.FrameState.UsedLastFrame)
            {
                tile.FrameState.UsedLastFrame = false;
                if (!tile.FrameState.IsUsedThisFrame)
                {
                    // This tile was active last frame but isn't active any more
                    if (tile.Content != null)
                    {
                        tile.Content.SetActive(false);
                    }
                }
                else 
                {
                    // this tile is in the used set this frame
                    if (tile.Content != null)
                    {
                        tile.Content.SetActive(tile.FrameState.InColliderSet || tile.FrameState.InRenderSet);
                        tile.Content.EnableColliders(tile.FrameState.InColliderSet);
                        if (tilesetOptions.Show)
                        {
                            if (tile.FrameState.InRenderSet)
                            {
                                tile.Content.EnableRenderers(true);
                                tile.Content.SetShadowMode(tilesetOptions.ShadowCastingMode,
                                                           tilesetOptions.RecieveShadows);
                                if (tilesetOptions.Style != null)
                                {
                                    tilesetOptions.Style.ApplyStyle(tile);
                                }
                            }
                            else if (tile.FrameState.InColliderSet &&
                                     tilesetOptions.ShadowCastingMode != ShadowCastingMode.Off)
                            {
                                tile.Content.SetShadowMode(ShadowCastingMode.ShadowsOnly, false);
                            }
                        }
                    }
                    tile.FrameState.UsedLastFrame = true;
                }
                for (int i = 0; i < tile.Children.Count; i++)
                {
                    ToggleTiles(tile.Children[i]);
                }
            }
        }

        int DepthFromFirstUsedAncestor(Unity3DTile tile)
        {
            if (!tile.FrameState.IsUsedThisFrame ||
                tile.Parent == null || !tile.Parent.FrameState.IsUsedThisFrame)
            {
                return 0;
            }

            return 1 + DepthFromFirstUsedAncestor(tile.Parent);
        }

        float MinUsedAncestorPixelsToCameraCenter(Unity3DTile tile, float pixels = 10000)
        {
            if (!tile.FrameState.IsUsedThisFrame || tile.Parent == null || !tile.Parent.FrameState.IsUsedThisFrame)
            {
                return pixels;
            }
            pixels = Mathf.Min(pixels, tile.FrameState.PixelsToCameraCenter);
            return MinUsedAncestorPixelsToCameraCenter(tile.Parent, pixels);
        }

        float TilePriority(Unity3DTile tile)
        {
            if (ForceTiles.Contains(tile))
            {
                return 0;
            }

            if (!tile.FrameState.IsUsedThisFrame || !tile.FrameState.InFrustumSet || tile.HasEmptyContent)
            {
                return float.MaxValue;
            }

            if (tilesetOptions.TilePriority != null)
            {
                return tilesetOptions.TilePriority(tile);
            }

            float distLimit = 1000;
            float d2c = tile.FrameState.DistanceToCamera;
            d2c = float.IsNaN(d2c) ? distLimit : Mathf.Max(0, Mathf.Min(distLimit, d2c));

            float pixelsLimit = 10000;
            float p2c = tile.FrameState.PixelsToCameraCenter;
            //float p2c = MinUsedAncestorPixelsToCameraCenter(tile, pixelsLimit);
            p2c = float.IsNaN(p2c) ? pixelsLimit : Mathf.Max(0, Mathf.Min(pixelsLimit, p2c));

            float depthLimit = 100;
            float depth = tile.Depth;
            //float depth = DepthFromFirstUsedAncestor(tile);
            depth = float.IsNaN(depth) ? depthLimit : Mathf.Max(0, Mathf.Min(depthLimit, depth));

            //prioritize by distance from camera
            //return d2c;

            //prioritize by pixels from camera
            //return p2c;

            //prioritize by depth
            //return depth;

            //prioritize first by depth, then pixels to camera
            //return depth + (p2c / pixelsLimit);

            //prioritize first by pixels to camera, then depth
            //return (int)(p2c / 100) + (depth / depthLimit);

            //prioritize first by used set leaf, then distance to camera, then depth, then pixels to camera
            return (tile.FrameState.IsUsedSetLeaf ? 0 : 100) +
                (d2c < 1 ? 0 : 10) +
                (int)(9 * depth / depthLimit) +
                (p2c / pixelsLimit);
        }

        
        /// <summary>
        /// If the tile has siblings in the used set, request them at the same time since we will need all of
        /// them to split the parent tile.
        /// Set request priority based on the closest sibling, request all siblings with
        /// the same priority since we need all of them to load before any of them can be made visible.
        /// </summary>
        void AssignPrioritiesRecursively(Unity3DTile tile)
        {
            if (tile.FrameState.IsUsedThisFrame)
            {
                if (!tile.HasEmptyContent)
                {
                    tile.FrameState.Priority = TilePriority(tile);
                }
                float minChildPriority = float.MaxValue;
                foreach (var child in tile.Children)
                {
                    AssignPrioritiesRecursively(child);
                    minChildPriority = Mathf.Min(minChildPriority, child.FrameState.Priority);
                }
                foreach (var child in tile.Children)
                {
                    child.FrameState.Priority = minChildPriority;
                }
            }
        }

        void RequestTile(Unity3DTile tile)
        {
            if (tile.Parent == null)
            {
                tile.RequestContent(tile.FrameState.Priority);
            }
            else
            {
                for (int i = 0; i < tile.Parent.Children.Count; i++)
                {
                    if (tile.Parent.Children[i].FrameState.IsUsedThisFrame)
                    {
                        tile.Parent.Children[i].RequestContent(tile.FrameState.Priority);
                    }
                }
            }
        }

        public void DrawDebug()
        {
            DebugDrawUsedSet(tileset.Root);
            DebugDrawFrustumSet(tileset.Root);
        }

        void DebugDrawUsedSet(Unity3DTile tile)
        {
            if (tile.FrameState.IsUsedThisFrame)
            {
                if (tile.FrameState.IsUsedSetLeaf)
                {
                    tile.BoundingVolume.DebugDraw(Color.white, tileset.Behaviour.transform);
                }
                for (int i = 0; i < tile.Children.Count; i++)
                {
                    DebugDrawUsedSet(tile.Children[i]);
                }
            }
        }

        void DebugDrawFrustumSet(Unity3DTile tile)
        {
            if (tile.FrameState.IsUsedThisFrame && tile.FrameState.InFrustumSet)
            {
                if (tile.FrameState.IsUsedSetLeaf)
                {
                    tile.BoundingVolume.DebugDraw(Color.green, tileset.Behaviour.transform);
                }
                for (int i = 0; i < tile.Children.Count; i++)
                {
                    DebugDrawFrustumSet(tile.Children[i]);
                }
            }
        }
    }
}
