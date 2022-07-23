using Microsoft.EntityFrameworkCore;
using TgAssistBot.Models.Database;

namespace TgAssistBot
{
    class Repository
    {
        private AppDbContext _dbContext = new AppDbContext();



        public List<DbCity> GetCities() => _dbContext.Cities.ToList();
        public void AddCity(DbCity city) => _dbContext.Cities.Add(city);



        public List<WeatherSubscribtion> GetWeatherSubscribtions() => 
            _dbContext.WeatherSubscribtions.Include(s => s.Subscriber).Include(s => s.DbCity).ToList();
        public void AddWeatherSubsctibtion(WeatherSubscribtion relation) => _dbContext.WeatherSubscribtions.Add(relation);



        public List<Subscriber> GetSubscribers() => _dbContext.Subscribers.ToList();
        public void AddSubscriber(Subscriber subscriber) => _dbContext.Subscribers.Add(subscriber);



        public void DeleteEntry(object obj) => _dbContext.Entry(obj).State = EntityState.Deleted;



        public bool SaveChanges() => _dbContext.SaveChanges() >= 0;
    }
}
