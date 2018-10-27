using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleWebCopy
{
    public class ConsoleRender
    {
        private Crawler crawler;
        private object renderLock;

        private int statusRow;
        private Dictionary<int, int> threadRows;

        public ConsoleRender(Crawler crawler)
        {
            this.crawler = crawler;
            this.renderLock = new object();

            this.threadRows = new Dictionary<int, int>();

            this.crawler.CrawlStarted += Crawler_CrawlStarted;
            this.crawler.ThreadUpdated += Crawler_ThreadUpdated;
            this.crawler.ThreadComplete += Crawler_ThreadComplete;
            this.crawler.State.ItemAdded += State_ItemAdded;
        }

        private void Crawler_ThreadUpdated(object sender, Events.ThreadUpdatedEventArgs e)
        {
            lock (renderLock)
            {
                UpdateThread(e.Thread, e.Item, e.Status);
            }
        }

        private void Crawler_ThreadComplete(object sender, Events.ThreadCompleteEventArgs e)
        {
            lock (renderLock)
            {
                UpdateStatus();
                UpdateThread(e.Thread, null, "Complete");
            }
        }

        private void State_ItemAdded(object sender, Events.ItemAddedEventArgs e)
        {
            lock (renderLock)
            {
                UpdateStatus();
            }
        }

        private void Crawler_CrawlStarted(object sender, EventArgs e)
        {
            lock (renderLock)
            {
                Console.WriteLine($"Currently scanning: {crawler.Site}, Output: {crawler.Output}");

                statusRow = Console.CursorTop;
                Console.WriteLine($"Processed status: {crawler.State.ItemProcessedCount}/{crawler.State.ItemCount}");

                Console.WriteLine();

                for (int i = 0; i < crawler.Threads; i++)
                {
                    threadRows.Add(i, Console.CursorTop);
                    Console.WriteLine($"Thread {i + 1} - Paused");
                    Console.WriteLine($"---");
                    Console.WriteLine();
                }
            }
        }

        private void UpdateStatus()
        {
            Console.SetCursorPosition(18, statusRow);
            Console.Write($"{crawler.State.ItemProcessedCount}/{crawler.State.ItemCount}");
        }

        private void UpdateThread(int thread, string item, string status)
        {
            Console.SetCursorPosition(11, threadRows[thread]);
            Console.Write($"{status}      ");
            
            if (item == null)
                item = "---";

            Console.SetCursorPosition(0, threadRows[thread] + 1);
            Console.Write(item);
            Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));

            Console.SetCursorPosition(0, threadRows[thread] + 2);
            Console.Write(new string(' ', Console.WindowWidth));
        }
    }
}
