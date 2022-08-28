using System.Text.Json.Serialization;
using TgAssistBot.Models.GeoDb;

namespace TgAssistBot.Engines
{
    class GeoDbEngine
    {
        public static GeoDbGetPlacesResponse GetPlacesByName(string placeName)
        {
			var client = new HttpClient();
			var request = GetRequest($"https://wft-geo-db.p.rapidapi.com/v1/geo/cities?namePrefix={placeName}");

			using (var response = client.Send(request))
			{
				response.EnsureSuccessStatusCode();
				var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				var obj = System.Text.Json.JsonSerializer.Deserialize<GeoDbGetPlacesResponse>(body);

				if (obj == null)
					throw new Exception("Bad response from GeoDbAPI");

				return obj;
			}
		}

		public static bool CityExists(string cityName)
        {
            try
            {
				var places = GetPlacesByName(cityName).PlaceData;

				foreach (var item in places)
				{
					if (item.Type.ToLower() == "city")
						return true;
				}
			}
            catch (Exception) { }

			return false;
        }

		public static Place GetCity(string cityName)
		{
            try
            {
				var places = GetPlacesByName(cityName).PlaceData;

				foreach (var item in places)
				{
					if (item.Type.ToLower() == "city")
						return item;
				}
			}
            catch (Exception) { }

			return null;
		}

		public static DateTimeOffset GetCurrentCityTime(string wikiDataCityId)
        {
			var client = new HttpClient();
			var request = GetRequest($"https://wft-geo-db.p.rapidapi.com/v1/geo/cities/{wikiDataCityId}/dateTime");

			using (var response = client.Send(request))
			{
				response.EnsureSuccessStatusCode();
				var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				var obj = System.Text.Json.JsonSerializer.Deserialize<MidnightTimeResponse>(body);

				if (obj == null)
					throw new Exception("Bad response from GeoDbAPI");

				return DateTimeOffset.Parse(obj.Data);
			}
		}

		private static HttpRequestMessage GetRequest(string url)
        {
			var request = new HttpRequestMessage
			{
				Method = HttpMethod.Get,
				RequestUri = new Uri(url),
				Headers =
				{
					{ "X-RapidAPI-Key", ConfigLoader.GetRapidApiKey() },
					{ "X-RapidAPI-Host", "wft-geo-db.p.rapidapi.com" },
				},
			};

			return request;
		}

		public class MidnightTimeResponse
		{
			[JsonPropertyName("data")]
			public string Data { get; set; } = "";
		}

	}
}
