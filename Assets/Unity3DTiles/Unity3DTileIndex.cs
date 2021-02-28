//#define IMAGE_SHARP_PNG
#define PNGCS_PNG
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
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
#if IMAGE_SHARP_PNG
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#elif PNGCS_PNG
using Hjg.Pngcs;
#endif
using UnityGLTF;
using UnityGLTF.Loader;

namespace Unity3DTiles
{
    public enum IndexMode
    {
        Default,      //use mode from SceneOptions, if any, to override TilesetOptions, else don't load indices
        None,         //don't load indices
        ExternalPNG,  //load index for foo.{b3dm,glb,gltf} from sibling url foo.png
        ExternalPPM,  //load index for foo.{b3dm,glb,gltf} from sibling url foo.ppm
        ExternalPPMZ, //load index for foo.{b3dm,glb,gltf} from sigling url foo.ppmz (gzip compressed)
        EmbeddedPNG,  //load index embedded in foo.{b3dm,glb,gltf} as 16 bit png
        EmbeddedPPM,  //load index embedded in foo.{b3dm,glb,gltf} as 16 bit ppm
        EmbeddedPPMZ  //load index embedded in foo.{b3dm,glb,gltf} as 16 bit ppm (gzip compressed)
    };

    /// <summary>
    /// Represents an index map which may optionally be associated with a tile.
    ///
    /// Typically, if a tile has an index map, it has exactly one texture, and the index map dimensions are the same as
    /// the texture dimensions.  The index map associates each pixel in the tile texture to another pixel in some other
    /// image.  For a pixel at (row=i, col=j) in the tile texture:
    ///
    /// tile.Content.Index[0, i, j] = an application specific identifier of the other image, typically 0 if none
    /// tile.Content.Index[1, i, j] = the row of the corresponding pixel in the other image
    /// tile.Content.Index[2, i, j] = the column of the corresponding pixel in the other image
    ///
    /// One use for indices is to allow for re-texturing of tilesets, where the pixels in the tile textures were taken
    /// from pixels in some other set of source images, and there is some other variant of those source images available
    /// at runtime.  A custom Unity3DTilesetStyle could be used to implement custom shading of the tile using an
    /// alternate texture built from the index and the other variant of the source images.
    ///
    /// To request indices be loaded indicate the format of the expected index data in the
    /// Unity3DTilesetOptions.LoadIndices. If an index map is available and successfully loads for a tile, it will
    /// become available (non-null) at tile.Content.Index when the load is complete.
    /// </summary>
    public class Unity3DTileIndex
    {
        /// Warnings on failed loads are normally disbled by default to avoid spamming the log when indices are
        /// generally expected but a tileset is loaded that lacks them.
        public const bool EnableLoadWarnings = false;

        public readonly int Width;
        public readonly int Height;

        public int NumNonzero { get; private set; } = 0;

        public uint this[int band, int row, int column]
        {
            get
            {
                return data[band][(row * Width) + column];
            }

            set
            {
                data[band][(row * Width) + column] = value;
                if (band == 0 && value != 0)
                {
                    NumNonzero++;
                }
            }
        }

        private uint[][] data;

        private Unity3DTileIndex(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            data = new uint[3][];
            for (int b = 0; b < 3; b++)
            {
                data[b] = new uint[width * height];
            }
        }

