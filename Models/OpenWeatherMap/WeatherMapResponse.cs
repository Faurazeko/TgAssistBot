using System.Text.Json.Serialization;

#pragma warning disable CS8618

namespace TgAssistBot.Models.OpenWeatherMap
{
    public class WeatherMapResponse
    {
        [JsonPropertyName("cod")]
        public string Cod { get; set; }

        [JsonPropertyName("message")]
        public int Message { get; set; }

        [JsonPropertyName("cnt")]
        public int Cnt { get; set; }

        [JsonPropertyName("list")]
        public List<WeatherList> WeatherList { get; set; }

        [JsonPropertyName("city")]
        public City City { get; set; }
    }
}
