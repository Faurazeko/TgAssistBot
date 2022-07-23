using System.Text.Json.Serialization;

#pragma warning disable CS8618

namespace TgAssistBot.Models.GeoDb
{
    public class Place
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("wikiDataId")]
        public string WikiDataId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; }

        [JsonPropertyName("region")]
        public string Region { get; set; }

        [JsonPropertyName("regionCode")]
        public string RegionCode { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("population")]
        public int Population { get; set; }
    }
}
