using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotCiSharpHelper
{
    internal class TelegramBotHelper
    {
        private string _botToken;
        TelegramBotClient _botClient;

        public TelegramBotHelper(string token)
        {
            _botToken = token;
        }

        internal async void GetUpdates()
        {
            _botClient = new TelegramBotClient(_botToken);
            _botClient.StartReceiving(ProcessUpdate, Errors);
        }

        private Task Errors(ITelegramBotClient botClient, Exception exception, CancellationToken token)
        {
            throw exception;
        }

        private async Task ProcessUpdate(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            switch (update.Type)
            {
                case Telegram.Bot.Types.Enums.UpdateType.Unknown:
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.Message:
                    await UpdateMessage(update);
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.InlineQuery:
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.ChosenInlineResult:
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.CallbackQuery:
                    // обработка InlineKeyboardButton
                    await ProcessingInlineButtons(update);
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.EditedMessage:
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.ChannelPost:
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.EditedChannelPost:
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.ShippingQuery:
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.PreCheckoutQuery:
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.Poll:
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.PollAnswer:
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.MyChatMember:
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.ChatMember:
                    break;
                case Telegram.Bot.Types.Enums.UpdateType.ChatJoinRequest:
                    break;
                default:
                    Console.WriteLine($"Тип {update.Type} не обрабатывается");
                    break;
            }
        }

        private async Task ProcessingInlineButtons(Update update)
        {
            var foodTitle = Regex.Match(update.CallbackQuery.Message.Caption, @"\'(.+)\'");
            await _botClient.SendTextMessageAsync(update.CallbackQuery.From.Id, $"Вы успешно заказали товар {foodTitle.Value}");
        }

        private async Task UpdateMessage(Update update)
        {
            var message = update.Message;
            if (message != null)
            {
                if (message.Text is not null)
                {
                    switch (message.Text.ToLower())
                    {
                        case "здарова":
                            await _botClient.SendTextMessageAsync(message.Chat.Id, "Здоровей видали!");
                            break;
                        case "/start":
                            await _botClient.SendTextMessageAsync(
                                    message.Chat.Id, "Выберите товар", replyMarkup: GetButtons()
                                );
                            break;
                        case "десерт":
                            await GetFood(update, "1.jpg");
                            break;
                        case "лосось с картофелем":
                            await GetFood(update, "2.jpg");
                            break;
                        case "колбаски":
                            await GetFood(update, "3.jpg");
                            break;
                        case "перепелка":
                            await GetFood(update, "4.jpg");
                            break;
                    }
                }
                else if (message.Photo is not null)
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Крутое фото! Но лучше отправь документом.");
                }
                else if (message.Document is not null)
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Ща, погодь, сделаю лучше...");

                    /* загрузка фото от пользователя */
                    var fileId = update.Message.Document.FileId;
                    var fileInfo = await _botClient.GetFileAsync(fileId);
                    var filePath = fileInfo.FilePath;

                    if (!Directory.Exists("temp"))
                        Directory.CreateDirectory("temp");

                    string destinationFilePath = $@"temp/{message.Document.FileName}";
                    await using Stream fileStream = System.IO.File.OpenWrite(destinationFilePath);
                    await _botClient.DownloadFileAsync(filePath, fileStream);
                    fileStream.Close();

                    //Process.Start("colorsFilter.exe", $@"""{destinationFilePath}""");

                    /* отдача фото пользователю обратно */
                    await using Stream stream = System.IO.File.OpenRead($"temp/{message.Document.FileName}");
                    await _botClient.SendPhotoAsync(message.Chat.Id, new InputOnlineFile(stream, message.Document.FileName));
                }

                return;
            }
        }

        private async Task GetFood(Update update, string fileName)
        {
            var message = update.Message;
            var imagePath = Path.Combine(Environment.CurrentDirectory, fileName);
            using (var stream = System.IO.File.OpenRead(imagePath))
            {
                await _botClient.SendPhotoAsync(
                        message.Chat.Id,
                        new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream),
                        caption: $"Заказать '{message.Text}'",
                        replyMarkup: GetInlineButtons(1));
            }
        }

        private IReplyMarkup? GetButtons()
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
            {
                new KeyboardButton[] { "Десерт", "Лосось с картофелем"},
                new KeyboardButton[] { "Колбаски", "Перепелка" },
            })
            {
                ResizeKeyboard = true
            };

            return replyKeyboardMarkup;
        }

        private IReplyMarkup? GetInlineButtons(int id)
        {
            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
                // first row
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "Заказать", callbackData: id.ToString()),
                },
            });

            return inlineKeyboard;
        }
    }
}