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

        /// <summary>
        /// Removes the last segment of a url (everything after and including the last /)
        /// but optionally retrains qurey (?) and fragments (#)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="includeQuery"></param>
        /// <returns></returns>
        public static string GetBaseUri(string url, bool includeQuery)
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


        public static string SetQuery(string url, string query)
        {
            var builder = new UriBuilder(new Uri(url));
            builder.Query = query;
            return builder.ToString();
        }

        /// <summary>
        /// Appends one url onto another
        /// </summary>
        /// <param name="url1"></param>
        /// <param name="url2"></param>
        /// <returns></returns>
        public static string JoinUrls(string first, string second, bool appendSlash = false)
        {
            if(appendSlash)
            {
                first = first.TrimEnd('/') + '/';
            }
            Uri f = new Uri(first, UriKind.RelativeOrAbsolute);
            Uri s = new Uri(second, UriKind.RelativeOrAbsolute);
   
            // Early out in the case of data uris
            if (f.Scheme == "data")
            {
                return f.ToString();
            }
            if (s.IsAbsoluteUri && s.Scheme == "data")
            {
                return s.ToString();
            }
            
            // Make this absolute (its okay we are done using its earlier elements
            // This will allow us to query scheme and fragments
            if (!s.IsAbsoluteUri)
            {
                s = new UriBuilder(f.Scheme, f.Host, f.Port, second).Uri;
            }

            // We use second string here because we don't want s which we may have made absolute
            Uri r;
            Uri.TryCreate(f, second, out r);
            UriBuilder builder = new UriBuilder(r);

            // Query
            if (f.Query != null && s.Query != null)
            {
                builder.Query = f.Query.TrimStart('?') + s.Query.Replace('?', '&');
            }
            else if (!string.IsNullOrEmpty(f.Query) && string.IsNullOrEmpty(s.Query))
            {
                builder.Query = f.Query.TrimStart('?');
            }
            else if (string.IsNullOrEmpty(f.Query) && !string.IsNullOrEmpty(s.Query))
            {
                builder.Query = s.Query.TrimStart('?');
            }
            // Fragment
            if (!string.IsNullOrEmpty(f.Fragment) && String.IsNullOrEmpty(s.Fragment))
            {
                builder.Fragment = f.Fragment.TrimStart('#');
            }
            else if (!string.IsNullOrEmpty(s.Fragment))
            {
                builder.Fragment = s.Fragment.TrimStart('#');
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
