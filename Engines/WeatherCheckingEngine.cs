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
                    foreach (var item in _repository.GetCities())
                    {
                        if (_repository.GetWeatherSubscribtions().Where(s => s.DbCity.WikiDataCityId == item.WikiDataCityId).Count() <= 0)
                            continue;

                        if (item.LastDailyCheckUtcTime.AddDays(1) < DateTime.UtcNow)
                        {
                            item.LastWeather = OpenWeatherMapEngine.GetWeather(item.Name);
                            item.LastDailyCheckUtcTime = new DateTime(
                                DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                                item.LastDailyCheckUtcTime.Hour, item.LastDailyCheckUtcTime.Minute, 0);
                            _repository.SaveChanges();
                            DailyCityNotify?.Invoke(item.WikiDataCityId);
                        }
                    }

                    Thread.Sleep(600000); // 10 minutes
                }
            });

            _checkingThread.Start();
        }


        public bool CreateSubscribtion(long chatId, string wikiDataCityId, string cityName)
        {
            var subscriber = _repository.GetSubscribers().FirstOrDefault(s => s.ChatId == chatId);

            if (subscriber == null)
            {
                subscriber = new Subscriber() { ChatId = chatId };
                _repository.AddSubscriber(subscriber);
                _repository.SaveChanges();
            }
            else
            {
                var relations = _repository.GetWeatherSubscribtions().Where(r => r.Subscriber.ChatId == chatId).ToList();

                if (relations.Count >= 1)
                    return false;
            }

            var city = _repository.GetCities().FirstOrDefault(c => c.WikiDataCityId == wikiDataCityId);

            if (city == null)
            {
                var time = GeoDbEngine.GetCurrentCityTime(wikiDataCityId);

                var midnightTime = new DateTime(time.Year, time.Month, time.Day + 1, 0, 0, 0).Add(-time.Offset);

                city = new DbCity()
                {
                    MidnightUtcTime = midnightTime,
                    WikiDataCityId = wikiDataCityId,
                    LastDailyCheckUtcTime = midnightTime.AddDays(-10),
                    UtcOffset = new TimeOnly(time.Offset.Hours, time.Offset.Minutes, 0),
                    Name = cityName,
                };

                _repository.AddCity(city);
                _repository.SaveChanges();

                OpenWeatherMapEngine.GetWeather(cityName);
            }

            var relation = _repository.GetWeatherSubscribtions().FirstOrDefault(s => s.SubscriberId == subscriber.Id && s.DbCityId == city.Id);

            if (relation != null)
                return true;

            relation = new WeatherSubscribtion() { DbCityId = city.Id, SubscriberId = subscriber.Id };
            _repository.AddWeatherSubsctibtion(relation);
            _repository.SaveChanges();

            return true;
        }

        public void DeleteSubscribtion(long chatId, string wikiDataCityId)
        {
            var relation = _repository.GetWeatherSubscribtions().
                FirstOrDefault(r => r.Subscriber.ChatId == chatId && r.DbCity.WikiDataCityId == wikiDataCityId);

            if (relation == null)
                return;

            _repository.DeleteEntry(relation);
            _repository.SaveChanges();
        }

        public List<WeatherSubscribtion> GetSubscribtions(long chatId) =>
            _repository.GetWeatherSubscribtions().Where(r => r.Subscriber.ChatId == chatId).ToList();
    }
}
