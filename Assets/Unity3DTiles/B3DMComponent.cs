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
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityGLTF;
using Newtonsoft.Json;
using RSG;
using UnityGLTF.Loader;

namespace Unity3DTiles
{
    class B3DMComponent : MonoBehaviour
    {
        public string Url;
        public bool Multithreaded = true;

        public int MaximumLod = 300;

        public Shader ShaderOverride = null;

        public bool addColliders = false;

        public bool DownloadOnStart = true;

        void Start()
        {
            if (this.DownloadOnStart)
            {
                StartCoroutine(Download(null));
            }

        }

        public IEnumerator Download(Promise<bool> loadComplete)
        {

            string directoryPath = URIHelper.GetDirectoryName(Url);
            string relativePath = Url.Replace(directoryPath, "");
            var wrl = new B3DMLoader(AbstractWebRequestLoader.CreateDefaultRequestLoader(directoryPath));
            GLTFSceneImporter sceneImporter = new GLTFSceneImporter(
                    relativePath,
                    wrl
            );
            sceneImporter.SceneParent = gameObject.transform;
            sceneImporter.CustomShaderName = ShaderOverride ? ShaderOverride.name : null;
            sceneImporter.MaximumLod = MaximumLod;
            if (addColliders)
            {
                sceneImporter.Collider = GLTFSceneImporter.ColliderType.Mesh;
            }
            yield return sceneImporter.LoadScene(-1, Multithreaded, sceneObject =>
            {
                if (sceneObject != null)
                {
                    loadComplete.Resolve(true);                    
                }
                else
                {                
                    loadComplete.Resolve(false);
                }
                Destroy(this);
            });
            // Override the shaders on all materials if a shader is provided
            if (ShaderOverride != null)
            {
                Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.sharedMaterial.shader = ShaderOverride;
                }
            }
        }
    }
}
