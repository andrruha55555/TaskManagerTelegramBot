using Telegram.Bot;
using TaskManagerTelegramBot_Pikulev.Classes;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;

namespace TaskManagerTelegramBot_Pikulev
{
    public class Worker : BackgroundService
    {
        readonly string Token = "7947738335:AAHipmScN5ZlaCgCs9maWV0y0p3-QOuKgzY";
        TelegramBotClient TelegramBotClient;
        List<Users> Users = new List<Users>();
        Timer Timer;

        static List<string> Messages = new List<string>()
        {
            "Здравствуйте!" +
            "\nРады приветствовать вас в Telegram-боте «Напоминатор»!" +
            "\nНаш бот создан для того, чтобы напоминать вам о важных событиях и мероприятиях. С ним вы точно не пропустите ничего важного! " +
            "\nНе забудьте добавить бота в список своих контактов и настроить уведомления. Тогда вы всегда будете в курсе событий! ",

            "Укажите дату и время напоминания в следующем формате:" +
            "\n<i><b>12:51 26.04.2025</b>" +
            "\nНапомни о том что я хотел сходить в магазин.</i>" +
            "\n\nДля повторяющихся задач используйте:" +
            "\n<i><b>* 21:00 ср,вс</b>" +
            "\nПолить цветы.</i>" +
            "\n\nПоддерживаются дни: пн, вт, ср, чт, пт, сб, вс",

            "Кажется, что-то не получилось." +
            "Укажите дату и время напоминания в следующем формате:" +
            "\n<i><b>12:51 26.04.2025</b>" +
            "\nНапомни о том что я хотел сходить в магазин.</i>" +
            "\n\nИли для повторяющихся задач:" +
            "\n<i><b>* 21:00 ср,вс</b>" +
            "\nЗадача.</i>",

            "Задачи пользователя не найдены.",
            "Событие удалено.",
            "Все события удалены."
        };
        private static readonly Dictionary<string, DayOfWeek> DayOfWeekMap = new Dictionary<string, DayOfWeek>
        {
            {"пн", DayOfWeek.Monday},
            {"вт", DayOfWeek.Tuesday},
            {"ср", DayOfWeek.Wednesday},
            {"чт", DayOfWeek.Thursday},
            {"пт", DayOfWeek.Friday},
            {"сб", DayOfWeek.Saturday},
            {"вс", DayOfWeek.Sunday}
        };

        private static readonly Dictionary<DayOfWeek, string> DayOfWeekReverseMap = new Dictionary<DayOfWeek, string>
        {
            {DayOfWeek.Monday, "пн"},
            {DayOfWeek.Tuesday, "вт"},
            {DayOfWeek.Wednesday, "ср"},
            {DayOfWeek.Thursday, "чт"},
            {DayOfWeek.Friday, "пт"},
            {DayOfWeek.Saturday, "сб"},
            {DayOfWeek.Sunday, "вс"}
        };

        public bool CheckFormatDateTime(string value, out DateTime time)
        {
            return DateTime.TryParse(value, out time);
        }
        public bool CheckRecurringTaskFormat(string value, out TimeSpan time, out List<DayOfWeek> days)
        {
            time = TimeSpan.Zero;
            days = new List<DayOfWeek>();

            if (!value.StartsWith("* "))
                return false;

            var parts = value.Split(' ');
            if (parts.Length < 3)
                return false;
            if (!TimeSpan.TryParse(parts[1], out time))
                return false;
            var dayParts = parts[2].Split(',');
            foreach (var dayStr in dayParts)
            {
                if (DayOfWeekMap.TryGetValue(dayStr.ToLower(), out DayOfWeek day))
                {
                    if (!days.Contains(day))
                        days.Add(day);
                }
                else
                {
                    return false;
                }
            }

            return days.Count > 0;
        }

        private static ReplyKeyboardMarkup GetButtons()
        {
            List<KeyboardButton> keyboardButtons = new List<KeyboardButton>();
            keyboardButtons.Add(new KeyboardButton("Удалить все задачи"));

            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>{
                    keyboardButtons
                }
            };
        }

