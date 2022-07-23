using System.Text.Json.Serialization;

namespace TgAssistBot.Models.GeoDb
{
    public class Metadata
    {
        [JsonPropertyName("currentOffset")]
        public int CurrentOffset { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
    }
}
