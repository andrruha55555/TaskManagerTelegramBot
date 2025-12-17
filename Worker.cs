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
        readonly string Token = "8597751564:AAHZmAMcwdV-2FzwipaOGm_ymxXvcUBkNgs";
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

            "Кажется, что-то не получилось." +
            "Укажите дату и время напоминания в следующем формате:" +
            "\n<i><b>12:51 26.04.2025</b>" +
            "\nНапомни о том что я хотел сходить в магазин.</i>" +
            " ",

            "Задачи пользователя не найдены.",
            "Событие удалено.",
            "Все события удалены."
        };
        public bool CheckFormatDateTime(string value, out DateTime date)
        {
            return DateTime.TryParse(value, out date);
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


        public async void SendMessage(long chatId, int typeMessage)
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
        public static InlineKeyboardMarkup DeleteEvent(string Message)
        {
            List<InlineKeyboardButton> inlineKeyboards = new List<InlineKeyboardButton>();
            inlineKeyboards.Add(new InlineKeyboardButton("Удалить", Message));

            return new InlineKeyboardMarkup(inlineKeyboards);
        }
        public async void Command(long chatId, string command)
        {
            if (command.ToLower() == "/start") SendMessage(chatId, 0);
            else if (command.ToLower() == "/create_task") SendMessage(chatId, 1);
            else if (command.ToLower() == "/list_tasks")
            {
                Users User = Users.Find(x => x.IdUser == chatId);
                if (User == null) SendMessage(chatId, 4);
                else if (User.Events.Count == 0) SendMessage(chatId, 4);
                else
                {
                    foreach (Events Event in User.Events)
                    {
                        await TelegramBotClient.SendMessage(
                            chatId,
                            $"Уведомить пользователя: {Event.Time.ToString("HH:mm dd:MM:yyyy")}"
                            + $"\nÑîîáùåíèå: {Event.Message}",
                            replyMarkup: DeleteEvent(Event.Message)
                            );
                    }
                }
            }
        }
        private void GetMessages(Message message)
        {
            Console.WriteLine("Получено сообщение: " + message.Text + " от пользователя: " + message.Chat.Username);
            long IdUser = message.Chat.Id;
            string MessageUser = message.Text;

            if (message.Text.Contains("/")) Command(message.Chat.Id, message.Text);
            else if (message.Text.Equals("Удалить все задачи"))
            {
                Users User = Users.Find(x => x.IdUser == message.Chat.Id);
                if (Users == null) SendMessage(message.Chat.Id, 4);
                else if (User.Events.Count == 0) SendMessage(User.IdUser, 4);
                else
                {
                    User.Events = new List<Events>();
                    SendMessage(User.IdUser, 6);
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
                    SendMessage(message.Chat.Id, 2);
                    return;
                }

                DateTime Time;
                if (CheckFormatDateTime(Info[0], out Time) == false)
                {
                    SendMessage(message.Chat.Id, 2);
                    return;
                }
                if (Time < DateTime.Now) SendMessage(message.Chat.Id, 3);

                User.Events.Add(new Events(
                    Time,
                    message.Text.Replace(Time.ToString("HH:mm dd.MM.yyyy") + "\n", "")));
            }
        }
        private async Task HandleUpdateAsync(
            ITelegramBotClient client,
            Update update,
            CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
                GetMessages(update.Message);
            else if (update.Type == UpdateType.CallbackQuery)
            {
                CallbackQuery query = update.CallbackQuery;
                Users User = Users.Find(x => x.IdUser == query.Message.Chat.Id);
                Events Event = User.Events.Find(x => x.Message == query.Data);
                User.Events.Remove(Event);
                SendMessage(query.Message.Chat.Id, 5);
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
                for (int i = User.Events.Count; i++)
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
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
