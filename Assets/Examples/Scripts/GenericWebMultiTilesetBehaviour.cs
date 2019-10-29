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
using UnityEngine.Networking;
using Unity3DTiles;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RSG;

/* Extends TilesetBehavior to optionally retrieve configuration from URL parameters in a WebGL build.
 *
 * Any parameters that aren't in the URL can take defaults from the prefab or scene object containing this script.
 *
 * 1) if URL param "MaxConcurrentRequests" is present then MaxConcurrentRequests is overridden by that
 * 2) if URL param "TilesetOptions" is present then TilesetOptions are overlaid by JSON from that URL
 * 3) if URL param "TilesetURL" is present then TilesetOptions.Url is overriden by that
 *
 * This script can only be used on the Unity WebGL platform.
 *
 * Example: http://uri.to/generic/web/deployment/index.html?TilesetURL=http%3A%2F%2Furi.to%2Ftileset.json&TilesetOptions=http%3A%2F%2Furi.to%2Foptions.json&MaxConcurrentRequests=100
 */
public class GenericWebMultiTilesetBehaviour : MultiTilesetBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string getURLParameter(string name);
#else
    private static string getURLParameter(string name)
    {
        Debug.LogWarning("cannot get URL parameter \"" + name + "\", not running WebGL build");

        return null;
    }
#endif

    IEnumerator DownloadSceneJson(string url, Promise<string> promise)
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
                promise.Reject(new System.Exception("Error downloading " + url + " " + uwr.error));
            }
            else
            {
                promise.Resolve(uwr.downloadHandler.text);
            }
        }
    }

    public Promise<string> LoadSceneJson(string url)
    {
        Promise<string> promise = new Promise<string>();
        this.StartCoroutine(DownloadSceneJson(url, promise));
        return promise;
    }

    private IEnumerator GetOptions(string optionsURL)
    {
        Debug.Log("downloading tileset options from URL parameter: " + optionsURL);

        using (UnityWebRequest www = UnityWebRequest.Get(optionsURL))
        {
            yield return www.Send();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log("error downloading tileset options: " + www.error);
            }

            try
            {
                //use PopulateObject() so that the downloaded options can be partial
                Unity3DTilesetOptions opts = new Unity3DTilesetOptions();
                JsonConvert.PopulateObject(www.downloadHandler.text, opts);
                Debug.Log("set tileset options from " + optionsURL + ":\n" + www.downloadHandler.text);
                Debug.Log(JsonConvert.SerializeObject(opts, Formatting.Indented,
                                                      new JsonConverter[] { new StringEnumConverter() }));
                AddTileset(opts);
            }
            catch (System.Exception ex)
            {
                Debug.Log("error parsing tileset options: " + ex.Message);
            }
        }
        
    }

    protected override void _start()
    {
        string maxRequests = getURLParameter("MaxConcurrentRequests");
        if (!string.IsNullOrEmpty(maxRequests))
        {
            if (int.TryParse(maxRequests, out MaxConcurrentRequests))
            {
                Debug.Log("set MaxConcurrentRequests=" + MaxConcurrentRequests + " from URL parameter");
            }
            else
            {
                Debug.Log("error setting MaxConcurrentRequests=" + maxRequests + " from URL parameter");
            }
        }

        string sceneManifestUrl = getURLParameter("SceneManifestURL");
        if (!string.IsNullOrEmpty(sceneManifestUrl))
        {
            SceneManifestUrl = sceneManifestUrl;
        }
        if (!string.IsNullOrEmpty(SceneManifestUrl))
        {
            MakeTilesetsFromSceneFile();
        }
        else
        {
            int n = 0;
            string tilesetOptionsURL = getURLParameter("TilesetOptions" + n);
            while (!string.IsNullOrEmpty(tilesetOptionsURL))
            {
                GetOptions(tilesetOptionsURL);
                ++n;
                tilesetOptionsURL = getURLParameter("TilesetOptions" + n);
            }
            if(n == 0)
            {
                Debug.Log("empty scene manifest URL, consider setting URL parmeter \"SceneManifestUrl\" or pass individual tilesets with \"TilesetURL0\", \"TilesetURL1\", etc");
            }
        }
    }

    protected void MakeTilesetsFromSceneFile()
    {
        if (!string.IsNullOrEmpty(SceneManifestUrl))
        {
            //Read in scene json from options url
            LoadSceneJson(SceneManifestUrl).Done(json =>
            {
                SceneFormat.Scene scene = SceneFormat.Scene.FromJson(json);
                MakeTilesetFromScene(scene);
            });          
        }
        else
        {
            Debug.Log("empty scene URL, consider setting URL parmeter \"TilesetURL\"");
        }
    }

    protected void MakeTilesets(params string[] tilesetOptionsURLs)
    {
        foreach(string url in tilesetOptionsURLs)
        {
            Unity3DTilesetOptions opts = new Unity3DTilesetOptions();
            opts.Name = url;
            opts.Url = url;
            AddTileset(opts);
        }
    }
}