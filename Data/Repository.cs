using Microsoft.EntityFrameworkCore;
using TgAssistBot.Models.Database;

namespace TgAssistBot.Data
{
    class Repository
    {
        private AppDbContext _dbContext = new AppDbContext();



        //Cities
        public List<DbCity> GetCitiesList() => _dbContext.Cities.ToList();
        public IEnumerable<DbCity> GetCities(Func<DbCity, bool> predicate) => _dbContext.Cities.Where(predicate);
        public DbCity GetCity(Func<DbCity, bool> predicate) => _dbContext.Cities.FirstOrDefault(predicate);
        public void AddCity(DbCity city) => _dbContext.Cities.Add(city);



        //Weather subscribtions
        public List<WeatherSubscribtion> GetWeatherSubscribtionsList() =>
            _dbContext.WeatherSubscribtions.Include(s => s.Subscriber).Include(s => s.DbCity).ToList();
        public IEnumerable<WeatherSubscribtion> GetWeatherSubscribtions(Func<WeatherSubscribtion, bool> predicate) =>
            _dbContext.WeatherSubscribtions.Include(s => s.Subscriber).Include(s => s.DbCity).Where(predicate);
        public WeatherSubscribtion GetWeatherSubscribtion(Func<WeatherSubscribtion, bool> predicate) =>
            _dbContext.WeatherSubscribtions.Include(s => s.Subscriber).Include(s => s.DbCity).FirstOrDefault(predicate);
        public void AddWeatherSubsctibtion(WeatherSubscribtion relation) => _dbContext.WeatherSubscribtions.Add(relation);



        //Subscribers
        public List<Subscriber> GetSubscribersList() => _dbContext.Subscribers.ToList();
        public IEnumerable<Subscriber> GetSubscribers(Func<Subscriber, bool> predicate) => _dbContext.Subscribers.ToList().Where(predicate);
        public Subscriber GetSubscriber(Func<Subscriber, bool> predicate) => _dbContext.Subscribers.ToList().FirstOrDefault(predicate);
        public void AddSubscriber(Subscriber subscriber) => _dbContext.Subscribers.Add(subscriber);



        //Other
        public void DeleteEntry(object obj) => _dbContext.Entry(obj).State = EntityState.Deleted;



        //Obvious.
        public bool SaveChanges() => _dbContext.SaveChanges() >= 0;
    }
}
