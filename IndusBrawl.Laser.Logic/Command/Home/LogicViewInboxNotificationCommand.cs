// LogicViewInboxNotificationCommand.cs
using IndusBrawl.Laser.Logic.Home;
using IndusBrawl.Laser.Logic.Home.Items;
using IndusBrawl.Laser.Logic.Message.Home;
using IndusBrawl.Laser.Logic.Notification;
using IndusBrawl.Laser.Titan.DataStream;
using IndusBrawl.Laser.Titan.Debug;
using IndusBrawl.Laser.Logic.Home.Gatcha;
using IndusBrawl.Laser.Logic.Data;
using IndusBrawl.Laser.Logic.Data.Helper;

namespace IndusBrawl.Laser.Logic.Command.Home
{
    public class LogicViewInboxNotificationCommand : Command
    {
        private int NotificationIndex;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            NotificationIndex = stream.ReadVInt(); // Индекс уведомления
            int unused = stream.ReadVInt();        // Calendar / unused

            Debugger.Print($"[LogicViewInboxNotificationCommand] Received. Index: {NotificationIndex}");
        }

        public override int Execute(HomeMode homeMode)
        {
            // 🔹 Проверка: существует ли Home и NotificationFactory
            if (homeMode?.Home?.NotificationFactory == null)
            {
                Debugger.Print("[LogicViewInboxNotificationCommand] Home or NotificationFactory is null!");
                return -1;
            }

            var factory = homeMode.Home.NotificationFactory;

            // 🔹 Проверка: индекс в допустимом диапазоне
            if (NotificationIndex < 0 || NotificationIndex >= factory.NotificationList.Count)
            {
                Debugger.Print($"[LogicViewInboxNotificationCommand] Invalid index: {NotificationIndex}");
                return -1;
            }

            var notification = factory.NotificationList[NotificationIndex];

            // 🔐 Проверка: уже просмотрено?
            if (notification.IsViewed)
            {
                Debugger.Print($"[LogicViewInboxNotificationCommand] Cheater attempt! Notification {NotificationIndex} already viewed.");
                return -1;
            }

            // ✅ Отмечаем как просмотренное
            notification.IsViewed = true;

            // 🔍 Обработка по ID
            switch (notification.Id)
            {
                case 81:
                    Debugger.Print($"[LogicViewInboxNotificationCommand] ID 81 (text) marked as viewed.");
                    break;

                case 88:
                    // Удвоители боевого опыта
                    int coinAmount = notification.DonationCount > 0 ? notification.DonationCount : 100;

                    var coinCommand = new LogicGiveDeliveryItemsCommand();
                    var coinUnit = new DeliveryUnit(100);
                    var coinReward = new GatchaDrop(2);
                    coinReward.Count = coinAmount;
                    coinUnit.AddDrop(coinReward);
                    coinCommand.DeliveryUnits.Add(coinUnit);
                    coinCommand.Execute(homeMode);

                    var coinMessage = new AvailableServerCommandMessage();
                    coinMessage.Command = coinCommand;
                    homeMode.GameListener.SendMessage(coinMessage);

                    Debugger.Print($"[LogicViewInboxNotificationCommand] Collected {coinAmount} token doublers from notification {NotificationIndex}.");
                    break;

                case 89:
                    // Гемы
                    int gemAmount = notification.DonationCount > 0 ? notification.DonationCount : 100;

                    var gemCommand = new LogicGiveDeliveryItemsCommand();
                    var gemUnit = new DeliveryUnit(100);
                    var gemReward = new GatchaDrop(8);
                    gemReward.Count = gemAmount;
                    gemUnit.AddDrop(gemReward);
                    gemCommand.DeliveryUnits.Add(gemUnit);
                    gemCommand.Execute(homeMode);

                    var gemMessage = new AvailableServerCommandMessage();
                    gemMessage.Command = gemCommand;
                    homeMode.GameListener.SendMessage(gemMessage);

                    Debugger.Print($"[LogicViewInboxNotificationCommand] Collected {gemAmount} gems from notification {NotificationIndex}.");
                    break;

                case 90:
                    // Ресурсы из рассылки: 1 - монеты, 22 - очки силы, 23 - блинги
                    int resourceDropType = notification.ResourceID switch { 1 => 7, 22 => 24, 23 => 25, _ => 0 };
                    if (resourceDropType == 0 || notification.ResourceCount <= 0)
                    {
                        notification.IsViewed = false;
                        return -1;
                    }

                    var resourceCommand = new LogicGiveDeliveryItemsCommand();
                    var resourceUnit = new DeliveryUnit(100);
                    var resourceReward = new GatchaDrop(resourceDropType);
                    resourceReward.Count = notification.ResourceCount;
                    resourceUnit.AddDrop(resourceReward);
                    resourceCommand.DeliveryUnits.Add(resourceUnit);
                    resourceCommand.Execute(homeMode);

                    var resourceMessage = new AvailableServerCommandMessage();
                    resourceMessage.Command = resourceCommand;
                    homeMode.GameListener.SendMessage(resourceMessage);
                    break;

                case 94:
                    // ID 94: Скин
                    int skinId = notification.SkinID;

                    if (skinId <= 0)
                    {
                        Debugger.Print($"[LogicViewInboxNotificationCommand] Invalid SkinID: {skinId}");
                        notification.IsViewed = false;
                        return -1;
                    }

                    SkinData skinData = DataTables.Get(DataType.Skin).GetDataWithId<SkinData>(skinId);
                    if (skinData == null)
                    {
                        Debugger.Print($"[LogicViewInboxNotificationCommand] SkinData not found for ID {skinId}");
                        notification.IsViewed = false;
                        return -1;
                    }

                    // Создаём команду
                    var skinCommand = new LogicGiveDeliveryItemsCommand();
                    var skinUnit = new DeliveryUnit(100);
                    var skinReward = new GatchaDrop(9);
                    skinReward.SkinGlobalId = GlobalId.CreateGlobalId(29, skinId);
                    skinReward.Count = 1;
                    skinUnit.AddDrop(skinReward);

                    skinCommand.DeliveryUnits.Add(skinUnit);
                    skinCommand.Execute(homeMode);

                    // Отправляем клиенту
                    var skinMessage = new AvailableServerCommandMessage();
                    skinMessage.Command = skinCommand;
                    homeMode.GameListener.SendMessage(skinMessage);

                    // Выдаём бонусы (эмодзи, рамки)
                    GiveSkinBonuses(skinData, skinCommand, homeMode);

                    Debugger.Print($"[LogicViewInboxNotificationCommand] Collected skin ID {skinId} from notification {NotificationIndex}.");
                    break;


                default:
                    notification.IsViewed = false;
                    Debugger.Print($"[LogicViewInboxNotificationCommand] Unsupported ID: {notification.Id}");
                    return -1;
            }

            return 0;
        }

