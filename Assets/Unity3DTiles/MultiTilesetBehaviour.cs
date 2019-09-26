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

        public void Fill(Vector3 v3)
        {
            x = v3.x;
            y = v3.y;
            z = v3.z;
        }

        public Vector3 V3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    public struct QuaternionSerializer
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public void Fill(Quaternion q)
        {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }

        public Quaternion Q()
        {
            return new Quaternion(x, y, z, w);
        }
    }

    public class SceneTileset
    {
        [JsonPropertyAttribute("id")]
        public string id { get; set; }
        [JsonPropertyAttribute("uri")]
        public string uri { get; set; }
        [JsonPropertyAttribute("frame_id")]
        public string frame_id { get; set; }
        [JsonPropertyAttribute("show")]
        public bool show { get; set; }
    }

    public class SceneImage
    {
        [JsonPropertyAttribute("id")]
        public string id { get; set; }
        [JsonPropertyAttribute("uri")]
        public string uri { get; set; }
        [JsonPropertyAttribute("frame_id")]
        public string frame_id { get; set; }
        [JsonPropertyAttribute("width")]
        public int width { get; set; }
        [JsonPropertyAttribute("height")]
        public int height { get; set; }
        [JsonPropertyAttribute("bands")]
        public int bands { get; set; }
    }

    public class SceneFrame
    {
        [JsonPropertyAttribute("frame_id")]
        public string frame_id { get; set; }
        [JsonPropertyAttribute("translation")]
        public Vector3Serializer translation { get; set; }
        [JsonPropertyAttribute("rotation")]
        public QuaternionSerializer rotation { get; set; }
        [JsonPropertyAttribute("scale")]
        public Vector3Serializer scale { get; set; }
        [JsonPropertyAttribute("parent_id")]
        public string parent_id { get; set; }
    }

    public class Scene
    {
        [JsonPropertyAttribute("version")]
        public string version { get; set; }
        [JsonPropertyAttribute("tilesets")]
        public List<SceneTileset> tilesets { get; set; }
        [JsonPropertyAttribute("images")]
        public List<SceneImage> images { get; set; }
        [JsonPropertyAttribute("frames")]
        public List<SceneFrame> frames { get; set; }

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
            Scene scene = JsonConvert.DeserializeObject<Scene>(data);
            return scene;
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
                return Matrix4x4.identity;
            } else
            {
                Matrix4x4 transform = Matrix4x4.Translate(frame.translation.V3()) * 
                    Matrix4x4.Scale(frame.scale.V3()) * 
                    Matrix4x4.Rotate(frame.rotation.Q());
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
        private RequestManager RequestManager;

        protected override void _lateUpdate()
        {
            foreach (var t in Tilesets.Values)
            {
                t.Update();
            }
            Stats = Unity3DTilesetStatistics.aggregate(Tilesets.Values.Select(t => t.Statistics).ToArray());
        }

        public void AddTileset(string tilesetName, string tilesetURL, Matrix4x4 rootTransform)
        {
            if(this.RequestManager == null || true)
            {
                this.RequestManager = new RequestManager(MaxConcurrentRequests);
            }
            Unity3DTilesetOptions options = new Unity3DTilesetOptions();
            options.Name = tilesetName;
            options.Url = tilesetURL;
            options.Show = true;
            options.Transform = rootTransform;
            Tilesets.Add(tilesetName, new Unity3DTileset(options, this, RequestManager, LRUCache));
            this.TilesetOptionsArray = Tilesets.Values.ToList().Select(t => t.TilesetOptions).ToArray();
            Stats = Unity3DTilesetStatistics.aggregate(this.Tilesets.Values.Select(t => t.Statistics).ToArray());
        }

        public void ShowTilesets(params string[] names)
        {
            foreach(string name in names)
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

        public Promise<string> LoadSceneJson(string url)
        {
            Promise<string> promise = new Promise<string>();
            this.StartCoroutine(DownloadSceneJson(url, promise));
            return promise;
        }

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

        protected virtual void MakeTilesetsFromSceneFile()
        {
            //Read in scene json from options url
            LoadSceneJson(SceneManifestUrl).Done(json =>
            {
                SceneFormat.Scene scene = SceneFormat.Scene.FromJson(json);

                //Create unity tilesets
                foreach (SceneFormat.SceneTileset tileset in scene.tilesets)
                {
                    var transform = scene.GetTransform(tileset.frame_id);
                    AddTileset(tileset.id, tileset.uri, transform);
                }

                Debug.Log(string.Format("Finished loading {0} tileset(s).", Tilesets.Count));
            });
        }
    }
}