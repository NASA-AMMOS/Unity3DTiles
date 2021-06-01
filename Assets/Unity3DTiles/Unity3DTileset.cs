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
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using RSG;
using UnityGLTF.Loader;
using UnityGLTF.Extensions;

namespace Unity3DTiles
{
    /// <summary>
    /// A 3D Tiles tileset used for streaming large 3D datasets
    /// See https://github.com/AnalyticalGraphicsInc/cesium/blob/master/Source/Scene/Cesium3DTileset.js
    /// </summary>
    public class Unity3DTileset
    {
        public Unity3DTilesetOptions TilesetOptions { get; private set;}

        public Unity3DTile Root { get; private set; }

        public TileCache TileCache { get; private set; }

        public RequestManager RequestManager { get; private set;}

        public Queue<Unity3DTile> ProcessingQueue { get; private set; }

        public AbstractTilesetBehaviour Behaviour { get; private set; }

        /// <summary>
        /// The deepest depth of the tree as specified by the loaded json structure.  This may increase as
        /// recursive json tilesets are loaded.  This value does not depend on what renderable content has been loaded.
        /// </summary>
        public int DeepestDepth { get; private set; }

        public readonly Unity3DTilesetStatistics Statistics = new Unity3DTilesetStatistics();

        public Unity3DTilesetTraversal Traversal { get; private set; }

        public bool Ready { get { return Root != null; } }

        private Schema.Tileset schemaTileset;

        public void GetRootTransform(out Vector3 translation, out Quaternion rotation, out Vector3 scale,
                                     bool convertToUnityFrame = true)
        {
            var m = Matrix4x4.TRS(TilesetOptions.Translation, TilesetOptions.Rotation, TilesetOptions.Scale);
            if (convertToUnityFrame)
            {
                m = m.UnityMatrix4x4ConvertFromGLTF();
            }
            translation = new Vector3(m.m03, m.m13, m.m23);
            rotation = m.rotation;
            scale = m.lossyScale;
        }

        public Matrix4x4 GetRootTransform(bool convertToUnityFrame = true)
        {
            GetRootTransform(out Vector3 translation, out Quaternion rotation, out Vector3 scale, convertToUnityFrame);
            return Matrix4x4.TRS(translation, rotation, scale);
        }

        public Unity3DTileset(Unity3DTilesetOptions tilesetOptions, AbstractTilesetBehaviour behaviour)
        {
            TilesetOptions = tilesetOptions;
            Behaviour = behaviour;
            RequestManager = behaviour.RequestManager;
            ProcessingQueue = behaviour.ProcessingQueue;
            TileCache = behaviour.TileCache;
            Traversal = new Unity3DTilesetTraversal(this, behaviour.SceneOptions);
            DeepestDepth = 0;

            string url = UrlUtils.ReplaceDataProtocol(tilesetOptions.Url);
            string tilesetUrl = url;
            if (!UrlUtils.GetLastPathSegment(url).EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                tilesetUrl = UrlUtils.JoinUrls(url, "tileset.json");
            }

            LoadTilesetJson(tilesetUrl).Then(json =>
            {
                // Load Tileset (main tileset or a reference tileset)
                schemaTileset = Schema.Tileset.FromJson(json);
                Root = LoadTileset(tilesetUrl, schemaTileset, null);
            }).Catch(error =>
            {
                Debug.LogError(error.Message + "\n" + error.StackTrace);
            });
        }

        public Promise<string> LoadTilesetJson(string url)
        {
            Promise<string> promise = new Promise<string>();
            Behaviour.StartCoroutine(DownloadTilesetJson(url, promise));
            return promise;
        }

        IEnumerator DownloadTilesetJson(string url, Promise<string> promise)
        {
            Action<string, string> onDownload = new Action<string, string>((text, error) =>
            {
                if (error == null || error == "")
                {
                    promise.Resolve(text);
                    
                }
                else
                {
                    promise.Reject(new System.Exception("Error downloading " + url + " " + error));
                }
            });

            UnityWebRequestLoader wrapper = new UnityWebRequestLoader("");
            yield return wrapper.Send(url, "", onDownloadString: onDownload);
        }

        private Unity3DTile LoadTileset(string tilesetUrl, Schema.Tileset tileset, Unity3DTile parentTile)
        {
            if (tileset.Asset == null)
            {
                Debug.LogError("Tileset must have an asset property");
                return null;
            }
            if (tileset.Asset.Version != "0.0" && tileset.Asset.Version != "1.0")
            {
                Debug.LogError("Tileset must be 3D Tiles version 0.0 or 1.0");
                return null;
            }
            // Add tileset version to base path
            bool hasVersionQuery = new Regex(@"/[?&]v=/").IsMatch(tilesetUrl);
            if (!hasVersionQuery && !new Uri(tilesetUrl).IsFile)
            {
                string version = "0.0";
                if (tileset.Asset.TilesetVersion != null)
                {
                    version = tileset.Asset.TilesetVersion;
                }
                string versionQuery = "v=" + version;
                tilesetUrl = UrlUtils.SetQuery(tilesetUrl, versionQuery);
            }
            // A tileset.json referenced from a tile may exist in a different directory than the root tileset.
            // Get the basePath relative to the external tileset.
            string basePath = UrlUtils.GetBaseUri(tilesetUrl);
            Unity3DTile rootTile = new Unity3DTile(this, basePath, tileset.Root, parentTile);
            Statistics.NumberOfTilesTotal++;

            // Loop through the Tile json data and create a tree of Unity3DTiles
            Stack<Unity3DTile> stack = new Stack<Unity3DTile>();
            stack.Push(rootTile);
            while (stack.Count > 0)
            {
                Unity3DTile tile3D = stack.Pop();
                for (int i = 0; i < tile3D.schemaTile.Children.Count; i++)
                {
                    Unity3DTile child = new Unity3DTile(this, basePath, tile3D.schemaTile.Children[i], tile3D);
                    DeepestDepth = Math.Max(child.Depth, DeepestDepth);
                    Statistics.NumberOfTilesTotal++;
                    stack.Push(child);
                }
                // TODO consider using CullWithChildrenBounds optimization here
            }
            return rootTile;
        }

        public void Update()
        {
            Statistics.Clear();
            if (Ready)
            {
                Traversal.Run();
                if (TilesetOptions.DebugDrawBounds)
                {
                    Traversal.DrawDebug();
                }
            }
        }

        public void UpdateStats()
        {
            Statistics.RequestQueueLength = RequestManager.Count(t => t.Tileset == this);
            Statistics.ActiveDownloads = RequestManager.CountActiveDownloads(t => t.Tileset == this);
            Statistics.ProcessingQueueLength = ProcessingQueue.Count(t => t.Tileset == this);
            Statistics.DownloadedTiles = TileCache.Count(t => t.Tileset == this);
            Statistics.ReadyTiles =
                TileCache.Count(t => t.Tileset == this && t.ContentState == Unity3DTileContentState.READY);
            Unity3DTilesetStatistics.MaxLoadedTiles = TileCache.MaxSize;
        }
    }
}
