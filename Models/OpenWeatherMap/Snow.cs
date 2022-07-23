using System.Text.Json.Serialization;

namespace TgAssistBot.Models.OpenWeatherMap
{
    public class Snow
    {
        [JsonPropertyName("3h")]
        public double _3h { get; set; }
    }
}
