using System.Text.Json.Serialization;

#pragma warning disable CS8618

namespace TgAssistBot.Models.OpenWeatherMap
{
    public class Sys
    {
        [JsonPropertyName("pod")]
        public string Pod { get; set; }
    }
}
