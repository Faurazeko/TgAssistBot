using TgAssistBot.Engines;
using TgAssistBot.Models.Database;

namespace TgAssistBot.Data
{
    class SubscribtionManager
    {

        public static int SubscribtionLimitPerAccount { get; private set; } = 1;

        public CreateSubStatusCode CreateSubscribtion(long chatId, string wikiDataCityId, string cityName)
        {
            using (var repository = new Repository())
            {
                var subscriber = repository.GetSubscriber(s => s.ChatId == chatId);

                if (subscriber == null)
                {
                    subscriber = new Subscriber() { ChatId = chatId };
                    repository.AddSubscriber(subscriber);
                    repository.SaveChanges();
                }
                else
                {
                    var relations = repository.GetWeatherSubscribtions(r => r.Subscriber.ChatId == chatId).ToList();

                    if (relations.Count >= SubscribtionLimitPerAccount)
                        return CreateSubStatusCode.LimitReached;
                }

                var city = repository.GetCity(c => c.WikiDataCityId == wikiDataCityId);

                if (city == null)
                {
                    DateTimeOffset time;

                    try
                    {
                        time = GeoDbEngine.GetCurrentCityTime(wikiDataCityId);
                    }
                    catch (Exception)
                    {
                        return CreateSubStatusCode.InternalError;
                    }

                    var midnightTime = new DateTime(time.Year, time.Month, time.Day + 1, 0, 0, 0).Add(-time.Offset);

                    city = new DbCity()
                    {
                        MidnightUtcTime = midnightTime,
                        WikiDataCityId = wikiDataCityId,
                        UtcOffset = new TimeOnly(time.Offset.Hours, time.Offset.Minutes, 0),
                        Name = cityName,
                    };

                    WeatherCheckingEngine.UpdateCityWeatherIfNecessary(ref city);

                    repository.AddCity(city);
                    repository.SaveChanges();
                }

                var relation = repository.GetWeatherSubscribtion(s => s.SubscriberId == subscriber.Id && s.DbCityId == city.Id);

                if (relation != null)
                    return CreateSubStatusCode.AlreadyExists;

                relation = new WeatherSubscribtion() { DbCityId = city.Id, SubscriberId = subscriber.Id };
                repository.AddWeatherSubsctibtion(relation);
                repository.SaveChanges();

                return CreateSubStatusCode.Created;
            }
        }

        public void DeleteSubscribtion(long chatId, string wikiDataCityId)
        {
            using (var repository = new Repository())
            {
                var relation = repository.GetWeatherSubscribtionsList().
                    FirstOrDefault(r => r.Subscriber.ChatId == chatId && r.DbCity.WikiDataCityId == wikiDataCityId);

                if (relation == null)
                    return;

                repository.DeleteEntry(relation);
                repository.SaveChanges();
            }
        }

        public enum CreateSubStatusCode
        {
            Created,
            LimitReached,
            InternalError,
            AlreadyExists
        }
    }
}
