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
using RSG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SceneFormat
{
    //Serializers taken from https://forum.unity.com/threads/vector3-not-serializable.7766/
    [Serializable]
    public struct Vector3Serializer
    {
        public float x;
        public float y;
        public float z;

        public static implicit operator Vector3(Vector3Serializer v3s)
        {
            return new Vector3(v3s.x, v3s.y, v3s.z);
        }
    }

    [Serializable]
    public struct QuaternionSerializer
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public static implicit operator Quaternion(QuaternionSerializer qs)
        {
            return new Quaternion(qs.x, qs.y, qs.z, qs.w);
        }
    }

    public class SceneTileset
    {
        public string id;
        public string uri;
        public string frame_id;
        public bool show;
    }

    public class SceneImage
    {
        public string id;
        public string uri;
        public string frame_id;
        public int width;
        public int height;
        public int bands;
    }

    public class SceneFrame
    {
        public string frame_id;
        public Vector3Serializer translation;
        public QuaternionSerializer rotation;
        public Vector3Serializer scale;
        public string parent_id;
    }

    public class Scene
    {
        public string version;
        public List<SceneTileset> tilesets;
        public List<SceneImage> images;
        public List<SceneFrame> frames;

        public Scene()
        {
            this.version = "1.0";
            this.tilesets = new List<SceneTileset>();
            this.images = new List<SceneImage>();
            this.frames = new List<SceneFrame>();
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static Scene FromJson(string data)
        {
            return JsonConvert.DeserializeObject<Scene>(data);
        }

        private SceneFrame GetFrame(string frame_id)
        {
            foreach(SceneFrame frame in this.frames)
            {
                if(string.Compare(frame_id, frame.frame_id) == 0)
                {
                    return frame;
                }
            }
            Debug.LogWarning("Could not find frame: " + frame_id);
            return null;
        }

        public Matrix4x4 GetTransform(string frame_id)
        {
            SceneFrame frame = this.GetFrame(frame_id);
            if (frame == null)
            {
                throw new Exception("Could not get a transform for frame: " + frame_id);
            }
            if(frame.parent_id == "")
            {
                return Matrix4x4.TRS(frame.translation, frame.rotation, frame.scale);
            } else
            {
                Matrix4x4 transform = Matrix4x4.TRS(frame.translation, frame.rotation, frame.scale);
                return GetTransform(frame.parent_id) * transform; //Apply this transform before parent transform
            }
        }
    }
}

namespace Unity3DTiles
{
    public class MultiTilesetBehaviour : AbstractTilesetBehaviour
    {
        public string SceneManifestUrl = null;
        public Unity3DTilesetOptions[] TilesetOptionsArray = new Unity3DTilesetOptions[] { };
        private Dictionary<string, Unity3DTileset> Tilesets = new Dictionary<string, Unity3DTileset>();

        private int startIndex = 0;

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

        protected override void updateStats()
        {
            Stats = Unity3DTilesetStatistics.aggregate(Tilesets.Values.Select(t => t.Statistics).ToArray());
        }

        public bool AddTileset(string tilesetName, string tilesetURL, Matrix4x4 rootTransform)
        {
            Unity3DTilesetOptions options = new Unity3DTilesetOptions();
            options.Name = tilesetName;
            options.Url = tilesetURL;
            options.Show = true;
            options.Transform = rootTransform;
            return AddTileset(options);
        }

        public bool AddTileset(Unity3DTilesetOptions options)
        {
            if(Tilesets.ContainsKey(name))
            {
                Debug.LogWarning(String.Format("Attempt to add tileset with duplicate name {0} failed.", name));
                return false;
            }
            this.requestManager = this.requestManager ?? new RequestManager(MaxConcurrentRequests);
            Tilesets.Add(options.Name, new Unity3DTileset(options, this, requestManager, postDownloadQueue, LRUCache));
            updateOptionsAndStats();
            return true;
        }

        public Unity3DTileset RemoveTileset(string name)
        {
            var tileset = GetTileset(name);
            Tilesets.Remove(name);
            updateOptionsAndStats();
            return tileset;
        }

        private void updateOptionsAndStats()
        {
            this.TilesetOptionsArray = Tilesets.Values.ToList().Select(t => t.TilesetOptions).ToArray();
            updateStats();
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

        protected virtual void MakeTilesetFromScene(SceneFormat.Scene scene)
        {
            //Create unity tilesets
            foreach (SceneFormat.SceneTileset tileset in scene.tilesets)
            {
                var transform = scene.GetTransform(tileset.frame_id);
                AddTileset(tileset.id, tileset.uri, transform);
            }
        }
    }
}