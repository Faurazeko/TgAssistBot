using TgAssistBot.Data;
using TgAssistBot.Models.Database;

namespace TgAssistBot.Engines
{
    class WeatherCheckingEngine
    {
        private Thread _checkingThread;
        private Repository _repository = new Repository();

        public delegate void DailyCityUpdateHadnler(string wikiDataCityId);
        public event DailyCityUpdateHadnler DailyCityNotify;

        public WeatherCheckingEngine()
        {
            _checkingThread = new Thread( t => 
            {
                Logger.Log("Starting checking thread loop");

                while (true)
                {
                    var cities = _repository.GetCitiesList();

                    for (int i = 0; i < cities.Count(); i++)
                    {
                        var dbCity = cities[i];

                        if (_repository.GetWeatherSubscribtions(s => s.DbCity.WikiDataCityId == dbCity.WikiDataCityId).Count() <= 0)
                            continue;

                        var isUpdated = UpdateCityWeatherIfNecessary(ref dbCity);

                        if (isUpdated)
                        {
                            _repository.SaveChanges();
                            DailyCityNotify?.Invoke(dbCity.WikiDataCityId);
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
                dbCity.LastWeather = OpenWeatherMapEngine.GetWeather(dbCity.Name);

                dbCity.LastDailyCheckUtcTime = new DateTime(
                    DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                    dbCity.LastDailyCheckUtcTime.Hour, dbCity.LastDailyCheckUtcTime.Minute, 0);

                return true;
            }

            return false;
        }
    }
}
