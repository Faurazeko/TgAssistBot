using TgAssistBot.Data;
using TgAssistBot.Models.Database;
using TgAssistBot.Models.OpenWeatherMap;

namespace TgAssistBot.Engines
{
    class WeatherCheckingEngine
    {
        private Thread _checkingThread;

        public delegate void DailyCityUpdateHadnler(string wikiDataCityId);
        public event DailyCityUpdateHadnler DailyCityNotify;

        public WeatherCheckingEngine()
        {
            _checkingThread = new Thread( t => 
            {
                Logger.Log("Starting checking thread loop");

                while (true)
                {
                    using (var repo = new Repository())
                    {
                        var cities = repo.GetCitiesList();

                        for (int i = 0; i < cities.Count(); i++)
                        {
                            var dbCity = cities[i];

                            if (repo.GetWeatherSubscribtions(s => s.DbCity.WikiDataCityId == dbCity.WikiDataCityId).Count() <= 0)
                                continue;

                            var isUpdated = UpdateCityWeatherIfNecessary(ref dbCity);

                            if (isUpdated)
                            {
                                repo.SaveChanges();
                                DailyCityNotify?.Invoke(dbCity.WikiDataCityId);
                            }

                        }
                    }
                    Thread.Sleep(600000); // 10 minutes
                }
            });

            _checkingThread.Start();
        }


        /// <returns>returns true if updated</returns>
        public static bool UpdateCityWeatherIfNecessary(ref DbCity dbCity)
        {
            if ((dbCity.LastDailyCheckUtcTime.AddDays(1) < DateTime.UtcNow) || (dbCity.LastWeather == null))
            {
                WeatherMapResponse newWeather = null;

                try
                {
                    newWeather = OpenWeatherMapEngine.GetWeather(dbCity.Name);
                }
                catch (Exception)
                {
                    return false;
                }

                dbCity.LastWeather = newWeather;

                return true;
            }

            return false;
        }
    }
}
