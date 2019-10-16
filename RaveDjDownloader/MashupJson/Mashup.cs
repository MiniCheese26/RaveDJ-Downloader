using Newtonsoft.Json;

namespace RaveDjDownloader.MashupJson
{
    internal class Mashup
    {
        [JsonProperty("data")]
        public MashupData? MashupData { get; set; }
    }
}