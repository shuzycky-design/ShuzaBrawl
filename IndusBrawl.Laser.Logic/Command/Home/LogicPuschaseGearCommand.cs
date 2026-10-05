// LogicPurchaseGearCommand.cs
using System.Collections.Generic;
using IndusBrawl.Laser.Logic.Data;
using IndusBrawl.Laser.Logic.Home;
using IndusBrawl.Laser.Logic.Home.Structures;
using IndusBrawl.Laser.Titan.DataStream;
using IndusBrawl.Laser.Logic.Avatar.Structures;
using IndusBrawl.Laser.Titan.Debug;

namespace IndusBrawl.Laser.Logic.Command.Home
{
    public class LogicPuschaseGearCommand : Command
    {
        // Цены на Gear по их InstanceId
        readonly Dictionary<int, int> GearPrices = new Dictionary<int, int>
        {
            { 0, 1000 }, { 1, 1000 }, { 2, 1000 }, { 3, 1000 }, { 4, 1000 },
            { 5, 1500 }, { 6, 1500 }, { 7, 2000 }, { 8, 2000 }, { 9, 2000 },
            { 10, 2000 }, { 11, 2000 }, { 12, 2000 }, { 13, 2000 }, { 14, 1500 },
            { 15, 2000 }, { 16, 2000 }, { 17, 1000 }, { 18, 2000 }
        };

        // Список бойцов, на которых можно покупать Gear
        // Если GearID отсутствует — можно на всех (в рамках глобальных правил)
        readonly Dictionary<int, List<int>> GearAllowedBrawlers = new Dictionary<int, List<int>>
        {
            { 5, new List<int> { 46, 56, 53, 14, 3, 1, 27, 50, 40, 4 } },
            { 6, new List<int> { 51, 41, 59, 2, 36, 58, 43, 37, 10, 34 } },
            { 7, new List<int> { 22 } },
            { 8, new List<int> { 21 } },
            { 9, new List<int> { 12 } },
            { 10, new List<int> { 5 } },
            { 11, new List<int> { 23 } },
            { 12, new List<int> { 28 } },
            { 13, new List<int> { 40 } },
            { 14, new List<int> { 7, 8, 31, 17, 19 } },
            { 15, new List<int> { 56 } },
            { 16, new List<int> { 16 } },
            { 18, new List<int> { 11 } }
            
        };

        private int BrawlerID;
        private int GearID;
        private int Slot; // 0 = слот 1, 1 = слот 2

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            try
            {
                stream.ReadVInt(); // 16
                BrawlerID = stream.ReadVInt(); // ID героя

                stream.ReadVInt(); // 62
                GearID = stream.ReadVInt(); // InstanceId Gear'а

                Slot = stream.ReadVInt(); // 0 или 1 — слот
                stream.ReadVInt(); // 0

                Debugger.Print($"[Gear] Decode: BrawlerID={BrawlerID}, GearID={GearID}, Slot={Slot}");
            }
            catch (System.Exception ex)
            {
                Debugger.Print($"[Gear] Ошибка при декодировании: {ex.Message}");
                BrawlerID = -1;
                GearID = -1;
                Slot = -1;
            }
        }

