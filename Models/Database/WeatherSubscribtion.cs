#pragma warning disable CS8618

namespace TgAssistBot.Models.Database
{
    class WeatherSubscribtion
    {
        public int Id { get; set; }
        public int SubscriberId { get; set; }
        public int DbCityId { get; set; }

        public Subscriber Subscriber { get; set; }
        public DbCity DbCity { get; set; }
    }
}
