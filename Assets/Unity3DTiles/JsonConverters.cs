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
using UnityEngine;
using Newtonsoft.Json;

namespace Unity3DTiles
{
    public class Vector3Converter : JsonConverter
    {
        private class Vector3Proxy
        {
            public float x, y, z;
        }

        public override bool CanRead { get { return true; } }

        public override bool CanWrite { get { return true; } }

        public override bool CanConvert(Type type)
        {
            return type == typeof(Vector3);
        }

        public override object ReadJson(JsonReader reader, Type  type, object existing, JsonSerializer serializer)
        {
            var proxy = serializer.Deserialize<Vector3Proxy>(reader);
            return new Vector3(x: proxy.x, y: proxy.y, z: proxy.z);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var v3 = (Vector3)value;
            var proxy = new Vector3Proxy { x = v3.x, y = v3.y, z = v3.z };
            serializer.Serialize(writer, proxy);
        }
    }

    public class QuaternionConverter : JsonConverter
    {
        private class QuaternionProxy
        {
            public float x, y, z, w;
        }

        public override bool CanRead { get { return true; } }

        public override bool CanWrite { get { return true; } }

        public override bool CanConvert(Type type)
        {
            return type == typeof(Quaternion);
        }

        public override object ReadJson(JsonReader reader, Type  type, object existing, JsonSerializer serializer)
        {
            var proxy = serializer.Deserialize<QuaternionProxy>(reader);
            return new Quaternion(x: proxy.x, y: proxy.y, z: proxy.z, w: proxy.w);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var q = (Quaternion)value;
            var proxy = new QuaternionProxy { x = q.x, y = q.y, z = q.z, w = q.w };
            serializer.Serialize(writer, proxy);
        }
    }
}
