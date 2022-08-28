using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using TgAssistBot.Models.OpenWeatherMap;

namespace TgAssistBot.Models.Database
{
    class DbCity
    {
        public int Id { get; set; }

        [DataType("datetime2")]
        public DateTime LastDailyCheckUtcTime { get; private set; } = DateTime.MinValue;

        [DataType("datetime2")]
        public DateTime MidnightUtcTime { get; set; } = DateTime.MinValue;
        [DataType("datetime2")]
        public TimeOnly UtcOffset { get; set; } = TimeOnly.MinValue;

        [NotMapped]
        public WeatherMapResponse LastWeather { 
            get
            {
                if (string.IsNullOrEmpty(_lastWeatherSerialized))
                    return null!;

                return System.Text.Json.JsonSerializer.Deserialize<WeatherMapResponse>(_lastWeatherSerialized)!;
            } 
            set
            {
                _lastWeatherSerialized = System.Text.Json.JsonSerializer.Serialize(value);

                LastDailyCheckUtcTime = new DateTime(
                    DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                    LastDailyCheckUtcTime.Hour, LastDailyCheckUtcTime.Minute, 0);
            }
        }
        public string _lastWeatherSerialized { get; private set; } = "";
        public string WikiDataCityId { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
