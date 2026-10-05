using IndusBrawl.Laser.Logic.Command.Home;
namespace IndusBrawl.Laser.Logic.Home
{
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Command;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Home.Gatcha;
    using IndusBrawl.Laser.Logic.Home.Items;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Logic.Listener;
    using IndusBrawl.Laser.Logic.Message.Home;
    using IndusBrawl.Laser.Titan.Math;

    public class HomeMode
    {
        private static readonly Random _random = new Random();
        private static readonly object _randomLock = new object();
        
        private static HashSet<int> _usedBrawlersInSession = new HashSet<int>();

        public const int UNLOCKABLE_HEROES_COUNT = 64;

        public readonly LogicGameListener GameListener;

        public ClientHome Home;
        public ClientAvatar Avatar;

        public Action<int> CharacterChanged;

        public HomeMode(ClientHome home, ClientAvatar avatar, LogicGameListener gameListener)
        {
            Home = home;
            Avatar = avatar;

            Home.HomeMode = this;
            Avatar.HomeMode = this;

            GameListener = gameListener;
        }

        public static HomeMode LoadHomeState(LogicGameListener gameListener, ClientHome home, ClientAvatar avatar, EventData[] events)
        {
            home.Events = events;

            HomeMode homeMode = new HomeMode(home, avatar, gameListener);
            homeMode.Enter(DateTime.UtcNow);

            return homeMode;
        }

        private bool GetRandomBrawlerForGatcha(Random rand, DeliveryUnit unit)
        {
            int brawlersCount = UNLOCKABLE_HEROES_COUNT;
            int brawlerId = -1;

            List<int> availableBrawlers = new List<int>();
            for (int i = 0; i < brawlersCount; i++)
            {
                int testBrawlerId = GlobalId.CreateGlobalId(16, i);
                CharacterData data = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(testBrawlerId);
                if (!data.Disabled && !Avatar.HasHero(testBrawlerId) && !_usedBrawlersInSession.Contains(testBrawlerId))
                {
                    availableBrawlers.Add(testBrawlerId);
                }
            }

            if (availableBrawlers.Count > 0)
            {
                brawlerId = availableBrawlers[rand.Next(0, availableBrawlers.Count)];
                
                _usedBrawlersInSession.Add(brawlerId);
                
                GatchaDrop drop = new GatchaDrop(1);
                drop.DataGlobalId = brawlerId;
                drop.Count = 1;
                unit.AddDrop(drop);
                
                return true;
            }

            return false;
        }

        private bool GetRandomBrawlerForGatchaWithTracking(Random rand, DeliveryUnit unit, HashSet<int> usedBrawlersInBox)
        {
            int brawlersCount = UNLOCKABLE_HEROES_COUNT;
            int brawlerId = -1;

            List<int> availableBrawlers = new List<int>();
            for (int i = 0; i < brawlersCount; i++)
            {
                int testBrawlerId = GlobalId.CreateGlobalId(16, i);
                CharacterData data = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(testBrawlerId);
                if (!data.Disabled && !Avatar.HasHero(testBrawlerId) && !usedBrawlersInBox.Contains(testBrawlerId) && !_usedBrawlersInSession.Contains(testBrawlerId))
                {
                    availableBrawlers.Add(testBrawlerId);
                }
            }

            if (availableBrawlers.Count > 0)
            {
                brawlerId = availableBrawlers[rand.Next(0, availableBrawlers.Count)];
                
                _usedBrawlersInSession.Add(brawlerId);
                
                GatchaDrop drop = new GatchaDrop(1);
                drop.DataGlobalId = brawlerId;
                drop.Count = 1;
                unit.AddDrop(drop);
                usedBrawlersInBox.Add(brawlerId);
                
                return true;
            }

            return false;
        }

