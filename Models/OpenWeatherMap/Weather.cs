using System.Text.Json.Serialization;

#pragma warning disable CS8618

namespace TgAssistBot.Models.OpenWeatherMap
{
    public class Weather
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("main")]
        public string Main { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }
    }
}
