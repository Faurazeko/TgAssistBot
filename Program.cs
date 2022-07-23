using System.Text.Json;

using TgAssistBot.Engines;
using TgAssistBot.Models.OpenWeatherMap;

namespace TgAssistBot
{
    public class Progam
    {
        public static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            //var response = JsonSerializer.Deserialize<WeatherMapResponse>(File.ReadAllText("response.txt"));
            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            //ImageCreationEngine.SaveImageForForecastAsPng(
            //    new Models.Database.DbCity()
            //    {
            //        UtcOffset = new TimeOnly(3, 0),
            //        LastWeather = response
            //    }, $"{desktopPath}/img.png");

            var tgEngine = new TelegramEngine();
            
            while(true)
                Console.ReadLine();
        }
    }
}