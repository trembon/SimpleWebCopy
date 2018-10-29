using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebCopy
{
    class Program
    {
        private static Crawler crawler;
        private static ConsoleRender renderer;

        static void Main(string[] args)
        {
            Console.CursorVisible = false;

            CommandLineApplication app = new CommandLineApplication();
            app.Name = "SimpleWebCopy";
            app.Description = "Creates an offline copy of the specified website";

            app.HelpOption("-?|-h|--help");
            CommandOption outputOption = app.Option("-o|--output <output>", "Output folder for the offline files. Default: ./output", CommandOptionType.SingleValue);
            CommandOption threadsOption = app.Option("-t|--threads <thread>", "Number of threads the crawler will use. Default: 5", CommandOptionType.SingleValue);
            CommandOption userAgentOption = app.Option("-ua|--user-agent <user-agent>", "Number of threads the crawler will use. Default: SimpleWebCopy vX.X", CommandOptionType.SingleValue);
            CommandOption reportOption = app.Option("-r|--report <report>", "Where the report will be stored, containing the result of the copy. Default: <output>/_report.json", CommandOptionType.SingleValue);

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
                
                string userAgent = "SimpleWebCopy v1.0";
                if (userAgentOption.HasValue())
                    userAgent = userAgentOption.Value();

                string reportPath = Path.Combine(outputFolder, "_report.json");
                if (reportOption.HasValue())
                    reportPath = reportOption.Value();

                crawler = new Crawler(siteArgument.Value, outputFolder, numberOfThreads, userAgent);
                renderer = new ConsoleRender(crawler);

                DateTime startTime = DateTime.Now;
                await crawler.Start();

                Report.Generate(reportPath, crawler, DateTime.Now - startTime);
                return 0;
            });
            
            app.Execute(args);
        }
    }
}
