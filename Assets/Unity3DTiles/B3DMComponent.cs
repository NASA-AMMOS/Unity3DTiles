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
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Loader;
using RSG;

namespace Unity3DTiles
{
    class B3DMComponent : MonoBehaviour
    {
        public string Url;
        public bool Multithreaded = true;
        public int MaximumLod = 300;
        public Shader ShaderOverride = null;
        public bool AddColliders = false;
        public bool DownloadOnStart = true;

        public void Start()
        {
            if (DownloadOnStart)
            {
                StartCoroutine(Download(null));
            }
        }

        public IEnumerator Download(Promise<bool> loadComplete)
        {
            string url = UrlUtils.ReplaceDataProtocol(Url);
            string dir = UrlUtils.GetBaseUri(url);
            string file = UrlUtils.GetLastPathSegment(url);

            ILoader loader = AbstractWebRequestLoader.CreateDefaultRequestLoader(dir); //.glb, .gltf
            if (file.EndsWith(".b3dm", StringComparison.OrdinalIgnoreCase))
            {
                loader = new B3DMLoader(loader);
            }
            var sceneImporter = new GLTFSceneImporter(file, loader);

            sceneImporter.SceneParent = gameObject.transform;
            sceneImporter.CustomShaderName = ShaderOverride ? ShaderOverride.name : null;
            sceneImporter.MaximumLod = MaximumLod;
            sceneImporter.Collider =
                AddColliders ? GLTFSceneImporter.ColliderType.Mesh : GLTFSceneImporter.ColliderType.None;

            loadComplete = loadComplete ?? new Promise<bool>();
            yield return sceneImporter.LoadScene(-1, Multithreaded,
                                                 sceneObject => loadComplete.Resolve(sceneObject != null));
        }
    }
}
