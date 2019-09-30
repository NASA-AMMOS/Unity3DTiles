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
public class GenericWebTilesetBehaviour : TilesetBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string getURLParameter(string name);
#else
    private static string getURLParameter(string name)
    {
        Debug.LogWarning("cannot get URL parameter \"" + name + "\"");

        return null;
    }
#endif

    public override void Start()
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

        string optionsURL = getURLParameter("TilesetOptions");
        if (!string.IsNullOrEmpty(optionsURL))
        {
            StartCoroutine(GetOptions(optionsURL));
        }
        else
        {
            MakeTileset();
        }
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
                JsonConvert.PopulateObject(www.downloadHandler.text, TilesetOptions);
                Debug.Log("set tileset options from " + optionsURL + ":\n" + www.downloadHandler.text);
                Debug.Log(JsonConvert.SerializeObject(TilesetOptions, Formatting.Indented,
                                                      new JsonConverter[] {new StringEnumConverter()}));
            }
            catch (System.Exception ex)
            {
                Debug.Log("error parsing tileset options: " + ex.Message);
            }
        }

        MakeTileset();
    }

    protected override void MakeTileset()
    {
        string tilesetURL = getURLParameter("TilesetURL");
        if (!string.IsNullOrEmpty(tilesetURL))
        {
            TilesetOptions.Url = tilesetURL;
            Debug.Log("set tileset URL from URL parameter: " + tilesetURL);
        }

        Camera.main.transform.position = TilesetOptions.DefaultCameraPosition;
        Camera.main.transform.eulerAngles = TilesetOptions.DefaultCameraRotation;

        if (!string.IsNullOrEmpty(TilesetOptions.Url))
        {
            base.MakeTileset();
        }
        else
        {
            Debug.Log("empty tileset URL, consider setting URL parmeter \"TilesetURL\"");
        }
    }
}
