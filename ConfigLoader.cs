using System.Configuration;

namespace TgAssistBot
{
    class ConfigLoader
    {
        static ExeConfigurationFileMap _configMap = new ExeConfigurationFileMap() { ExeConfigFilename = "configuration.config" };
        static Configuration _config = ConfigurationManager.OpenMappedExeConfiguration(_configMap, ConfigurationUserLevel.None);

        public static string GetRapidApiKey() => _config.AppSettings.Settings["X-RapidAPI-Key"].Value;

        public static string GetTelegramToken() => _config.AppSettings.Settings["Telegram-Token"].Value;
    }
}
