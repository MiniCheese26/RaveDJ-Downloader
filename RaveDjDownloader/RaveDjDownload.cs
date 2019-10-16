using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RaveDjDownloader.MashupJson;

namespace RaveDjDownloader
{
    internal class RaveDjDownload
    {
        private readonly HttpClient _httpClient;
        private readonly Configuration _configuration;
        private readonly string _id;

        public RaveDjDownload(HttpClient httpClient, Configuration configuration, string id)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _id = id;
        }

        public async Task<bool> DownloadMashup()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                return false;
            }
            
            Mashup? mashupJson = await GetMashupJsonAsync();

            if (mashupJson?.MashupData == null)
            {
                Console.WriteLine("Mashup JSON/data was null");
                return false;
            }

            if (!string.Equals(mashupJson.MashupData.Stage, "complete", StringComparison.OrdinalIgnoreCase))
            {
                Message($"Mashup is not complete | Current stage: {mashupJson.MashupData.Stage}");
                return false;
            }

            Uri? downloadUrl = GetUrl(mashupJson.MashupData);

            if (downloadUrl == null)
            {
                Message("Failed to find any valid download URL");
                return false;
            }

            return await StartDownload(downloadUrl, mashupJson.MashupData.Title ?? "");
        }

        private async Task<bool> StartDownload(Uri downloadUri, string title)
        {
            string downloadPath = GetDownloadPath(title);
            string downloadName = Path.GetFileName(downloadPath);
            bool fileExists = await DoesFileExist(downloadPath, downloadUri);

            if (fileExists)
            {
                Message($"{downloadName} already exists");
                return true;
            }

            var downloadBytes = await DownloadFile(downloadUri);

            if (downloadBytes.Length == 0)
            {
                Message($"Failed to download {downloadName}");
                return false;
            }

            try
            {
                File.WriteAllBytes(downloadPath, downloadBytes);
            }
            catch (Exception ex)
            {
                Message(ex.Message);
                Console.WriteLine($"Failed to write to file {downloadPath}");
            }
            
            Message($"{downloadName} download complete");
            return true;
        }

        private string GetDownloadPath(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                title = Path.GetRandomFileName().Split(".")[0];
            }

            string saveDirectory = Path.Combine(_configuration.SaveLocation ?? "", title);

            if (!Directory.Exists(saveDirectory))
            {
                try
                {
                    Directory.CreateDirectory(saveDirectory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine($"\nFailed to create {saveDirectory}");
                    return string.Empty;
                }
            }

            return Path.Combine(saveDirectory, $"{title}.mp4");
        }

        private async Task<bool> DoesFileExist(string filePath, Uri downloadUri)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            long existingFileBytesLength;

            try
            {
                existingFileBytesLength = File.ReadAllBytes(filePath).Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Message($"Failed to read bytes of existing file {Path.GetFileName(filePath)}");
                return false;
            }

            using var headRequest = new HttpRequestMessage(HttpMethod.Head, downloadUri);
            using HttpResponseMessage headResponse = await _httpClient.SendAsync(headRequest);

            if (!headResponse.IsSuccessStatusCode)
            {
                Message("Failed to send HEAD request for MP4 download");
                return false;
            }

            var downloadLength = headResponse.Content.Headers.ContentLength;

            if (downloadLength.HasValue)
            {
                return downloadLength.Value == existingFileBytesLength;
            }

            return false;
        }

        private async Task<byte[]> DownloadFile(Uri downloadUri)
        {
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    using HttpResponseMessage mashupFileResponse = await _httpClient.GetAsync(downloadUri);
                    
                    if (mashupFileResponse.IsSuccessStatusCode)
                    {
                        return await mashupFileResponse.Content.ReadAsByteArrayAsync();
                    }

                    Message("Request for mashup file failed");
                }
                catch (HttpRequestException ex)
                {
                    Message(ex.Message);
                    Message("Failed to download mashup file");
                }
            }
            
            Message($"Failed to download {downloadUri}");
            return new byte[0];
        }

        private async Task<Mashup?> GetMashupJsonAsync()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                return null;
            }

            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using HttpResponseMessage mashupJsonRequest =
                        await _httpClient.GetAsync($"https://api.red.wemesh.ca/mashups/{_id}");

                    if (mashupJsonRequest.IsSuccessStatusCode)
                    {
                        string content = await mashupJsonRequest.Content.ReadAsStringAsync();

                        try
                        {
                            return JsonConvert.DeserializeObject<Mashup>(content);
                        }
                        catch (JsonReaderException ex)
                        {
                            Console.WriteLine(ex.Message);
                            Message("Failed to parse mashup JSON response");
                        }
                    }
                    else
                    {
                        Message("Request for mashup JSON failed");
                    }
                }
                catch (HttpRequestException)
                {
                    Message("Failed to send GET request to mashup content URL");
                }
            }

            Message("Failed to get mashup JSON content after 3 attempts");
            return null;
        }

        private Uri? GetUrl(MashupData mashupData)
        {
            if (mashupData.MaxUrl != null)
            {
                return mashupData.MaxUrl;
            }

            return mashupData.MedUrl != null
                ? mashupData.MedUrl
                : mashupData.MinUrl;
        }

        private void Message(string message) => Console.WriteLine($"[{_id}] {message}");
    }
}