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

        public override IEnumerator Send(string rootUri, string httpRequestPath,
                                         Action<string, string> onDownloadString = null,
                                         Action<byte[], string> onDownloadBytes = null)
        {
            if(onDownloadBytes == null && onDownloadString == null ||
                onDownloadBytes != null && onDownloadString != null)
            {
                throw new Exception("Send must use either string or byte[] data type");
            }

            //vona 5/25/21 wrap in using statement
            using (UnityWebRequest www = new UnityWebRequest(Path.Combine(rootUri, httpRequestPath), "GET",
                                                             new DownloadHandlerBuffer(), null)) {
                www.timeout = 5000;
#if UNITY_2017_2_OR_NEWER
                yield return www.SendWebRequest();
#else
                yield return www.Send();
#endif
                string error = null;
                if ((int)(www.responseCode) >= 400) //www.error is *not* set if this is all that went wrong
                {
                    error = "HTTP " + www.responseCode;
                }
                else if (!string.IsNullOrEmpty(www.error))
                {
                    error = www.error;
                }
                else if (www.isNetworkError)
                {
                    error = "network error";
                }
                else if (www.isHttpError)
                {
                    error = "HTTP error";
                }
                else if (www.downloadedBytes > int.MaxValue)
                {
                    error = "downloaded " + www.downloadedBytes + " bytes > " + int.MaxValue;
                }
                onDownloadString?.Invoke(www.downloadHandler.text, error);
                onDownloadBytes?.Invoke(www.downloadHandler.data, error);
            }
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

        public abstract IEnumerator Send(string rootUri, string httpRequestPath,
                                         Action<string, string> onDownloadString = null, Action<byte[],
                                         string> onDownloadBytes = null);

        public AbstractWebRequestLoader(string rootURI) : base()
		{
			_rootURI = rootURI;
		}

        //vona 5/25/21
//        public IEnumerator LoadStream(string filePath)
        public virtual IEnumerator LoadStream(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            Action<byte[], string> onDownload = (data, error) =>
            {
                if (!string.IsNullOrEmpty(error) || data == null || data.Length == 0)
                {
                    LoadedStream = new MemoryStream(new byte[] { }, 0, 0, true, true);
                }
                else
                {
                    LoadedStream = new MemoryStream(data, 0, data.Length, true, true);
                }
            };

            //yield return Send(_rootURI, filePath, onDownloadBytes: onDownload);
            var enumerator = Send(_rootURI, filePath, onDownloadBytes: onDownload); 
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
