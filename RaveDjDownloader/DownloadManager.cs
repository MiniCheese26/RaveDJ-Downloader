using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Konsole;

namespace RaveDjDownloader
{
    internal class DownloadManager
    {
        private readonly HttpClient _httpClient;
        
        public DownloadManager()
        {
            _httpClient = new HttpClient(new HttpClientHandler {Proxy = null});
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; rv:68.0) Gecko/20100101 Firefox/68.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-GB,en;q=0.5");
        }

        public async Task StartDownloads(Configuration configuration, IReadOnlyCollection<string> args)
        {
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
                            var raveDjDownload = new RaveDjDownload(_httpClient, configuration, url);
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
            
            string GetUrlId(string url)
            {
                Match urlMatch = Regex.Match(url, @"https?:\/\/rave\.dj\/(?'id'.+)");

                return urlMatch.Groups.Count < 2
                    ? string.Empty
                    : urlMatch.Groups["id"].Value;
            }
        }
    }
}