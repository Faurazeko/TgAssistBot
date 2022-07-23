using TgAssistBot.Models.WeatherApi;

namespace TgAssistBot.Engines
{
    static class WeatherApiEngine
    {
        public static WeatherApiResponse GetRealtimeWeather(string cityName)
        {
			var client = new HttpClient();
			var request = new HttpRequestMessage
			{
				Method = HttpMethod.Get,
				RequestUri = new Uri($"https://weatherapi-com.p.rapidapi.com/current.json?q={cityName}"),
				Headers =
				{
					{ "X-RapidAPI-Key", ConfigLoader.GetRapidApiKey() },
					{ "X-RapidAPI-Host", "weatherapi-com.p.rapidapi.com" },
				},
			};
			using (var response = client.Send(request))
			{
				response.EnsureSuccessStatusCode();
				var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

				var obj = System.Text.Json.JsonSerializer.Deserialize<WeatherApiResponse>(body);
				return obj!;
			}
		}
    }
}
