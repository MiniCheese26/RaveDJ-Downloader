using System;
using Newtonsoft.Json;

namespace RaveDjDownloader.MashupJson
{
    internal class MashupData
    {
        [JsonProperty("maxUrl")]
        public Uri? MaxUrl { get; set; }
        
        [JsonProperty("medUrl")]
        public Uri? MedUrl { get; set; }
        
        [JsonProperty("minUrl")]
        public Uri? MinUrl { get; set; }
        
        [JsonProperty("title")]
        public string? Title { get; set; }
        
        [JsonProperty("stage")]
        public string? Stage { get; set; }
        
        [JsonProperty("artist")]
        public string? Artist { get; set; }
    }
}