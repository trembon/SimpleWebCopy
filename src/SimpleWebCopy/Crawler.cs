using HtmlAgilityPack;
using SimpleWebCopy.Enums;
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
using System.Web;

namespace SimpleWebCopy
{
    public class Crawler : IDisposable
    {
        private string userAgent;
        private IEnumerable<string> extraLinks;

        private HttpClient httpClient;
        private CookieContainer cookieContainer;
        private HttpClientHandler httpClientHandler;

        private ConcurrentQueue<string> queue;

        private Regex regexCssUrls = new Regex("url\\(['\"]?(.*?)['\"]?\\)", RegexOptions.Compiled | RegexOptions.Multiline);
        private Regex regexUrlVariables = new Regex("\\+(.*?)\\+", RegexOptions.Compiled);

        public string Site { get; }

        public string Output { get; }

        public int Threads { get; }

        public SiteItemState State { get; }

        public event EventHandler<ThreadUpdatedEventArgs> ThreadUpdated;

        public event EventHandler<ThreadCompleteEventArgs> ThreadComplete;

        public event EventHandler CrawlStarted;

        public event EventHandler CrawlComplete;

        public Crawler(string site, string output, int threads, string userAgent, IEnumerable<string> links, Dictionary<string, string> cookies)
        {
            this.Site = UrlHelper.Standardize(site);
            this.Output = output;
            this.Threads = threads;

            this.userAgent = userAgent;
            this.extraLinks = links;

            queue = new ConcurrentQueue<string>();

            State = new SiteItemState(this.Site);
            State.ItemAdded += State_ItemAdded;

            cookieContainer = new CookieContainer();
            if (cookies != null)
            {
                Uri domain = new Uri(this.Site);
                foreach (string name in cookies.Keys)
                    cookieContainer.Add(new Cookie(name, cookies[name], "/", domain.DnsSafeHost));
            }

            httpClientHandler = new HttpClientHandler { CookieContainer = cookieContainer, AllowAutoRedirect = true };
            httpClient = new HttpClient(httpClientHandler);

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }

        private void State_ItemAdded(object sender, Events.ItemAddedEventArgs e)
        {
            queue.Enqueue(e.ItemURL);
        }

        public async Task Start()
        {
            CrawlStarted.Trigger(this, new EventArgs());

            State.AddLink(Site, null, null);

            foreach (string link in extraLinks)
                State.AddLink(link, null, null);

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < Threads; i++)
            {
                Task thread = Crawl(i);
                tasks.Add(thread);
            }

            await Task.WhenAll(tasks);
            
            CrawlComplete.Trigger(this, new EventArgs());
        }

        private void UpdateThread(int threadId, string itemUrl, string state)
        {
            ThreadUpdated.Trigger(this, new ThreadUpdatedEventArgs(threadId, itemUrl, state));
        }