        public override int Execute(HomeMode homeMode)
        {
            try
            {
                Debugger.Print($"[Gear] Execute: Попытка купить Gear {GearID} для героя {BrawlerID}, слот: {Slot}");

                // --- 1. Проверка: корректный ли слот? ---
                if (Slot != 0 && Slot != 1)
                {
                    Debugger.Print($"[Gear] Ошибка: Неверный слот: {Slot}");
                    return -1;
                }

                // --- 2. Глобальные ограничения на BrawlerID ---
                if (BrawlerID < 0 || BrawlerID > 77)
                {
                    Debugger.Print($"[Gear] Ошибка: BrawlerID {BrawlerID} вне диапазона [0; 77].");
                    return -1;
                }

                if (BrawlerID == 33 || BrawlerID == 55)
                {
                    Debugger.Print($"[Gear] Ошибка: Запрещено для героя {BrawlerID}.");
                    return -1;
                }

                // --- 3. Проверка: есть ли Gear в списке цен? ---
                if (!GearPrices.TryGetValue(GearID, out int price))
                {
                    Debugger.Print($"[Gear] Ошибка: Gear {GearID} не в списке цен.");
                    return -1;
                }

                // --- 4. Проверка: разрешён ли Gear на этом бойце? ---
                if (GearAllowedBrawlers.TryGetValue(GearID, out List<int> allowedBrawlers))
                {
                    if (!allowedBrawlers.Contains(BrawlerID))
                    {
                        Debugger.Print($"[Gear] Ошибка: Gear {GearID} не разрешён для героя {BrawlerID}.");
                        return -1;
                    }
                }
                // Если GearID нет в словаре — разрешено (если прошёл глобальные проверки)

                // --- 5. Проверка: существует ли герой? ---
                int globalBrawlerId = GlobalId.CreateGlobalId(16, BrawlerID);
                if (!homeMode.Avatar.HasHero(globalBrawlerId))
                {
                    Debugger.Print($"[Gear] Ошибка: Герой {BrawlerID} не разблокирован.");
                    return -1;
                }

                Hero hero = homeMode.Avatar.GetHero(globalBrawlerId);
                if (hero == null)
                {
                    Debugger.Print("[Gear] Ошибка: Hero object is null.");
                    return -1;
                }

                // --- 6. Уже разблокирован? ---
                if (hero.UnlockedGearInstanceIds.Contains(GearID))
                {
                    Debugger.Print($"[Gear] Gear {GearID} уже разблокирован.");
                    return -1;
                }

                // --- 7. Первая покупка бесплатно? ---
                bool hasFreeGearBeenUsed = homeMode.Avatar.HasUsedFreeGear;
                if (!hasFreeGearBeenUsed)
                {
                    // Разблокируем Gear
                    hero.UnlockedGearInstanceIds.Add(GearID);
                    Debugger.Print($"[Gear] Gear {GearID} разблокирован бесплатно.");

                    // Экипируем в указанный слот
                    if (Slot == 0)
                    {
                        hero.SelectedGearId1 = GearID;
                        Debugger.Print($"[Gear] Экипирован в слот 1: {GearID}");
                    }
                    else
                    {
                        hero.SelectedGearId2 = GearID;
                        Debugger.Print($"[Gear] Экипирован в слот 2: {GearID}");
                    }

                    // Отмечаем, что бесплатная покупка использована
                    homeMode.Avatar.MarkFreeGearAsUsed();
                    Debugger.Print("[Gear] HasUsedFreeGear = true");

                    return 0;
                }

                // --- 8. Основная логика: докупка за гемы ---
                int playerCoins = homeMode.Avatar.Gold;
                int playerGems = homeMode.Avatar.Diamonds;

                int coinsToUse = 0;
                int gemsToUse = 0;

                if (playerCoins >= price)
                {
                    coinsToUse = price;
                }
                else
                {
                    coinsToUse = playerCoins;
                    int coinsMissing = price - coinsToUse;
                    gemsToUse = (coinsMissing + 9) / 10; // 1 гем = 10 монет
                }

                // Проверка: хватает ли гемов?
                if (gemsToUse > playerGems)
                {
                    Debugger.Print($"[Gear] Не хватает гемов: нужно {gemsToUse}, есть {playerGems}");
                    return -1;
                }

                // Списываем ресурсы
                if (coinsToUse > 0 && !homeMode.Avatar.UseGold(coinsToUse))
                {
                    Debugger.Print($"[Gear] Ошибка: не удалось списать {coinsToUse} монет.");
                    return -1;
                }

                if (gemsToUse > 0 && !homeMode.Avatar.UseDiamonds(gemsToUse))
                {
                    // Возвращаем монеты, если гемы не списались
                    if (coinsToUse > 0)
                    {
                        homeMode.Avatar.AddGold(coinsToUse);
                    }
                    Debugger.Print($"[Gear] Ошибка: не удалось списать {gemsToUse} гемов.");
                    return -1;
                }

                // --- 9. Разблокируем Gear ---
                hero.UnlockedGearInstanceIds.Add(GearID);
                Debugger.Print($"[Gear] Gear {GearID} успешно разблокирован.");

                // --- 10. Экипируем в указанный слот ---
                if (Slot == 0)
                {
                    hero.SelectedGearId1 = GearID;
                    Debugger.Print($"[Gear] Gear {GearID} установлен в слот 1");
                }
                else
                {
                    hero.SelectedGearId2 = GearID;
                    Debugger.Print($"[Gear] Gear {GearID} установлен в слот 2");
                }

                Debugger.Print($"[Gear] Gear {GearID} куплен и установлен в слот {Slot + 1}: {coinsToUse} монет + {gemsToUse} гемов.");
                return 0;
            }
            catch (System.Exception ex)
            {
                Debugger.Print($"[Gear] Исключение: {ex.Message}\n{ex.StackTrace}");
                return -1;
            }
        }

        public override int GetCommandType()
        {
            return 558; // Тип команды — покупка Gear
        }
    }
}