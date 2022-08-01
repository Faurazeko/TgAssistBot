using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TgAssistBot.Models.Telegram;

namespace TgAssistBot.Engines
{
	class TelegramEngine
	{
		static private TelegramBotClient _bot;
		static private WeatherCheckingEngine _weatherEngine = new WeatherCheckingEngine();
		static private Repository _repository = new Repository();
		static private CancellationTokenSource _cancellationTokenSource;
		static private ReceiverOptions _tgReceiverOptions = 
			new ReceiverOptions
			{
				AllowedUpdates = Array.Empty<UpdateType>() // all update types
			};
		static int _lastUpdateId;

		public TelegramEngine() => InitBot();

		private void InitBot()
		{
			_bot = new TelegramBotClient(ConfigLoader.GetTelegramToken());
			_cancellationTokenSource = new CancellationTokenSource();

			_weatherEngine.DailyCityNotify += DailyCityNotifyHandler;

			_bot.SetMyCommandsAsync(_botCommands).GetAwaiter().GetResult();
			_bot.StartReceiving(
				updateHandler: HandleUpdateAsync,
				pollingErrorHandler: HandlePollingErrorAsync,
				receiverOptions: _tgReceiverOptions,
				cancellationToken: _cancellationTokenSource.Token
			);

			Logger.Log("Bot started!", "Telegram");
		}

		private void RestartBot()
		{
			_cancellationTokenSource.Cancel();

			InitBot();
		}

		async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			if (_lastUpdateId == update.Id)
				return;
			else
				_lastUpdateId = update.Id;


			if (update.Type == UpdateType.CallbackQuery)
			{
				await HandleCallbackQuery(update.CallbackQuery!);
				return;
			}

			if (update.Message is not { } message)
				return;

			if (message.Text is not { } messageText)
				return;

			if (update.Message.Chat.Type != ChatType.Private)
				return;
			
			Logger.TgMsgLog(message);
			UpdateSubscriberInfo(message);

			var chatId = message.Chat.Id;

			var trimmedText = message.Text.Trim();
			var commandString = trimmedText.Split(" ").First();

			foreach (var cmd in _botCommands)
			{
				if (string.Compare(cmd.Command, 0, commandString, 1, commandString.Length) == 0)
				{
					await cmd.Callback(message);
					break;
				}
			}
		}

		Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
		{
			var ErrorMessage = exception switch
			{
				ApiRequestException apiRequestException
					=> $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
				_ => exception.ToString()
			};

			Console.WriteLine(ErrorMessage);
			return Task.CompletedTask;
		}

		async Task HandleCallbackQuery(CallbackQuery callbackQuery)
		{
			var splittedData = callbackQuery.Data.Split(' ');
			var chatId = callbackQuery.Message.Chat.Id;

			UpdateSubscriberInfo(callbackQuery.Message);

			switch (splittedData[0])
			{
				case "weater_subscribtion_deny":
					await _bot.SendTextMessageAsync(chatId, "Ну тогда заново пиши /weathersubscribe и " +
						"название города (на английском, можешь использовать гугл переводчик.)");
					break;

				case "weater_subscribtion_confirm":
					var cityName = string.Join(' ', splittedData.Skip(1).SkipLast(1));
					var cityId = string.Join(' ', splittedData.Skip(2));

					await _bot.SendTextMessageAsync(chatId, $"Сейчас подпишу тебя на {cityName} (Погода)");
					
					var success = _weatherEngine.CreateSubscribtion(chatId, cityId, cityName);

					if (success)
					{
						await _bot.SendTextMessageAsync(chatId, $"Все! Подписал! Теперь ты будешь получать уведомления о погоде, поздравляю!");
						await _bot.SendTextMessageAsync(chatId, $"Кстати о погоде...");
						SendWeatherDailyUnplannedInfo(cityName, chatId);
					}
					else
					{
						await _bot.SendTextMessageAsync(chatId, $"Достигнут лимит подписок на погоду. " +
							$"Сначала отпишись от одного города, потом подпишемся на этот.\n\n" +
							$"Чтобы посмотреть список подписок и управлять ими, напиши /getweathersubscribtions");
					}

					break;

				case "weather_unsubscribe":
					var cityWikiDataId = string.Join(' ', splittedData.Skip(1));
					_weatherEngine.DeleteSubscribtion(chatId, cityWikiDataId);
					await _bot.SendTextMessageAsync(chatId, $"Все, отписал.");
					break;

				default:
					break;
			}

		}