        public async Task Crawl(int threadId)
        {
            while (true)
            {
                if (queue.TryDequeue(out string itemId))
                {
                    if (State.StartProcessing(itemId))
                    {
                        string itemUrl = State.GetURL(itemId);
                        string itemLocalUrl = State.GetLocalLink(itemId);

                        UpdateThread(threadId, itemUrl, "Starting");

                        if (!ShouldIndex(itemUrl))
                        {
                            State.SetResult(itemId, ItemStatus.Ignored);
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
                                            // check so the requested return 200 OK
                                            State.SetResult(itemId, ItemStatus.Error, $"Invalid status code in response (Was: {response.StatusCode})");
                                        }
                                        else if (!ShouldIndex(response.RequestMessage.RequestUri.ToString()))
                                        {
                                            // check if the request was redirected
                                            State.SetResult(itemId, ItemStatus.Ignored, $"Ignored, was redirected to '{response.RequestMessage.RequestUri}'");
                                        }
                                        else
                                        {
                                            string contentType = response.Content.Headers.ContentType.MediaType;
                                            if (contentType == "text/html")
                                            {
                                                html = await response.Content.ReadAsStringAsync();
                                            }
                                            else if (contentType.StartsWith("text/"))
                                            {
                                                content = await response.Content.ReadAsStringAsync();
                                            }
                                            else
                                            {
                                                string filePath = null;
                                                try
                                                {
                                                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                                                    {
                                                        filePath = GetFileOutput(itemLocalUrl);

                                                        using (Stream file = File.Create(filePath))
                                                            await stream.CopyToAsync(file);

                                                        State.SetResult(itemId, ItemStatus.Processed);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    State.SetResult(itemId, ItemStatus.Error, "Failed to download binary file.", ex);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                State.SetResult(itemId, ItemStatus.Error, "Failed to download content", ex);
                            }

                            HtmlDocument document = null;
                            if (html != null)
                            {
                                try
                                {
                                    UpdateThread(threadId, itemUrl, "Parsing");
                                    document = new HtmlDocument();
                                    document.LoadHtml(html);

                                    FindAndReplaceURLs(document, itemUrl, itemLocalUrl, "link", "href", "rel");
                                    FindAndReplaceURLs(document, itemUrl, itemLocalUrl, "script", "src");
                                    FindAndReplaceURLs(document, itemUrl, itemLocalUrl, "a", "href");
                                    FindAndReplaceURLs(document, itemUrl, itemLocalUrl, "img", "src");
                                    FindAndReplaceURLs(document, itemUrl, itemLocalUrl, "source", "srcset");
                                    FindAndReplaceURLs(document, itemUrl, itemLocalUrl, "*", "data-src"); // semi-standard way to lazy-load?
                                }
                                catch (Exception ex)
                                {
                                    document = null;
                                    State.SetResult(itemId, ItemStatus.Error, "Failed to parse html document.", ex);
                                }
                            }

                            if (document != null)
                            {
                                try
                                {
                                    UpdateThread(threadId, itemUrl, "Converting");
                                    content = document.DocumentNode.OuterHtml;
                                }
                                catch (Exception ex)
                                {
                                    State.SetResult(itemId, ItemStatus.Error, "Failed to convert html to content.", ex);
                                }
                            }

                            if (content != null)
                            {
                                try
                                {
                                    UpdateThread(threadId, itemUrl, "Parsing");
                                    content = regexCssUrls.Replace(content, new MatchEvaluator(m =>
                                        {
                                            string url = HttpUtility.HtmlDecode(m.Groups[1].Value).Replace("'", "");
                                            if (!string.IsNullOrWhiteSpace(url) && !regexUrlVariables.IsMatch(url) && Uri.IsWellFormedUriString(url, UriKind.Relative))
                                            {
                                                string id = State.AddLink(url, itemUrl, null);
                                                string localUrl = State.GetLocalLink(id);

                                                string relativeUrl = UrlHelper.CreateRelativeURL(itemLocalUrl, localUrl);
                                                if (url.Contains("#"))
                                                    relativeUrl += url.Substring(url.LastIndexOf('#'));

                                                return m.Value.Replace(m.Groups[1].Value, relativeUrl);
                                            }

                                            return m.Value;
                                        }));
                                }
                                catch (Exception ex)
                                {
                                    State.SetResult(itemId, ItemStatus.Error, "Failed to parse content.", ex);
                                }
                            }

                            if (content != null)
                            {
                                try
                                {
                                    UpdateThread(threadId, itemUrl, "Saving");

                                    string filePath = GetFileOutput(itemLocalUrl);
                                    await File.WriteAllTextAsync(filePath, content);

                                    State.SetResult(itemId, ItemStatus.Processed);
                                }
                                catch (Exception ex)
                                {
                                    State.SetResult(itemId, ItemStatus.Error, "Failed to store file on filesystem.", ex);
                                }
                            }
                        }
                    }
                }

                // if no more pages need scanning and queue is empty, throw complete event for the current thread
                if (!State.NeedsScanning() && queue.IsEmpty)
                {
                    ThreadComplete.Trigger(this, new ThreadCompleteEventArgs(threadId));
                    break;
                }

                // if not done and queue is empty, sleep to go easy on the CPU a bit
                if (queue.IsEmpty)
                    Thread.Sleep(1000);
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
            catch { }
        }

        private void FindAndReplaceURLs(HtmlDocument document, string sourceUrl, string itemLocalUrl, string element, string attribute, string extraAttribute = null)
        {
            HtmlNodeCollection elements = document.DocumentNode.SelectNodes($"//{element}[@{attribute}]");
            if (elements == null || elements.Count == 0)
                return;

            foreach (HtmlNode node in elements)
            {
                string href = node.GetAttributeValue(attribute, "");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    href = HttpUtility.HtmlDecode(href);

                    // if the links is only data behind a hastag, dont process it
                    if (href.StartsWith("#"))
                        continue;

                    string sourceElement = node.OriginalName;
                    if (extraAttribute != null)
                        sourceElement += $".{node.GetAttributeValue(extraAttribute, "")}";

                    string id = State.AddLink(href, sourceUrl, sourceElement);
                    string localUrl = State.GetLocalLink(id);

                    string relativeUrl = UrlHelper.CreateRelativeURL(itemLocalUrl, localUrl);
                    if (href.Contains("#"))
                        relativeUrl += href.Substring(href.LastIndexOf('#'));

                    node.SetAttributeValue(attribute, relativeUrl);
                }
            }
        }

        private string GetFileOutput(string itemLocalUrl)
        {
            string filePath = Path.Combine(Output, itemLocalUrl);

            string folder = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return filePath;
        }
    }
}
