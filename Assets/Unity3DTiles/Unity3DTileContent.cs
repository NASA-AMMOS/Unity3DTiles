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

namespace Unity3DTiles
{
    public class Unity3DTileContent
    {
        public GameObject Go { get; private set; }

        public int FaceCount { get; private set; }
        public int PixelCount { get; private set; }
        public int TextureCount { get; private set; }
        public Vector2Int MaxTextureSize { get; private set; }

        public Unity3DTileIndex Index;

        public bool IsActive { get { return Go != null && Go.activeSelf; } }

        private bool collidersEnabled;
        private bool renderersEnabled;
        private ShadowCastingMode? shadowMode;
        private bool? recieveShadows;
        private MeshRenderer[] renderers;
        private Collider[] colliders;
        

        public Unity3DTileContent(GameObject go)
        {
            this.Go = go;
            this.collidersEnabled = false;
            this.renderersEnabled = false;
        }

        /// <summary>
        /// This method should be called after the GameObject has finished loading its content
        /// It will scrape the subtree for all mesh renderers and colliders
        /// </summary>
        public void Initialize(bool createColliders)
        {
            // Update layer of child game objects to match the parent
            var childTransforms = this.Go.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < childTransforms.Length; i++)
            {
                childTransforms[i].gameObject.layer = this.Go.layer;
            }

            this.renderers = this.Go.GetComponentsInChildren<MeshRenderer>();
            var meshFilters = this.Go.GetComponentsInChildren<MeshFilter>();
            if (createColliders)
            {
                for( int i =0; i < meshFilters.Length; i++)
                {
                    var mf = meshFilters[i];
                    if (mf.sharedMesh.GetTopology(0) == MeshTopology.Triangles)
                    {
                        var mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                    }
                }
                // Need to toggle active and then inactive to bake collider data
                // We do this here so that we can control the number of bakes per frame
                // otherwise we can have lots of colliders bake in one frame if many tiles become
                // active for the first time
                Go.SetActive(true);
                Go.SetActive(false);
            }

            for (int i = 0; i < meshFilters.Length; i++)
            {
                var m = meshFilters[i].sharedMesh;
                if (m.GetTopology(0) == MeshTopology.Triangles)
                {
                    FaceCount += m.triangles.Length / 3;
                }
            }

            int maxPixels = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                for(int j = 0; j < r.materials.Length; j++)
                {
                    if (r.materials[j].HasProperty("_MainTex"))
                    {
                        var t = r.materials[j].mainTexture;
                        if (t != null)
                        {
                            int pixels = t.width * t.height;
                            if (pixels > maxPixels)
                            {
                                MaxTextureSize = new Vector2Int(t.width, t.height);
                            }
                            PixelCount += pixels;
                            TextureCount += 1;
                        }
                    }
                }
            }
            colliders = Go.GetComponentsInChildren<Collider>();
            collidersEnabled = true;
            renderersEnabled = true;
        }

        /// <summary>
        /// Set the game object for this content as active or inactive
        /// Only apply the state if it has changed for performance
        /// </summary>
        /// <param name="active"></param>
        public void SetActive(bool active)
        {
            if (active != Go.activeSelf)
            {
                Go.SetActive(active);
            }
        }

        /// <summary>
        /// Enable or disable all colliders for this content
        /// Only apply the state if it has changed for performance
        /// </summary>
        /// <param name="enabled"></param>
        public void EnableColliders(bool enabled)
        {
            if (enabled != collidersEnabled)
            {
                collidersEnabled = enabled;
                if (colliders != null)
                {
                    for (int i = 0; i < colliders.Length; i++)
                    {
                        colliders[i].enabled = enabled;
                    }
                }
            }
        }

        /// <summary>
        /// Enable or disable all mesh renderers for this content
        /// Only apply the state if it has changed for performance
        /// </summary>
        /// <param name="enabled"></param>
        public void EnableRenderers(bool enabled)
        {
            if (enabled != renderersEnabled)
            {
                renderersEnabled = enabled;
                if (renderers != null)
                {
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        renderers[i].enabled = enabled;
                    }
                }
            }
        }

        public void SetShadowMode(ShadowCastingMode shadowMode, bool recieveShadows)
        {
            if (!this.shadowMode.HasValue || !this.recieveShadows.HasValue ||
                this.shadowMode.Value != shadowMode || this.recieveShadows.Value != recieveShadows)
            {
                this.shadowMode = shadowMode;
                this.recieveShadows = recieveShadows;
                if (renderers != null)
                {
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        renderers[i].receiveShadows = recieveShadows;
                        renderers[i].shadowCastingMode = shadowMode;
                    }
                }
            }
        }

        public MeshRenderer[] GetRenderers()
        {
            return renderers;
        }
    }
}
