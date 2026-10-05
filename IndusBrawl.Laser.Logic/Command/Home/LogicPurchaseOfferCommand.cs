// LogicPurchaseOfferCommand.cs
namespace IndusBrawl.Laser.Logic.Command.Home
{
    using System;
    using System.Security.Cryptography;
    using Newtonsoft.Json;
    using System.Numerics;
    using IndusBrawl.Laser.Logic.Command.Home;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Gatcha;
    using IndusBrawl.Laser.Logic.Home.Items;
    using IndusBrawl.Laser.Logic.Home.Quest;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Logic.Message.Account.Auth;
    using IndusBrawl.Laser.Logic.Message.Home;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;
    using System.Text;
    using IndusBrawl.Laser.Logic.Time;
    using IndusBrawl.Laser.Logic.Command;
    using System.Xml.Linq;
    using IndusBrawl.Laser.Logic.Notification;
    using IndusBrawl.Laser.Logic.Avatar;
    using System.Collections.Generic;

    public class LogicPurchaseOfferCommand : Command
    {
        public int OfferIndex;
        public int DataGlobalId;
        public int Unknown; // Всегда 0 в текущем клиенте?
        public int Currency;
        // 0 - Coins (только для скинов)
        // 1 - Gems
        // 2 - Blings (только)
        // 3 - Blings (с докупкой)
        // 4 - Coins (докупка гемов за монеты если не хватает)

        // Курсы обмена
        const int BLING_PER_GEM = 33;  // 1 гем = 33 блинга
        const int COINS_PER_GEM = 10;  // 1 гем = 10 монет


        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            OfferIndex = stream.ReadVInt();
            DataGlobalId = ByteStreamHelper.ReadDataReference(stream);
            Unknown = ByteStreamHelper.ReadDataReference(stream); // Всегда 0?
            Currency = stream.ReadVInt();
            Debugger.Print($"[PurchaseOffer] Decoded command. Index: {OfferIndex}, DataId: {DataGlobalId}, Unknown: {Unknown}, Currency: {Currency}");
        }

        public override int Execute(HomeMode homeMode)
        {
            if (OfferIndex > -1)
            {
                if (!CanExecute(homeMode)) return -1;
                homeMode.Home.PurchaseOffer(OfferIndex);
            }
            else if (OfferIndex == -1)
            {
                return PurchaseOfferWithCatalog(homeMode, DataGlobalId, Currency);
            }
            return 0;
        }

        private int PurchaseOfferWithCatalog(HomeMode homeMode, int dataGlobalId, int currency)
        {
            var avatar = homeMode.Avatar;
            LogicData itemData = null;
            int priceGems = 0;
            int priceBling = 0;
            int priceCoins = 0; // Будет заполнено только для SkinData
            int dropType = 0;

            // === Определяем тип предмета и данные ===
            if (dataGlobalId > 29000000 && dataGlobalId < 30000000) // Skin
            {
                SkinData skinData = DataTables.Get(DataType.Skin).GetDataWithId<SkinData>(dataGlobalId);
                if (skinData == null)
                {
                    Debugger.Warning($"[PurchaseOffer] Скин с DataGlobalId {dataGlobalId} не найден в данных.");
                    return -1; // Предмет не существует
                }
                itemData = skinData;
                priceGems = skinData.PriceGems;
                priceBling = skinData.PriceBling;
                priceCoins = skinData.PriceCoins; // Цена в монетах только для скинов
                dropType = 9;
            }
            else if (dataGlobalId > 52000000 && dataGlobalId < 53000000) // Emote
            {
                EmoteData emoteData = DataTables.Get(DataType.Emote).GetDataWithId<EmoteData>(dataGlobalId);
                if (emoteData == null)
                {
                    Debugger.Warning($"[PurchaseOffer] Эмоция с DataGlobalId {dataGlobalId} не найдена в данных.");
                    return -1; // Предмет не существует
                }
                itemData = emoteData;
                priceGems = emoteData.PriceGems;
                priceBling = emoteData.PriceBling;
                // У EmoteData нет PriceCoins, оставляем 0
                dropType = 11;
            }
            else if (dataGlobalId > 28000000 && dataGlobalId < 29000000) // Thumbnail
            {
                PlayerThumbnailData thumbnailData = DataTables.Get(DataType.PlayerThumbnail).GetDataWithId<PlayerThumbnailData>(dataGlobalId);
                if (thumbnailData == null)
                {
                    Debugger.Warning($"[PurchaseOffer] Значок с DataGlobalId {dataGlobalId} не найден в данных.");
                    return -1; // Предмет не существует
                }
                itemData = thumbnailData;
                priceGems = thumbnailData.PriceGems;
                priceBling = thumbnailData.PriceBling;
                // У PlayerThumbnailData нет PriceCoins, оставляем 0
                dropType = 11;
            }
            else
            {
                Debugger.Warning($"[PurchaseOffer] Unknown item type for DataGlobalId: {dataGlobalId}");
                return -1; // Неизвестный тип предмета
            }

            // === Проверки доступности и цены ===
            if (!IsItemAvailableForCatalog(itemData))
            {
                Debugger.Warning($"[PurchaseOffer] Предмет {itemData.GetName()} ({dataGlobalId}) недоступен для покупки в каталоге.");
                return -1; // Предмет не для каталога
            }

            // === Проверка на запрещённые скины ===
            if (itemData is SkinData skin && IsRestrictedSkin(skin))
            {
                Debugger.Warning($"[PurchaseOffer] Скин {skin.GetName()} ({dataGlobalId}) запрещён для покупки в каталоге (ObtainType = 7).");
                return -1; // Скин в списке запрещённых
            }

            if (IsPriceInvalid(currency, itemData, priceGems, priceBling, priceCoins))
            {
                Debugger.Warning($"[PurchaseOffer] Неверная цена для предмета {itemData.GetName()} ({dataGlobalId}). Currency: {currency}, Gems: {priceGems}, Bling: {priceBling}, Coins: {priceCoins}");
                return -1; // Неверная цена
            }

            // === Обработка покупки ===
            return ProcessPurchase(homeMode, currency, priceGems, priceBling, priceCoins, dataGlobalId, dropType, itemData);
        }

