using System;
using System.IO;
using Newtonsoft.Json;

namespace RaveDjDownloader
{
    internal class Configuration
    {
        private int _maxConcurrentDownloads;
        [JsonProperty("maxConcurrentDownloads")]
        public int MaxConcurrentDownloads
        {
            get => _maxConcurrentDownloads;
            set => _maxConcurrentDownloads = value <= 0 || value >= 5
                ? 3
                : value;
        }

        private string? _saveLocation;
        [JsonProperty("saveLocation")]
        public string? SaveLocation
        {
            get => _saveLocation;
            set => _saveLocation = string.IsNullOrWhiteSpace(value)
                ? Path.Combine(AppContext.BaseDirectory, "Downloads")
                : value;
        }
    }
}