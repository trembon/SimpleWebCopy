using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleWebCopy
{
    public static class UrlHelper
    {
        private static int queryCounter = 1;
        private static object queryCounterLock = new object();

        private static string[] knownHtmlExtensions = new string[]
        {
            ".asp",
            ".aspx",
            ".cshtml",
            ".dhtml",
            ".dll",
            ".jsp",
            ".jhtml",
            ".jspx",
            ".php",
            ".rhtml",
            ".vbhtml",
            ".xhtml"
        };

        public static string Standardize(string url)
        {
            Uri uri = new Uri(url);

            // make the base url and path to lower case
            uri = new Uri($"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath.ToLowerInvariant()}{uri.Query}");

            // append a trailing slash to all urls if they dont contain an extension
            if (!Path.HasExtension(uri.LocalPath) && !uri.LocalPath.EndsWith("/"))
                uri = new Uri(uri, $"{uri.LocalPath}/");

            return uri.ToString();
        }

        public static string Standardize(string baseUrl, string url)
        {
            Uri baseUri = new Uri(baseUrl);
            Uri uri = null;

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                uri = new Uri(baseUri, url);
            else
                uri = new Uri(url);

            if (uri.Scheme != "http" && uri.Scheme != "https")
                uri = baseUri;
            
            return Standardize(uri.ToString());
        }

        public static string CreateLocalFileURL(string baseUrl, string url)
        {
            Uri baseUri = new Uri(baseUrl);
            Uri uri = new Uri(url);

            if (baseUri.Authority != uri.Authority)
                return url;

            url = uri.LocalPath.Substring(1);

            if (url.EndsWith("/"))
                url = url.Substring(0, url.Length - 1) + ".html";

            if (string.IsNullOrWhiteSpace(url))
                url = "index.html";

            if (knownHtmlExtensions.Contains(Path.GetExtension(url)))
                url = url.Replace(Path.GetExtension(url), ".html");

            if (!string.IsNullOrWhiteSpace(uri.Query))
            {
                lock (queryCounterLock)
                {
                    string extension = Path.GetExtension(url);
                    string path = url.Replace(extension, "");

                    url = $"{path}___{queryCounter++}{extension}";
                }
            }

            return url;
        }

        public static string CreateRelativeURL(string from, string to)
        {
            Uri baseUri = new Uri("file://folder/");

            Uri fromUri = new Uri(baseUri, from);
            Uri toUri = new Uri(baseUri, to);

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            return relativeUri.ToString();
        }
    }
}
