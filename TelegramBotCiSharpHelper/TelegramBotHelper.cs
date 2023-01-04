using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotCiSharpHelper
{
    internal class TelegramBotHelper
    {
        private const string TEXT_DESERT = "Десерт";
        private const string TEXT_FISH = "Лосось с картофелем";
        private const string TEXT_SAUSAGE = "Колбаски";
        private const string TEXT_QUAIL = "Перепелка";
        private const string TEXT_MUSIC = "Музыка";
        public readonly string TEXT_AUTHORS_MIAGI = "Мияги";
        public readonly string TEXT_AUTHORS_COI = "Кино";
        public readonly string TEXT_BACK = "Вернуться назад";

        private string _botToken;
        TelegramBotClient _botClient;
        private Dictionary<long, UserState> _clientStates = new Dictionary<long, UserState>();



        public TelegramBotHelper(string token)
        {
            _botToken = token;
        }

        internal void GetUpdates()
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
                    var state = _clientStates.ContainsKey(message.Chat.Id) ? _clientStates[message.Chat.Id] : null;
                    if (state != null)
                    {
                        switch (state.State)
                        {
                            case State.SearchAuthor:
                                if (message.Text.Equals(TEXT_BACK))
                                {
                                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Выберите:", replyMarkup: GetButtons());
                                    _clientStates[message.Chat.Id] = null;
                                    break;
                                }

                                List<string> song = GetSongByAuthor(author: message.Text);
                                if (song != null && song.Count > 0)
                                {
                                    state.State = State.SearchSong;
                                    state.Author = message.Text;
                                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Введите название песни", replyMarkup: getSongsButtons(song));
                                }
                                else
                                {
                                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Ничего не найдено\nВведите автора:", replyMarkup: getAuthors());
                                }

                                break;
                            case State.SearchSong:
                                if (message.Text.Equals(TEXT_BACK))
                                {
                                    state.State = State.SearchAuthor;
                                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Введите автора:", replyMarkup: getAuthors());
                                    break;
                                }

                                var songPath = getSongPath(message.Text);
                                List<string> author = GetSongByAuthor(author: state.Author);

                                if (!String.IsNullOrEmpty(songPath) && System.IO.File.Exists(songPath))
                                {
                                    /* отдача файла пользователю обратно */
                                    await using Stream stream = System.IO.File.OpenRead(songPath);
                                    await _botClient.SendAudioAsync(message.Chat.Id, new InputOnlineFile(stream, message.Text), replyMarkup: getSongsButtons(author));
                                }
                                else
                                {
                                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Ничего не найдено\nВведите название песни:", replyMarkup: getSongsButtons(author));
                                }
                                break;
                        }
                    }
                    else
                    {
                        switch (message.Text)
                        {
                            case TEXT_MUSIC:
                                _clientStates[message.Chat.Id] = new UserState { State = State.SearchAuthor };
                                _botClient.SendTextMessageAsync(message.Chat.Id, "Введите автора:", replyMarkup: getAuthors());
                                break;
                            case "Здарова":
                            case "Привет":
                                await _botClient.SendTextMessageAsync(message.Chat.Id, "Здоровей видали!");
                                break;
                            case "/start":
                                await _botClient.SendTextMessageAsync(message.Chat.Id, "Что закажем?", replyMarkup: GetButtons());
                                break;
                            case TEXT_DESERT:
                                await GetFood(update, "1.jpg");
                                break;
                            case TEXT_FISH:
                                await GetFood(update, "2.jpg");
                                break;
                            case TEXT_SAUSAGE:
                                await GetFood(update, "3.jpg");
                                break;
                            case TEXT_QUAIL:
                                await GetFood(update, "4.jpg");
                                break;
                            default:
                                await _botClient.SendTextMessageAsync(message.Chat.Id, "Выберите:", replyMarkup: GetButtons());
                                break;
                        }
                    }
                }
                else if (message.Photo is not null)
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Крутое фото! Но лучше отправь документом.");
                }
                else if (message.Document is not null)
                {
                    // TODO: сделать проверку, что документ это фото 
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
                    await _botClient.SendDocumentAsync(message.Chat.Id, new InputOnlineFile(stream, message.Document.FileName));
                }

                return;
            }
        }

        private string? getSongPath(string text)
        {
            // TODO: get song path from DB
            if (text.Equals("Минор"))
            {
                return Path.Combine(Environment.CurrentDirectory, "miyagi_minor.mp3");
            }
            else if (text.Equals("Звезда по имени Солнце"))
            {
                return Path.Combine(Environment.CurrentDirectory, "viktor_coj_-_zvezda_po_imeni_solnce.mp3");
            }
            else
            {
                return null;
            }
        }

        private IReplyMarkup? getSongsButtons(List<string> song)
        {
            var keyboard = new List<KeyboardButton>();
            song.ForEach(s => keyboard.Add(new KeyboardButton(s)));
            keyboard.Add(new KeyboardButton(TEXT_BACK));

            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
            {
                keyboard
            })
            {
                ResizeKeyboard = true
            };

            return replyKeyboardMarkup;
        }

        private List<string>? GetSongByAuthor(string author)
        {
            // TODO: get songs from DB
            if (author == TEXT_AUTHORS_MIAGI)
            {
                return new List<string> { "Минор" };
            }
            else if (author == TEXT_AUTHORS_COI)
            {
                return new List<string> { "Звезда по имени Солнце" };
            }

            return null;
        }

        private IReplyMarkup? getAuthors()
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
            {
                new KeyboardButton[] { TEXT_AUTHORS_MIAGI, TEXT_AUTHORS_COI },
                new KeyboardButton[] { TEXT_BACK, },
            })
            {
                ResizeKeyboard = true
            };

            return replyKeyboardMarkup;
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
                new KeyboardButton[] { TEXT_DESERT, TEXT_FISH },
                new KeyboardButton[] { TEXT_SAUSAGE, TEXT_QUAIL },
                new KeyboardButton[] { TEXT_MUSIC, },
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