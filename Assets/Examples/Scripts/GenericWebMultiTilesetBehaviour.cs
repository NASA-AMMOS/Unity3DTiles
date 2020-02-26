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
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityGLTF;
using Unity3DTiles;
using Unity3DTiles.SceneManifest;

/* Extends MultiTilesetBehavior to optionally retrieve configuration from URL parameters in a WebGL build.
 *
 * Recognizes the following URL parameters, all of which are optional:
 *
 * SceneOptions - URL to JSON file overriding any subset of Unity3DTilesetSceneOptions.
 * Scene - URL to JSON file containing scene to load (see SceneManifest.cs).
 * Tileset - URL to single tileset to load.
 * TilesetOptions - URL to JSON overriding any subset of single tileset Unity3DTilesetOptions.
 *
 * Tileset and TilesetOptions are ignored if Scene is specified.
 *
 * When running in the Unity editor use the SceneOptions and SceneUrl inspectors instead of the SceneOptions and Scene
 * URL parameters.  Also, any tilesets pre-populated in the TilesetOptions list inspector will be loaded at start.
 *
 * URLs starting with "data://" will be loaded from the Unity StreamingAssets folder.
 *
 * When running in a web browser (not the Unity editor):
 * 1) Relative URLs will be interpreted relative to the window location URL.
 * 2) Any request params in the window location URL other than those above will be passed on to subsidiary fetches.
 */
public class GenericWebMultiTilesetBehaviour : MultiTilesetBehaviour
{
    public string SceneUrl; //mainly for unity editor

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string getURLParameter(string name);
    [DllImport("__Internal")]
    private static extern string getWindowLocationURL();
#else
    private static string getURLParameter(string name)
    {
        return null;
    }
    private static string getWindowLocationURL()
    {
        return null;
    }
#endif

    private void DownloadText(string url, Action<string> handler)
    {
        StartCoroutine(DownloadTextImpl(url, handler));
    }

    private IEnumerator DownloadTextImpl(string url, Action<string> handler)
    {
        using (var uwr = UnityWebRequest.Get(url))
        {

#if UNITY_2017_2_OR_NEWER
            yield return uwr.SendWebRequest();
#else
            yield return uwr.Send();
#endif
            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.LogError("error downloading " + url + ": " + uwr.error);
            }
            else
            {
                handler(uwr.downloadHandler.text);
            }
        }
    }

    protected override void _start()
    {
        base._start();

        var fullURL = getWindowLocationURL();
        if (!string.IsNullOrEmpty(fullURL))
        {
            Debug.Log("full URL: " + fullURL);
            baseUrl = UriHelper.GetBaseUri(fullURL, excludeQueryParams:
                                           new string[] { "SceneOptions", "Scene", "Tileset", "TilesetOptions" });
        }

        string sceneOptionsUrl = MakeAbsoluteUrl(getURLParameter("SceneOptions"));
        if (!string.IsNullOrEmpty(sceneOptionsUrl))
        {
            Debug.Log("overriding scene options from URL: " + sceneOptionsUrl);
            DownloadText(sceneOptionsUrl, json => JsonConvert.PopulateObject(json, SceneOptions));
        }

        Camera.main.transform.position = SceneOptions.DefaultCameraPosition;
        Camera.main.transform.eulerAngles = SceneOptions.DefaultCameraRotation;

        string sceneManifestUrl = MakeAbsoluteUrl(getURLParameter("Scene") ?? SceneUrl);
        string singleTilesetUrlRaw = getURLParameter("Tileset");
        string singleTilesetUrl = MakeAbsoluteUrl(singleTilesetUrlRaw);
        if (!string.IsNullOrEmpty(sceneManifestUrl))
        {
            Debug.Log("loading scene from URL: " + sceneManifestUrl);
            DownloadText(sceneManifestUrl, json => {
                    baseUrl = UriHelper.GetBaseUri(sceneManifestUrl);
                    AddScene(Scene.FromJson(json));
                });
        }
        else if (!string.IsNullOrEmpty(singleTilesetUrl))
        {
            Debug.Log("loading tileset from URL: " + singleTilesetUrl);
            Unity3DTilesetOptions opts = new Unity3DTilesetOptions();
            string tilesetOptionsUrl = MakeAbsoluteUrl(getURLParameter("TilesetOptions"));
            if (!string.IsNullOrEmpty(tilesetOptionsUrl))
            {
                Debug.Log("overriding tileset options from URL: " + tilesetOptionsUrl);
                DownloadText(tilesetOptionsUrl, json =>
                {
                    JsonConvert.PopulateObject(json, opts);
                    opts.Name = singleTilesetUrlRaw;
                    opts.Url = singleTilesetUrlRaw;
                    AddTileset(opts);
                });
            }
            else
            {
                opts.Name = singleTilesetUrlRaw;
                opts.Url = singleTilesetUrlRaw;
                AddTileset(opts);
            }
        }
        else
        {
            Debug.Log("consider setting URL parameter Scene or Tileset");
        }
    }
}
