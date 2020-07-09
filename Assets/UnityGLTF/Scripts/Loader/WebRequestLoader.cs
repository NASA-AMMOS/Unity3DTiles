using System;
using System.Collections;
using System.IO;
using GLTF;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Net;
using UnityEngine.Networking;

#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

namespace UnityGLTF.Loader
{
    public class UnityWebRequestLoader : AbstractWebRequestLoader
    {
        public UnityWebRequestLoader(string rootURI) : base(rootURI) { }

        protected override AbstractWebRequestLoader GenerateNewWebRequestLoader(string rootUri)
        {
            return new UnityWebRequestLoader(rootUri);
        }

        public override IEnumerator Send(string rootUri, string httpRequestPath, Action<string, string> onDownloadString = null, Action<byte[], string> onDownloadBytes = null)
        {
            if(onDownloadBytes == null && onDownloadString == null ||
                onDownloadBytes != null && onDownloadString != null)
            {
                throw new Exception("Send must use either string or byte[] data type");
            }

            UnityWebRequest www = new UnityWebRequest(Path.Combine(rootUri, httpRequestPath), "GET",
                new DownloadHandlerBuffer(), null);
            www.timeout = 5000;
#if UNITY_2017_2_OR_NEWER
            yield return www.SendWebRequest();
#else
            yield return www.Send();
#endif
            if ((int)www.responseCode >= 400)
            {
                Debug.LogErrorFormat("{0} - {1}", www.responseCode, www.url);
                throw new Exception("Response code invalid");
            }
            if (www.downloadedBytes > int.MaxValue)
            {
                throw new Exception("Stream is larger than can be copied into byte array");
            }
            bool isError = (www.isNetworkError || www.isHttpError);
            onDownloadString?.Invoke(www.downloadHandler.text, isError ? www.error : null);
            onDownloadBytes?.Invoke(www.downloadHandler.data, isError ? www.error : null);
        }
    }

    public abstract class AbstractWebRequestLoader : ILoader
	{
		protected string _rootURI;
        public bool CreateDownloadHandlerBuffer = true;

        public Stream LoadedStream { get; protected set; }

        // This property can be set to a different subclass of AbstractWebRequestLoader to change default loader
        public static AbstractWebRequestLoader LoaderPrototype = new UnityWebRequestLoader("");
        protected abstract AbstractWebRequestLoader GenerateNewWebRequestLoader(string rootUri);

        public static AbstractWebRequestLoader CreateDefaultRequestLoader(string rootUri)
        {
            return LoaderPrototype.GenerateNewWebRequestLoader(rootUri);
        }      

        public abstract IEnumerator Send(string rootUri, string httpRequestPath, Action<string, string> onDownloadString = null, Action<byte[], string> onDownloadBytes = null);

        public AbstractWebRequestLoader(string rootURI) : base()
		{
			_rootURI = rootURI;
		}

        public IEnumerator LoadStream(string gltfFilePath)
        {
            if (gltfFilePath == null)
            {
                throw new ArgumentNullException("gltfFilePath");
            }

            Action<byte[], string> onDownload = (data, error) =>
            {
                if (error != null || data.Length == 0)
                {
                    LoadedStream = new MemoryStream(new byte[] { }, 0, 0, true, true);
                }
                else
                {
                    LoadedStream = new MemoryStream(data, 0, data.Length, true, true);
                }
            };

            //yield return Send(_rootURI, gltfFilePath, onDownloadBytes: onDownload);
            var enumerator = Send(_rootURI, gltfFilePath, onDownloadBytes: onDownload); 
            while (true)
            {
                try
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                yield return enumerator.Current;
            }
        }
	}
}
