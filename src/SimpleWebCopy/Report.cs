using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SimpleWebCopy.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleWebCopy
{
    public class Report
    {
        public int TotalItems { get; set; }

        public int Errors { get; set; }

        public int Ignored { get; set; }

        public string ExecutionTime { get; set; }

        public List<ReportItem> Items { get; set; }

        public class ReportItem
        {
            public string URL { get; set; }

            public string LocalPath { get; set; }

            public string Source { get; set; }

            public string Result { get; set; }

            public string Message { get; set; }

            public Exception Exception { get; set; }

            public ReportItem()
            {
            }

            public ReportItem(SiteItem item)
            {
                this.URL = item.FullURL;
                this.LocalPath = item.LocalFilePath;
                this.Source = item.Source;
                this.Result = item.Status.ToString().ToLowerInvariant();
                this.Message = item.StatusMessage;
                this.Exception = item.StatusException;
            }
        }

        public static void Generate(string outputFile, Crawler crawler, TimeSpan executionTime)
        {
            Report report = new Report();
            report.TotalItems = crawler.State.ItemCount;
            report.Errors = crawler.State.Items.Count(i => i.Status == ItemStatus.Error);
            report.Ignored = crawler.State.Items.Count(i => i.Status == ItemStatus.Ignored);
            report.ExecutionTime = $"{executionTime.Hours}h {executionTime.Minutes}m {executionTime.Seconds}s";
            report.Items = crawler.State.Items.Select(i => new ReportItem(i)).ToList();

            string data = JsonConvert.SerializeObject(report, Formatting.Indented, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            File.WriteAllText(outputFile, data, Encoding.UTF8);

        }
    }
}