        public static IEnumerator Load(IndexMode mode, string tileUrl, Action<Unity3DTileIndex> success,
                                       Action<IndexMode, string, string> fail)
        {
            if (mode == IndexMode.Default || mode == IndexMode.None)
            {
                success(null);
                yield break;
            }

            string url = UrlUtils.ReplaceDataProtocol(tileUrl);
            string dir = UrlUtils.GetBaseUri(tileUrl);
            string file = UrlUtils.GetLastPathSegment(tileUrl);

            var filesToTry = new Queue<string>();
            if (mode == IndexMode.ExternalPNG)
            {
                filesToTry.Enqueue(UrlUtils.StripUrlExtension(file) + "_index.png");
                filesToTry.Enqueue(UrlUtils.ChangeUrlExtension(file, ".png"));
            }
            else if (mode == IndexMode.ExternalPPM)
            {
                filesToTry.Enqueue(UrlUtils.StripUrlExtension(file) + "_index.ppm");
                filesToTry.Enqueue(UrlUtils.ChangeUrlExtension(file, ".ppm"));
            }
            else if (mode == IndexMode.ExternalPPMZ)
            {
                filesToTry.Enqueue(UrlUtils.StripUrlExtension(file) + "_index.ppmz");
                filesToTry.Enqueue(UrlUtils.ChangeUrlExtension(file, ".ppmz"));
            }
            else
            {
                filesToTry.Enqueue(file);
            }

            Stream stream = null;
            Exception exception = null;
            string ext = null;
            while (stream == null && filesToTry.Count > 0)
            {
                exception = null;
                IEnumerator enumerator = null;
                ILoader loader = null;
                try
                {
                    file = filesToTry.Dequeue();
                    ext = UrlUtils.GetUrlExtension(file).ToLower();
                    loader = AbstractWebRequestLoader.CreateDefaultRequestLoader(dir);
                    if (ext == ".b3dm")
                    {
                        loader = new B3DMLoader(loader);
                    }
                    //yield return loader.LoadStream(file); //works but can't catch exceptions
                    enumerator = loader.LoadStream(file);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                while (exception == null)
                {
                    try
                    {
                        if (!enumerator.MoveNext())
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        break;
                    }
                    yield return enumerator.Current;
                }

                if (exception == null && loader.LoadedStream != null && loader.LoadedStream.Length > 0)
                {
                    stream = loader.LoadedStream;
                    stream.Seek(0, SeekOrigin.Begin);
                }
            }

            if (stream == null || stream.Length == 0)
            {
                fail(mode, tileUrl, "download failed" + (exception != null ? (" " + exception.Message) : ""));
                yield break;
            }

            try
            {
                if (ext == ".b3dm" || ext == ".glb")
                {
                    success(LoadFromGLB(stream));
                }
                else if (ext == ".gltf")
                {
                    success(LoadFromGLTF(stream));
                }
                else if (ext == ".png")
                {
                    success(LoadFromPNG(stream));
                }
                else if (ext == ".ppm" || ext == ".ppmz")
                {
                    success(LoadFromPPM(stream, compressed: ext == ".ppmz"));
                }
                else
                {
                    fail(mode, tileUrl, "unhandled file type: " + ext);
                }
            }
            catch (Exception ex)
            {
                fail(mode, tileUrl, "failed to parse " + file + ": " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public static Unity3DTileIndex LoadRaw(byte[] raw, string mimeType)
        {
            switch (mimeType)
            {
                case GLTFFile.PNG_MIME: return LoadFromPNG(new MemoryStream(raw));
                case GLTFFile.PPM_MIME: return LoadFromPPM(new MemoryStream(raw), compressed: false);
                case GLTFFile.PPMZ_MIME: return LoadFromPPM(new MemoryStream(raw), compressed: true);
                default: throw new Exception("unsupported tile index mime type " + mimeType);
            }
        }

        public static Unity3DTileIndex LoadFromGLB(Stream stream)
        {
            using (var br = new BinaryReader(stream)) //always reads little endian
            {
                var startPos = br.BaseStream.Position;
                if (br.ReadByte() != 'g' || br.ReadByte() != 'l' || br.ReadByte() != 'T' || br.ReadByte() != 'F')
                {
                    throw new Exception("invalid glb magic");
                }
                UInt32 ver = br.ReadUInt32();
                if (ver != 2)
                {
                    throw new Exception("invalid glb version: " + ver);
                }
                UInt32 len = br.ReadUInt32();
                byte[] jsonChunk = null, binChunk = null;
                while (br.BaseStream.Position - startPos < len)
                {
                    UInt32 chunkLen = br.ReadUInt32();
                    if (chunkLen > int.MaxValue)
                    {
                        throw new Exception("unsupported glb chunk length: " + chunkLen);
                    }
                    string chunkType = Encoding.ASCII.GetString(br.ReadBytes(4));
                    switch (chunkType)
                    {
                        case "JSON":
                        {
                            if (jsonChunk != null)
                            {
                                throw new Exception("more than one JSON chunk in glb");
                            }
                            jsonChunk = br.ReadBytes((int)chunkLen);
                            break;
                        }
                        case "BIN\0":
                        {
                            if (binChunk != null)
                            {
                                throw new Exception("more than one BIN chunk in glb");
                            }
                            binChunk = br.ReadBytes((int)chunkLen);
                            break;
                        }
                        default: throw new Exception("invalid glb chunk type: " + chunkType);
                    }
                }
                var gltf = GLTFFile.FromJson(Encoding.ASCII.GetString(jsonChunk));
                if (binChunk != null)
                {
                    gltf.Data = binChunk;
                }
                return LoadRaw(gltf.DecodeIndex(out string mimeType), mimeType);
            }
        }

        public static Unity3DTileIndex LoadFromGLTF(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                return LoadRaw(GLTFFile.FromJson(sr.ReadToEnd()).DecodeIndex(out string mimeType), mimeType);
            }
        }

        //http://netpbm.sourceforge.net/doc/ppm.html
        public static Unity3DTileIndex LoadFromPPM(Stream stream, bool compressed)
        {
            if (compressed)
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }

            using (var br = new BinaryReader(stream))
            {
                char readChar()
                {
                    int b = br.Read();
                    if (b < 0)
                    {
                        throw new Exception("unexpected EOF parsing PPM header");
                    }
                    return (char)b;
                }

                char ch = readChar();

                string readToken(bool eatWhitespace = true)
                {
                    var sb = new StringBuilder();
                    bool ignoring = false;
                    string tok = null;
                    for (int i = 0; i < 1000; i++)
                    {
                        if ((!ignoring && char.IsWhiteSpace(ch)) || (ignoring && (ch == '\r' || ch == '\n')))
                        {
                            tok = sb.ToString();
                            break;
                        }
                        else if (ch == '#')
                        {
                            ignoring = true;
                        }
                        else
                        {
                            sb.Append(ch);
                        }
                        ch = readChar();
                    }
                    for (int i = 0; eatWhitespace && char.IsWhiteSpace(ch) && i < 1000; i++)
                    {
                        ch = readChar();
                    }
                    return tok;
                }

                string f = readToken();
                if (f != "P6")
                {
                    throw new Exception("unexpected PPM magic: " + f);
                }

                int width = 0, height = 0, maxVal = 0;

                string w = readToken();
                if (w != null && !int.TryParse(w, out width))
                {
                    throw new Exception("error parsing PPM width: " + w);
                }

                string h = readToken();
                if (h != null && !int.TryParse(h, out height))
                {
                    throw new Exception("error parsing PPM height: " + h);
                }

                string m = readToken(eatWhitespace: false);
                if (m != null && !int.TryParse(m, out maxVal))
                {
                    throw new Exception("unexpected PPM max val: " + m);
                }
                
                if (maxVal <= 0 || maxVal > 65535)
                {
                    throw new Exception("max value must be in range 1-65535, got: " + maxVal);
                }

                var index = new Unity3DTileIndex(width, height);
                
                int bytesPerVal = maxVal < 256 ? 1 : 2;

                for (int r = 0; r < height; r++)
                {
                    for (int c = 0; c < width; c++)
                    {
                        for (int b = 0; b < 3; b++)
                        {
                            byte[] bytes = br.ReadBytes(bytesPerVal); //PPM data is in network byte order (big endian)
                            if (BitConverter.IsLittleEndian)
                            {
                                Array.Reverse(bytes);
                            }
                            if (bytes.Length < bytesPerVal)
                            {
                                throw new Exception($"unexpected EOF at PPM row={r} of {height}, col={c} of {width}");
                            }
                            index[b, r, c] = bytesPerVal == 2 ? BitConverter.ToUInt16(bytes, 0) : (ushort)bytes[0];
                        }
                    }
                }
                return index;
            }
        }

        public static Unity3DTileIndex LoadFromPNG(Stream stream)
        {
#if IMAGE_SHARP_PNG
            //requires extra dependency DLLs which bloat the webgl build
            using (var png = SixLabors.ImageSharp.Image.Load<Rgba64>(stream))
            var index = new Unity3DTileIndex(png.Width, png.Height);
            {
                for (int r = 0; r < png.Height; r++)
                {
                    for (int c = 0; c < png.Width; c++)
                    {
                        var pixel = png[c, r];
                        index[0, r, c] = pixel.R;
                        index[1, r, c] = pixel.G;
                        index[2, r, c] = pixel.B;
                    }
                }
            }
            return index;
#elif PNGCS_PNG
            var png = new PngReader(stream);
            png.SetUnpackedMode(true);
            var info = png.ImgInfo;
            if (info.Channels != 3)
            {
                throw new Exception("expected 3 channel PNG, got " + info.Channels);
            }
            var index = new Unity3DTileIndex(info.Cols, info.Rows);
            var buf = new int[3 * info.Cols];
            for (int r = 0; r < info.Rows; r++)
            {
                png.ReadRow(buf, r);
                for (int c = 0; c < info.Cols; c++)
                {
                    index[0, r, c] = (uint)buf[3 * c + 0];
                    index[1, r, c] = (uint)buf[3 * c + 1];
                    index[2, r, c] = (uint)buf[3 * c + 2];
                }
            }
            png.End();
            return index;
#else
            return null;
#endif
        }
    }

    public class GLTFFile
    {
        public const string JPG_MIME = "image/jpeg";
        public const string PNG_MIME = "image/png";
        public const string PPMZ_MIME = "image/x-portable-pixmap+gzip";
        public const string PPM_MIME = "image/x-portable-pixmap";
        public const string BIN_MIME = "application/octet-stream";

        public class GLTFBuffer
        {
            public int byteLength;
            public string uri;
        }
        
        public class GLTFBufferView
        {
            public int buffer;
            public int byteLength;
            public int byteOffset;
            public int? byteStride;
        }
        
        public class GLTFImage
        {
            public string uri;
            public string mimeType;
            public int? bufferView;
        }

        public List<GLTFBuffer> buffers = new List<GLTFBuffer>();
        public List<GLTFBufferView> bufferViews = new List<GLTFBufferView>();
        public List<GLTFImage> images = new List<GLTFImage>();

        [JsonIgnore]
        public byte[] Data;

        public GLTFFile() { }

        public static GLTFFile FromJson(string json)
        {
            var ignoreNulls = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
            var gltf = JsonConvert.DeserializeObject<GLTFFile>(json, ignoreNulls);
            if (gltf.buffers.Count > 0 && !string.IsNullOrEmpty(gltf.buffers[0].uri) &&
                gltf.buffers[0].uri.StartsWith("data:" + BIN_MIME))
            {
                gltf.Data = Base64Decode(gltf.buffers[0].uri, out string mimeType);
                gltf.buffers[0].uri = null;
            }
            return gltf;
        }

        public byte[] DecodeIndex(out string mimeType)
        {
            mimeType = null;
            int index = 1;
            if (index >= images.Count)
            {
                throw new Exception("no glTF image at index " + index);
            }
            var image = images[index];
            if (image.bufferView.HasValue && !string.IsNullOrEmpty(image.mimeType))
            {
                mimeType = image.mimeType;
                return GetDataSlice(image.bufferView.Value);
            }
            else if (image.uri.StartsWith("data:"))
            {
                return Base64Decode(image.uri, out mimeType);
            }
            else
            {
                throw new Exception("unhandled glTF image URI: " + Abbreviate(image.uri));
            }
        }

        private GLTFBufferView GetBufferView(int index, int extraOffset = 0, int minBytes = 0)
        {
            if (index >= bufferViews.Count)
            {
                throw new Exception("no glTF buffer view at index " + index);
            }
            var bufferView = bufferViews[index];
            if (bufferView.buffer >= buffers.Count)
            {
                throw new Exception("no glTF buffer at index " + bufferView.buffer);
            }
            if (!string.IsNullOrEmpty(buffers[bufferView.buffer].uri))
            {
                throw new Exception("glTF buffer uri not supported " + Abbreviate(buffers[bufferView.buffer].uri));
            }
            if (bufferView.buffer > 0)
            {
                throw new Exception("glTF buffer index not supported " + bufferView.buffer);
            }
            if (bufferView.byteLength < minBytes)
            {
                throw new Exception("glTF buffer view too small");
            }
            if (Data == null || Data.Length < extraOffset + bufferView.byteOffset + bufferView.byteLength)
            {
                throw new Exception("glTF buffer view exceeds available data");
            }
            if (bufferView.byteStride.HasValue && bufferView.byteStride > 1)
            {
                throw new Exception("glTF byte stride not supported: " + bufferView.byteStride.Value);
            }
            return bufferView;
        }

        private byte[] GetDataSlice(int index)
        {
            var bufferView = GetBufferView(index);
            byte[] slice = new byte[bufferView.byteLength];
            Array.Copy(Data, bufferView.byteOffset, slice, 0, bufferView.byteLength);
            return slice;
        }

        private static byte[] Base64Decode(string str, out string mimeType)
        {
            mimeType = null;
            foreach (var mt in new string[] { BIN_MIME, JPG_MIME, PNG_MIME, PPMZ_MIME, PPM_MIME })
            {
                string pfx = $"data:{mt};base64,";
                if (str.StartsWith(pfx))
                {
                    mimeType = mt;
                    str = str.Substring(pfx.Length);
                }
            }
            if (mimeType == null)
            {
                throw new Exception("unsupported format for glTF: " + Abbreviate(str));
            }
            return System.Convert.FromBase64String(str);
        }

        private static string Abbreviate(string str, int maxLen = 100)
        {
            return str.Length > maxLen ? (str.Substring(0, maxLen) + "...") : str;
        }
    }
}
