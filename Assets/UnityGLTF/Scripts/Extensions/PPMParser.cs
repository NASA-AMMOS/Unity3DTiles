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
    /// <summary>
    /// Parse 16 bit per channel ppm (P6 only) file given as raw byte array; may optionally be gzipped
    /// Creates a 64 bit (16 bit/channel) RGBA texture by padding with 0 bytes
    /// TODO: Output channels remain UInt16, but texture format assumes half floating point type.
    ///       Only an issue if index images used as shader input (not implemented).
    ///       Could instead bake alternate textures in Unity3DTile.Initialize()
    /// </summary>
    public class PPMHeader
    {
        private struct PPMHeaderStruct
        {
            public int size;
            public int width;
            public int height;
            public int bytesPerVal;
            public int bands;
            public bool isZipped;
        }

        private static bool ReadHeader(byte[] data, ref PPMHeaderStruct header)
        {
            header.isZipped = true;
            using (Stream s = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    try
                    {
                        sr.ReadLine();
                    }
                    catch
                    {
                        header.isZipped = false; //Gzip failed
                    }
                }
            }
            using (Stream s = header.isZipped ? 
                              (Stream)new GZipStream(new MemoryStream(data), CompressionMode.Decompress)
                              : new MemoryStream(data))
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    int processedHeaderLines = 0;
                    int expectedHeaderLines = 3;
                    header.size = 0;
                    string[] headerLines = new string[3];

                    while (processedHeaderLines < expectedHeaderLines)
                    {
                        string line;
                        try
                        {
                            line = sr.ReadLine();
                        }
                        catch
                        {
                            return false; //Gzip failed
                        }
                        header.size += line.Length + 1; //1 byte new line
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
                    if (header.bytesPerVal != 2)
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
                using (Stream ms = header.isZipped ?
                                   (Stream)new GZipStream(new MemoryStream(data), CompressionMode.Decompress)
                                   : new MemoryStream(data))
                {       
                    using (var br = new BinaryReader(ms))
                    {
                        RawTextureInfo info = new RawTextureInfo();
                        info.Width = header.width;
                        info.Height = header.height;
                        info.HasMips = false;
                        info.Format = TextureFormat.RGBAHalf;
                        br.ReadBytes(header.size);
                        int rgbaBands = 4;
                        int dataSize = header.width * header.height * header.bytesPerVal * rgbaBands;
                        info.RawData = new byte[dataSize];
                        int index;
                        for (int r = 0; r < header.height; r++)
                        {
                            for (int c = 0; c < header.width; c++)
                            {
                                index = (r * header.width + c) * header.bytesPerVal * rgbaBands;
                                br.Read(info.RawData, index, header.bytesPerVal * header.bands);
                                //Pad remaining channels with 0 bytes for RGBA
                                for (int b = header.bands; b < rgbaBands; ++b)
                                {
                                    for (int i = 0; i < header.bytesPerVal; ++i)
                                    {
                                        info.RawData[index + b * header.bytesPerVal + i] = 0;
                                    }
                                }
                            }
                        }
                        return info;
                    }
                }
            }
            return null;
        }
    }
}
