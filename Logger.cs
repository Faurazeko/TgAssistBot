using Telegram.Bot.Types;

namespace TgAssistBot
{
    class Logger
    {
        public static void Log(string message)
        {
            var prefix = Utils.GetLastNameOfCallingClassWithSpaces();

            Log(message, prefix);
        }

        public static void Log(string message, string prefix)
        {
            var logMsg = GetLogMsgBase(prefix);
            logMsg += message;

            ExecuteLog(logMsg);
        }

        private static void ExecuteLog(string logMsg) => Console.WriteLine(logMsg);

        private static string GetLogMsgBase(string prefix) => $"[{DateTime.Now}] ({prefix}) => ";
    }
}
