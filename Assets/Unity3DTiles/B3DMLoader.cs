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
using UnityEngine;
using UnityGLTF.Loader;

public class B3DMLoader : ILoader
{
    private ILoader loader;

    public B3DMLoader(ILoader loader)
    {
        this.loader = loader;

    }

    public Stream LoadedStream { get; private set; }

    struct FeatureTable
    {
#pragma warning disable 0649
        public int BATCH_LENGTH;
        public double[] RTC_CENTER;
#pragma warning restore 0649
    }

    struct BatchTable
    {
#pragma warning disable 0649
        public double[] Longitude;
        public double[] Latitude;
        public int[] Height;
#pragma warning restore 0649
    }

    public IEnumerator LoadStream(string relativeFilePath)
    {
        yield return this.loader.LoadStream(relativeFilePath);

        if (this.loader.LoadedStream.Length == 0)
        {
            LoadedStream = new MemoryStream(0);
        }
        else
        {
            // We need to read the header info off of the .b3dm file
            // Using statment will ensure this.loader.LoadedStream is disposed
            using (BinaryReader br = new BinaryReader(this.loader.LoadedStream))
            {
                // Remove query parameters if there are any
                string filename = relativeFilePath.Split('?')[0];
                // If this isn't a b3dm file (i.e. gltf or glb) this should just copy the underlying stream
                if (Path.GetExtension(filename).ToLower() == ".b3dm")
                {
                    UInt32 magic = br.ReadUInt32();
                    if (magic != 0x6D643362)
                    {
                        Debug.LogError("Unsupported magic number in b3dm file: " + magic + " " + relativeFilePath);
                    }
                    UInt32 version = br.ReadUInt32();
                    if (version != 1)
                    {
                        Debug.LogError("Unsupported version number in b3dm file: " + version + " " + relativeFilePath);
                    }
                    // The length of the entire tile, including the header, in bytes.
                    UInt32 byteLength = br.ReadUInt32();
                    if (byteLength == 0)
                    {
                        Debug.LogError("Unexpected zero length in b3dm file: " + relativeFilePath);
                    }
                    // The length of the Feature Table JSON section in bytes.
                    UInt32 featureTableLength = br.ReadUInt32();
                    if (featureTableLength == 0)
                    {
                        Debug.LogError("Unexpected zero length feature table in b3dm file: " + relativeFilePath);
                    }
                    // The length of the Feature Table binary section in bytes.
                    UInt32 featureBinaryLength = br.ReadUInt32();
                    if (featureBinaryLength != 0)
                    {
                        Debug.LogError("Unexpected non-zero length feature binary in b3dm file: " + relativeFilePath);
                    }
                    // The length of the Batch Table JSON section in bytes. Zero indicates there is no Batch Table.
                    UInt32 batchTableLength = br.ReadUInt32();
                    // The length of the Batch Table binary section in bytes.
                    // If batchTableJSONByteLength is zero, this will also be zero.
                    UInt32 batchBinaryLength = br.ReadUInt32();
                    if (batchTableLength == 0 && batchBinaryLength != 0)
                    {
                        Debug.LogError("Unexpected non-zero length batch binary in b3dm file: " + relativeFilePath);
                    }
                    string featureTableJson = new String(br.ReadChars((int)featureTableLength));
                    FeatureTable ft = JsonConvert.DeserializeObject<FeatureTable>(featureTableJson);
                    if (batchTableLength == 0)
                    {
                        if(ft.BATCH_LENGTH != 0)
                        {
                            Debug.LogError("Unexpected non-zero length feature table BATCH_LENGTH in b3dm file: " + relativeFilePath);
                        }
                    } else
                    {
                        string batchTableJson = new string(br.ReadChars((int)batchTableLength));
                        BatchTable bt = JsonConvert.DeserializeObject<BatchTable>(batchTableJson);
                    }
                    // after this will be the binary glTF
                }
                LoadedStream = new MemoryStream((int)(loader.LoadedStream.Length - loader.LoadedStream.Position));
                CopyStream(loader.LoadedStream, LoadedStream);
            }
        }
    }

    public static void CopyStream(Stream input, Stream output)
    {
        byte[] b = new byte[32768];
        int r;
        while ((r = input.Read(b, 0, b.Length)) > 0)
        {
            output.Write(b, 0, r);
        }
    }
}
