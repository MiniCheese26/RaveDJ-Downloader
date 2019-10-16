using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Konsole;
using Newtonsoft.Json;

namespace RaveDjDownloader
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Help();
                Environment.Exit(1);
            }
            
            string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");

            if (!File.Exists(configPath))
            {
                Console.WriteLine("Failed to find config.json");
                Environment.Exit(1);
            }

            Configuration? configuration = DeserializeConfiguration(configPath);

            if (configuration == null)
            {
                Console.WriteLine("Failed to parse configuration");
                Environment.Exit(1);
            }

            await StartDownloads(configuration, args);
        }

        private static async Task StartDownloads(Configuration configuration, IReadOnlyCollection<string> args)
        {
            var httpClient = new HttpClient(new HttpClientHandler {Proxy = null});
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; rv:68.0) Gecko/20100101 Firefox/68.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-GB,en;q=0.5");

            var tasks = new List<Task>();
            var throttler = new SemaphoreSlim(configuration.MaxConcurrentDownloads);

            int numberOfLinks = args.Count;
            var complete = 0;
            var failed = 0;
            var progressBar = new ProgressBar(PbStyle.SingleLine, numberOfLinks);
            progressBar.Refresh(complete, $"Downloads processed {complete}/{numberOfLinks} | Failed: {failed}");

            foreach (string url in args.Select(GetUrlId))
            {
                await throttler.WaitAsync();

                tasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            var raveDjDownload = new RaveDjDownload(httpClient, configuration, url);
                            bool downloadResult = await raveDjDownload.DownloadMashup();

                            if (downloadResult)
                            {
                                complete++;
                            }
                            else
                            {
                                failed++;
                            }

                            progressBar.Refresh(complete, $"Downloads processed {complete}/{numberOfLinks} | Failed: {failed}");
                        }
                        finally
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            throttler.Release();
                        }
                    }));
            }

            await Task.WhenAll(tasks);

            throttler.Dispose();
        }
        
        private static Configuration? DeserializeConfiguration(string configPath)
        {
            try
            {
                string configFile = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<Configuration>(configFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }

            return null;
        }

        private static string GetUrlId(string url)
        {
            Match urlMatch = Regex.Match(url, @"https?:\/\/rave\.dj\/(?'id'.+)");

            if (urlMatch.Groups.Count < 2)
            {
                return string.Empty;
            }

            return urlMatch.Groups["id"].Value;
        }

        private static void Help()
        {
            Console.WriteLine("Usage: dotnet raveDjDownloader.dll <URL1> <URL2> ...");
        }
    }
}