using System.Text.Json.Serialization;

#pragma warning disable CS8618

namespace TgAssistBot.Models.GeoDb
{
    public class GeoDbGetPlacesResponse
    {
        [JsonPropertyName("data")]
        public List<Place> PlaceData { get; set; }

        [JsonPropertyName("metadata")]
        public Metadata Metadata { get; set; }
    }
}
