using SimpleWebCopy.Enums;
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

        public int ItemCount => state.Count;

        public IEnumerable<SiteItem> Items => state.Values;

        public int ItemProcessedCount
        {
            get { return state.Count(kvp => kvp.Value.Status != ItemStatus.NotProcessed && kvp.Value.Status != ItemStatus.IsProcessing); }
        }

        public event EventHandler<ItemAddedEventArgs> ItemAdded;

        public SiteItemState(string baseUrl)
        {
            this.baseUrl = baseUrl;

            state = new ConcurrentDictionary<string, SiteItem>();
            stateLocks = new ConcurrentDictionary<string, object>();
        }

        public string AddLink(string url, string source)
        {
            url = UrlHelper.Standardize(baseUrl, url);

            SiteItem item = state.GetOrAdd(url, new SiteItem(baseUrl, url, source));
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
            return ItemCount != ItemProcessedCount;
        }

        public void SetResult(string url, ItemStatus status, string message = null, Exception exception = null)
        {
            object lockObject = stateLocks.GetOrAdd(url, new object());
            lock (lockObject)
            {
                if (state.TryGetValue(url, out SiteItem item))
                {
                    item.Status = status;
                    item.StatusMessage = message;
                    item.StatusException = exception;
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
                    if(item.Status == ItemStatus.NotProcessed)
                    {
                        item.Status = ItemStatus.IsProcessing;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
