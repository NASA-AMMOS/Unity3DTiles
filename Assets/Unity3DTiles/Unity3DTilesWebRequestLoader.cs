using System;
using System.Collections;
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
    public class Unity3DTilesWebRequestLoader : AbstractWebRequestLoader
    {
        public Unity3DTilesWebRequestLoader(string rootURI) : base(rootURI) { }

        protected override AbstractWebRequestLoader GenerateNewWebRequestLoader(string rootUri)
        {
            return new Unity3DTilesWebRequestLoader(rootUri);
        }

        public override IEnumerator Send(string rootUri, string httpRequestPath, Action<string, string> onDownloadString = null, Action<byte[], string> onDownloadBytes = null)
        {
            if(onDownloadBytes == null && onDownloadString == null ||
                onDownloadBytes != null && onDownloadString != null)
            {
                throw new Exception("Send must use either string or byte[] data type");
            }

            string uri = null;
            if (!string.IsNullOrEmpty(rootUri) && !string.IsNullOrEmpty(httpRequestPath))
            {
                uri = UrlUtils.JoinUrls(rootUri, httpRequestPath);
            }
            else if (!string.IsNullOrEmpty(rootUri))
            {
                uri = rootUri;
            }
            else if (!string.IsNullOrEmpty(httpRequestPath))
            {
                uri = httpRequestPath;
            }
            else
            {
                throw new Exception("root URI and http request path both empty");
            }

            UnityWebRequest www = new UnityWebRequest(uri, "GET", new DownloadHandlerBuffer(), null);

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
            bool isError = (www.isNetworkError || www.isHttpError);
            onDownloadString?.Invoke(www.downloadHandler.text, isError ? www.error : null);
            onDownloadBytes?.Invoke(www.downloadHandler.data, isError ? www.error : null);
        }
    }
}
