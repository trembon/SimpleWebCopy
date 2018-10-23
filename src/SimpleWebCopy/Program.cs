using System;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebCopy
{
    class Program
    {
        private static Crawler crawler;

        static void Main(string[] args)
        {
            Console.CursorVisible = false;

            crawler = new Crawler(args[0]);
            crawler.State.ItemAdded += State_ItemAdded;
            crawler.ThreadUpdated += Crawler_ThreadUpdated;
            crawler.ThreadComplete += Crawler_ThreadComplete;

            PreRender(5);

            crawler.Start().Wait();

            Console.ReadLine();

            // TODO: test library
            //string standardize1 = UrlHelper.Standardize("HTTP://Local.host/", "/Templates/Something/Handlers/IR.ashx?T=CS&P=/globalassets/media/images/push-images/125998-diakonia-ammattikorkeakoulu-push-285x236.jpg&PI=24970&PL=en&R=47677&W=175&H=145");
            //string standardize2 = UrlHelper.Standardize("http://local.host/", "http://local.host/Templates/Something/Handlers/IR.ashx?T=CS&P=/globalassets/media/images/push-images/125998-diakonia-ammattikorkeakoulu-push-285x236.jpg&PI=24970&PL=en&R=47677&W=175&H=145");
            //string standardize3 = UrlHelper.Standardize("http://local.host/", "http://test.local.host/akjsdhkajdhsaa/lkjasdklas.jpg");
            //string standardize4 = UrlHelper.Standardize("http://local.host/", "http://local.host");
            //string standardize5 = UrlHelper.Standardize("http://local.host/", "/something");
            //string standardize6 = UrlHelper.Standardize("http://local.host/", "#");
            //string standardize7 = UrlHelper.Standardize("http://local.host/", "tel: +12345667");
            //string standardize8 = UrlHelper.Standardize("htTp://local.host/", "mailto: demo@local.host");
            //string localUrl1 = UrlHelper.CreateLocalFileURL("http://local.host/", "http://local.host/Templates/Something/Handlers/IR.ashx?T=CS&P=/globalassets/media/images/push-images/125998-diakonia-ammattikorkeakoulu-push-285x236.jpg&PI=24970&PL=en&R=47677&W=175&H=145");
            //string localUrl2 = UrlHelper.CreateLocalFileURL("http://local.host:8080/", "http://local.host/Templates/Something/Handlers/IR.ashx?T=CS&P=/globalassets/media/images/push-images/125998-diakonia-ammattikorkeakoulu-push-285x236.jpg&PI=24970&PL=en&R=47677&W=175&H=145");
            //string localUrl3 = UrlHelper.CreateLocalFileURL("http://local.host/", "http://local.host/");
            //string localUrl4 = UrlHelper.CreateLocalFileURL("http://local.host/", "http://local.host/en/images");
            //string localUrl5 = UrlHelper.CreateLocalFileURL("http://local.host/", "http://local.host/en/images/index.aspx");
            //string relativeUrl1 = UrlHelper.CreateRelativeURL("hello.html", "world.html");
            //string relativeUrl2 = UrlHelper.CreateRelativeURL("foo/bar/hello.html", "world.html");
            //string relativeUrl3 = UrlHelper.CreateRelativeURL("foo/hello.html", "foo/baz/world.html");
            //string relativeUrl4 = UrlHelper.CreateRelativeURL("foo/baz/hello.html", "too/real/with/foo/bar/world.html");
            //string relativeUrl5 = UrlHelper.CreateRelativeURL("foo/baz/hello.html", "and//bugg/hello/world.html");
            //string relativeUrl6 = UrlHelper.CreateRelativeURL("hello.html", "foo/baz/world.html");
            //Console.ReadLine();
        }

        private static void Crawler_ThreadComplete(object sender, Events.ThreadCompleteEventArgs e)
        {
            Task.Run(() =>
            {
                lock (renderLock)
                {
                    Console.SetCursorPosition(18, 1);
                    Console.Write($"{crawler.State.ItemProcessedCount}/{crawler.State.ItemCount}");
                    
                    int lineStart = 3 + (e.Thread * 3);
                    Console.SetCursorPosition(11, lineStart);
                    Console.Write($"Complete      ");

                    Console.SetCursorPosition(0, lineStart + 1);
                    Console.Write("---");
                    Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));

                    Console.SetCursorPosition(0, lineStart + 2);
                    Console.Write(new string(' ', Console.WindowWidth));
                }
            });
        }

        private static object renderLock = new object();

        private static void Crawler_ThreadUpdated(object sender, Events.ThreadUpdatedEventArgs e)
        {
            Task.Run(() =>
            {
                lock (renderLock)
                {
                    int lineStart = 3 + (e.Thread * 3);
                    Console.SetCursorPosition(11, lineStart);
                    Console.Write($"{e.Status}      ");

                    string item = e.Item;
                    if (item == null)
                        item = "---";

                    Console.SetCursorPosition(0, lineStart + 1);
                    Console.Write(item);
                    Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));
                    
                    Console.SetCursorPosition(0, lineStart + 2);
                    Console.Write(new string(' ', Console.WindowWidth));
                }
            });
        }

        private static void State_ItemAdded(object sender, Events.ItemAddedEventArgs e)
        {
            Task.Run(() =>
            {
                lock (renderLock)
                {
                    Console.SetCursorPosition(18, 1);
                    Console.Write($"{crawler.State.ItemProcessedCount}/{crawler.State.ItemCount}");
                }
            });
        }

        private static void PreRender(int threads)
        {
            Console.WriteLine($"Currently scanning: {crawler.Site}");
            Console.WriteLine($"Processed status: {crawler.State.ItemProcessedCount}/{crawler.State.ItemCount}");
            Console.WriteLine();

            for(int i = 0; i < threads; i++)
            {
                Console.WriteLine($"Thread {i + 1} - Paused");
                Console.WriteLine($"---");
                Console.WriteLine();
            }
        }
    }
}
