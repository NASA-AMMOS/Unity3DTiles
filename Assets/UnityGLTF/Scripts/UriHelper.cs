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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using UnityEngine;

namespace UnityGLTF
{
    public class UriHelper
    {
        public static string RemoveQuery(string url)
        {
            int i = url.IndexOf('?');
            if(i >= 0)
            {
                return url.Substring(0, i);
            }
            return url;
        }

        public static string SetQuery(string url, string query)
        {
            var builder = new UriBuilder(new Uri(url));
            builder.Query = query;
            return builder.ToString();
        }

        /// <summary>
        /// Removes the last segment of a url (last path component including the /)
        /// but retains query (?) and fragments (#) by default
        /// </summary>
        public static string GetBaseUri(string url, bool includeQuery = true, string[] excludeQueryParams = null)
        {
            Uri uri = new Uri(url);
            string noLastSegment = "";
            for (int i = 0; i < uri.Segments.Length - 1; i++)
            {
                noLastSegment += uri.Segments[i];
            }
            noLastSegment.TrimEnd('/');
            UriBuilder builder = new UriBuilder(uri);
            builder.Path = noLastSegment;
            if (!includeQuery)
            {
                builder.Query = null;
                builder.Fragment = null;
            }
            else if (excludeQueryParams != null && excludeQueryParams.Length > 0)
            {
                var queryParams = HttpUtility.ParseQueryString(builder.Query);
                foreach (var name in excludeQueryParams)
                {
                    queryParams.Remove(name);
                }
                builder.Query = string.Join("&",
                                            queryParams.AllKeys
                                            .Select(name => name + "=" + HttpUtility.UrlEncode(queryParams[name]))
                                            .ToArray());
            }
            return builder.Uri.ToString();
        }

        public static string GetLastPathSegment(string url)
        {
            Uri uri = new Uri(url);
            return uri.Segments[uri.Segments.Length - 1];
        }

        /// <summary>
        /// Appends a relative URL onto an absolute base url, combining query parameters of both.
        /// If the relative URL has a fragment, it is used, otherwise if the base URL has a fragment, it is used.
        /// The base URL must be absolute.
        /// All of the base URI segments will be used including the last one even if it doesn't end with a slash.
        /// Any "." or ".." path segments in the combined path will be elided appropriately.
        /// Tolerant to backslashes.
        /// </summary>
        public static string JoinUrls(string baseUrl, string relPath)
        {
            //these will throw if the provided strings do not parse as the required kind of URI
            var baseUri = new Uri(baseUrl.Replace("\\", "/"), UriKind.Absolute);
            var relUri = new Uri(relPath.Replace("\\", "/"), UriKind.Relative);

            //force the path part of baseUri to end with a slash
            //otherwise the last path segment of baseUri will get dropped
            //if that's what you wanted to happen then call GetBaseUri() before JoinUrls()
            var baseSeg = baseUri.Segments;
            if (baseSeg != null && baseSeg.Length > 0 && !baseSeg[baseSeg.Length - 1].EndsWith("/"))
            {
                var tmpBuilder = new UriBuilder(baseUri);
                tmpBuilder.Path += "/";
                baseUri = tmpBuilder.Uri;
            }
                
            // this will allow access scheme and fragments of relPath
            var relUriAsAbs = new Uri("dummy://host/" + relPath, UriKind.Absolute);

            var builder = new UriBuilder(new Uri(baseUri, relUri));
   
            if (!string.IsNullOrEmpty(baseUri.Query) && !string.IsNullOrEmpty(relUriAsAbs.Query))
            {
                builder.Query = baseUri.Query.TrimStart('?') + "&" + relUriAsAbs.Query.TrimStart('?');
            }
            else if (!string.IsNullOrEmpty(baseUri.Query))
            {
                builder.Query = baseUri.Query.TrimStart('?');
            }
            else if (!string.IsNullOrEmpty(relUriAsAbs.Query))
            {
                builder.Query = relUriAsAbs.Query.TrimStart('?');
            }

            if (!string.IsNullOrEmpty(relUriAsAbs.Fragment))
            {
                builder.Fragment = relUriAsAbs.Fragment.TrimStart('#');
            }
            else if (!string.IsNullOrEmpty(baseUri.Fragment))
            {
                builder.Fragment = baseUri.Fragment.TrimStart('#');
            }

            return builder.ToString();
        }

        /// <summary>
        /// Prepends any query params from baseUrl onto url.
        /// If url has a fragment, it is kept, otherwise if baseUrl has a fragment, it is appended.
        /// </summary>
        public static string JoinQuery(string baseUrl, string url)
        {
            var baseUri = new Uri(baseUrl.Replace("\\", "/"));
            var uri = new Uri(url.Replace("\\", "/"));

            var builder = new UriBuilder(uri);
   
            if (!string.IsNullOrEmpty(baseUri.Query))
            {
                if (!string.IsNullOrEmpty(uri.Query))
                {
                    builder.Query = baseUri.Query.TrimStart('?') + "&" + uri.Query.TrimStart('?');
                }
                else
                {
                    builder.Query = baseUri.Query.TrimStart('?');
                }
            }

            if (string.IsNullOrEmpty(uri.Fragment) && !string.IsNullOrEmpty(baseUri.Fragment))
            {
                builder.Fragment = baseUri.Fragment.TrimStart('#');
            }

            return builder.ToString();
        }

        public static string ReplaceDataProtocol(string url)
        {
            string dataProtocol = "data://";
            if (url.StartsWith(dataProtocol, StringComparison.OrdinalIgnoreCase))
            {
                url = Application.streamingAssetsPath + "/" + url.Substring(dataProtocol.Length);
#if UNITY_EDITOR
                if (!IsAbsolute(url))
                {
                    url = "file://" + url;
                }
#endif
            }
            return url;
        }

        public static bool IsAbsolute(string uri)
        {
            return !string.IsNullOrEmpty(uri) && uri.Contains("://");
        }
    }
}
