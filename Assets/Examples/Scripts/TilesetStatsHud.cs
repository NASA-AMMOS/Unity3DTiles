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
using UnityEngine.UI;
using Unity3DTiles;

public class TilesetStatsHud : MonoBehaviour
{
    public AbstractTilesetBehaviour tileset;
    public Text Text;

    public string ExtraMessage;

    [Tooltip("How many frames should we consider into our average calculation?")]
    public int FPSFrameRange = 60;

    private int[] fpsBuffer;
    private int fpsBufferIndex;

    private StringBuilder builder = new StringBuilder();

    public void OnEnable()
    {
        if (Text != null)
        {
            Text.transform.parent.gameObject.SetActive(true);
        }
    }

    public void OnDisable()
    {
        if (Text != null)
        {
            Text.transform.parent.gameObject.SetActive(false);
        }
    }

	public void Update ()
    {
        if (Text == null)
        {
            return;
        }

        if (tileset == null || tileset.Stats == null || !tileset.enabled || !tileset.gameObject.activeInHierarchy)
        {
            Text.text = "";
            return;
        }

        if (fpsBuffer == null || fpsBuffer.Length != FPSFrameRange)
        {
            InitFPSBuffer();
        }

        UpdateFPSBuffer();

        string fpsString = GetFPSString(CalculateFps());

        var stats = tileset.Stats;

        builder.Clear();
        builder.Append("FPS: ");
        builder.Append(fpsString);
        builder.AppendLine();
#if UNITY_2017_2_OR_NEWER
        int missedFrames;
        if (UnityEngine.XR.XRStats.TryGetDroppedFrameCount(out missedFrames))
        {
            builder.Append("missed frames: ");
            builder.Append(missedFrames);
            builder.AppendLine();
        }
#endif        
        builder.Append(stats.NumberOfTilesTotal);
        builder.Append(" total tiles, ");
        builder.Append(stats.ReadyTiles);
        builder.Append(" ready, ");
        builder.Append(stats.ProcessingQueueLength);
        builder.Append(" to process");
        builder.AppendLine();

        builder.Append(stats.UsedSet);
        builder.Append(" used tiles, ");
        builder.Append(stats.FrustumSet);
        builder.Append(" in frustum, ");
        builder.Append(stats.ColliderSet);
        builder.Append(" with colliders");
        builder.AppendLine();

        builder.Append("load progress ");
        builder.Append((int)(stats.LoadProgress * 100));
        builder.Append("%, ");
        builder.Append(stats.PendingTiles);
        builder.Append(" pending");
        builder.AppendLine();

        builder.Append(stats.ActiveDownloads);
        builder.Append(" active downloads, ");
        builder.Append(stats.RequestQueueLength.ToString("d3"));
        builder.Append(" queued");
        builder.AppendLine();

        builder.Append("cached tiles: ");
        builder.Append(stats.DownloadedTiles);
        builder.Append("/");
        builder.Append(tileset.SceneOptions.CacheMaxSize);
        builder.Append(", ");
        builder.Append(tileset.TileCache.Unused);
        builder.Append(" unused");
        builder.AppendLine();

        builder.Append("visible tiles: ");
        builder.Append(stats.VisibleTiles);
        builder.Append(" (depth ");
        builder.Append(stats.MinVisibleTileDepth);
        builder.Append("-");
        builder.Append(stats.MaxVisibleTileDepth);
        builder.Append(" / ");
        builder.Append(tileset.DeepestDepth());
        builder.Append(")");
        builder.AppendLine();

        builder.Append("visible faces: ");
        builder.Append(stats.VisibleFaces / 1000);
        builder.Append(" k");
        builder.AppendLine();
        builder.Append("visible textures: ");
        builder.Append(stats.VisibleTextures);
        builder.AppendLine();
        builder.Append("visible megapixels: ");
        builder.Append((stats.VisiblePixels / 1000000f).ToString("0.00"));
        builder.AppendLine();

        if (!string.IsNullOrEmpty(ExtraMessage))
        {
            builder.Append(ExtraMessage);
            builder.AppendLine();
        }

        Text.text = builder.ToString();
    }   

    private void InitFPSBuffer()
    {      
        if (FPSFrameRange <= 0)
        {
            FPSFrameRange = 1;
        }
        fpsBuffer = new int[FPSFrameRange];
        fpsBufferIndex = 0;
    }

    private void UpdateFPSBuffer()
    {
        fpsBuffer[fpsBufferIndex++] = (int)(1f / Time.unscaledDeltaTime);
        if (fpsBufferIndex >= FPSFrameRange)
        {
            fpsBufferIndex = 0;
        }
    }

    private float CalculateFps()
    {
        int sum = 0;
        for (int i = 0; i < FPSFrameRange; i++)
        {
            int fps = fpsBuffer[i];
            sum += fps;
        }
        return sum / FPSFrameRange;
    }

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

    private string GetFPSString(float fps)
    {
        return StringsFrom00To99[Mathf.Clamp((int)Mathf.Round(fps), 0, 99)];
    }
}
