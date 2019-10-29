using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity3DTiles;

public class StreamingAssetRelativePathUpdated : MonoBehaviour {


    public GameObject[] GameObjects;

	// Use this for initialization
	void Start ()
    {
		foreach(var go in GameObjects)
        {
            if (go.gameObject != null)
            {
                var b3dm = go.gameObject.GetComponent<Unity3DTiles.B3DMComponent>();
                var gltf = go.gameObject.GetComponent<UnityGLTF.GLTFComponent>();
                var tileset = go.gameObject.GetComponent<TilesetBehaviour>();

                if (b3dm != null)
                {
                    if (!b3dm.Url.ToLower().StartsWith("http"))
                    {
                        b3dm.Url = Path.Combine(Application.dataPath, Path.Combine("StreamingAssets", b3dm.Url));
                    }
                }
                else if (gltf != null)
                {
                    if (!gltf.GLTFUri.ToLower().StartsWith("http"))
                    {
                        gltf.GLTFUri = Path.Combine(Application.dataPath, Path.Combine("StreamingAssets", gltf.GLTFUri));
                    }
                }
                else if(tileset != null)
                {
                    Unity3DTilesetOptions tilesetOptions = tileset.TilesetOptions;
                    if (!tilesetOptions.Url.ToLower().StartsWith("http"))
                    {
                        tilesetOptions.Url = Path.Combine(Application.dataPath, Path.Combine("StreamingAssets", tilesetOptions.Url));
                    }
                }
                go.gameObject.SetActive(true);
            }
        }

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
