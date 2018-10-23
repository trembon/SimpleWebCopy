using SimpleWebCopy.Events;
using SimpleWebCopy.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SimpleWebCopy
{
    public class SiteItemState
    {
        private string baseUrl;

        private ConcurrentDictionary<string, SiteItem> state;
        private ConcurrentDictionary<string, object> stateLocks;

        public int ItemCount
        {
            get { return state.Count; }
        }

        public int ItemProcessedCount
        {
            get { return state.Count(kvp => kvp.Value.IsScanned); }
        }

        public event EventHandler<ItemAddedEventArgs> ItemAdded;

        public SiteItemState(string baseUrl)
        {
            this.baseUrl = baseUrl;

            state = new ConcurrentDictionary<string, SiteItem>();
            stateLocks = new ConcurrentDictionary<string, object>();
        }

        public string AddLink(string url)
        {
            url = UrlHelper.Standardize(baseUrl, url);

            SiteItem item = state.GetOrAdd(url, new SiteItem(baseUrl, url));
            ItemAdded.Trigger(this, new ItemAddedEventArgs(item.FullURL));

            return item.FullURL;
        }

        public string GetLocalLink(string url)
        {
            if (state.TryGetValue(url, out SiteItem item))
                return item.LocalFilePath;

            return string.Empty;
        }

        public bool NeedsScanning()
        {
            return state.Values.Any(v => !v.IsScanned);
        }

        public void SetResult(string url, HttpStatusCode? statusCode)
        {
            object lockObject = stateLocks.GetOrAdd(url, new object());
            lock (lockObject)
            {
                if (state.TryGetValue(url, out SiteItem item))
                {
                    item.IsScanned = true;
                    // TODO: set result
                }
            }
        }

        public bool StartProcessing(string url)
        {
            object lockObject = stateLocks.GetOrAdd(url, new object());
            lock (lockObject)
            {
                if(state.TryGetValue(url, out SiteItem item))
                {
                    if(!item.IsScanned && !item.IsScanning)
                    {
                        item.IsScanning = true;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
