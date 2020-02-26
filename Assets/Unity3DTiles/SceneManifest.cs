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
using UnityEngine;

namespace Unity3DTiles
{
namespace SceneManifest
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
        public Unity3DTilesetOptions options; //Name overriden by id, Url by uri, Show by show, Transform by frame_id
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
        public string id;
        public Vector3Serializer translation;
        public QuaternionSerializer rotation = new QuaternionSerializer { x = 0, y = 0, z = 0, w = 1 };
        public Vector3Serializer scale = new Vector3Serializer { x = 1, y = 1, z = 1 };
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
                if(string.Compare(frame_id, frame.id) == 0)
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
            if (string.IsNullOrEmpty(frame.parent_id))
            {
                return Matrix4x4.TRS(frame.translation, frame.rotation, frame.scale);
            }
            else
            {
                Matrix4x4 transform = Matrix4x4.TRS(frame.translation, frame.rotation, frame.scale);
                return GetTransform(frame.parent_id) * transform; //Apply this transform before parent transform
            }
        }
    }
}
}
