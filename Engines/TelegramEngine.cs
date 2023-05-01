using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TgAssistBot.Data;
using TgAssistBot.Models.Telegram;

namespace TgAssistBot.Engines
{
	class TelegramEngine
	{
		static private TelegramBotClient _bot;
		static private WeatherCheckingEngine _weatherEngine = new();
		static private SubscribtionManager _subscribtionManager = new();
		static private CancellationTokenSource _cancellationTokenSource;
		static private ReceiverOptions _tgReceiverOptions = 
			new ReceiverOptions
			{
				AllowedUpdates = Array.Empty<UpdateType>() // all update types
			};
		static int _lastUpdateId;
		static DateTime InitUtcDateTime = DateTime.UtcNow;

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

			Logger.Log("Bot started!");
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

			switch (update.Type)
			{
				case UpdateType.CallbackQuery:
                    await HandleCallbackQuery(update.CallbackQuery!);
                    return;
				case UpdateType.InlineQuery:
                case UpdateType.ChosenInlineResult:
                case UpdateType.EditedMessage:
				case UpdateType.ChannelPost:
				case UpdateType.EditedChannelPost:
				case UpdateType.ShippingQuery:
				case UpdateType.PreCheckoutQuery:
				case UpdateType.Poll:
				case UpdateType.PollAnswer:
				case UpdateType.MyChatMember:
				case UpdateType.ChatMember:
				case UpdateType.ChatJoinRequest:
                case UpdateType.Unknown:
                case UpdateType.Message:
                default:
					break;
			}

			if (update.Message is not { } message)
				return;

			if (message.Text is not { } messageText)
				return;

			if (update.Message.Chat.Type != ChatType.Private)
				return;

			LogTgMsg(message);
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

		public static void LogTgMsg(Message message)
		{
			var logMsg = "";

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

			Logger.Log(logMsg);
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
					
					var statusCode = _subscribtionManager.CreateSubscribtion(chatId, cityId, cityName);

                    switch (statusCode)
                    {
                        case SubscribtionManager.CreateSubStatusCode.Created:
							await _bot.SendTextMessageAsync(chatId, $"Все! Подписал! Теперь ты будешь получать уведомления о погоде, поздравляю!");
							await _bot.SendTextMessageAsync(chatId, $"Кстати о погоде...");
							SendWeatherDailyUnplannedInfo(cityName, chatId);
							break;
                        case SubscribtionManager.CreateSubStatusCode.LimitReached:
							await _bot.SendTextMessageAsync(chatId, $"Достигнут лимит подписок на погоду. " +
								$"Сначала отпишись от одного города, потом подпишемся на этот.\n\n" +
								$"Чтобы посмотреть список подписок и управлять ими, напиши /getweathersubscribtions");
							break;
                        case SubscribtionManager.CreateSubStatusCode.InternalError:
							await _bot.SendTextMessageAsync(chatId, "Произошла ошибка! :( Попробуй еще раз попозже");
							break;
                        case SubscribtionManager.CreateSubStatusCode.AlreadyExists:
							await _bot.SendTextMessageAsync(chatId, "Ты уже подписан на этот город!");
							break;
                        default:
							await _bot.SendTextMessageAsync(chatId, "Произошла неизвестная ошибка");
							break;
                    }
					break;

				case "weather_unsubscribe":
					var cityWikiDataId = string.Join(' ', splittedData.Skip(1));
					_subscribtionManager.DeleteSubscribtion(chatId, cityWikiDataId);
					await _bot.SendTextMessageAsync(chatId, $"Все, отписал.");
					break;

				default:
					break;
			}

		}

		private void DailyCityNotifyHandler(string wikiDataCityId)
		{
			using (var repository = new Repository())
			{
                var relations = repository.GetWeatherSubscribtions(s => s.DbCity.WikiDataCityId == wikiDataCityId).ToList();

                var dbCity = repository.GetCity(e => e.WikiDataCityId == wikiDataCityId);

                ForecastImageEngine.SaveImageToStream(relations[0].DbCity, out MemoryStream stream);

                foreach (var item in relations)
                {
                    stream.Position = 0;

					try
					{
						_bot.SendPhotoAsync(item.Subscriber.ChatId, new InputOnlineFile(stream)).GetAwaiter().GetResult();
					}
					catch (ApiRequestException ex)
					{
						if (ex.Message == "Forbidden: bot was blocked by the user")
						{
							_subscribtionManager.DeleteSubscribtion(item.Subscriber.ChatId, wikiDataCityId);
							Logger.Log($"User with ChatID {item.Subscriber.ChatId} blocked the bot. User was unsubscribed.");
						}
						continue;
					}

                    Logger.Log($"Successfully sended daily weather info to {item.Subscriber.ChatId} [{item.DbCity.Name}]");
                }
            }

		}

