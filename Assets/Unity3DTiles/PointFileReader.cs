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
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class PointFileReader
{
    private const int VERSION_NUM = 1;

    class BinaryBodyReference
    {
        public int byteOffset = 0;
    }

    class FeatureTable
    {
        public int POINTS_LENGTH = 0;
        public BinaryBodyReference POSITION = null;
        public BinaryBodyReference RGB = null;
        public BinaryBodyReference NORMAL = null;
    }

    public static List<Mesh> Read(byte[] data)
    {
        List<Mesh> meshes = new List<Mesh>();
        using (var ms = new MemoryStream(data))
        {
            using (var br = new BinaryReader(ms))
            {
                byte[] magic = br.ReadBytes(4);
                if (Encoding.ASCII.GetString(magic) != "pnts")
                {
                    throw new Exception("Pnts magic field mismatch");
                }
                UInt32 version = br.ReadUInt32();
                if (version != VERSION_NUM)
                {
                    throw new Exception("Pnts version mismatch");
                }
                br.ReadUInt32();    //total byte length
                UInt32 featureTableJsonByteLength = br.ReadUInt32();
                UInt32 featureTableBinaryByteLength = br.ReadUInt32();
                br.ReadUInt32();    //batch table JSON byte length
                br.ReadUInt32();    //batch table binary byte length
                var jsonString = Encoding.ASCII.GetString(br.ReadBytes((int)featureTableJsonByteLength));
                FeatureTable featureTableJson = JsonConvert.DeserializeObject<FeatureTable>(jsonString);
                byte[] binary = br.ReadBytes((int)featureTableBinaryByteLength);

                int pointsProcessed = 0;
                while (pointsProcessed < featureTableJson.POINTS_LENGTH)
                {
                    Mesh mesh = new Mesh();
                    Vector3[] vertices = new Vector3[Math.Min(ushort.MaxValue, featureTableJson.POINTS_LENGTH - pointsProcessed)];
                    Vector3[] normals;
                    Color[] colors;
                    int[] indices = new int[vertices.Length];

                    int index = 0;
                    for (int i = featureTableJson.POSITION.byteOffset + pointsProcessed * 12; i < featureTableJson.POSITION.byteOffset + (pointsProcessed + vertices.Length) * 12; i += 12)
                    {
                        float x = BitConverter.ToSingle(binary, i);
                        float y = BitConverter.ToSingle(binary, i + 4);
                        float z = BitConverter.ToSingle(binary, i + 8);
                        vertices[index++] = new Vector3(x, y, z);
                    }
                    mesh.vertices = vertices;

                    if (featureTableJson.NORMAL != null)
                    {
                        index = 0;
                        normals = new Vector3[vertices.Length];
                        for (int i = featureTableJson.NORMAL.byteOffset + pointsProcessed * 12; i < featureTableJson.NORMAL.byteOffset + (pointsProcessed + vertices.Length) * 12; i += 12)
                        {
                            float x = BitConverter.ToSingle(binary, i);
                            float y = BitConverter.ToSingle(binary, i + 4);
                            float z = BitConverter.ToSingle(binary, i + 8);
                            normals[index++] = new Vector3(x, y, z);
                        }
                        mesh.normals = normals;
                    }

                    if (featureTableJson.RGB != null)
                    {
                        index = 0;
                        colors = new Color[vertices.Length];
                        for (int i = featureTableJson.RGB.byteOffset + pointsProcessed * 3; i < featureTableJson.RGB.byteOffset + (pointsProcessed + vertices.Length) * 3; i += 3)
                        {
                            float r = (float)binary[i] / 255;
                            float g = (float)binary[i + 1] / 255;
                            float b = (float)binary[i + 2] / 255;
                            colors[index++] = new Color(r, g, b, 1);
                        }
                        mesh.colors = colors;
                    }

                    for (int i = 0; i < indices.Length; i++)
                    {
                        indices[i] = i;
                    }
                    mesh.SetIndices(indices, MeshTopology.Points, 0);
                    meshes.Add(mesh);
                    pointsProcessed += ushort.MaxValue;
                }
            }
        }
        return meshes;
    }
}
