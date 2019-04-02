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
using RSG;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PNTSComponent : MonoBehaviour
{

    public string Url;
    public Shader ShaderOverride = null;
    public bool DownloadOnStart = true;

    void Start()
    {
        if (DownloadOnStart)
        {
            StartCoroutine(Download(null));
        }
    }

    void Update()
    {

    }

    public IEnumerator Download(Promise<bool> loadComplete)
    {
        using (UnityWebRequest www = new UnityWebRequest(Url, "GET", new DownloadHandlerBuffer(), null))
        {
            www.timeout = 5000;
#if UNITY_2017_2_OR_NEWER
            yield return www.SendWebRequest();
#else
            yield return www.Send();
#endif
            if ((int)www.responseCode >= 400)
            {
                Debug.LogErrorFormat("{0} - {1}", www.responseCode, www.url);
                throw new Exception("Response code invalid");
            }

            if (www.downloadedBytes > int.MaxValue)
            {
                throw new Exception("Stream is larger than can be copied into byte array");
            }

            var meshes = PointFileReader.Read(www.downloadHandler.data);
            foreach (Mesh m in meshes)
            {
                var go = new GameObject();
                go.transform.parent = this.transform;
                go.AddComponent<MeshFilter>().sharedMesh = m;
                go.AddComponent<MeshRenderer>().material = new Material(ShaderOverride);
            }
            loadComplete.Resolve(true);
        }
    }
}
