using SimpleWebCopy.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleWebCopy
{
    public class SiteItem
    {
        public string FullURL { get; set; }

        public string LocalFilePath { get; set; }

        public string Source { get; set; }

        public ItemStatus Status { get; set; }

        public string StatusMessage { get; set; }

        public Exception StatusException { get; set; }

        public SiteItem(string baseUrl, string url, string source)
        {
            this.FullURL = url;
            this.Source = source;
            this.Status = ItemStatus.NotProcessed;
            this.LocalFilePath = UrlHelper.CreateLocalFileURL(baseUrl, url);
        }
    }
}
