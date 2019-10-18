using System;
using System.IO;
using System.Net.Http;
using System.Security;
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
                Message("ID was null/empty/whitespace", true);
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
                Message($"Mashup is not complete | Current stage: {mashupJson.MashupData.Stage}", true);
                return false;
            }

            Uri? downloadUrl = GetUrl(mashupJson.MashupData);

            if (downloadUrl != null) 
                return await StartDownload(downloadUrl, mashupJson.MashupData.Title ?? "");
            
            Message("Failed to find any valid download URL", true);
            return false;

        }

        private async Task<bool> StartDownload(Uri downloadUri, string title)
        {
            Message($"Starting download of {title}.mp4");
            string downloadPath = GetDownloadPath(title);
            string downloadName = Path.GetFileName(downloadPath);
            bool fileExists = await DoesFileExist(downloadPath, downloadUri);

            if (fileExists)
            {
                Message($"{downloadName} already exists");
                return true;
            }

            bool downloadSuccessful = await DownloadFile(downloadUri, downloadPath);

            if (!downloadSuccessful)
            {
                Message($"Failed to download {downloadName}", true);
                return false;
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

            if (Directory.Exists(saveDirectory)) 
                return Path.Combine(saveDirectory, $"{title}.mp4");
            
            try
            {
                Directory.CreateDirectory(saveDirectory);
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case UnauthorizedAccessException _:
                        Message($"Invalid permissions to create save directory, {saveDirectory}", true);
                        break;
                    case DirectoryNotFoundException _:
                        Message($"Attempted to create save directory on unmapped drive, {saveDirectory}", true);
                        break;
                    case PathTooLongException _:
                        Message($"Save directory path is too long, {saveDirectory}", true);
                        break;
                    case IOException _:
                        Message($"I\\O error occured whilst creating download directory, {saveDirectory}", true);
                        break;
                    case NotSupportedException _:
                        Message($"Save directory contains illegal characters, {saveDirectory}", true);
                        break;
                    default:
                        throw;
                }

                return string.Empty;
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
                existingFileBytesLength = new FileInfo(filePath).Length;
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case SecurityException _:
                    case UnauthorizedAccessException _:
                        Message($"Access unauthorised to file {filePath}", true);
                        break;
                    case NotSupportedException _: 
                        Message($"Path to existing download of {_id} contains illegal characters, {filePath}", true);
                        break;
                    case PathTooLongException _:
                        Message($"Path to existing download of {_id} is too long, {filePath}", true);
                        break;
                    default:
                        throw;
                }
                
                return false;
            }

            using var headRequest = new HttpRequestMessage(HttpMethod.Head, downloadUri);

            try
            {
                using HttpResponseMessage headResponse = await _httpClient.SendAsync(headRequest);

                if (!headResponse.IsSuccessStatusCode)
                {
                    Message("Failed to send HEAD request for MP4 download", true);
                    return false;
                }

                var downloadLength = headResponse.Content.Headers.ContentLength;

                if (downloadLength.HasValue)
                {
                    return downloadLength.Value == existingFileBytesLength;
                }
            }
            catch (HttpRequestException)
            {
                Message("Failed to send HEAD request for MP4 download", true);
            }

            return false;
        }

        private async Task<bool> DownloadFile(Uri downloadUri, string downloadPath)
        {
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    using HttpResponseMessage mashupFileResponse = await _httpClient.GetAsync(downloadUri);

                    if (!mashupFileResponse.IsSuccessStatusCode)
                        continue;

                    await using Stream downloadStream = await mashupFileResponse.Content.ReadAsStreamAsync();

                    if (downloadStream == null || downloadStream.Length <= 0)
                        continue;

                    if (await WriteDownloadFile(downloadStream, downloadPath))
                    {
                        return true;
                    }
                }
                catch (HttpRequestException ex)
                {
                    Message(ex.Message, true);
                }
                
                Message($"Request to download {downloadUri} failed", true);
            }
            
            Message($"Failed to download {downloadUri}", true);
            return false;
        }

        private async Task<bool> WriteDownloadFile(Stream downloadStream, string downloadPath)
        {
            FileStream? file = null;
                    
            try
            {
                file = File.Create(downloadPath);
            }
            catch (Exception ex)
            {
                Message(ex.Message, true);
            }

            if (file == null)
                return false;
                    
            try
            {
                await downloadStream.CopyToAsync(file);
                file.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                Message(ex.Message, true);
            }
                    
            file.Dispose();
            return true;
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
                        catch (JsonReaderException)
                        {
                            Message("Failed to parse mashup JSON response", true);
                        }
                    }
                    else
                    {
                        Message("Request for mashup JSON failed", true);
                    }
                }
                catch (HttpRequestException)
                {
                    Message("Failed to send GET request to mashup content URL", true);
                }
            }

            Message("Failed to get mashup JSON content after 3 attempts", true);
            return null;
        }

        private static Uri? GetUrl(MashupData mashupData)
        {
            if (mashupData.MaxUrl != null)
            {
                return mashupData.MaxUrl;
            }

            return mashupData.MedUrl != null
                ? mashupData.MedUrl
                : mashupData.MinUrl;
        }
        
        private void Message(string message, bool failure = false)
        {
            Console.Write($"[{_id}] ");
            
            if (failure)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
            }
            
            Console.Write(message + Environment.NewLine);
            
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}