        /// <summary>
        /// Проверяет, является ли скин запрещённым для покупки в каталоге, основываясь на значении ObtainType.
        /// </summary>
        private bool IsRestrictedSkin(SkinData skinData)
        {
            // Предполагается, что у SkinData есть свойство ObtainType, полученное из CSV
            Console.WriteLine(skinData.ObtainType);
            // Если ObtainType равно 7, скин нельзя купить.
            return skinData.ObtainType != 0;
        }

        /// <summary>
        /// Проверяет, доступен ли предмет для покупки в каталоге.
        /// </summary>
        private bool IsItemAvailableForCatalog(LogicData data)
        {
            switch (data)
            {
                case SkinData skin:
                    // Предмет недоступен, если он отмечен как DisableCatalogRelease или если у него нет *никакой* цены
                    return !skin.DisableCatalogRelease && (skin.PriceGems > 0 || skin.PriceBling > 0 || skin.PriceCoins > 0);

                case EmoteData emote:
                    // Предмет недоступен, если он отмечен как DisableCatalogRelease или если у него нет *никакой* цены
                    return !emote.DisableCatalogRelease && (emote.PriceGems > 0 || emote.PriceBling > 0);
                    // TODO: Учесть LockedForChronos, если нужно

                case PlayerThumbnailData thumbnail:
                    // Предмет недоступен, если IsAvailableForOffers == false или если у него нет *никакой* цены
                    return thumbnail.IsAvailableForOffers && (thumbnail.PriceGems > 0 || thumbnail.PriceBling > 0);
                    // Предполагаем, что значки не продаются за монеты

                default:
                    // Неизвестный тип данных
                    Debugger.Warning($"[PurchaseOffer] IsItemAvailableForCatalog: Неизвестный тип данных {data?.GetType().Name}");
                    return false; // Считаем недоступным для неизвестных типов
            }
        }

        /// <summary>
        /// Проверяет, является ли цена недопустимой (например, 0) для выбранной валюты.
        /// </summary>
        private bool IsPriceInvalid(int currency, LogicData data, int priceGems, int priceBling, int priceCoins)
        {
            switch (currency)
            {
                case 0: // Coins (только для скинов)
                case 4: // Coins (докупка гемов за монеты если не хватает)
                    // Цена в монетах актуальна только для скинов
                    if (data is SkinData)
                    {
                        return priceCoins <= 0;
                    }
                    else
                    {
                        // Для других типов предметов покупка за монеты запрещена
                        return true;
                    }
                case 1: // Gems
                    return priceGems <= 0;
                case 2: // Blings (только)
                case 3: // Blings (с докупкой)
                    return priceBling <= 0;
                default:
                    return true; // Неизвестная валюта
            }
        }

