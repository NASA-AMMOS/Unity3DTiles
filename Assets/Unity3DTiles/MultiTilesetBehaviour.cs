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
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Unity3DTiles.SceneManifest;

namespace Unity3DTiles
{
public class MultiTilesetBehaviour : AbstractTilesetBehaviour
#if UNITY_EDITOR
    , ISerializationCallbackReceiver
#endif
    {
        //mainly for inspecting/modifying tilesets when running in unity editor
        public List<Unity3DTilesetOptions> TilesetOptions = new List<Unity3DTilesetOptions>();

#if UNITY_EDITOR
        //workaround Unity editor not respecting defaults when adding element to a list
        //https://forum.unity.com/threads/lists-default-values.206956/
        //this is a nasty hack but it is only for dev use in editor
        //and it makes it a lot more friendly to use the editor inspector to add tilesets
        private static Thread mainThread = Thread.CurrentThread;
        private int numTilesetsWas = 0;
        public void OnBeforeSerialize()
        {
            numTilesetsWas = TilesetOptions.Count;
        }
        public void OnAfterDeserialize()
        {
            if (TilesetOptions.Count == 1 && numTilesetsWas == 0 && Thread.CurrentThread == mainThread)
            {
                //init first new element to defaults
                if (string.IsNullOrEmpty(TilesetOptions[0].Url))
                {
                    TilesetOptions[0] = new Unity3DTilesetOptions();
                }
            }
            numTilesetsWas = TilesetOptions.Count;
        }
#endif

        protected string baseUrl;

        private List<Unity3DTileset> tilesets = new List<Unity3DTileset>();
        private Dictionary<string, Unity3DTileset> nameToTileset = new Dictionary<string, Unity3DTileset>();

        private int startIndex = 0;

        protected override void _start()
        {
            foreach (var opts in TilesetOptions)
            {
                AddTileset(opts);
            }
        }

        protected override void _lateUpdate()
        {
            // Rotate processing order of tilesets each frame to avoid starvation (only upon request queue / cache full)
            startIndex = Mathf.Clamp(startIndex, 0, tilesets.Count - 1);
            for (int i = 0; i < tilesets.Count; i++)
            {
                tilesets[(i + startIndex) % tilesets.Count].Update();
            }
            if (tilesets.Count > 0)
            {
                startIndex = (startIndex + 1) % tilesets.Count;
            }
        }

        protected override void UpdateStats()
        {
            //this works but is very inefficient and drags down framerate significantly when many tilesets are loaded
            //foreach (var tileset in tilesets)
            //{
            //    tileset.UpdateStats();
            //}
            RequestManager.ForEachQueuedDownload(t => { t.Tileset.Statistics.RequestQueueLength++; });
            RequestManager.ForEachActiveDownload(t => { t.Tileset.Statistics.ActiveDownloads++; });
            foreach (var tile in ProcessingQueue)
            {
                tile.Tileset.Statistics.ProcessingQueueLength++;
            }
            TileCache.ForEach(t => {
                t.Tileset.Statistics.DownloadedTiles++;
                if (t.ContentState == Unity3DTileContentState.READY)
                {
                    t.Tileset.Statistics.ReadyTiles++;
                }
            });
            Unity3DTilesetStatistics.MaxLoadedTiles = TileCache.MaxSize;
            Stats = Unity3DTilesetStatistics.Aggregate(tilesets.Select(t => t.Statistics).ToArray());
        }

        public override bool Ready()
        {
            return tilesets.Count > 0 && tilesets.All(ts => ts.Ready);
        }

        public override BoundingSphere BoundingSphere(Func<Unity3DTileset, bool> filter = null)
        {
            var spheres = tilesets
                .Where(ts => filter == null || filter(ts))
                .Select(ts => ts.Root.BoundingVolume.BoundingSphere())
                .ToList();
            if (spheres.Count == 0)
            {
                return new BoundingSphere(Vector3.zero, 0.0f);
            }
            var ctr = spheres.Aggregate(Vector3.zero, (sum, sph) => sum += sph.position);
            ctr *= 1.0f / spheres.Count;
            var radius = spheres.Max(sph => Vector3.Distance(ctr, sph.position) + sph.radius);
            return new BoundingSphere(ctr, radius);
        }

        public override int DeepestDepth()
        {
            if (tilesets.Count == 0)
            {
                return 0;
            }
            return tilesets.Max(ts => ts.DeepestDepth);
        }

        public override void ClearForcedTiles()
        {
            foreach (var tileset in tilesets)
            {
                tileset.Traversal.ForceTiles.Clear();
            }
        }

        public bool AddTileset(string name, string url, Matrix4x4 rootTransform, bool show,
                               Unity3DTilesetOptions options = null)
        {
            options = options ?? new Unity3DTilesetOptions();
            options.Name = name;
            options.Url = url;
            options.Translation = new Vector3(rootTransform.m03, rootTransform.m13, rootTransform.m23);
            options.Rotation = rootTransform.rotation;
            options.Scale = rootTransform.lossyScale;
            options.Show = show;
            return AddTileset(options);
        }

        public bool AddTileset(Unity3DTilesetOptions options)
        {
            if (string.IsNullOrEmpty(options.Name))
            {
                options.Name = options.Url;
            }
            if (string.IsNullOrEmpty(options.Name) || string.IsNullOrEmpty(options.Url))
            {
                Debug.LogWarning("Attempt to add tileset with null or empty name or url failed.");
                return false;
            }
            options.Url = MakeAbsoluteUrl(options.Url);
            if (nameToTileset.ContainsKey(options.Name))
            {
                Debug.LogWarning(String.Format("Attempt to add tileset with duplicate name {0} failed.", options.Name));
                return false;
            }
            if (string.IsNullOrEmpty(options.ShaderOverride))
            {
                options.ShaderOverride = SceneOptions.ShaderOverride;
            }
            if (options.Style == null)
            {
                options.Style = SceneOptions.Style;
            }
            if (options.LoadIndices == IndexMode.Default)
            {
                options.LoadIndices = SceneOptions.LoadIndices;
            }
            var tileset = new Unity3DTileset(options, this);
            tilesets.Add(tileset);
            nameToTileset[options.Name] = tileset;
            if (!TilesetOptions.Contains(options))
            {
                TilesetOptions.Add(options);
            }
            UpdateStats();
            return true;
        }

        public Unity3DTileset RemoveTileset(string name)
        {
            if (!nameToTileset.ContainsKey(name))
            {
                return null;
            }
            var tileset = nameToTileset[name];
            nameToTileset.Remove(name);
            tilesets.Remove(tileset);
            TilesetOptions.Remove(tileset.TilesetOptions);
            UpdateStats();
            return tileset;
        }

        public IEnumerable<Unity3DTileset> GetTilesets()
        {
            return tilesets;
        }

        public Unity3DTileset GetTileset(string name)
        {
            if (nameToTileset.ContainsKey(name))
            {
                return nameToTileset[name];
            }
            return null;
        }

        public void ShowTilesets(params string[] names)
        {
            foreach (string name in names)
            {
                nameToTileset[name].TilesetOptions.Show = true;
            }
        }

        public void HideTilesets(params string[] names)
        {
            foreach (string name in names)
            {
                nameToTileset[name].TilesetOptions.Show = false;
            }
        }

        public IEnumerable<Unity3DTilesetStatistics> GetStatistics(params string[] names)
        {
            foreach (string name in names)
            {
                yield return nameToTileset[name].Statistics;
            }
        }

        /// <summary>
        /// Add all tilesets in a scene.
        /// If tilesetOptionsJson is given then it can (partially or fully) override the individual tileset options
        /// in the scene.
        /// </summary>
        public void AddScene(Scene scene, string tilesetOptionsJson = null)
        {
            foreach (var tileset in scene.tilesets)
            {
                var rootTransform = scene.GetTransform(tileset.frame_id);
                var opts = tileset.options;
                if (tilesetOptionsJson != null)
                {
                    opts = opts ?? new Unity3DTilesetOptions();
                    JsonConvert.PopulateObject(tilesetOptionsJson, opts);
                }
                AddTileset(tileset.id, tileset.uri, rootTransform, tileset.show, opts);
            }
        }

        protected string MakeAbsoluteUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }
            url = UrlUtils.ReplaceDataProtocol(url);
            if (!string.IsNullOrEmpty(baseUrl))
            {
                if (!UrlUtils.IsAbsolute(url))
                {
                    url = UrlUtils.JoinUrls(baseUrl, url);
                }
                else
                {
                    url = UrlUtils.JoinQuery(baseUrl, url);
                }
            }
            return url;
        }
    }
}