        public static InlineKeyboardMarkup DeleteEvent(string Message)
        {
            List<InlineKeyboardButton> inlineKeyboards = new List<InlineKeyboardButton>();
            inlineKeyboards.Add(new InlineKeyboardButton("Удалить", Message));

            return new InlineKeyboardMarkup(inlineKeyboards);
        }

        public async Task SendMessageAsync(long chatId, int typeMessage)
        {
            if (typeMessage != 3)
            {
                await TelegramBotClient.SendMessage(
                    chatId,
                    Messages[typeMessage],
                    ParseMode.Html,
                    replyMarkup: GetButtons());
            }
            else if (typeMessage == 3)
            {
                await TelegramBotClient.SendMessage(
                    chatId,
                    $"Указанное вами время и дата не могут быть установлены; " +
                    $"потому что сейчас уже: {DateTime.Now.ToString("HH.mm dd.MM.yyyy")}");
            }
        }

        public async Task CommandAsync(long chatId, string command)
        {
            if (command.ToLower() == "/start") await SendMessageAsync(chatId, 0);
            else if (command.ToLower() == "/create_task") await SendMessageAsync(chatId, 1);
            else if (command.ToLower() == "/list_tasks")
            {
                Users User = Users.Find(x => x.IdUser == chatId);
                if (User == null) await SendMessageAsync(chatId, 4);
                else if (User.Events.Count == 0) await SendMessageAsync(chatId, 4);
                else
                {
                    foreach (Events Event in User.Events)
                    {
                        string eventType = Event.IsRecurring ? "🔁 Повторяющаяся: " : "📅 Одноразовая: ";
                        string eventInfo = Event.IsRecurring ?
                            $"Каждый {GetDaysString(Event.RecurringDays)} в {Event.Time:t}" :
                            $"Напоминание: {Event.Time.ToString("HH:mm dd.MM.yyyy")}";

                        await TelegramBotClient.SendMessage(
                            chatId,
                            $"{eventType}{eventInfo}" +
                            $"\nСообщение: {Event.Message}",
                            replyMarkup: DeleteEvent(Event.Message)
                            );
                    }
                }
            }
        }

        private string GetDaysString(List<DayOfWeek> days)
        {
            return string.Join(",", days.Select(d => DayOfWeekReverseMap[d]));
        }

        private async Task GetMessagesAsync(Message message)
        {
            Console.WriteLine("Получаено сообщение: " + message.Text + "от пользователя: " + message.Chat.Username);
            long IdUser = message.Chat.Id;
            string MessageUser = message.Text;

            DatabaseHelper.SaveUser(message.Chat.Id, message.Chat.Username);

            if (message.Text.Contains("/")) await CommandAsync(message.Chat.Id, message.Text);
            else if (message.Text.Equals("Удалить все задачи"))
            {
                Users User = Users.Find(x => x.IdUser == message.Chat.Id);
                if (User == null) await SendMessageAsync(message.Chat.Id, 4);
                else if (User.Events.Count == 0) await SendMessageAsync(User.IdUser, 4);
                else
                {
                    User.Events = new List<Events>();
                    DatabaseHelper.DeleteAllUserEvents(User.IdUser);
                    await SendMessageAsync(User.IdUser, 6);
                }
            }
            else
            {
                Users User = Users.Find(x => x.IdUser == message.Chat.Id);

                if (User == null)
                {
                    User = new Users(message.Chat.Id);
                    Users.Add(User);
                }

                string[] Info = message.Text.Split('\n');
                if (Info.Length < 2)
                {
                    await SendMessageAsync(message.Chat.Id, 2);
                    return;
                }

                string firstLine = Info[0];

                if (firstLine.StartsWith("* "))
                {
                    if (CheckRecurringTaskFormat(firstLine, out TimeSpan time, out List<DayOfWeek> days))
                    {
                        string eventMessage = message.Text.Replace(firstLine + "\n", "");

                        Events recurringEvent = new Events(
                            DateTime.Today.Add(time),
                            eventMessage,
                            isRecurring: true,
                            days: days
                        );

                        User.Events.Add(recurringEvent);
                        DatabaseHelper.SaveEvent(message.Chat.Id, recurringEvent.Time, eventMessage);

                        await TelegramBotClient.SendMessage(
                            message.Chat.Id,
                            $"✅ Создано повторяющееся событие:\n" +
                            $"Каждый {GetDaysString(days)} в {time:hh\\:mm}\n" +
                            $"Сообщение: {eventMessage}\n"
                            );
                    }
                    else
                    {
                        await SendMessageAsync(message.Chat.Id, 2);
                    }
                }
                else
                {
                    DateTime Time;
                    if (CheckFormatDateTime(firstLine, out Time) == false)
                    {
                        await SendMessageAsync(message.Chat.Id, 2);
                        return;
                    }
                    if (Time < DateTime.Now) await SendMessageAsync(message.Chat.Id, 3);

                    string eventMessage = message.Text.Replace(Time.ToString("HH:mm dd.MM.yyyy") + "\n", "");

                    Events regularEvent = new Events(Time, eventMessage);
                    User.Events.Add(regularEvent);

                    DatabaseHelper.SaveEvent(message.Chat.Id, Time, eventMessage);
                }
            }
        }