        private int ProcessPurchase(HomeMode homeMode, int currency, int priceGems, int priceBling, int priceCoins, int dataGlobalId, int dropType, LogicData itemData)
        {
            var avatar = homeMode.Avatar;
            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();

            // === Логика покупки ===
            if (currency == 0) // Coins (только для скинов)
            {
                // Дополнительная проверка типа, на всякий случай
                if (!(itemData is SkinData))
                {
                     Debugger.Warning($"[PurchaseOffer] Попытка покупки не-скина за монеты. DataId: {dataGlobalId}, Type: {itemData?.GetType().Name}");
                     return -1; // Запрещено
                }

                if (avatar.Gold < priceCoins)
                {
                    Debugger.Print($"[PurchaseOffer] Недостаточно монет для прямой покупки. Нужно: {priceCoins}, Есть: {avatar.Gold}");
                    return -2; // Недостаточно монет
                }

                // Хватает монет - списываем
                avatar.UseGold(priceCoins);
                Debugger.Print($"[PurchaseOffer] Предмет куплен за {priceCoins} монет.");
            }
            else if (currency == 1) // Gems
            {
                if (!avatar.UseDiamonds(priceGems))
                {
                    Debugger.Print($"[PurchaseOffer] Недостаточно гемов для прямой покупки. Нужно: {priceGems}, Есть: {avatar.Diamonds}");
                    return -2; // Недостаточно гемов
                }
                Debugger.Print($"[PurchaseOffer] Предмет куплен за {priceGems} гемов.");
            }
            else if (currency == 2) // Blings (только)
            {
                if (!avatar.UseBlings(priceBling))
                {
                    // Не хватает блингов для прямой покупки
                    Debugger.Print($"[PurchaseOffer] Недостаточно блингов для прямой покупки. Нужно: {priceBling}, Есть: {avatar.Blings}");
                    return -2; // Недостаточно блингов
                }
                Debugger.Print($"[PurchaseOffer] Предмет куплен за {priceBling} блингов (только).");
            }
            else if (currency == 3) // Blings (с докупкой)
            {
                if (avatar.Blings >= priceBling)
                {
                    // Хватает блингов - просто списываем
                    avatar.Blings = 0; // Все имеющиеся блинги уходят
                    avatar.UseDiamonds(priceBling);
                    Debugger.Print($"[PurchaseOffer] Предмет куплен за {priceBling} блингов.");
                }
                else
                {
                    // Не хватает блингов - рассчитываем стоимость докупки
                    int blingNeeded = priceBling - avatar.Blings;
                    int gemsNeeded = CalculateGemsForBling(blingNeeded);

                    Debugger.Print($"[PurchaseOffer] Недостаточно блингов. Нужно: {priceBling}, Есть: {avatar.Blings}");
                    Debugger.Print($"[PurchaseOffer] Требуется докупить {blingNeeded} блингов за {gemsNeeded} гемов");

                    if (avatar.Diamonds < gemsNeeded)
                    {
                        Debugger.Print($"[PurchaseOffer] Недостаточно гемов для докупки блингов. Нужно: {gemsNeeded}, Есть: {avatar.Diamonds}");
                        return -2; // Недостаточно гемов
                    }

                    // Списываем все имеющиеся блинги и нужное количество гемов
                    avatar.Blings = 0; // Все имеющиеся блинги уходят
                    avatar.UseDiamonds(gemsNeeded);
                    Debugger.Print($"[PurchaseOffer] Предмет куплен: {priceBling} блингов (докупка за {gemsNeeded} гемов).");
                }
            }
            else if (currency == 4) // Coins (докупка гемов за монеты если не хватает) - только для скинов
            {
                // Дополнительная проверка типа, на всякий случай, хотя IsPriceInvalid уже должна была это отсеять
                if (!(itemData is SkinData))
                {
                     Debugger.Warning($"[PurchaseOffer] Попытка покупки не-скина за монеты. DataId: {dataGlobalId}, Type: {itemData?.GetType().Name}");
                     return -1; // Запрещено
                }

                if (avatar.Gold >= priceCoins)
                {
                    // Хватает монет - просто списываем
                    avatar.UseGold(priceCoins);
                    Debugger.Print($"[PurchaseOffer] Предмет куплен за {priceCoins} монет.");
                }
                else
                {
                    // Не хватает монет - рассчитываем стоимость докупки
                    int coinsNeeded = priceCoins - avatar.Gold;
                    int gemsNeeded = CalculateGemsForCoins(coinsNeeded);

                    Debugger.Print($"[PurchaseOffer] Недостаточно монет. Нужно: {priceCoins}, Есть: {avatar.Gold}");
                    Debugger.Print($"[PurchaseOffer] Требуется докупить {coinsNeeded} монет за {gemsNeeded} гемов");

                    if (avatar.Diamonds < gemsNeeded)
                    {
                        Debugger.Print($"[PurchaseOffer] Недостаточно гемов для докупки монет. Нужно: {gemsNeeded}, Есть: {avatar.Diamonds}");
                        return -2; // Недостаточно гемов
                    }

                    // Списываем все имеющиеся монеты и нужное количество гемов
                    avatar.Gold = 0; // Все имеющиеся монеты уходят
                    avatar.UseDiamonds(gemsNeeded);
                    Debugger.Print($"[PurchaseOffer] Предмет куплен: {priceCoins} монет (докупка за {gemsNeeded} гемов).");
                }
            }
            else
            {
                Debugger.Warning($"[PurchaseOffer] Неизвестная валюта: {currency}");
                return -1; // Неверный тип валюты
            }

            // === Выдача предмета ===
            DeliveryUnit unit = new DeliveryUnit(100);
            GatchaDrop reward = new GatchaDrop(dropType);

            if (dropType == 9) // Skin
                reward.SkinGlobalId = dataGlobalId;
            else // Emote or Thumbnail
                reward.DataGlobalId = dataGlobalId;

            reward.Count = 1;
            unit.AddDrop(reward);
            command.DeliveryUnits.Add(unit);
            command.Execute(homeMode);

            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            homeMode.GameListener.SendMessage(message);

            // === Для скинов - дополнительные предметы (эмоции, thumbnails) ===
            if (dropType == 9 && itemData is SkinData skinData)
            {
                GiveAdditionalItemsForSkin(homeMode, skinData, command);
            }

            Debugger.Print($"[PurchaseOffer] Успешная покупка предмета {dataGlobalId} за валюту {currency}");
            return 0;
        }

