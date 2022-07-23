using System.Text.Json.Serialization;

#pragma warning disable CS8618

namespace TgAssistBot.Models.WeatherApi
{
    public class WeatherApiResponse
    {
        [JsonPropertyName("location")]
        public Location Location { get; set; }

        [JsonPropertyName("current")]
        public Current Current { get; set; }
    }
}
