using System.Text.Json;

using TgAssistBot.Engines;
using TgAssistBot.Models.OpenWeatherMap;
using TgAssistBot.Models.WeatherApi;

namespace TgAssistBot
{
    public class Progam
    {
        public static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            #region ForecastImageEngineTest
            //var response = JsonSerializer.Deserialize<WeatherMapResponse>(File.ReadAllText("forecastResponse.txt"));
            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            //ForecastImageEngine.SaveImageAsPng(
            //    new Models.Database.DbCity()
            //    {
            //        UtcOffset = new TimeOnly(3, 0),
            //        LastWeather = response!
            //    }, $"{desktopPath}/img.png");
            #endregion

            #region RealtimeWeatherImageEngineTest
            //var response = JsonSerializer.Deserialize<WeatherApiResponse>(File.ReadAllText("realtimeResponse.txt"));
            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            //RealtimeWeatherImageEngine.SaveImageAsPng(response!, $"{desktopPath}/img2.png");
            #endregion

            #region Primary
            var tgEngine = new TelegramEngine();

            while (true)
                Console.ReadLine();
            #endregion
        }
    }
}