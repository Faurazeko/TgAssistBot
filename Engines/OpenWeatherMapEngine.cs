using System.Text.Json;
using TgAssistBot.Models.OpenWeatherMap;

namespace TgAssistBot.Engines
{
    class OpenWeatherMapEngine
    {
        
        public static WeatherMapResponse GetWeather(string cityName)
        {
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

                return responseAsClass;
            }
        }
    }
}