        // Выдача бонусов за скин (эмодзи, рамки)
        private void GiveSkinBonuses(SkinData skinData, LogicGiveDeliveryItemsCommand command, HomeMode homeMode)
        {
            if (skinData == null) return;

            var bonusUnit = new DeliveryUnit(100);

            // Эмодзи
            foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
            {
                if (emoteData.Skin == skinData.Name)
                {
                    var reward = new GatchaDrop(11);
                    reward.DataGlobalId = emoteData.GetGlobalId();
                    reward.Count = 1;
                    bonusUnit.AddDrop(reward);
                }
            }

            // Рамки
            foreach (PlayerThumbnailData thumbnailData in DataTables.Get(DataType.PlayerThumbnail).GetDatas())
            {
                if (thumbnailData.CatalogPreRequirementSkin == skinData.Name)
                {
                    var reward = new GatchaDrop(11);
                    reward.DataGlobalId = thumbnailData.GetGlobalId();
                    reward.Count = 1;
                    bonusUnit.AddDrop(reward);
                }
            }

            // Добавляем бонусы
            if (bonusUnit.GetDrops().Length > 0)
            {
                command.DeliveryUnits.Add(bonusUnit);
                command.Execute(homeMode); // Перезапускаем, чтобы бонусы тоже выдались
            }
        }

        public override int GetCommandType() => 528;
    }
}