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

    public void Start()
    {
        if (!string.IsNullOrEmpty(SceneManifestUrl))
        {
            //StartCoroutine(GetOptions(sceneManifestUrl));
            LoadSceneJson(SceneManifestUrl).Done(json =>
            {
                SceneFormat.Scene scene = SceneFormat.Scene.FromJson(json);
                Matrix4x4 transform = scene.GetTransform(scene.tilesets[0].frame_id);
                MakeTilesetsFromSceneFile();
            });
        }
        else
        {
            Debug.Log("empty scene manifest URL, consider setting URL parmeter \"SceneManifestUrl\"");       
        }
    }

    protected override void MakeTilesetsFromSceneFile()
    {
        if (!string.IsNullOrEmpty(SceneManifestUrl))
        {
            base.MakeTilesetsFromSceneFile();
        }
        else
        {
            Debug.Log("empty scene URL, consider setting URL parmeter \"TilesetURL\"");
        }
    }
}