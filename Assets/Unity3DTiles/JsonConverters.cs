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

    public class Matrix4x4Converter : JsonConverter
    {
        private class Matrix4x4Proxy
        {
            public float
                m00, m01, m02, m03,
                m10, m11, m12, m13,
                m20, m21, m22, m23,
                m30, m31, m32, m33;
        }

        public override bool CanRead { get { return true; } }

        public override bool CanWrite { get { return true; } }

        public override bool CanConvert(Type type)
        {
            return type == typeof(Matrix4x4);
        }

        public override object ReadJson(JsonReader reader, Type  type, object existing, JsonSerializer serializer)
        {
            var proxy = serializer.Deserialize<Matrix4x4Proxy>(reader);
            var ret = new Matrix4x4();
            ret.m00 = proxy.m00;
            ret.m01 = proxy.m01;
            ret.m02 = proxy.m02;
            ret.m03 = proxy.m03;
            ret.m10 = proxy.m10;
            ret.m11 = proxy.m11;
            ret.m12 = proxy.m12;
            ret.m13 = proxy.m13;
            ret.m20 = proxy.m20;
            ret.m21 = proxy.m21;
            ret.m22 = proxy.m22;
            ret.m23 = proxy.m23;
            ret.m30 = proxy.m30;
            ret.m31 = proxy.m31;
            ret.m32 = proxy.m32;
            ret.m33 = proxy.m33;
            return ret;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var m = (Matrix4x4)value;
            var proxy = new Matrix4x4Proxy();
            proxy.m00 = m.m00;
            proxy.m01 = m.m01;
            proxy.m02 = m.m02;
            proxy.m03 = m.m03;
            proxy.m10 = m.m10;
            proxy.m11 = m.m11;
            proxy.m12 = m.m12;
            proxy.m13 = m.m13;
            proxy.m20 = m.m20;
            proxy.m21 = m.m21;
            proxy.m22 = m.m22;
            proxy.m23 = m.m23;
            proxy.m30 = m.m30;
            proxy.m31 = m.m31;
            proxy.m32 = m.m32;
            proxy.m33 = m.m33;
            serializer.Serialize(writer, proxy);
        }
    }
}