        private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
                await GetMessagesAsync(update.Message);
            else if (update.Type == UpdateType.CallbackQuery)
            {
                CallbackQuery query = update.CallbackQuery;
                Users User = Users.Find(x => x.IdUser == query.Message.Chat.Id);
                Events Event = User.Events.Find(x => x.Message == query.Data);
                User.Events.Remove(Event);
                DatabaseHelper.DeleteEvent(query.Message.Chat.Id, query.Data);

                await SendMessageAsync(query.Message.Chat.Id, 5);
            }
        }

        private async Task HandleErrorAsync(
            ITelegramBotClient client,
            Exception exception,
            HandleErrorSource source,
            CancellationToken token)
        {
            Console.WriteLine("Ошибка: " + exception.Message);
            await Task.CompletedTask;
        }

        public async void Tick(object obj)
        {
            DateTime currentDate = DateTime.Now;
            string TimeNow = currentDate.ToString("HH:mm");
            DayOfWeek currentDay = currentDate.DayOfWeek;

            foreach (Users User in Users)
            {
                for (int i = User.Events.Count - 1; i >= 0; i--)
                {
                    Events currentEvent = User.Events[i];


                    if (currentEvent.IsRecurring)
                    {
                        if (currentEvent.RecurringDays.Contains(currentDay) &&
                            currentEvent.Time.ToString("HH:mm") == TimeNow)
                        {
                            await TelegramBotClient.SendMessage(User.IdUser,
                                $"🔁 Напоминание (повторяющееся): {currentEvent.Message}\n" +
                                $"📅 Следующее напоминание: {GetNextOccurrence(currentEvent, currentDate):HH:mm dd.MM.yyyy}");
                        }
                    }
                    else
                    {
                        if (currentEvent.Time.ToString("HH:mm dd.MM.yyyy") == currentDate.ToString("HH:mm dd.MM.yyyy"))
                        {
                            await TelegramBotClient.SendMessage(User.IdUser,
                                "📅 Напоминание: " + currentEvent.Message);
                            User.Events.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private DateTime GetNextOccurrence(Events recurringEvent, DateTime currentDate)
        {
            var timeOfDay = recurringEvent.Time.TimeOfDay;
            var nextDate = currentDate.AddDays(1);

            for (int i = 0; i < 7; i++)
            {
                var checkDate = nextDate.AddDays(i);
                if (recurringEvent.RecurringDays.Contains(checkDate.DayOfWeek))
                {
                    return checkDate.Date.Add(timeOfDay);
                }
            }

            return currentDate.Date.AddDays(7).Add(timeOfDay);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DatabaseHelper.InitializeDatabase();

            TelegramBotClient = new TelegramBotClient(Token);
            TelegramBotClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                null,
                new CancellationTokenSource().Token);
            TimerCallback timerCallback = new TimerCallback(Tick);
            Timer = new Timer(timerCallback, 0, 0, 60 * 1000);

            await Task.CompletedTask;
        }

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }
    }
}
