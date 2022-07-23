using Microsoft.EntityFrameworkCore;
using TgAssistBot.Models.Database;

#pragma warning disable CS8618

namespace TgAssistBot
{
    class AppDbContext : DbContext
    {
        public DbSet<Subscriber> Subscribers { get; set; }
        public DbSet<DbCity> Cities { get; set; }
        public DbSet<WeatherSubscribtion> WeatherSubscribtions { get; set; }

        public AppDbContext() => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("filename=tgAssistBot.db");
            //optionsBuilder.UseInMemoryDatabase("InMem");
        }
    }
}