		private void SendWeatherDailyUnplannedInfo(string cityName, long chatId)
		{
			var city = new Repository().GetCity(c => c.Name == cityName);

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
			using (var repository = new Repository())
			{
                var subscriber = repository.GetSubscribers(s => s.ChatId == message.Chat.Id).FirstOrDefault();

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

                repository.SaveChanges();
            }
		}

		static private BotCommandCallback[] _botCommands =
		{
			new BotCommandCallback {
				Command = "start",
				Description = "Информация о боте",

				Callback = async message =>
				{
					await _bot!.SendTextMessageAsync(
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
						await _bot!.SendTextMessageAsync(message.Chat.Id, "Неправильное использование команды. Пример использования:\n" +
							"/weathersubscribe Moscow\n" +
							"В команде должно быть использовано название на английском. Для этого можно использовать например " +
							"гугл переводчик.");

						return;
					}

					var regex = new Regex("[a-zA-Z]+");

					if (!regex.IsMatch(cityName))
					{
						await _bot!.SendTextMessageAsync(message.Chat.Id, "Название города должно быть записано английскими буквами.");
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

					await _bot!.SendTextMessageAsync(message.Chat.Id, msgTextToSend, replyMarkup: keyboard);

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

					var relations = new Repository().GetWeatherSubscribtions(r => r.Subscriber.ChatId == message.Chat.Id).ToList();

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

						await _bot!.SendTextMessageAsync(message.Chat.Id, "Чтобы отписаться от города, нажми на него ниже. " +
							"(Это твои подписки)", replyMarkup: keyboard);
					}
					else
						await _bot!.SendTextMessageAsync(message.Chat.Id, "Ты еще не подписан на погоду.");
				}
			},

			new BotCommandCallback {
				Command = "ping",
				Description = "Проверить, работает ли бот",

				Callback = async message =>
				{
					await _bot!.SendTextMessageAsync(message.Chat.Id, "Да, я работаю");
				}
			},

			new BotCommandCallback {
				Command = "weather",
				Description = "Получить текущую погоду в городе. Использование: /weather Moscow",

				Callback = async message =>
				{
					var trimmedText = message.Text!.Trim();
					var splittedMsg = trimmedText.Split(" ");
					var cityName = string.Join(' ', splittedMsg.Skip(1));

					if(string.IsNullOrEmpty(cityName) || string.IsNullOrWhiteSpace(cityName) || cityName == null)
					{
						var subscribtions = new Repository().GetWeatherSubscribtions(e => e.Subscriber.ChatId == message.Chat.Id).ToList();

						if(subscribtions.Count() <= 0 )
						{
							await _bot!.SendTextMessageAsync(message.Chat.Id, "Город не указан. Пример: /weather Moscow или /weather Москва");
							return;
						}

						cityName = subscribtions[0].DbCity.Name;
					}

					try
					{
						var weatherResponse = WeatherApiEngine.GetRealtimeWeather(cityName);

						if(weatherResponse == null)
							throw new Exception();
						else if(weatherResponse.Current == null)
							throw new Exception();

						RealtimeWeatherImageEngine.SaveImageToStream(weatherResponse, out MemoryStream stream);

						await _bot!.SendPhotoAsync(message.Chat.Id, stream!);
					}
					catch (Exception)
					{
						await _bot!.SendTextMessageAsync(message.Chat.Id, "Нету такого города, либо произошла ошибка");
						return;
					}
				}
			},
            new BotCommandCallback {
                Command = "uptime",
                Description = "Узнать время бесперерывной работы бота (если он вообще работает)",

                Callback = async message =>
                {
					var elapsed = DateTime.UtcNow - InitUtcDateTime;

					var elapsedString = $"{elapsed.Days} д, {elapsed.Hours} ч, {elapsed.Minutes} мин. и {elapsed.Seconds} сек.";

                    await _bot!.SendTextMessageAsync(message.Chat.Id, $"Бот работает уже {elapsedString}\n" +
                        $"Время запуска бота: {InitUtcDateTime.ToString("dd.MM.yyyy (HH:mm:ss)")} UTC");
                }
            },
        };
	}
}
