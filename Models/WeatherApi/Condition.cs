using System.Text.Json.Serialization;

#pragma warning disable CS8618

namespace TgAssistBot.Models.WeatherApi
{
    public class Condition
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }
    }
}
