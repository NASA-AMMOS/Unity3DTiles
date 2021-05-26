#define POOL_DOWNLOAD_HANDLERS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityGLTF.Loader;

#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

namespace Unity3DTiles
{
    public class ObjectPool<T> where T : class, new()
    {
        List<WeakReference<T>> pool = new List<WeakReference<T>>();

        public T Acquire()
        {
            T obj = null;
            foreach (var wr in pool)
            {
                if (wr.TryGetTarget(out obj))
                {
                    wr.SetTarget(null);
                    break;
                }
            }
            if (obj == null)
            {
                obj = new T();
            }
            if (obj is PooledObject<T>)
            {
                (obj as PooledObject<T>).Reinit(this);
            }
            return obj;
        }

        public void Return(T obj)
        {
            foreach (var wr in pool)
            {
                if (!wr.TryGetTarget(out T _))
                {
                    wr.SetTarget(obj);
                    return;
                }
            }
            pool.Add(new WeakReference<T>(obj));
        }

        public int PoolSize()
        {
            return pool.Count;
        }
    }

    public interface PooledObject<T> where T : class, new()
    {
        void Reinit(ObjectPool<T> pool);
    }

    public class PooledMemoryStream : MemoryStream, PooledObject<PooledMemoryStream>
    {
        private ObjectPool<PooledMemoryStream> pool;
        private bool returned;

        public PooledMemoryStream() : base(32768)
        {
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (pool != null)
                {
                    if (!returned)
                    {
                        returned = true;
                        pool.Return(this);
                        //Debug.Log("memory stream pool size " + pool.PoolSize() + ", returned stream size " + Length);
                    }
                }
                else
                {
                    base.Dispose(true);
                }
            }
            else
            {
                base.Dispose(false);
            }
        }

        public void Reinit(ObjectPool<PooledMemoryStream> pool)
        {
            this.pool = pool;
            returned = false;
            Position = 0;
            SetLength(0);
        }
    }

    public class PooledBuffer
    {
        public byte[] Buffer = new byte[32768];
    }

    public class DownloadHandlerPooled : DownloadHandlerScript
    {
        public MemoryStream Stream { get; private set; }

        public DownloadHandlerPooled(MemoryStream stream, byte[] buf) : base(buf)
        {
            Stream = stream;
        }

        protected override void ReceiveContentLengthHeader(ulong contentLength)
        {
            ulong cap = (ulong)(Stream.Capacity);
            if (cap < contentLength)
            {
                cap = 32768 * (ulong)Math.Ceiling(contentLength / 32768.0);
                if (cap > int.MaxValue)
                {
                    throw new OverflowException();
                }
            }
            if (Stream.Capacity < (int)cap)
            {
                Stream.Capacity = (int)cap;
            }
        }

        protected override bool ReceiveData(byte[] data, int len)
        {
            Stream.Write(data, 0, len);
            return true;
        }

        protected override void CompleteContent()
        {
            Stream.Position = 0;
        }

        protected override byte[] GetData()
        {
            throw new NotImplementedException();
        }

        protected override string GetText()
        {
            throw new NotImplementedException();
        }

        protected override float GetProgress()
        {
            throw new NotImplementedException();
        }
    }

    public class Unity3DTilesWebRequestLoader : AbstractWebRequestLoader
    {
#if POOL_DOWNLOAD_HANDLERS
        private static ObjectPool<PooledMemoryStream> streamPool = new ObjectPool<PooledMemoryStream>();
        private static ObjectPool<PooledBuffer> bufferPool = new ObjectPool<PooledBuffer>();
#endif

        public Unity3DTilesWebRequestLoader(string rootURI) : base(rootURI) { }

        protected override AbstractWebRequestLoader GenerateNewWebRequestLoader(string rootUri)
        {
            return new Unity3DTilesWebRequestLoader(rootUri);
        }

        public override IEnumerator Send(string rootUri, string httpRequestPath,
                                         Action<string, string> onDownloadString = null, Action<byte[],
                                         string> onDownloadBytes = null)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator LoadStream(string filePath)
        {
            string uri = null;
            if (!string.IsNullOrEmpty(_rootURI) && !string.IsNullOrEmpty(filePath))
            {
                uri = UrlUtils.JoinUrls(_rootURI, filePath);
            }
            else if (!string.IsNullOrEmpty(_rootURI))
            {
                uri = _rootURI;
            }
            else if (!string.IsNullOrEmpty(filePath))
            {
                uri = filePath;
            }
            else
            {
                throw new Exception("root URI and http request path both empty");
            }

#if POOL_DOWNLOAD_HANDLERS
            var downloadBuffer = bufferPool.Acquire();
            var downloadHandler = new DownloadHandlerPooled(streamPool.Acquire(), downloadBuffer.Buffer);
#else
            var downloadHandler = new DownloadHandlerBuffer();
#endif
            using (UnityWebRequest www = new UnityWebRequest(uri, "GET", downloadHandler, null)) {

                www.timeout = 5000;
                
#if UNITY_2017_2_OR_NEWER
                yield return www.SendWebRequest();
#else
                yield return www.Send();
#endif
                if ((int)www.responseCode >= 400)
                {
                    throw new Exception(string.Format("{0} - {1}", www.responseCode, www.url));
                }
                if (www.downloadedBytes > int.MaxValue)
                {
                    throw new Exception("Stream is larger than can be copied into byte array");
                }
                if (www.isNetworkError || www.isHttpError)
                {
                    LoadedStream = new MemoryStream(new byte[] { }, 0, 0, true, true);
                }
                else
                {
#if POOL_DOWNLOAD_HANDLERS
                    LoadedStream = ((DownloadHandlerPooled)downloadHandler).Stream;
#else
                    byte[] data = downloadHandler.data;
                    LoadedStream = new MemoryStream(data, 0, data.Length, true, true);
#endif
                }
            }
#if POOL_DOWNLOAD_HANDLERS
            bufferPool.Return(downloadBuffer);
            //Debug.Log("buffer pool size " + bufferPool.PoolSize());
#endif
        }
    }
}
