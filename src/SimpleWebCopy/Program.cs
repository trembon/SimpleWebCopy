using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebCopy
{
    class Program
    {
        private static Crawler crawler;
        private static object renderLock = new object();
        
        private static int cursorStart = 0;

        static void Main(string[] args)
        {
            Console.CursorVisible = false;

            CommandLineApplication app = new CommandLineApplication();
            app.Name = "SimpleWebCopy";
            app.Description = ".NET Core console app with argument parsing.";

            app.HelpOption("-?|-h|--help");
            CommandOption outputOption = app.Option("-o|--output <output>", "Output folder for the offline files. Default: ./output", CommandOptionType.SingleValue);
            CommandOption threadsOption = app.Option("-t|--threads <thread>", "Number of threads the crawler will use. Default: 5", CommandOptionType.SingleValue);

            var siteArgument = app.Argument("[website]", "The website to crawl and make an offline copy of");

            app.OnExecute(async () =>
            {
                if(siteArgument.Value == null)
                {
                    app.ShowHelp();
                    return 0;
                }

                int numberOfThreads = 5;
                if (threadsOption.HasValue() && int.TryParse(threadsOption.Value(), out int parsedThreads))
                    numberOfThreads = parsedThreads;

                string outputFolder = "./output";
                if (outputOption.HasValue())
                    outputFolder = outputOption.Value().Replace("\"", "").Replace("'", "");

                crawler = new Crawler(siteArgument.Value, outputFolder, numberOfThreads);
                crawler.State.ItemAdded += State_ItemAdded;
                crawler.ThreadUpdated += Crawler_ThreadUpdated;
                crawler.ThreadComplete += Crawler_ThreadComplete;
                
                cursorStart = Console.CursorTop;
                PreRender(numberOfThreads);

                await crawler.Start();

                return 0;
            });
            
            app.Execute(args);
        }

        private static void Crawler_ThreadComplete(object sender, Events.ThreadCompleteEventArgs e)
        {
            lock (renderLock)
            {
                Console.SetCursorPosition(18, cursorStart + 1);
                Console.Write($"{crawler.State.ItemProcessedCount}/{crawler.State.ItemCount}");
                    
                int lineStart = cursorStart + 3 + (e.Thread * 3);
                Console.SetCursorPosition(11, lineStart);
                Console.Write($"Complete      ");

                Console.SetCursorPosition(0, lineStart + 1);
                Console.Write("---");
                Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));

                Console.SetCursorPosition(0, lineStart + 2);
                Console.Write(new string(' ', Console.WindowWidth));
            }
        }

        private static void Crawler_ThreadUpdated(object sender, Events.ThreadUpdatedEventArgs e)
        {
            lock (renderLock)
            {
                int lineStart = cursorStart + 3 + (e.Thread * 3);
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
        }

        private static void State_ItemAdded(object sender, Events.ItemAddedEventArgs e)
        {
            lock (renderLock)
            {
                Console.SetCursorPosition(cursorStart + 18, 1);
                Console.Write($"{crawler.State.ItemProcessedCount}/{crawler.State.ItemCount}");
            }
        }

        private static void PreRender(int threads)
        {
            Console.WriteLine($"Currently scanning: {crawler.Site}, Output: {crawler.Output}");
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
