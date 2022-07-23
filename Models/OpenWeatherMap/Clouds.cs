using System.Text.Json.Serialization;

namespace TgAssistBot.Models.OpenWeatherMap
{
    public class Clouds
    {
        [JsonPropertyName("all")]
        public int All { get; set; }
    }
}
