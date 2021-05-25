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

    public Stream LoadedStream
    { 
        get
        {
            return loader.LoadedStream;
        }
    }

    struct FeatureTable
    {
#pragma warning disable 0649
        public int BATCH_LENGTH;
#pragma warning restore 0649
    }

    public IEnumerator LoadStream(string relativeFilePath)
    {
        yield return loader.LoadStream(relativeFilePath);

        if (loader.LoadedStream.Length > 0)
        {
            string filename = relativeFilePath.Split('?')[0]; // Remove query parameters if there are any

            if (Path.GetExtension(filename).ToLower() == ".b3dm")
            {
                BinaryReader br = new BinaryReader(loader.LoadedStream);
                UInt32 magic = br.ReadUInt32();
                if (magic != 0x6D643362)
                {
                    Debug.LogError("Unsupported magic number in b3dm: " + magic + " " + relativeFilePath);
                }
                UInt32 version = br.ReadUInt32();
                if (version != 1)
                {
                    Debug.LogError("Unsupported version number in b3dm: " + version + " " + relativeFilePath);
                }
                br.ReadUInt32(); // Total length
                UInt32 featureTableJsonLength = br.ReadUInt32();
                if (featureTableJsonLength == 0)
                {
                    Debug.LogError("Unexpected zero length JSON feature table in b3dm: " + relativeFilePath);
                }
                UInt32 featureTableBinaryLength = br.ReadUInt32();
                if (featureTableBinaryLength != 0)
                {
                    Debug.LogError("Unexpected non-zero length binary feature table in b3dm: " + relativeFilePath);
                }
                UInt32 batchTableJsonLength = br.ReadUInt32();
                if (batchTableJsonLength != 0)
                {
                    Debug.LogError("Unexpected non-zero length JSON batch table in b3dm: " + relativeFilePath);
                }
                UInt32 batchTableBinaryLength = br.ReadUInt32();
                if (batchTableBinaryLength != 0)
                {
                    Debug.LogError("Unexpected non-zero length binary batch table in b3dm: " + relativeFilePath);
                }
                string featureTableJson = new String(br.ReadChars((int)featureTableJsonLength));
                FeatureTable ft = JsonConvert.DeserializeObject<FeatureTable>(featureTableJson);
                if (ft.BATCH_LENGTH != 0)
                {
                    Debug.LogError("Unexpected non-zero length feature table BATCH_LENGTH in b3dm: " +
                                   relativeFilePath);
                }
            }
        }
    }
}
