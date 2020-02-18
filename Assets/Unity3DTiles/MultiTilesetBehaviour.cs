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

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
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
        private static Thread mainThread = Thread.CurrentThread;
        private int numTilesetsWas = 0;
        public void OnBeforeSerialize()
        {
            numTilesetsWas = TilesetOptions.Count;
        }
        public void OnAfterDeserialize()
        {
            int added = TilesetOptions.Count - numTilesetsWas;
            if (added > 0 && Thread.CurrentThread == mainThread)
            {
                for (int i = 0; i < added; i++)
                {
                    //init fields of newly added elements to defaults
                    TilesetOptions[TilesetOptions.Count - i - 1] = new Unity3DTilesetOptions();
                }
            }
            numTilesetsWas = TilesetOptions.Count;
        }
#endif

        private Dictionary<string, Unity3DTileset> Tilesets = new Dictionary<string, Unity3DTileset>();

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
            var tempList = Tilesets.Values.ToList(); 
            var tilesetList = tempList.Skip(startIndex).ToList();
            tilesetList.AddRange(tempList.Take(startIndex));
            if(++startIndex >= tilesetList.Count)
            {
                startIndex = 0;
            }
            // Update
            foreach(var t in tilesetList)
            {
                t.Update();
            }
        }

        protected override void UpdateStats()
        {
            Stats = Unity3DTilesetStatistics.aggregate(Tilesets.Values.Select(t => t.Statistics).ToArray());
        }

        public override BoundingSphere BoundingSphere()
        {
            if (Tilesets.Count == 0)
            {
                return new BoundingSphere(Vector3.zero, 0.0f);
            }
            var spheres = Tilesets.Values.Select(ts => ts.Root.BoundingVolume.BoundingSphere()).ToList();
            var ctr = spheres.Aggregate(Vector3.zero, (sum, sph) => sum += sph.position);
            ctr *= 1.0f / spheres.Count;
            var radius = spheres.Max(sph => Vector3.Distance(ctr, sph.position) + sph.radius);
            return new BoundingSphere(ctr, radius);
        }

        public bool AddTileset(string name, string url, Matrix4x4 rootTransform, bool show,
                               Unity3DTilesetOptions options = null)
        {
            options = options ?? new Unity3DTilesetOptions();
            options.Name = name;
            options.Url = url;
            options.Transform = rootTransform;
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
            if (Tilesets.ContainsKey(options.Name))
            {
                Debug.LogWarning(String.Format("Attempt to add tileset with duplicate name {0} failed.", options.Name));
                return false;
            }
            if (SceneOptions.GLTFShaderOverride != null && options.GLTFShaderOverride == null)
            {
                options.GLTFShaderOverride = SceneOptions.GLTFShaderOverride;
            }
            Tilesets.Add(options.Name, new Unity3DTileset(options, this));
            if (!TilesetOptions.Contains(options))
            {
                TilesetOptions.Add(options);
            }
            UpdateStats();
            return true;
        }

        public Unity3DTileset RemoveTileset(string name)
        {
            var tileset = GetTileset(name);
            Tilesets.Remove(name);
            TilesetOptions.Remove(tileset.TilesetOptions); //ok if it wasn't there
            UpdateStats();
            return tileset;
        }

        public IEnumerable<Unity3DTileset> GetTilesets()
        {
            return Tilesets.Values;
        }

        public Unity3DTileset GetTileset(string name)
        {
            if (Tilesets.ContainsKey(name))
            {
                return Tilesets[name];
            }
            Debug.LogWarning(String.Format("No tileset: {0}", name));
            return null;
        }

        public void ShowTilesets(params string[] names)
        {
            foreach (string name in names)
            {
                Tilesets[name].TilesetOptions.Show = true;
            }
        }

        public void HideTilesets(params string[] names)
        {
            foreach (string name in names)
            {
                Tilesets[name].TilesetOptions.Show = false;
            }
        }

        public IEnumerable<Unity3DTilesetStatistics> GetStatistics(params string[] names)
        {
            foreach (string name in names)
            {
                yield return Tilesets[name].Statistics;
            }
        }

        public void AddScene(Scene scene)
        {
            foreach (var tileset in scene.tilesets)
            {
                var rootTransform = scene.GetTransform(tileset.frame_id);
                AddTileset(tileset.id, tileset.uri, rootTransform, tileset.show, tileset.options);
            }
        }
    }
}
