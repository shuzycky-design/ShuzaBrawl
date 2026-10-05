namespace IndusBrawl.Laser.Logic.Util
{
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Message.Account.Auth;
    using System;

    public static class BanUtil
    {
        // Перечисление для типов нарушений
        public enum ViolationType
        {
            AdInName,       // Реклама в нике/имени клуба
            ModifiedClient  // Модифицированный клиент
        }

        /// <summary>
        /// Банит игрока за нарушение.
        /// </summary>
        /// <param name="homeMode">HomeMode игрока</param>
        /// <param name="violation">Тип нарушения</param>
        /// <param name="durationDays">Длительность бана в днях (только для AdInName)</param>
        public static void BanPlayer(HomeMode homeMode, ViolationType violation, int durationDays = 5)
        {
            long accountId = homeMode.Avatar.AccountId;
            string reason = "";
            DateTime endTime = DateTime.UtcNow;

            switch (violation)
            {
                case ViolationType.AdInName:
                    // Устанавливаем временный бан (BanID = 1)
                    homeMode.Avatar.BanID = 1;
                    endTime = DateTime.UtcNow.AddDays(durationDays);
                    reason = "использование рекламы в нике, чате или где-то ещё";
                    break;

                case ViolationType.ModifiedClient:
    // Закомментируйте или удалите бан
    // homeMode.Avatar.BanID = 3;
    // endTime = DateTime.MaxValue;
    // reason = "использование модифицированной версии игры";
    
    // Вместо бана просто логируем и выходим
    Console.WriteLine($"[BanUtil] Обнаружен ModifiedClient, но бан отключен");
    return; // <- не баним

                default:
                    homeMode.Avatar.BanID = 3;
                    endTime = DateTime.MaxValue;
                    reason = "нарушение правил сервера";
                    break;
            }

            // Устанавливаем время окончания бана
            homeMode.Avatar.BanEndTime = endTime;

            // Формируем сообщение
            if (violation == ViolationType.AdInName)
            {
                // Для временного бана добавляем оставшееся время
                string timeLeft = GetTimeLeftString(endTime);
                homeMode.Avatar.TextReason = $"Вы были заблокированы за {reason}.\n" +
                                            $"До конца бана: {timeLeft}.";
            }
            else
            {
                // Для перманентного бана - только причина
                homeMode.Avatar.TextReason = $"Ваш аккаунт был заблокирован за {reason}.";
            }

            // Отправляем сообщение об ошибке и отключаем игрока (как в вашей команде)
            homeMode.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
            {
                Message = homeMode.Avatar.TextReason
            });

            // Логируем действие
            Console.WriteLine($"[BanUtil] Игрок {homeMode.Avatar.Name} (ID: {accountId}) забанен за: {violation}. BanID: {homeMode.Avatar.BanID}");
        }

        /// <summary>
        /// Проверяет, забанен ли игрок.
        /// </summary>
        /// <param name="homeMode">HomeMode игрока</param>
        /// <returns>True, если игрок забанен</returns>
        public static bool IsPlayerBanned(HomeMode homeMode)
        {
            return homeMode.Avatar.BanID > 0 && homeMode.Avatar.BanEndTime > DateTime.UtcNow;
        }

        /// <summary>
        /// Снимает бан с игрока.
        /// </summary>
        /// <param name="homeMode">HomeMode игрока</param>
        public static void UnbanPlayer(HomeMode homeMode)
        {
            if (IsPlayerBanned(homeMode))
            {
                homeMode.Avatar.BanID = 0;
                homeMode.Avatar.BanEndTime = DateTime.MinValue;
                homeMode.Avatar.TextReason = null;
                Console.WriteLine($"[BanUtil] Бан с игрока {homeMode.Avatar.Name} (ID: {homeMode.Avatar.AccountId}) снят.");
            }
        }

        /// <summary>
        /// Возвращает строку с оставшимся временем.
        /// </summary>
        /// <param name="endTime">Дата окончания бана</param>
        /// <returns>Форматированная строка</returns>
        private static string GetTimeLeftString(DateTime endTime)
        {
            TimeSpan timeLeft = endTime - DateTime.UtcNow;
            int days = (int)timeLeft.TotalDays;
            int hours = timeLeft.Hours;
            int minutes = timeLeft.Minutes;
            int seconds = timeLeft.Seconds;

            string result = "";
            if (days > 0) result += $"{days} д ";
            if (hours > 0) result += $"{hours} ч ";
            if (minutes > 0) result += $"{minutes} м ";
            result += $"{seconds} с";

            return result.Trim();
        }
    }
}