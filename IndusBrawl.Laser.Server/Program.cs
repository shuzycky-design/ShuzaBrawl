// Program.cs
namespace IndusBrawl.Laser.Server
{
    using Masuda.Net;
    using Masuda.Net.HelpMessage;
    using Masuda.Net.Models;
    using IndusBrawl.Laser.Server.Database.Models;
    using IndusBrawl.Laser.Server.Database;
    using IndusBrawl.Laser.Server.Handler;
    using IndusBrawl.Laser.Server.Settings;
    using IndusBrawl.Laser.Titan.Debug;
    using System.Drawing;
    using IndusBrawl.Laser.Server.Networking.Session;
    using IndusBrawl.Laser.Server.Fingerprint;
    using System.Threading.Tasks;
    using IndusBrawl.Laser.Server.Bot;
    using IndusBrawl.Laser.Server.Web; // 👈 ДОБАВЬ ЭТУ СТРОКУ

    static class Program
    {
        public const string SERVER_VERSION = "1.7";
        public const string BUILD_TYPE = "Beta";

        private static async Task Main(string[] args)
        {
            Console.Title = "ShuzaBrawl";
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            RefreshFingerprint.Main();

            Colorful.Console.WriteWithGradient(
                @"    


                            START

                           " + "\n\n\n", Color.DarkRed, Color.Red, 12);

            Logger.Print("72%");

            // 1. Инициализация логгера
            Logger.Init();

            // 2. Загрузка конфигурации
            Configuration.Instance = Configuration.LoadFromFile("config.json");

            // 3. Инициализация ресурсов
            Resources.InitDatabase();
            Resources.InitLogic();
            Resources.InitNetwork();

            // 4. Вывод сообщения о запуске
            
            Logger.Print("ShuzaBrawl");
          
            // 5. Инициализация ExitHandler
            ExitHandler.Init();

            // 6. ЗАПУСК CmdHandler В ОТДЕЛЬНОМ ПОТОКЕ
            Task.Run(() => CmdHandler.Start());

// 7. Запуск веб-сервера с топами
try
{
    var webServer = new WebServer();
    webServer.Start();
}
catch (Exception ex)
{
    Logger.Print($"[WEB] Критическая ошибка: {ex.Message}");
}

            // Админ-панель
            try
            {
                IndusBrawl.Laser.Logic.Home.CustomOffers.Load();
                new AdminServer().Start();
            }
            catch (Exception ex)
            {
                Logger.Print($"[ADMIN] Ошибка запуска: {ex.Message}");
            }

            // 8. Запуск Telegram-бота
            // Telegram-бот (@shuzabrawl_bot)
            // SHUZA_NO_TELEGRAM=1 отключает бота (например, для тестового запуска)
            if (string.IsNullOrEmpty(Configuration.Instance.TelegramBotToken))
            {
                Logger.Print("[TELEGRAM] telegram_bot_token не задан в config.json - бот выключен");
            }
            else if (Environment.GetEnvironmentVariable("SHUZA_NO_TELEGRAM") != "1")
            {
                var telegramBot = new IndusBrawl.Laser.Server.Bot.TelegramBot();
                await telegramBot.StartAsync();
            }

            // 9. Бесконечная задержка (должна быть ПОСЛЕ всех инициализаций)
            await Task.Delay(-1);
        }
    }
}