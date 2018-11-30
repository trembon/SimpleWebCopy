using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
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

            CommandLineApplication app = new CommandLineApplication(false);
            app.Name = "SimpleWebCopy";
            app.Description = "Creates an offline copy of the specified website";

            app.HelpOption("-?|-h|--help");
            CommandOption outputOption = app.Option("-o|--output <output>", "Output folder for the offline files. Default: ./output", CommandOptionType.SingleValue);
            CommandOption threadsOption = app.Option("-t|--threads <thread>", "Number of threads the crawler will use. Default: 5", CommandOptionType.SingleValue);
            CommandOption userAgentOption = app.Option("-ua|--user-agent <user-agent>", "The user agent that will be sent in the header with requests. Default: SimpleWebCopy vX.X", CommandOptionType.SingleValue);
            CommandOption reportOption = app.Option("-r|--report <report>", "Where the report will be stored, containing the result of the copy. Default: <output>/_report.json", CommandOptionType.SingleValue);
            CommandOption linksOption = app.Option("-l|--link <links>", "Extra links to add to the crawler that might not be linked on the site. Default: null", CommandOptionType.MultipleValue);
            CommandOption cookiesOption = app.Option("-c|--cookie <cookies>", "Cookies to be added for the crawler. Default: null", CommandOptionType.MultipleValue);

            var siteArgument = app.Argument("[website]", "The website to crawl and make an offline copy of");

            app.OnExecute(async () =>
            {
                if (siteArgument.Value == null)
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

                string[] links = new string[0];
                if (linksOption.HasValue())
                    links = linksOption.Values.ToArray();

                Dictionary<string, string> cookies = new Dictionary<string, string>();
                if (cookiesOption.HasValue())
                {
                    foreach (string value in cookiesOption.Values)
                    {
                        string[] cookie = value.Split("=");
                        cookies.Add(cookie[0], cookie.Length > 1 ? cookie[1] : "");
                    }
                }

                crawler = new Crawler(siteArgument.Value, outputFolder, numberOfThreads, userAgent, links, cookies);
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
