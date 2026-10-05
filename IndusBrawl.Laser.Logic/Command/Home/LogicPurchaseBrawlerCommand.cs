namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Gatcha;
    using IndusBrawl.Laser.Logic.Message.Home;
    using IndusBrawl.Laser.Titan.DataStream;
    using System;

    public class LogicPurchaseBrawlerCommand : Command
    {
        public int CharacterInstanceId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt(); // Пропускаем
            CharacterInstanceId = stream.ReadVInt(); // Читаем Instance ID

            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Decode: Получена команда. CharacterInstanceId (Instance ID) = {CharacterInstanceId}");
        }

        public override int Execute(HomeMode homeMode)
        {
            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Начало выполнения для игрока {homeMode.Avatar.Name} (ID: {homeMode.Avatar.AccountId})");

            // 🔹 Создаём глобальный ID из Instance ID
            int globalId = 16000000 + CharacterInstanceId;
            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Глобальный ID бойца = {globalId}");

            // 🔹 Проверяем, есть ли уже бравлер
            if (homeMode.Avatar.HasHero(globalId))
            {
                Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: ОШИБКА! Игрок уже имеет бравлера с глобальным ID: {globalId}");
                return -1;
            }

            // 🔹 Получаем список бойцов Star Road
            int[] starRoad = homeMode.Home.UnlockStarRoad;
            if (starRoad == null || starRoad.Length == 0)
            {
                Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: ОШИБКА! Star Road пуст или не инициализирован.");
                return -1;
            }

            // 🔹 Проверяем, есть ли этот боец в Star Road
            bool isInStarRoad = false;
            int currentIndex = -1;
            for (int i = 0; i < starRoad.Length; i++)
            {
                if (starRoad[i] == globalId)
                {
                    isInStarRoad = true;
                    currentIndex = i;
                    break;
                }
            }

            if (!isInStarRoad)
            {
                Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: ОШИБКА! Бравлер с Global ID {globalId} отсутствует в Star Road. Невозможно купить.");
                return -1;
            }

            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Бравлер найден в Star Road на позиции {currentIndex}.");

            // 🔹 Определяем, является ли боец текущим (первым в списке)
            bool isCurrentBrawler = (currentIndex == 0);
            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Боец является текущим? {isCurrentBrawler}");

            // 🔹 Получаем данные о бойце
            CharacterData characterData = DataTables.Get(16).GetData<CharacterData>(CharacterInstanceId);
            if (characterData == null)
            {
                Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: ОШИБКА! CharacterData не найден для Instance ID: {CharacterInstanceId}");
                return -1;
            }
            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: CharacterData найден. Имя: {characterData.GetName()}");

            CardData cardData = DataTables.Get(23).GetData<CardData>(characterData.Name + "_unlock");
            if (cardData == null)
            {
                Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: ОШИБКА! CardData не найден для {characterData.Name}_unlock");
                return -1;
            }
            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: CardData найден. Редкость: {cardData.Rarity}");

            // 🔹 Определяем стоимость
            int totalTokensCost = 0;
            int initialGemsCost = 0;

            switch (cardData.Rarity)
            {
                case "rare":
                    totalTokensCost = 160;
                    initialGemsCost = 29;
                    break;
                case "super_rare":
                    totalTokensCost = 430;
                    initialGemsCost = 79;
                    break;
                case "epic":
                    totalTokensCost = 925;
                    initialGemsCost = 169;
                    break;
                case "mega_epic":
                    totalTokensCost = 1900;
                    initialGemsCost = 349;
                    break;
                case "legendary":
                    totalTokensCost = 3800;
                    initialGemsCost = 699;
                    break;
                default:
                    Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: ОШИБКА! Неизвестная редкость: {cardData.Rarity}");
                    return -1;
            }

            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Полная стоимость: {totalTokensCost} RareTokens, {initialGemsCost} гемов.");

            // 🔹 Накопленные токены
            int collectedTokens = homeMode.Avatar.RareTokens;
            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Накоплено RareTokens: {collectedTokens}");

            // 🔹 Расчёт стоимости гемов
            int gemsToPay;

            if (isCurrentBrawler)
            {
                // Текущий боец: скидка по токенам
                if (collectedTokens >= totalTokensCost)
                {
                    Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: ОШИБКА! Бравлер уже полностью разблокирован токенами.");
                    return -1;
                }

                int remainingTokens = totalTokensCost - collectedTokens;
                double fairPrice = (double)remainingTokens / totalTokensCost * initialGemsCost;
                gemsToPay = (int)Math.Ceiling(fairPrice);
                Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Расчет скидки: ({remainingTokens} / {totalTokensCost}) * {initialGemsCost} = {fairPrice:F2} -> Округлено до {gemsToPay} гемов.");
            }
            else
            {
                // Не текущий: полная стоимость, без скидок
                gemsToPay = initialGemsCost;
                Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Боец не текущий — покупка за полную стоимость: {gemsToPay} гемов.");
            }

            // 🔹 Проверка гемов
            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Проверка баланса гемов. Требуется: {gemsToPay}, Доступно: {homeMode.Avatar.Diamonds}");
            if (!homeMode.Avatar.UseDiamonds(gemsToPay))
            {
                Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: ОШИБКА! Недостаточно гемов.");
                return -1;
            }
            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Списание гемов прошло успешно.");

            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Бравлер с глобальным ID {globalId} успешно разблокирован.");

            // 🔹 Обнуление RareTokens — только если это текущий боец
            if (isCurrentBrawler)
            {
                int oldTokens = homeMode.Avatar.RareTokens;
                homeMode.Avatar.RareTokens = 0;
                Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: RareTokens обнулены. Старое значение: {oldTokens}, Новое значение: {homeMode.Avatar.RareTokens}");
            }
            else
            {
                Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Боец не текущий — RareTokens не обнуляются.");
            }

            // 🔹 Удаляем бойца из Star Road
            var list = new System.Collections.Generic.List<int>(starRoad);
            list.RemoveAt(currentIndex);
            homeMode.Home.UnlockStarRoad = list.ToArray();
            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Бравлер удалён из Star Road. Осталось бойцов: {list.Count}");

            // 🔹 Отправляем награду через Delivery
            LogicGiveDeliveryItemsCommand deliveryCommand = new LogicGiveDeliveryItemsCommand();
            DeliveryUnit unit = new DeliveryUnit(100); // Star Road

            GatchaDrop reward = new GatchaDrop(1); // Разблокировка героя
            reward.DataGlobalId = globalId;
            unit.AddDrop(reward);

            deliveryCommand.DeliveryUnits.Add(unit);
            deliveryCommand.Execute(homeMode);
            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Команда LogicGiveDeliveryItemsCommand выполнена.");

            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = deliveryCommand;
            homeMode.GameListener.SendMessage(message);
            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Сообщение AvailableServerCommandMessage отправлено клиенту.");

            // 🔹 Обновляем Star Road клиенту
            LogicBrawlerRecruitRoadChangedCommand roadChangedCommand = new LogicBrawlerRecruitRoadChangedCommand();
            AvailableServerCommandMessage roadChangedMessage = new AvailableServerCommandMessage();
            roadChangedCommand.homeMode = homeMode;
            roadChangedMessage.Command = roadChangedCommand;
            homeMode.GameListener.SendMessage(roadChangedMessage);
            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: Сообщение LogicBrawlerRecruitRoadChangedCommand отправлено.");

            Console.WriteLine($"[LogicPurchaseBrawlerCommand] Execute: КОМАНДА УСПЕШНО ВЫПОЛНЕНА. Игрок {homeMode.Avatar.Name} получил бравлера {characterData.GetName()}.");

            return 0;
        }

        public override int GetCommandType()
        {
            return 560;
        }
    }
}