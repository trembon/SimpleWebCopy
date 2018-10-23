using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleWebCopy
{
    public class SiteItem
    {
        public Guid ID { get; }

        public string FullURL { get; set; }

        public string LocalFilePath { get; set; }
        
        public bool IsScanned { get; set; }

        public bool IsScanning { get; set; }

        public SiteItem(string baseUrl, string url)
        {
            this.ID = Guid.NewGuid();
            this.FullURL = url;

            this.LocalFilePath = UrlHelper.CreateLocalFileURL(baseUrl, url);
        }
    }
}