		private void DailyCityNotifyHandler(string wikiDataCityId)
		{
			var relations = _repository.GetWeatherSubscribtions().Where(s => s.DbCity.WikiDataCityId == wikiDataCityId).ToList();

			Logger.Log($"Sending dily update of {wikiDataCityId}", "Telegram Engine");

			var path = $"{wikiDataCityId}.png";
			ForecastImageEngine.SaveImageToStream(relations[0].DbCity, out MemoryStream stream);

			foreach (var item in relations)
			{
				stream.Position = 0;
				_bot.SendPhotoAsync(item.Subscriber.ChatId, new InputOnlineFile(stream, path)).GetAwaiter().GetResult();

				Logger.Log($"Successfully sended daily weather info to {item.Subscriber.ChatId} [{item.DbCity.Name}]", "Telegram");
			}
		}

		private void SendWeatherDailyUnplannedInfo(string cityName, long chatId)
		{
			var city = _repository.GetCities().FirstOrDefault(c => c.Name == cityName);

			if(city == null)
			{
				_bot.SendTextMessageAsync(chatId, "Сегодня без погоды!").GetAwaiter().GetResult();
				return;
			}

			ForecastImageEngine.SaveImageToStream(city, out MemoryStream stream);

			_bot.SendPhotoAsync(chatId, new InputOnlineFile(stream)).GetAwaiter().GetResult();
		}

		private static void UpdateSubscriberInfo(Message message)
		{
			var subscriber = _repository.GetSubscribers().Where(s => s.ChatId == message.Chat.Id).FirstOrDefault();

			if (subscriber == null)
				return;

			var username = message.Chat.Username;

			if ((string.IsNullOrEmpty(username) || string.IsNullOrWhiteSpace(username)) || username == null)
				username = "UNKNOWN :(";

			subscriber.TelegramUsername = username;

			var name = message.Chat.FirstName;

			if (!string.IsNullOrEmpty(message.Chat.LastName))
				name += $" {message.Chat.LastName}";

			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(name))
				name = "UNKNOWN :(";

			subscriber.TelegramName = name;

