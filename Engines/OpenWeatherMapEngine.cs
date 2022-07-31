using System.Text.Json;
using TgAssistBot.Models.OpenWeatherMap;

namespace TgAssistBot.Engines
{
    class OpenWeatherMapEngine
    {
        
        public static WeatherMapResponse GetWeather(string cityName)
        {
            var repository = new Repository();

            var dbCity = repository.GetCities().FirstOrDefault(c => c.Name == cityName);

            if (dbCity != null)
            {
                if ((dbCity.LastDailyCheckUtcTime.AddDays(1) > DateTime.UtcNow) && dbCity.LastWeather != null)
                    return dbCity.LastWeather;
            }

            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = 
                new Uri($"https://api.openweathermap.org/data/2.5/forecast?q={cityName}&units=metric&lang=ru" +
                $"&appid={ConfigLoader.GetOpenWeatherMapApiKey()}")
            };

            using (var response = client.Send(request))
            {
                response.EnsureSuccessStatusCode();
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var responseAsClass = JsonSerializer.Deserialize<WeatherMapResponse>(body);

                if (responseAsClass == null)
                    throw new Exception("Response from API is invalid :(");

                if (dbCity != null)
                {
                    dbCity.LastWeather = responseAsClass;
                    dbCity.LastDailyCheckUtcTime = new DateTime(
                        DateTime.Now.Year,
                        DateTime.Now.Month,
                        DateTime.Now.Day,
                        dbCity.LastDailyCheckUtcTime.Hour,
                        dbCity.LastDailyCheckUtcTime.Minute,
                        0);

                    repository.SaveChanges();
                }

                return responseAsClass;
            }
        }
    }
}
