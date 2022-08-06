using TgAssistBot.Engines;
using TgAssistBot.Models.Database;

namespace TgAssistBot.Data
{
    class SubscribtionManager
    {
        private Repository _repository = new();

        public bool CreateSubscribtion(long chatId, string wikiDataCityId, string cityName)
        {
            var subscriber = _repository.GetSubscriber(s => s.ChatId == chatId);

            if (subscriber == null)
            {
                subscriber = new Subscriber() { ChatId = chatId };
                _repository.AddSubscriber(subscriber);
                _repository.SaveChanges();
            }
            else
            {
                var relations = _repository.GetWeatherSubscribtions(r => r.Subscriber.ChatId == chatId).ToList();

                if (relations.Count >= 1)
                    return false;
            }

            var city = _repository.GetCity(c => c.WikiDataCityId == wikiDataCityId);

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

                WeatherCheckingEngine.UpdateCityWeatherIfNecessary(ref city);

                _repository.AddCity(city);
                _repository.SaveChanges();
            }

            var relation = _repository.GetWeatherSubscribtion(s => s.SubscriberId == subscriber.Id && s.DbCityId == city.Id);

            if (relation != null)
                return true;

            relation = new WeatherSubscribtion() { DbCityId = city.Id, SubscriberId = subscriber.Id };
            _repository.AddWeatherSubsctibtion(relation);
            _repository.SaveChanges();

            return true;
        }

        public void DeleteSubscribtion(long chatId, string wikiDataCityId)
        {
            var relation = _repository.GetWeatherSubscribtionsList().
                FirstOrDefault(r => r.Subscriber.ChatId == chatId && r.DbCity.WikiDataCityId == wikiDataCityId);

            if (relation == null)
                return;

            _repository.DeleteEntry(relation);
            _repository.SaveChanges();
        }
    }
}