			_repository.SaveChanges();
		}

		static private BotCommandCallback[] _botCommands =
		{
			new BotCommandCallback {
				Command = "start",
				Description = "Информация о боте",

				Callback = async message =>
				{
					await _bot.SendTextMessageAsync(
						message.Chat.Id,
						"Бот находится в разработке.\n" +
						"\nПока что бот имеет только функции, связанные с погодой\n" +
						"Смотри список команд!"
					);
				}
			},

			new BotCommandCallback {
				Command = "weathersubscribe",
				Description = "Подписаться на погоду. Название города на английском. (Пример использования: /weathersubscribe Moscow)",

				Callback = async message =>
				{
					var trimmedText = message.Text!.Trim();
					var splittedMsg = trimmedText.Split(" ");

					var cityName = string.Join(' ', splittedMsg.Skip(1));

					if(splittedMsg.Length < 2)
					{
						await _bot.SendTextMessageAsync(message.Chat.Id, "Неправильное использование команды. Пример использования:\n" +
							"/weathersubscribe Moscow\n" +
							"В команде должно быть использовано название на английском. Для этого можно использовать например " +
							"гугл переводчик.");

						return;
					}

					var regex = new Regex("[a-zA-Z]+");

					if (!regex.IsMatch(cityName))
					{
						await _bot.SendTextMessageAsync(message.Chat.Id, "Название города должно быть записано английскими буквами.");
						return;
					}

					var city = GeoDbEngine.GetCity(cityName);

					var msgTextToSend =
					$"Найден город:\n\n" +
					$"Название: {city.Name}\n" +
					$"Страна: {city.Country}\n" +
					$"Регион: {city.Region}\n" +
					$"Координаты (lat, lon): {city.Latitude}, {city.Longitude}\n" +
					$"Население: {city.Population}\n" +
					$"WikiDataId: {city.WikiDataId} \n";

					InlineKeyboardMarkup keyboard = new(new[]
					{
						new[]
						{
							InlineKeyboardButton.WithCallbackData("Да, это мой город!", $"weater_subscribtion_confirm {city.Name} {city.WikiDataId}"),
							InlineKeyboardButton.WithCallbackData("Это не мой город...", "weater_subscribtion_deny"),
						}
					});

					await _bot.SendTextMessageAsync(message.Chat.Id, msgTextToSend, replyMarkup: keyboard);

				}
			},

			new BotCommandCallback {
				Command = "getweathersubscribtions",
				Description = "Посмотреть список подписок на погоду, тут же можно отписаться.",

				Callback = async message =>
				{
					var trimmedText = message.Text!.Trim();
					var splittedMsg = trimmedText.Split(" ");

					var buttons = new List<InlineKeyboardButton>();

					var relations = _repository.GetWeatherSubscribtions().Where(r => r.Subscriber.ChatId == message.Chat.Id).ToList();

					foreach (var item in relations)
					{
						var city = item.DbCity;
						var btn = InlineKeyboardButton.WithCallbackData($"{city.Name}", $"weather_unsubscribe {city.WikiDataCityId}");
						buttons.Add(btn);
					}

					if(buttons.Count > 0)
					{
						InlineKeyboardMarkup keyboard =
						new(new[]
						{
							buttons.ToArray()
						});

						await _bot.SendTextMessageAsync(message.Chat.Id, "Чтобы отписаться от города, нажми на него ниже. " +
							"(Это твои подписки)", replyMarkup: keyboard);
					}
					else
						await _bot.SendTextMessageAsync(message.Chat.Id, "Ты еще не подписан на погоду.");
				}
			},

			new BotCommandCallback {
				Command = "ping",
				Description = "Проверить, работает ли бот",

				Callback = async message =>
				{
					await _bot.SendTextMessageAsync(message.Chat.Id, "Да, я работаю");
				}
			},

			new BotCommandCallback {
				Command = "weather",
				Description = "Получить текущую погоду в городе. Использование: /weather Moscow",

				Callback = async message =>
				{
					async Task sendErrorMsg()
                    {
						await _bot.SendTextMessageAsync(message.Chat.Id, "Нету такого города, либо произошла ошибка");
					}

					var trimmedText = message.Text!.Trim();
					var splittedMsg = trimmedText.Split(" ");
					var cityName = string.Join(' ', splittedMsg.Skip(1));

					if(string.IsNullOrEmpty(cityName) || string.IsNullOrWhiteSpace(cityName) || cityName == null)
                    {
						var subscribtions = _repository.GetWeatherSubscribtions().Where(e => e.Subscriber.ChatId == message.Chat.Id).ToList();

						if(subscribtions.Count() <= 0 )
                        {
							await _bot.SendTextMessageAsync(message.Chat.Id, "Город не указан. Пример: /weather Moscow или /weather Москва");
							return;
                        }

						cityName = subscribtions[0].DbCity.Name;
                    }

					var weatherResponse = WeatherApiEngine.GetRealtimeWeather(cityName);

                    if(weatherResponse == null)
                    {
						await sendErrorMsg();
						return;
                    }
					else if(weatherResponse.Current == null)
                    {
						await sendErrorMsg();
						return;
                    }

					RealtimeWeatherImageEngine.SaveImageToStream(weatherResponse, out MemoryStream stream);

					await _bot.SendPhotoAsync(message.Chat.Id, stream);
				}
			},
		};
	}
}
