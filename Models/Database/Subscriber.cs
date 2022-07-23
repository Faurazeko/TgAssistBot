namespace TgAssistBot.Models.Database
{
    class Subscriber
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public string TelegramUsername { get; set; } = "";
        public string TelegramName { get; set; } = "";
    }
}
