// ChatCommandSystem.cs
namespace IndusBrawl.Laser.Server.Handler
{
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Battle.Level;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Listener;
    using IndusBrawl.Laser.Logic.Message.Account;
    using IndusBrawl.Laser.Logic.Message.Account.Auth;
    using IndusBrawl.Laser.Logic.Util;
    using IndusBrawl.Laser.Server.Database;
    using IndusBrawl.Laser.Server.Database.Cache;
    using IndusBrawl.Laser.Server.Database.Models;
    using IndusBrawl.Laser.Server.Networking.Session;
    using System.Reflection;
    using IndusBrawl.Laser.Logic.Home.Items;
    using IndusBrawl.Laser.Titan.Math;
    using IndusBrawl.Laser.Titan.Debug;

    public static class ChatCommandSystem
    {
        // Инициализация системы
        public static void Init()
        {
            // Ничего не регистрируем
        }

        // Обработка входящей команды из чата
        public static void HandleCommand(string message, long accountId)
        {
            Debugger.Print($"[ChatCommandSystem] Получена команда: {message} от аккаунта {accountId}");
            if (string.IsNullOrWhiteSpace(message) || !message.StartsWith("/"))
            {
                Debugger.Print($"null");
                return;
            }
            string[] cmd = message.Split(' ');
            if (cmd.Length == 0) return;

            string command = cmd[0].TrimStart('/').ToLower();

            // Обрабатываем только команду /load
            if (command == "load")
            {
                HandleLoadCommand(cmd, accountId);
                Debugger.Print($"YO");
            }
            else
            {
                // Отправляем сообщение об ошибке
                SendChatMessage(accountId, $"❌ Неизвестная команда: /{command}");
            }
        }

        // Обработчик команды /load
        private static void HandleLoadCommand(string[] cmd, long accountId)
        {
            // Удаляем использование LogicSetLogicEventDataCommand
            // Вместо этого сразу используем SendChatMessage
            Debugger.Print($"[HandleLoadCommand] Получен запрос на загрузку аккаунта.");
            if (cmd.Length != 3)
            {
                SendChatMessage(accountId, $"Использование: /load [ID аккаунта] [PassToken]"); // Пример: /load 123456789 a1b2c3d4e5f6...
                return;
            }

            // Парсим ID аккаунта
            if (!long.TryParse(cmd[1], out long targetAccountId))
            {
                SendChatMessage(accountId, $"Ошибка: Неверный формат ID аккаунта.");
                return;
            }

            string providedPassToken = cmd[2];

            // Загружаем аккаунт из базы данных
            Account targetAccount = Accounts.Load(targetAccountId);
            if (targetAccount == null)
            {
                SendChatMessage(accountId, $"Ошибка: Аккаунт с ID {targetAccountId} не найден.");
                return;
            }

            // Сравниваем PassToken
            if (targetAccount.Avatar.PassToken != providedPassToken)
            {
                SendChatMessage(accountId, $"Ошибка: Неверный PassToken для аккаунта {targetAccountId}.");
                return;
            }

            // Отправляем клиенту данные для входа
            var session = Sessions.GetSession(accountId);
            if (session?.GameListener != null)
            {
                // ✅ Используем UnlockAccountOkMessage
                session.GameListener.SendTCPMessage(new UnlockAccountOkMessage
                {
                    AccountId = targetAccount.AccountId,
                    PassToken = targetAccount.PassToken
                });
            }

            Debugger.Print($"[Load] Игрок {accountId} успешно вошёл на аккаунт {targetAccountId}.");
        }

        // Вспомогательная функция для отправки сообщения в чат
        private static void SendChatMessage(long accountId, string message)
        {
            if (Sessions.IsSessionActive(accountId))
            {
                var session = Sessions.GetSession(accountId);
                if (session?.GameListener != null)
                {
                    Debugger.Print($"[SendChatMessage] Отправка сообщения '{message}' игроку {accountId}");
                    var chatMsg = new AuthenticationFailedMessage
                    {
                        ErrorCode = 11,
                        Message = message
                    };
                    session.GameListener.SendTCPMessage(chatMsg);
                }
            }
        }
    }
}