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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity3DTiles
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
        public static string GetBaseUri(string url, bool includeQuery = true)
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
        /// </summary>
        public static string JoinUrls(string absBase, string relPath)
        {
            absBase = absBase.Replace("\\", "/").TrimEnd('/') + "/";
            relPath = relPath.Replace("\\", "/").TrimStart('/');

            var baseUri = new Uri(absBase, UriKind.Absolute);
            var relUri = new Uri(relPath, UriKind.Relative);

            var builder = new UriBuilder(new Uri(baseUri, relUri));
   
            // This will allow us to access scheme and fragments of relPath
            var relUriAsAbs = new UriBuilder(baseUri.Scheme, baseUri.Host, baseUri.Port, relPath).Uri;

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

        public static string ReplaceDataProtocol(string url)
        {
            string dataProtocol = "data://";
            if (url.StartsWith(dataProtocol, StringComparison.OrdinalIgnoreCase))
            {
                url = Application.streamingAssetsPath + "/" + url.Substring(dataProtocol.Length);
            }
            return url;
        }
    }
}