        public void SimulateGatcha(DeliveryUnit unit)
        {
            _usedBrawlersInSession.Clear();
            Avatar.RollsSinceGoodDrop++;
            Random rand;
            lock (_randomLock)
            {
                rand = new Random(_random.Next());
            }

            int unlockedBrawlersCount = Avatar.GetUnlockedHeroesCount();

            List<int> powerPoints = new List<int>();
            if (unit.Type == 10)
            {
                int brawlerChance = 12;
                if (unlockedBrawlersCount < 4)
                {
                    brawlerChance = 25;
                }
                else if (unlockedBrawlersCount < 6)
                {
                    brawlerChance = 17;
                }

                bool isBrawler = rand.Next(0, 100) < brawlerChance;
                if (isBrawler)
                {
                    isBrawler = GetRandomBrawlerForGatcha(rand, unit);
                    if (!isBrawler)
                    {
                        GatchaDrop coinsDropInner = new GatchaDrop(7);
                        coinsDropInner.Count = rand.Next(15, 40);
                        unit.AddDrop(coinsDropInner);
                    }
                }

                if (!isBrawler)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        List<Hero> unlockedHeroes = Avatar.Heroes.Where(h => h.PowerPoints < 1410).ToList();
                        if (unlockedHeroes.Count == 0) break;
                        bool heroValid = false;
                        int generateAttempts = 0;
                        int idx = -1;
                        while (!heroValid && generateAttempts < 10)
                        {
                            generateAttempts++;
                            idx = rand.Next(unlockedHeroes.Count);
                            heroValid = unlockedHeroes[idx].PowerPoints < 1410;
                            if (heroValid)
                            {
                                GatchaDrop drop = new GatchaDrop(6);
                                drop.DataGlobalId = unlockedHeroes[idx].CharacterId;
                                if (powerPoints.Contains(drop.DataGlobalId))
                                {
                                    continue;
                                }
                                powerPoints.Add(drop.DataGlobalId);
                                drop.Count = LogicMath.Min(rand.Next(5, 25), (1410 - unlockedHeroes[idx].PowerPoints));
                                unit.AddDrop(drop);
                            }
                        }
                    }
                }
            }
            else if (unit.Type == 10)
            {
                GatchaDrop coinsDrop = new GatchaDrop(7);
                coinsDrop.Count = rand.Next(322, 600);
                unit.AddDrop(coinsDrop);

                for (int i = 0; i < 5; i++)
                {
                    List<Hero> unlockedHeroes = Avatar.Heroes.Where(h => h.PowerPoints < 1410).ToList();
                    if (unlockedHeroes.Count == 0) break;
                    bool heroValid = false;
                    int generateAttempts = 0;
                    int idx = -1;
                    while (!heroValid && generateAttempts < 10)
                    {
                        generateAttempts++;
                        idx = rand.Next(unlockedHeroes.Count);
                        heroValid = unlockedHeroes[idx].PowerPoints < 1410;
                        if (heroValid)
                        {
                            GatchaDrop drop = new GatchaDrop(6);
                            drop.DataGlobalId = unlockedHeroes[idx].CharacterId;
                            if (powerPoints.Contains(drop.DataGlobalId))
                            {
                                continue;
                            }
                            powerPoints.Add(drop.DataGlobalId);
                            drop.Count = LogicMath.Min(rand.Next(40, 80), (1410 - unlockedHeroes[idx].PowerPoints));
                            unit.AddDrop(drop);
                        }
                    }
                }

                int brawlerChance = 30;
                if (unlockedBrawlersCount < 4)
                {
                    brawlerChance = 47;
                }
                else if (unlockedBrawlersCount < 6)
                {
                    brawlerChance = 37;
                }

                bool isBrawler = rand.Next(0, 100) < brawlerChance;
                if (isBrawler)
                {
                    isBrawler = GetRandomBrawlerForGatcha(rand, unit);
                    if (!isBrawler)
                    {
                        GatchaDrop coinsDropAlt = new GatchaDrop(7);
                        coinsDropAlt.Count = rand.Next(322, 600);
                        unit.AddDrop(coinsDropAlt);
                    }
                }
            }
            else if (unit.Type == 13)
            {
                // 1. 300-900 монет
                GatchaDrop coinsDrop = new GatchaDrop(7);
                coinsDrop.Count = rand.Next(100, 501) * 3;
                unit.AddDrop(coinsDrop);

                // 2. 30-90 кредитов
                GatchaDrop creditsDrop = new GatchaDrop(22);
                creditsDrop.Count = rand.Next(10, 51) * 3;
                unit.AddDrop(creditsDrop);

                // 3. 150-300 блингов
                GatchaDrop blingDrop = new GatchaDrop(25);
                blingDrop.Count = rand.Next(50, 101) * 3;
                unit.AddDrop(blingDrop);

                // 4. 600-900 очков силы одному случайному герою
                List<Hero> unlockedHeroesPP = Avatar.Heroes.Where(h => h.PowerPoints < 1410).ToList();
                if (unlockedHeroesPP.Count > 0)
                {
                    int idx = rand.Next(unlockedHeroesPP.Count);
                    GatchaDrop powerPointsDrop = new GatchaDrop(6);
                    powerPointsDrop.DataGlobalId = unlockedHeroesPP[idx].CharacterId;
                    powerPointsDrop.Count = LogicMath.Min(rand.Next(200, 301) * 3, (1410 - unlockedHeroesPP[idx].PowerPoints));
                    unit.AddDrop(powerPointsDrop);
                }

                // 5. 3-15 гемов (бонус, шанс 50%)
                if (rand.Next(0, 100) < 50)
                {
                    int count = rand.Next(1, 10) * 3;
                    GatchaDrop drop = new GatchaDrop(8);
                    drop.Count = count;
                    unit.AddDrop(drop);
                }

                // 6. 55% шанс сверхредкий скин
                if (rand.Next(0, 100) < 55)
                {
                    int skinGlobalId = StarrDrop.StarrDrop.GetRandomSuperRareSkin();
                    if (!Home.UnlockedSkins.Contains(skinGlobalId))
                    {
                        GatchaDrop skinDrop = new GatchaDrop(9);
                        skinDrop.SkinGlobalId = skinGlobalId;
                        unit.AddDrop(skinDrop);
                    }
                }

                // 7. Логика выдачи бравлеров (оставить как была)
                for (int i = 0; i < 8; i++)
                {
                    List<Hero> unlockedHeroes = Avatar.Heroes.Where(h => h.PowerPoints < 1410).ToList();
                    if (unlockedHeroes.Count == 0) break;
                    bool heroValid = false;
                    int generateAttempts = 0;
                    int idx = -1;
                    while (!heroValid && generateAttempts < 10)
                    {
                        generateAttempts++;
                        idx = rand.Next(unlockedHeroes.Count);
                        heroValid = unlockedHeroes[idx].PowerPoints < 1410;
                        if (heroValid)
                        {
                            GatchaDrop drop = new GatchaDrop(6);
                            drop.DataGlobalId = unlockedHeroes[idx].CharacterId;
                            if (powerPoints.Contains(drop.DataGlobalId))
                            {
                                continue;
                            }
                            powerPoints.Add(drop.DataGlobalId);
                            drop.Count = LogicMath.Min(rand.Next(10, 50), (1410 - unlockedHeroes[idx].PowerPoints));
                            unit.AddDrop(drop);
                        }
                    }
                }
            }
            else if (unit.Type == 11)
            {
                // Новый дроп мегаящика
                // 1. 100-300 монет
                GatchaDrop coinsDrop = new GatchaDrop(7);
                coinsDrop.Count = rand.Next(100, 401);
                unit.AddDrop(coinsDrop);

                // 2. 10-30 кредитов
                GatchaDrop creditsDrop = new GatchaDrop(22);
                creditsDrop.Count = rand.Next(10, 31);
                unit.AddDrop(creditsDrop);

                // 3. 50-100 блингов
                GatchaDrop blingDrop = new GatchaDrop(25);
                blingDrop.Count = rand.Next(50, 101);
                unit.AddDrop(blingDrop);

                // 3.1. 200-300 очков силы одному случайному герою
                List<Hero> unlockedHeroesPP = Avatar.Heroes.Where(h => h.PowerPoints < 1410).ToList();
                if (unlockedHeroesPP.Count > 0)
                {
                    int idx = rand.Next(unlockedHeroesPP.Count);
                    GatchaDrop powerPointsDrop = new GatchaDrop(6);
                    powerPointsDrop.DataGlobalId = unlockedHeroesPP[idx].CharacterId;
                    powerPointsDrop.Count = LogicMath.Min(rand.Next(200, 301), (1410 - unlockedHeroesPP[idx].PowerPoints));
                    unit.AddDrop(powerPointsDrop);
                }

                // 4. 40% шанс редкий скин
                if (rand.Next(0, 100) < 40)
                {
                    int skinGlobalId = StarrDrop.StarrDrop.GetRandomRareSkin();
                    if (!Home.UnlockedSkins.Contains(skinGlobalId))
                    {
                        GatchaDrop skinDrop = new GatchaDrop(9);
                        skinDrop.SkinGlobalId = skinGlobalId;
                        unit.AddDrop(skinDrop);
                    }
                }

                // 5. 20% шанс персонаж
                if (rand.Next(0, 100) < 20)
                {
                    bool brawler = GetRandomBrawlerForGatcha(rand, unit);
                }

                // 6. Бонус: гемы 1-5 с шансом 50%
                if (rand.Next(0, 100) < 50)
                {
                    int count = rand.Next(1, 6);
                    GatchaDrop drop = new GatchaDrop(8);
                    drop.Count = count;
                    unit.AddDrop(drop);
                }
            }
            else if (unit.Type == 12)
            {
                // 1. 300-900 монет
                var coinsDrop12 = new GatchaDrop(7);
                coinsDrop12.Count = rand.Next(100, 301) * 3;
                unit.AddDrop(coinsDrop12);

                // 2. 30-90 кредитов
                var creditsDrop12 = new GatchaDrop(22);
                creditsDrop12.Count = rand.Next(10, 31) * 3;
                unit.AddDrop(creditsDrop12);

                // 3. 150-300 блингов
                var blingDrop12 = new GatchaDrop(25);
                blingDrop12.Count = rand.Next(50, 101) * 3;
                unit.AddDrop(blingDrop12);

                // 4. 600-900 очков силы одному случайному герою
                var unlockedHeroesPP12 = Avatar.Heroes.Where(h => h.PowerPoints < 1410).ToList();
                if (unlockedHeroesPP12.Count > 0)
                {
                    int idx = rand.Next(unlockedHeroesPP12.Count);
                    var powerPointsDrop12 = new GatchaDrop(6);
                    powerPointsDrop12.DataGlobalId = unlockedHeroesPP12[idx].CharacterId;
                    powerPointsDrop12.Count = LogicMath.Min(rand.Next(200, 301) * 3, (1410 - unlockedHeroesPP12[idx].PowerPoints));
                    unit.AddDrop(powerPointsDrop12);
                }

                // 5. 3-15 гемов (бонус, шанс 50%)
                if (rand.Next(0, 100) < 50)
                {
                    int count = rand.Next(1, 6) * 3;
                    var gemsDrop12 = new GatchaDrop(8);
                    gemsDrop12.Count = count;
                    unit.AddDrop(gemsDrop12);
                }

                // 6. 55% шанс сверхредкий скин
                if (rand.Next(0, 100) < 55)
                {
                    int skinGlobalId = StarrDrop.StarrDrop.GetRandomSuperRareSkin();
                    if (!Home.UnlockedSkins.Contains(skinGlobalId))
                    {
                        var skinDrop12 = new GatchaDrop(9);
                        skinDrop12.SkinGlobalId = skinGlobalId;
                        unit.AddDrop(skinDrop12);
                    }
                }

                // 7. Логика выдачи бравлеров (как была)
                int brawlerChance = 22;
                if (unlockedBrawlersCount < 4)
                {
                    brawlerChance = 40;
                }
                else if (unlockedBrawlersCount < 6)
                {
                    brawlerChance = 30;
                }
                bool isBrawler = rand.Next(0, 100) < brawlerChance;
                if (isBrawler)
                {
                    isBrawler = GetRandomBrawlerForGatcha(rand, unit);
                    if (!isBrawler)
                    {
                        var coinsDropAlt2 = new GatchaDrop(7);
                        coinsDropAlt2.Count = rand.Next(60, 250);
                        unit.AddDrop(coinsDropAlt2);
                    }
                }
            }
        }

        public void Enter(DateTime dateTime)
        {
            Home.HomeVisited();
        }

        public void ClientTurnReceived(int tick, int checksum, List<Command> commands)
        {
            foreach (Command command in commands)
            {
                if (command.Execute(this) != 0)
                {
                    OutOfSyncMessage outOfSync = new OutOfSyncMessage();
                    GameListener.SendMessage(outOfSync);
                }
            }
            Home.Tick();
        }
        // Новый метод для выдачи наград с симуляцией Gatcha
        public void GiveDeliveryItemsWithGatcha(OfferBundle offer)
        {
            var command = new LogicGiveDeliveryItemsCommand();
            int count = offer.Items.Count;
            for (int x = 0; x < count; x++)
            {
                int unitId = 100;
                if (offer.Items[x] != null)
                {
                    unitId = offer.Items[x].SkinDataId != 0 ? offer.Items[x].SkinDataId : (offer.Items[x].ItemDataId != 0 ? offer.Items[x].ItemDataId : 100);
                }
                DeliveryUnit unit = new DeliveryUnit(unitId);
                SimulateGatcha(unit);
                if (x + 1 != count)
                {
                    command.Execute(this);
                }
                command.DeliveryUnits.Add(unit);
            }
            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            GameListener.SendMessage(message);
        }
    }
}