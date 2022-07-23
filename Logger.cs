using Telegram.Bot.Types;

namespace TgAssistBot
{
    class Logger
    {
		public static void Log(string message, string prefix = "INFO")
		{
			var logMsg = GetLogMsg(prefix);
			logMsg += message;

			ExecuteLog(logMsg);
		}

		public static void TgMsgLog(Message message)
		{
			var logMsg = GetLogMsg("Telegram");

			if (message.From.Username != null)
				logMsg += $"@{message.From.Username} ";

			if (message.From.FirstName.Length > 0)
			{
				logMsg += $"({message.From.FirstName}";

				if (message.From.LastName != null)
					logMsg += message.From.LastName;

				logMsg += ")";
			}

			logMsg += $" - \"{message.Text}\";";

			ExecuteLog(logMsg);
		}

		private static void ExecuteLog(string logMsg) => Console.WriteLine(logMsg);

		private static string GetLogMsg(string prefix) => $"[{DateTime.Now}] ({prefix}) => ";
	}
}
