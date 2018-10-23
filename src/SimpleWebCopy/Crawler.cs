using HtmlAgilityPack;
using SimpleWebCopy.Events;
using SimpleWebCopy.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWebCopy
{
    public class Crawler : IDisposable
    {

        private HttpClient httpClient;
        private CookieContainer cookieContainer;
        private HttpClientHandler httpClientHandler;

        private ConcurrentQueue<string> queue;


        //private TaskCompletionSource<bool> tcs;

        private Regex regexCssUrls = new Regex("url\\(['\"]?(.*?)['\"]?\\)", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public string Site { get; }

        public SiteItemState State { get; }

        //public ConcurrentDictionary<string, string> Threads { get; }

        public event EventHandler<ThreadUpdatedEventArgs> CrawlUpdated;

        public Crawler(string site)
        {
            this.Site = UrlHelper.Standardize(site);

            queue = new ConcurrentQueue<string>();

            State = new SiteItemState(this.Site);
            State.ItemAdded += State_ItemAdded;

            cookieContainer = new CookieContainer();
            httpClientHandler = new HttpClientHandler { CookieContainer = cookieContainer, AllowAutoRedirect = true };
            httpClient = new HttpClient(httpClientHandler);

            //Threads = new ConcurrentDictionary<string, string>();
        }

        private void State_ItemAdded(object sender, Events.ItemAddedEventArgs e)
        {
            queue.Enqueue(e.ItemURL);
        }

        public async Task Start()
        {
            State.AddLink(Site);

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                Task thread = Crawl(i);
                tasks.Add(thread);
            }

            await Task.WhenAll(tasks);
        }

        //public Task StartThreads()
        //{
        //    //if (!State.NeedsScanning())
        //    //{
        //    //    tcs.SetResult(true);
        //    //    return Task.CompletedTask;
        //    //}

        //    for (int i = 0; i < 5; i++)
        //    {
        //        //if (queue.TryDequeue(out string itemUrl))
        //        //{
        //        //    if (Threads.TryAdd(itemUrl, "Starting"))
        //        //    {
        //                //CrawlUpdated.Trigger(this, new EventArgs());
        //                Crawl(i + 1);
        //            //}
        //        //}
        //    }

        //    return Task.CompletedTask;
        //    //return Task.Delay(1000).ContinueWith(previousTask => StartThreads());
        //}

        private void UpdateThread(int threadId, string itemUrl, string state)
        {
            //if(Threads.TryUpdate(itemUrl, state, null))
            //{
                CrawlUpdated.Trigger(this, new ThreadUpdatedEventArgs(threadId, itemUrl, state));
            //}
        }

        public async Task Crawl(int threadId)
        {
            while (true)
            {
                if (queue.TryDequeue(out string itemUrl))
                {
                    if (State.StartProcessing(itemUrl))
                    {
                        UpdateThread(threadId, itemUrl, "Starting");

                        string itemLocalUrl = State.GetLocalLink(itemUrl);

                        if (!ShouldIndex(itemUrl))
                        {
                            State.SetResult(itemUrl, null);
                        }
                        else
                        {
                            string html = null;
                            string content = null;

                            try
                            {
                                UpdateThread(threadId, itemUrl, "Downloading");
                                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, itemUrl))
                                {
                                    using (HttpResponseMessage response = await httpClient.SendAsync(request))
                                    {
                                        if (!response.IsSuccessStatusCode)
                                        {
                                            State.SetResult(itemUrl, response.StatusCode);
                                        }
                                        else if (!ShouldIndex(response.RequestMessage.RequestUri.ToString()))
                                        {
                                            State.SetResult(itemUrl, null);
                                        }
                                        else
                                        {
                                            string contentType = response.Content.Headers.ContentType.MediaType;
                                            if (contentType == "text/html")
                                                html = await response.Content.ReadAsStringAsync();
                                            else if (contentType.StartsWith("text/"))
                                                content = await response.Content.ReadAsStringAsync();
                                            else
                                            {
                                                string filePath = null;
                                                try
                                                {
                                                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                                                    {
                                                        filePath = Path.Combine("output", State.GetLocalLink(itemUrl));

                                                        string folder = Path.GetDirectoryName(filePath);
                                                        if (!Directory.Exists(folder))
                                                            Directory.CreateDirectory(folder);

                                                        using (Stream file = File.Create(filePath))
                                                            await stream.CopyToAsync(file);

                                                        State.SetResult(itemUrl, null);
                                                    }
                                                }
                                                catch
                                                {
                                                    State.SetResult(itemUrl, null);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                State.SetResult(itemUrl, null);
                            }

                            HtmlDocument document = null;
                            if (html != null)
                            {
                                try
                                {

                                    UpdateThread(threadId, itemUrl, "Parsing");
                                    document = new HtmlDocument();
                                    document.LoadHtml(html);

                                    FindAndReplaceURLs(document, itemLocalUrl, "a", "href");
                                    FindAndReplaceURLs(document, itemLocalUrl, "link", "href");
                                    FindAndReplaceURLs(document, itemLocalUrl, "script", "src");
                                    FindAndReplaceURLs(document, itemLocalUrl, "img", "src");
                                    FindAndReplaceURLs(document, itemLocalUrl, "source", "srcset");
                                }
                                catch (Exception ex)
                                {
                                    document = null;
                                    State.SetResult(itemUrl, null);
                                }
                            }

                            if (document != null)
                            {
                                try
                                {

                                    UpdateThread(threadId, itemUrl, "Converting");
                                    content = document.DocumentNode.OuterHtml;
                                }
                                catch
                                {
                                    State.SetResult(itemUrl, null);
                                }
                            }

                            if (content != null)
                            {
                                try
                                {

                                    UpdateThread(threadId, itemUrl, "Parsing");
                                    content = regexCssUrls.Replace(content, new MatchEvaluator(m =>
                                        {
                                            string url = m.Groups[1].Value;
                                            if (!string.IsNullOrWhiteSpace(url))
                                            {
                                                string fullUrl = State.AddLink(url);
                                                string localUrl = State.GetLocalLink(fullUrl);

                                                string relativeUrl = UrlHelper.CreateRelativeURL(itemLocalUrl, localUrl);
                                                return m.Value.Replace(url, relativeUrl);
                                            }

                                            return m.Value;
                                        }));
                                }
                                catch
                                {
                                    State.SetResult(itemUrl, null);
                                }
                            }

                            if (content != null)
                            {
                                string filePath = null;
                                try
                                {
                                    filePath = Path.Combine("output", itemLocalUrl);

                                    UpdateThread(threadId, itemUrl, "Saving");

                                    string folder = Path.GetDirectoryName(filePath);
                                    if (!Directory.Exists(folder))
                                        Directory.CreateDirectory(folder);

                                    await File.WriteAllTextAsync(filePath, content);
                                    State.SetResult(itemUrl, null);
                                }
                                catch
                                {
                                    State.SetResult(itemUrl, null);
                                }
                            }
                        }
                    }
                }

                if (!State.NeedsScanning() && queue.IsEmpty)
                    break;

                //UpdateThread(threadId, null, "Paused");
                //Thread.Sleep(1000);

            }
        }

        private bool ShouldIndex(string itemUrl)
        {
            return itemUrl.ToLowerInvariant().StartsWith(Site);
        }

        public void Dispose()
        {
            try
            {
                httpClient?.Dispose();
                httpClientHandler?.Dispose();
            }
            catch
            {

            }
        }

        private void FindAndReplaceURLs(HtmlDocument document, string itemLocalUrl, string element, string attribute)
        {
            HtmlNodeCollection elements = document.DocumentNode.SelectNodes($"//{element}[@{attribute}]");
            if (elements == null || elements.Count == 0)
                return;

            foreach (HtmlNode node in elements)
            {
                string href = node.GetAttributeValue(attribute, "");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    string fullUrl = State.AddLink(href);
                    string localUrl = State.GetLocalLink(fullUrl);

                    string relativeUrl = UrlHelper.CreateRelativeURL(itemLocalUrl, localUrl);
                    node.SetAttributeValue(attribute, relativeUrl);
                }
            }
        }
    }
}