        /// <summary>
        /// Рассчитывает стоимость в гемах для нужного количества блингов
        /// </summary>
        private int CalculateGemsForBling(int blingNeeded)
        {
            if (blingNeeded <= 0) return 0;
            double gemsRequired = (double)blingNeeded / BLING_PER_GEM;
            return (int)Math.Ceiling(gemsRequired);
        }

        /// <summary>
        /// Рассчитывает стоимость в гемах для нужного количества монет
        /// </summary>
        private int CalculateGemsForCoins(int coinsNeeded)
        {
            if (coinsNeeded <= 0) return 0;
            double gemsRequired = (double)coinsNeeded / COINS_PER_GEM;
            return (int)Math.Ceiling(gemsRequired);
        }

        /// <summary>
        /// Выдача дополнительных предметов (эмоции, thumbnails) для скина
        /// </summary>
        private void GiveAdditionalItemsForSkin(HomeMode homeMode, SkinData skinData, LogicGiveDeliveryItemsCommand command)
        {
            if (skinData == null) return;

            DeliveryUnit unit = new DeliveryUnit(100);

            // Эмоции
            foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
            {
                if (emoteData.Skin == skinData.Name)
                {
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.DataGlobalId = DataTables.Get(DataType.Emote).GetData<EmoteData>(emoteData.Name).GetGlobalId();
                    reward.Count = 1;
                    unit.AddDrop(reward);
                }
            }

            // Thumbnails
            foreach (PlayerThumbnailData playerThumbnailData in DataTables.Get(DataType.PlayerThumbnail).GetDatas())
            {
                if (playerThumbnailData.CatalogPreRequirementSkin == skinData.Name)
                {
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.DataGlobalId = DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(playerThumbnailData.Name).GetGlobalId();
                    reward.Count = 1;
                    unit.AddDrop(reward);
                }
            }

            if (unit.Drops.Count > 0)
            {
                command.DeliveryUnits.Add(unit);
                command.Execute(homeMode);
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command;
                homeMode.GameListener.SendMessage(message);
            }
        }

        public override int GetCommandType()
        {
            return 519;
        }
    }
}