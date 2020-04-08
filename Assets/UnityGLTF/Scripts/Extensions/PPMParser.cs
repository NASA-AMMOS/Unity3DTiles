using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GLTF
{
    public class PPMHeader
    {
        private struct PPMHeaderStruct
        {
            public int size;
            public int width;
            public int height;
            public int bytesPerVal;
            public int bands;
        }

        private static bool ReadHeader(byte[] data, ref PPMHeaderStruct header)
        {
            Debug.Log("Data size = " + data.Count());
            Debug.Log(data[0] + " " + data[1] + " " + data[2] + " " + data[3] + " " + data[4]);
            //Debug.Log("Entering read header");
            using (Stream s = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
            {
                //Debug.Log("Created stream");
                using (StreamReader sr = new StreamReader(s))
                {
                    //Debug.Log("Processing");
                    int processedHeaderLines = 0;
                    int expectedHeaderLines = 3;
                    header.size = 0;
                    string[] headerLines = new string[3];

                    while (processedHeaderLines < expectedHeaderLines)
                    {
                        //Debug.Log("About to read");
                        string line;
                        try
                        {
                            line = sr.ReadLine();
                        }
                        catch
                        {
                            return false; //Gzip failed
                        }
                        //Debug.Log("Read complete");
                        header.size += line.Length + 1; //2 byte new line
                        if (line.Length == 0 || line[0] == '#') //skip empty lines/comments
                        {
                            continue;
                        }
                        else
                        {
                            headerLines[processedHeaderLines] = line;
                            processedHeaderLines++;
                        }
                    }

                    if (headerLines[0] != "P6")
                    {
                        return false;
                    }

                    var split = headerLines[1].Split(' ');
                    if (split.Count() != 2 ||
                       !Int32.TryParse(split[0], out header.width) ||
                       !Int32.TryParse(split[1], out header.height))
                    {
                        return false;
                    }

                    //Debug.Log("header.width = " + header.width);
                    //Debug.Log("header.height = " + header.height);

                    if (!Int32.TryParse(headerLines[2].Replace(" ", ""), out int maxVal))
                    {
                        return false;
                    }
                    if (maxVal <= 0 || maxVal > 65535)
                    {
                        return false;
                    }

                    sr.Close();

                    header.bands = 3;
                    header.bytesPerVal = maxVal < 256 ? 1 : 2;
                    if(header.bytesPerVal != 2)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static RawTextureInfo Read(byte[] data)
        {
            PPMHeaderStruct header = new PPMHeaderStruct();
            if (ReadHeader(data, ref header))
            {
                using (var ms = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
                {       
                    using (var br = new BinaryReader(ms))
                    {
                        RawTextureInfo info = new RawTextureInfo();
                        info.Width = header.width;
                        info.Height = header.height;
                        info.HasMips = false;
                        info.Format = TextureFormat.RGB565;
                        br.ReadBytes(header.size);
                        int dataSize = header.width * header.height * header.bytesPerVal * header.bands;
                        info.RawData = new byte[dataSize];
                        br.Read(info.RawData, 0, dataSize);
                        //for (int r = 0; r < header.height; r++)
                        //{
                        //    for (int c = 0; c < header.width; c++)
                        //    {
                        //        for (int b = 0; b < header.bands; b++)
                        //        {
                        //            img[b, r, c] = BitConverter.ToUInt16(br.ReadBytes(bytesPerVal), 0);
                        //        }
                        //    }
                        //}
                        return info;
                    }
                }
            }
            return null;
        }
    }
}
