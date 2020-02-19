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
using System.Text;
using UnityEngine;
using Unity3DTiles;

public class TilesetStatsHud : MonoBehaviour {

    public AbstractTilesetBehaviour tileset;
    public string message;

    [Tooltip("How many frames should we consider into our average calculation?")]
    [SerializeField]
    private int frameRange = 60;

    private int averageFps;
    private int[] fpsBuffer;
    private int fpsBufferIndex;

    private static readonly string[] StringsFrom00To99 =
    {
            "00", "01", "02", "03", "04", "05", "06", "07", "08", "09",
            "10", "11", "12", "13", "14", "15", "16", "17", "18", "19",
            "20", "21", "22", "23", "24", "25", "26", "27", "28", "29",
            "30", "31", "32", "33", "34", "35", "36", "37", "38", "39",
            "40", "41", "42", "43", "44", "45", "46", "47", "48", "49",
            "50", "51", "52", "53", "54", "55", "56", "57", "58", "59",
            "60", "61", "62", "63", "64", "65", "66", "67", "68", "69",
            "70", "71", "72", "73", "74", "75", "76", "77", "78", "79",
            "80", "81", "82", "83", "84", "85", "86", "87", "88", "89",
            "90", "91", "92", "93", "94", "95", "96", "97", "98", "99"
    };
    public UnityEngine.UI.Text text;
    StringBuilder builder = new StringBuilder();

	// Use this for initialization
	void Start ()
    {
        text = GetComponent<UnityEngine.UI.Text>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        if(tileset == null || tileset.Stats == null || !tileset.enabled || !tileset.gameObject.activeInHierarchy)
        {
            text.text = "";
            return;
        }
        if (fpsBuffer == null || fpsBuffer.Length != frameRange)
        {
            InitBuffer();
        }
        UpdateFrameBuffer();
        CalculateFps();
        string fpsString = GetFPSString(averageFps);

        var stats = tileset.Stats;
        builder.Remove(0, builder.Length);
        builder.Append("FPS: ");
        builder.Append(fpsString);
        builder.AppendLine();
#if UNITY_2017_2_OR_NEWER
        int missedFrames;
        if (UnityEngine.XR.XRStats.TryGetDroppedFrameCount(out missedFrames))
        {
            builder.Append("Missed Frames: ");
            builder.Append(missedFrames);
            builder.AppendLine();
        }
#endif        
        builder.Append("Visible Tiles: ");
        builder.Append(stats.VisibleTileCount);
        builder.AppendLine();
        builder.Append("Active Colliders: ");
        builder.Append(stats.ColliderTileCount);
        builder.AppendLine();
        builder.Append("Used Set: ");
        builder.Append(stats.UsedSetCount);
        builder.AppendLine();
        builder.Append("Content Loaded: ");
        builder.Append(stats.LoadedContentCount);
        builder.AppendLine();
        builder.Append("Content Cache: ");
        builder.Append(tileset.LRUCache.Count);
        builder.Append(" / ");
        builder.Append(tileset.SceneOptions.LRUCacheMaxSize);
        builder.AppendLine();
        builder.Append("Tiles Remaining: ");
        builder.Append(stats.TilesLeftToLoad);
        builder.AppendLine();
        builder.Append("Visible Faces: ");
        builder.Append(stats.VisibleFaces / 1000);
        builder.Append(" k");
        builder.AppendLine();
        builder.Append("Visible Textures: ");
        builder.Append(stats.VisibleTextures);
        builder.AppendLine();
        builder.Append("Visible Megapixels: ");
        builder.Append((stats.VisiblePixels / 1000000f).ToString("0.00"));
        builder.AppendLine();
        if (!string.IsNullOrEmpty(message))
        {
            builder.Append(message);
            builder.AppendLine();
        }

        text.text = builder.ToString();

    }   

    private void InitBuffer()
    {      
        if (frameRange <= 0)
        {
            frameRange = 1;
        }
        fpsBuffer = new int[frameRange];
        fpsBufferIndex = 0;
    }

    private string GetFPSString(int fps)
    {
        return StringsFrom00To99[Mathf.Clamp(fps, 0, 99)];
    }

    private void UpdateFrameBuffer()
    {
        fpsBuffer[fpsBufferIndex++] = (int)(1f / Time.unscaledDeltaTime);
        if (fpsBufferIndex >= frameRange)
        {
            fpsBufferIndex = 0;
        }
    }

    private void CalculateFps()
    {
        int sum = 0;
        for (int i = 0; i < frameRange; i++)
        {
            int fps = fpsBuffer[i];
            sum += fps;
        }
        averageFps = sum / frameRange;
    }
}
