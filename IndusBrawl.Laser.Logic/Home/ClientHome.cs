using System.Linq;

namespace IndusBrawl.Laser.Logic.Home
{
    using System;
    using System.Security.Cryptography;
    using Newtonsoft.Json;
    using System.Numerics;
    using IndusBrawl.Laser.Logic.Command.Home;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Helper;
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

    [JsonObject(MemberSerialization.OptIn)]
    public class ClientHome
    {
        public const int DAILYOFFERS_COUNT = 6;

        public static readonly int[] GoldPacksPrice = new int[]
        {
            20, 50, 140, 280
        };

        public static readonly int[] GoldPacksAmount = new int[]
        {
            150, 400, 1200, 2600
        };

        [JsonProperty] public long HomeId;
        [JsonProperty] public int ThumbnailId;
        [JsonProperty] public int NameColorId;
        [JsonProperty] public int[] CharacterIds;
        [JsonProperty] public int FavouriteCharacter;
        public int CharacterId => CharacterIds[0];
        [JsonProperty] public int TrophiesReward;
        [JsonProperty] public int TokenReward;
        [JsonProperty] public int StarTokenReward;
        [JsonProperty] public BigInteger BrawlPassProgress;
        [JsonProperty] public BigInteger PremiumPassProgress;
        [JsonProperty] public BigInteger BrawlPassPlusProgress;
        [JsonProperty] public int BrawlPassTokens;
        [JsonProperty] public bool HasPremiumPass = false;
        [JsonProperty] public bool HasPremiumPassPlus = false;
        [JsonProperty] public List<int> UnlockedEmotes;
        [JsonProperty] public List<int> UnlockedThumbnails;
        [JsonProperty] public List<int> UnlockedTituls;
        [JsonProperty] public NotificationFactory NotificationFactory;
        [JsonProperty] public List<int> UnlockedSkins;
        [JsonProperty] public int TrophyRoadProgress;
        [JsonProperty] public int EventId;
        [JsonProperty] public List<PlayerMap> PlayerMaps = new List<PlayerMap>();
        [JsonProperty] public BattleCard DefaultBattleCard;
        [JsonProperty] public int PreferredThemeId;
        [JsonProperty] public int RewardType;
        [JsonProperty] public int RewardCount;
        [JsonProperty] public int StarrDropRarity;
        [JsonProperty] public int TokenDoublers;
        [JsonProperty] public int BattleTokens;

        // ignore SerializeObject lore
        [JsonIgnore] public List<OfferBundle> OfferBundles;
        [JsonIgnore] public StarrDrop.StarrDrop StarrDrop;
        [JsonIgnore] public EventData[] Events;
        [JsonProperty] public Quests Quests;
        [JsonProperty] public bool CoinsEventEnabled { get; set; } = false;
[JsonProperty] public int CoinsReward { get; set; } = 0;
        [JsonIgnore] public BattleLogEntry.BattleLog BattleLogs;
        [JsonProperty] public Dictionary<int, int> PlayerSelectedEmotes = new Dictionary<int, int>(); // почему тут проперти
        public PlayerThumbnailData Thumbnail => DataTables.Get(DataType.PlayerThumbnail).GetDataByGlobalId<PlayerThumbnailData>(ThumbnailId);
        public NameColorData NameColor => DataTables.Get(DataType.NameColor).GetDataByGlobalId<NameColorData>(NameColorId);

        public HomeMode HomeMode;

        [JsonProperty] public DateTime LastVisitHomeTime;
        [JsonProperty] public DateTime AddBattleTokensTime;
        [JsonProperty] public List<string> OffersClaimed;
        [JsonProperty] public int[] UnlockStarRoad;
        [JsonProperty] public int RecruitTokens;
        [JsonProperty] public int RecruitBrawler;

        [JsonProperty] public int RankedSoloRank;
        [JsonProperty] public int RankedSoloMaxRank;
        [JsonProperty] public int RankedTrioRank;
        [JsonProperty] public int RankedTrioMaxRank;
        [JsonProperty] public int RankedSoloProgress;
        [JsonProperty] public int RankedTrioProgress;
        [JsonProperty] public int RankedSoloMaxProgress;
        [JsonProperty] public int RankedTrioMaxProgress;


        public ClientHome()
        {
            ThumbnailId = GlobalId.CreateGlobalId(28, 0);
            NameColorId = GlobalId.CreateGlobalId(43, 0);
            CharacterIds = new int[] { GlobalId.CreateGlobalId(16, 0), GlobalId.CreateGlobalId(16, 1), GlobalId.CreateGlobalId(16, 2) };
            FavouriteCharacter = GlobalId.CreateGlobalId(16, 0);
            PlayerSelectedEmotes.Add(1, 28);
            PlayerSelectedEmotes.Add(2, 28);
            PlayerSelectedEmotes.Add(3, 28);
            PlayerSelectedEmotes.Add(4, 28);
            PlayerSelectedEmotes.Add(5, 85 - 3);
this.UnlockStarRoad = new int[]
{
    16000008, 16000010, 16000007, 16000013, 16000034, 16000003, 16000006, 16000014,
    16000030, 16000001, 16000009, 16000027, 16000029, 16000002, 16000022, 16000025,
    16000020, 16000045, 16000011, 16000017, 16000021, 16000024, 16000018, 16000016,
    16000032, 16000023, 16000012, 16000005, 16000019, 16000050, 16000042, 16000004,
    16000043, 16000028, 16000015, 16000026, 16000061, 16000031, 16000047, 16000048,
    16000040, 16000052, 16000036, 16000063, 16000058, 16000037, 16000069, 16000064,
    16000038, 16000035, 16000067, 16000070, 16000039, 16000071, 16000076, 16000073,
    16000046, 16000041, 16000051, 16000044, 16000053, 16000049, 16000060, 16000054,
    16000065, 16000056, 16000068, 16000057, 16000072, 16000059, 16000077, 16000062,
    16000066, 16000075, 16000074, 16000198, 16000199, 16000200
};
            OfferBundles = new List<OfferBundle>();
            UnlockedSkins = new List<int>();

            UnlockedEmotes = new List<int>();
            UnlockedTituls = new List<int>();
            UnlockedThumbnails = new List<int>();
            LastVisitHomeTime = DateTime.UnixEpoch;
            OffersClaimed = new List<string>();
            TrophyRoadProgress = 1;
            TokenDoublers = 0;
            RecruitTokens = 0;
            RecruitBrawler = 1;
            HasPremiumPass = true;
            HasPremiumPassPlus = true;
            BrawlPassProgress = 1;
            PremiumPassProgress = 1;
            EventId = 1;
            NotificationFactory = new NotificationFactory();
            UnlockedEmotes = new List<int>();
            DefaultBattleCard = new BattleCard();
            StarrDrop = new StarrDrop.StarrDrop(HomeMode);
            PreferredThemeId = -1;
            BattleLogs = new BattleLogEntry.BattleLog();
            RankedSoloRank = 1;
            RankedSoloMaxRank = 1;
            RankedTrioRank = 1;
            RankedTrioMaxRank = 1;
        }

        public void HomeVisited()
        {
    if ((UnlockStarRoad == null || UnlockStarRoad.Length == 0) && RecruitTokens > 0)
    {
        HomeMode.Avatar.ConvertRecruitTokensToFame(RecruitTokens);
        RecruitTokens = 0;
    }
            if (HomeMode?.Avatar?.Heroes != null && HomeMode.Avatar.Heroes.Count > 0)
            {
                var uniqueHeroes = new Dictionary<int, Hero>(); // Ключ - CharacterId
                var heroesToRemove = new List<Hero>();
                var processedCharacterIds = new HashSet<int>(); // Для отслеживания уже добавленных

                foreach (var hero in HomeMode.Avatar.Heroes)
                {
                    if (hero == null) continue; // На всякий случай проверим на null

                    if (!processedCharacterIds.Contains(hero.CharacterId))
                    {
                        // Первый раз встречаем этого бойца - сохраняем
                        uniqueHeroes[hero.CharacterId] = hero;
                        processedCharacterIds.Add(hero.CharacterId);
                    }
                    else
                    {
                        // Дубликат найден
                        Debugger.Warning($"[ClientHome.Encode] Найден дубликат бойца CharacterId={hero.CharacterId}. Будет удален. Hero object hash: {hero.GetHashCode()}");
                        heroesToRemove.Add(hero);
                        // Здесь можно добавить логику восстановления ресурсов, если дубликат имел уникальные данные,
                        // но обычно это просто "лишние" записи.
                    }
                }

                // Удаляем дубликаты из основного списка
                if (heroesToRemove.Count > 0)
                {
                    foreach (var heroToRemove in heroesToRemove)
                    {
                        HomeMode.Avatar.Heroes.Remove(heroToRemove);
                    }
                    Debugger.Print($"[ClientHome.Encode] Удалено {heroesToRemove.Count} дубликатов бойцов.");
                    // ВАЖНО: Так как мы модифицируем список, из которого итерируемся, 
                    // использование отдельного списка для удаления безопасно.
                }
            }
            OfferBundles.RemoveAll(bundle => bundle.IsTrue);

    if (HomeMode?.Avatar == null)
    {
        Debugger.Print("[WARN] HomeMode or Avatar is null in HomeVisited()");
        return;
    }

    // ✅ Фильтруем UnlockStarRoad: оставляем только тех, кого ещё нет
    var filteredList = new List<int>();
    foreach (int globalId in this.UnlockStarRoad)
    {
        if (!HomeMode.Avatar.HasHero(globalId))
        {
            filteredList.Add(globalId);
        }
    }

    // ✅ Перезаписываем UnlockStarRoad — только недоступные
    this.UnlockStarRoad = filteredList.ToArray();

            

// 599 - пеньята Эш из силовой


GenerateOffer(
    new DateTime(2025, 1, 15, 0, 0, 0),     // Начало акции
    new DateTime(2026, 4, 1, 23, 59, 59),  // Конец акции
    1,                                       // Количество - 1 боец
    200,                                      // BrawlerID: 8-Бит = 27 (16000027 - 16000000 = 27)
    0,                                       // Extra параметр (не используется для бойцов)
    ShopItem.GuaranteedHero,                 // Тип товара - гарантированный боец
    0,                                     // Цена - 299 гемов
    349,                                     // Старая цена - 699 гемов (скидка более 50%)
    0,                                       // Валюта: 0 = гемы
    "8bit_melody349sale_offe6rksksfree",                       // Уникальный ID оффера
    "НОВЫЙ БОЕЦ!",                   // Название в магазине
    "offer_bgr_hindu",                        // Фон оффера (можно выбрать другой)
    0, 0, false,                              // Дополнительные параметры
    false, false, 0,
    0, 0, 0, 0, 0
);



GenerateOffer(
    new DateTime(2025, 12, 01, 0, 0, 0),
    new DateTime(2026, 03, 15, 11, 0, 0),
    1,                      // Count - всегда 1 для пина
    0,                      // BrawlerID (0 - не привязано к бойцу)
    331,                   // SkinDataId - ID пина (Extra параметр для Emote)
    ShopItem.Emote,         // Тип предмета - пин
    0,                      // Цена (0 - бесплатно)
    0,                      // Старая цена
    0,                      // Валюта (0-гемы, но цена 0)
    "emote_v_telephoneigraet_331_comp",  // Уникальный ID оффера
    "КОМПЕНСАЦИЯ!",      // Название
    "offer_bgr_phoenix",  // Фон оффера
    0, 0, false,             // Дополнительные параметры
    false, false, 0,
    0, 0, 0, 0, 0
);







GenerateOffer(
    new DateTime(2025, 1, 15, 0, 0, 0),     // Начало акции
    new DateTime(2026, 3, 11, 11, 0, 0),  // Конец акции
    1,                                       // Количество - 1 боец
    26,                                      // BrawlerID: 8-Бит = 27 (16000027 - 16000000 = 27)
    0,                                       // Extra параметр (не используется для бойцов)
    ShopItem.GuaranteedHero,                 // Тип товара - гарантированный боец
    0,                                     // Цена - 299 гемов
    0,                                     // Старая цена - 699 гемов (скидка более 50%)
    0,                                       // Валюта: 0 = гемы
    "event_1200sub_bibi",                       // Уникальный ID оффера
    "1200!",                   // Название в магазине
    "offer_bgr_hindu",                        // Фон оффера (можно выбрать другой)
    0, 0, false,                              // Дополнительные параметры
    false, false, 0,
    0, 0, 0, 0, 0
);


GenerateOffer(
            new DateTime(2026, 03, 07, 0, 0, 0), new DateTime(2026, 03, 10, 0, 0, 0),
            1, 999, 146, ShopItem.Skin,
            0, 0, 0,
            "geroinyabibi_2026_indusbrawl", "С 8 МАРТА!", "offer_bgr_giftshop",
            0, 0, false,
            false, false, 0,
            0, 0, 0, 0, 0
            );        

GenerateOffer(
    new DateTime(2024, 12, 01, 12, 0, 0),    // Начало акции
    new DateTime(2026, 04, 15, 11, 0, 0),   // Конец акции
    1000,                                    // Количество монет
    0,                                        // ID бойца
    0,                                        // Extra параметр
    ShopItem.Coin,                            // Тип товара - монеты
    0,                                       // Цена - 99 гемов
    0,                                      // Старая цена (зачёркнутая) - скидка 50%
    0,                                        // Валюта (0-гемы)
    "coins_1000_offer_indus_free",                        // Уникальный ID
    "БЕСПЛАТНО!",                          // Название
    "offer_coins",                            // Фон оффера
    0, 0, false,                               // Дополнительные параметры
    false, false, 0,
    0, 0, 0, 0, 0
);

GenerateOffer(
            new DateTime(2026, 02, 23, 0, 0, 0), new DateTime(2026, 03, 28, 0, 0, 0),
            1, 999, 466, ShopItem.Skin,
            0, 0, 0,
            "perviydenvesny_2026_indusbrawl", "С ПЕРВЫМ ДНЁМ\nВЕСНЫ!", "offer_bgr_jungle",
            0, 0, false,
            false, false, 0,
            0, 0, 0, 0, 0
            );        
            
            
            
            
GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0), new DateTime(2026, 04, 15, 11, 0, 0),
    4000, 0, 3, ShopItem.PowerPoint,
    0, 0, 0,
    "power_poin8t_pack4000", "4000 ОЧКОВ СИЛЫ!", "offer_power",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);




GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0), new DateTime(2026, 04, 15, 11, 0, 0),
    10000, 0, 4, ShopItem.Coin,
    0, 0, 0,
    "coin_bonan0zapack", "10К МОНЕТ!", "offer_coins",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);



GenerateOffer(
    new DateTime(2024, 12, 01, 12, 0, 0),    // Начало акции
    new DateTime(2026, 04, 15, 11, 0, 0),   // Конец акции
    100,                                      // Количество гемов
    0,                                        // ID бойца (0 - не привязано к бойцу)
    0,                                        // Extra параметр
    ShopItem.Gems,                            // Тип товара - гемы
    1000,                                     // Цена - 1000 монет
    2000,                                     // Старая цена (зачёркнутая)
    1,                                        // Валюта (1-монеты, 0-гемы)
    "gems_100_for_coins_indus_9373939393993",                     // Уникальный ID оффера
    "100 ГЕМОВ ЗА МОНЕТЫ!",                   // Название
    "bg_campaign_bunny_brigade",                      // Фон оффера
    0, 0, false,                               // Дополнительные параметры
    false, false, 0,
    0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 12, 01, 12, 0, 0),    // Начало акции
    new DateTime(2026, 04, 15, 11, 0, 0),   // Конец акции
    100000,                                   // Количество блингов
    0,                                        // ID бойца
    4,                                        // Extra параметр (для блингов)
    ShopItem.Bling,                           // Тип товара - блинги
    199,                                      // Цена - 199 гемов
    399,                                      // Старая цена (зачёркнутая) - скидка 50%
    0,                                        // Валюта (0-гемы)
    "bling_100k_offer_indus_93999333939393",                       // Уникальный ID
    "100 000 БЛИНГОВ!",                       // Название
    "offer_bling",                            // Фон оффера
    0, 0, false,                               // Дополнительные параметры
    false, false, 0,
    0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 12, 01, 12, 0, 0),    // Начало акции
    new DateTime(2026, 04, 15, 11, 0, 0),   // Конец акции
    250,                                      // Количество очков силы
    0,                                        // ID бойца
    0,                                        // Extra параметр
    ShopItem.PowerPoint,                      // Тип товара - очки силы
    9,                                       // Цена - 19 гемов
    19,                                       // Старая цена (зачёркнутая) - скидка 50%
    0,                                        // Валюта (0-гемы)
    "powerpoints_250_offer_indus_99993339",                  // Уникальный ID
    "250 ОЧКОВ СИЛЫ!",                        // Название
    "offer_power",                            // Фон оффера
    0, 0, false,                               // Дополнительные параметры
    false, false, 0,
    0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2024, 12, 01, 12, 0, 0),    // Начало акции
    new DateTime(2026, 04, 15, 11, 0, 0),   // Конец акции
    25000,                                    // Количество монет
    0,                                        // ID бойца
    0,                                        // Extra параметр
    ShopItem.Coin,                            // Тип товара - монеты
    99,                                       // Цена - 99 гемов
    199,                                      // Старая цена (зачёркнутая) - скидка 50%
    0,                                        // Валюта (0-гемы)
    "coins_25k_offer_indus_93939",                        // Уникальный ID
    "25 000 МОНЕТ!",                          // Название
    "offer_coins",                            // Фон оффера
    0, 0, false,                               // Дополнительные параметры
    false, false, 0,
    0, 0, 0, 0, 0
);

GenerateOffer(
            new DateTime(2026, 02, 23, 0, 0, 0), new DateTime(2026, 02, 24, 0, 0, 0),
            1, 999, 740, ShopItem.Skin,
            23, 23, 0,
            "s_prazd23zachitnika", "С 23 ФЕВРАЛЯ,\nБОЙЦЫ!", "offer_bgr_enchanted",
            0, 0, false,
            false, false, 0,
            0, 0, 0, 0, 0
            );        
            

GenerateOffer(
            new DateTime(2025, 12, 01, 0, 0, 0), new DateTime(2026, 03, 01, 11, 0, 0),
            1, 999, 500, ShopItem.Skin,
            0, 300, 0,
            "skin_edgartata_id500", "Подарок Любви", "offer_bgr_brawlentines23",
            0, 0, false,
            false, false, 0,
            0, 0, 0, 0, 0
            );            
            
            
            GenerateOffer(
    new DateTime(2025, 1, 15, 0, 0, 0),     // Начало акции
    new DateTime(2036, 3, 1, 23, 59, 59),  // Конец акции
    1,                                       // Количество - 1 боец
    198,                                      // BrawlerID: 8-Бит = 27 (16000027 - 16000000 = 27)
    0,                                       // Extra параметр (не используется для бойцов)
    ShopItem.GuaranteedHero,                 // Тип товара - гарантированный боец
    349,                                     // Цена - 299 гемов
    349,                                     // Старая цена - 699 гемов (скидка более 50%)
    0,                                       // Валюта: 0 = гемы
    "8bit_sale_offe6r78",                       // Уникальный ID оффера
    "НОВЫЙ БОЕЦ!",                   // Название в магазине
    "offer_bgr_hindu",                        // Фон оффера (можно выбрать другой)
    0, 0, false,                              // Дополнительные параметры
    false, false, 0,
    0, 0, 0, 0, 0
);



GenerateOffer(
    new DateTime(2025, 1, 15, 0, 0, 0),     // Начало акции
    new DateTime(2036, 3, 1, 23, 59, 59),  // Конец акции
    1,                                       // Количество - 1 боец
    199,                                      // BrawlerID: 8-Бит = 27 (16000027 - 16000000 = 27)
    0,                                       // Extra параметр (не используется для бойцов)
    ShopItem.GuaranteedHero,                 // Тип товара - гарантированный боец
    349,                                     // Цена - 299 гемов
    349,                                     // Старая цена - 699 гемов (скидка более 50%)
    0,                                       // Валюта: 0 = гемы
    "8bit_sale_offe6r79ksks",                       // Уникальный ID оффера
    "НОВЫЙ БОЕЦ!",                   // Название в магазине
    "offer_bgr_hindu",                        // Фон оффера (можно выбрать другой)
    0, 0, false,                              // Дополнительные параметры
    false, false, 0,
    0, 0, 0, 0, 0
);
            
            
            
            
            GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    131,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    25000,                     // Цена - 29 гемов
    35000,                     // Старая цена (зачёркнутая)
    1,                      // Валюта (0-гемы, 1-монеты)
    "title_delo_masterb8_indusbrawl_offer140ob",     // Уникальный ID оффера
    "Титул: Дело мастера боится!",         // Название в магазине
    "offer_bgr_lny23red",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);


   GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    139,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    4000,                     // Цена - 29 гемов
    5000,                     // Старая цена (зачёркнутая)
    1,                      // Валюта (0-гемы, 1-монеты)
    "title_delo_mast9eiirb7ki8_indusbrawl_offer140ob",     // Уникальный ID оффера
    "Титул: Негр",         // Название в магазине
    "offer_bgr_lny23red",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);
            
            GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    140,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    499,                     // Цена - 29 гемов
    699,                     // Старая цена (зачёркнутая)
    0,                      // Валюта (0-гемы, 1-монеты)
    "title_rastoks_indusbrawl_offer140ob",     // Уникальный ID оффера
    "Титул: Расчленитель токсиков",         // Название в магазине
    "offer_bgr_brawlentines23",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);
            
            GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    178,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    2999,                     // Цена - 29 гемов
    3990,                     // Старая цена (зачёркнутая)
    0,                      // Валюта (0-гемы, 1-монеты)
    "title_samiyglavniyindusb_indusbrawl_offer111pro",     // Уникальный ID оффера
    "Титул: Самый главный Индус",         // Название в магазине
    "offer_bgr_starter",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);
            
            
            GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    182,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    99,                     // Цена - 29 гемов
    149,                     // Старая цена (зачёркнутая)
    0,                      // Валюта (0-гемы, 1-монеты)
    "title_gogol_indusbrawl_offer111pro",     // Уникальный ID оффера
    "Титул: Гоголь",         // Название в магазине
    "offer_bgr_mrbeast",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);



            
            GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    179,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    100,                     // Цена - 29 гемов
    200,                     // Старая цена (зачёркнутая)
    0,                      // Валюта (0-гемы, 1-монеты)
    "title_killer_indusbrawl_offer111pro",     // Уникальный ID оффера
    "Титул: Киллер",         // Название в магазине
    "offer_bgr_bt21",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);


     GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    181,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    1,                     // Цена - 29 гемов
    25,                     // Старая цена (зачёркнутая)
    0,                      // Валюта (0-гемы, 1-монеты)
    "title_nyb_indusbrawl_offer111pro",     // Уникальный ID оффера
    "Титул: Нуб",         // Название в магазине
    "offer_bgr_bt21",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);
            
            GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    180,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    299,                     // Цена - 29 гемов
    549,                     // Старая цена (зачёркнутая)
    0,                      // Валюта (0-гемы, 1-монеты)
    "title_nepogreshim828_indusbrawl_offer111pro",     // Уникальный ID оффера
    "Титул: Непогрешим",         // Название в магазине
    "offer_bgr_bt21",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    183,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    40000,                     // Цена - 29 гемов
    90000,                     // Старая цена (зачёркнутая)
    1,                      // Валюта (0-гемы, 1-монеты)
    "title_zadrot_indusbrawl_offer137ob",     // Уникальный ID оффера
    "Титул: Задрот",         // Название в магазине
    "offer_bgr_lny23red",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    137,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    199,                     // Цена - 29 гемов
    299,                     // Старая цена (зачёркнутая)
    0,                      // Валюта (0-гемы, 1-монеты)
    "title_egoist_indusbrawl_offer137ob",     // Уникальный ID оффера
    "Титул: Золотой",         // Название в магазине
    "offer_bgr_legendary_dark",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    112,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    99,                     // Цена - 29 гемов
    199,                     // Старая цена (зачёркнутая)
    0,                      // Валюта (0-гемы, 1-монеты)
    "title_egoist_indusbrawl_offer112ob",     // Уникальный ID оффера
    "Титул: Рыцарь",         // Название в магазине
    "offer_bgr_legendary_dark",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);



GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    154,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    99,                     // Цена - 29 гемов
    199,                     // Старая цена (зачёркнутая)
    0,                      // Валюта (0-гемы, 1-монеты)
    "title_dedins_indusbrawl_offer112ob",     // Уникальный ID оффера
    "Титул: Дед инсайд",         // Название в магазине
    "offer_bgr_legendary_dark",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);
            
           GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    115,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    99,                     // Цена - 29 гемов
    199,                     // Старая цена (зачёркнутая)
    0,                      // Валюта (0-гемы, 1-монеты)
    "title_egoist_indusbrawl_offer115ob",     // Уникальный ID оффера
    "Титул: Эгоист",         // Название в магазине
    "offer_bgr_legendary_dark",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);
            
            GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 01, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    187,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    90000,                     // Цена - 29 гемов
    90000,                     // Старая цена (зачёркнутая)
    1,                      // Валюта (0-гемы, 1-монеты)
    "title_love187_ultra_indusbrawl",     // Уникальный ID оффера
    "Титул: Ультра Игрок",         // Название в магазине
    "offer_bgr_wasteland",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);



            
            
            
            GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    146,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    500,                     // Цена - 29 гемов
    750,                     // Старая цена (зачёркнутая)
    0,                      // Валюта (0-гемы, 1-монеты)
    "title_korol_indusbrawl_offer146",     // Уникальный ID оффера
    "Титул: Король",         // Название в магазине
    "offer_bgr_enchanted",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);
            
            GenerateOffer(
    new DateTime(2024, 11, 01, 12, 0, 0),    // Начало акции
    new DateTime(2026, 02, 10, 11, 0, 0),   // Конец акции
    100,                                      // Количество гемов
    0,                                        // ID бойца (0 - не привязано к бойцу)
    0,                                        // Extra параметр
    ShopItem.Gems,                            // Тип товара - гемы
    0,                                     // Цена - 1000 монет
    2000,                                     // Старая цена (зачёркнутая)
    0,                                        // Валюта (1-монеты, 0-гемы)
    "sorry_p100",                     // Уникальный ID оффера
    "Извините!",                   // Название
    "offer_bgr_brawlywood",                      // Фон оффера
    0, 0, false,                               // Дополнительные параметры
    false, false, 0,
    0, 0, 0, 0, 0
);
            
            
GenerateOffer(
            new DateTime(2025, 12, 01, 0, 0, 0), new DateTime(2026, 02, 20, 11, 0, 0),
            1, 999, 714, ShopItem.Skin,
            200, 300, 0,
            "varvara_frank_714", "Спасибо!\nНас уже 700!", "",
            0, 0, false,
            false, false, 0,
            0, 0, 0, 0, 0
            );            
            
            
                        
          GenerateOffer(
            new DateTime(2026, 02, 15, 0, 0, 0), new DateTime(2026, 03, 20, 11, 0, 0),
            1, 999, 751, ShopItem.Skin,
            210, 300, 0,
            "multi_spike_751_mart", "НОВОЕ!", "offer_bgr_candyland",
            0, 0, false,
            false, false, 0,
            0, 0, 0, 0, 0
            );        
            
            
          
           GenerateOffer(
    new DateTime(2024, 11, 20, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    1,                      // Count
    0,                      // BrawlerID (всегда 0 для титулов)
    188,                     // Extra = ID титула из CSV ("Герой")
    ShopItem.Titul,         // Тип предмета - титул
    185,                     // Цена - 29 гемов
    185,                     // Старая цена (зачёркнутая)
    0,                      // Валюта (0-гемы, 1-монеты)
    "title_tirex_indusbrawl_offer188",     // Уникальный ID оффера
    "Титул: Тирекс",         // Название в магазине
    "offer_bgr_candyland",    // Фон оффера
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2025, 12, 30, 0, 0, 0),
    new DateTime(2026, 1, 25, 23, 59, 59),
    500, 0, 0, ShopItem.Gems,
    0, 40, 0,
    "sorry_ddos_500", "Извините!", "offer_bgr_rarity_epic",
    0, 0, false, false, false, 0, 0, 0, 0, 0, 0
);
            
            
GenerateOffer(
            new DateTime(2025, 12, 01, 0, 0, 0), new DateTime(2026, 01, 20, 11, 0, 0),
            1, 999, 751, ShopItem.Skin,
            200, 300, 0,
            "multi_spike_751", "НОВОЕ!", "",
            0, 0, false,
            false, false, 0,
            0, 0, 0, 0, 0
            );            
            
          





            Random random = new Random();
            GenerateOffer(
            new DateTime(2024, 5, 9, 12, 0, 0), new DateTime(2025, 11, 05, 11, 0, 0),
            15, 0, 0, ShopItem.Gems,
            0, 0, 0,
            "comp1488", "Щедрость на высшем уровне!", "offer_bgr_retro2023",
            0, 0, false,
            false, false, 0,
            0, 0, 0, 0, 0
            );
            //Random random = new Random();
            //GenerateOffer(
            //new DateTime(2024, 5, 9, 12, 0, 0), new DateTime(2025, 10, 10, 11, 0, 0),
            //20, 4, 4, ShopItem.RarityStarrDrop,
            //0, 0, 0,
            //random.Next(1, 10000).ToString(), "Дропчiк", "",
            //0, 0, true,
            //true, false, 0,
            //0, 0, 0, 0, 0
            //);
            //GenerateOffer(
            //new DateTime(2024, 5, 9, 12, 0, 0), new DateTime(2025, 10, 10, 11, 0, 0),
            //1, 1, 0, ShopItem.GuaranteedHero,
            //0, 0, 0,
            //"colt", "Кольт", "",
            //0, 0, true,
            //true, false, 0,
            //0, 0, 0, 0, 0
            //);






            

            
            








GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,  // редкость 1 = обычный
    50,                               // Cost - 5000 монет ✅
    10000,                              // OldCost - зачёркнутая цена 10000
    1,                                  // Currency (1-монеты ✅ вместо 0-гемы)
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_1_normalultratopppppvigodno", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,  // редкость 1 = обычный
    50,                               // Cost - 5000 монет ✅
    10000,                              // OldCost - зачёркнутая цена 10000
    1,                                  // Currency (1-монеты ✅ вместо 0-гемы)
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_2_normalultratopppppvigodno", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,  // редкость 1 = обычный
    50,                               // Cost - 5000 монет ✅
    10000,                              // OldCost - зачёркнутая цена 10000
    1,                                  // Currency (1-монеты ✅ вместо 0-гемы)
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_3_normalultratopppppvigodno", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,  // редкость 1 = обычный
    50,                               // Cost - 5000 монет ✅
    10000,                              // OldCost - зачёркнутая цена 10000
    1,                                  // Currency (1-монеты ✅ вместо 0-гемы)
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_4_normalultratopppppvigodno", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,  // редкость 1 = обычный
    50,                               // Cost - 5000 монет ✅
    10000,                              // OldCost - зачёркнутая цена 10000
    1,                                  // Currency (1-монеты ✅ вместо 0-гемы)
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_5_normalultratopppppvigodno", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,  // редкость 1 = обычный
    50,                               // Cost - 5000 монет ✅
    10000,                              // OldCost - зачёркнутая цена 10000
    1,                                  // Currency (1-монеты ✅ вместо 0-гемы)
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_6_normalultratopppppvigodno", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,  // редкость 1 = обычный
    50,                               // Cost - 5000 монет ✅
    10000,                              // OldCost - зачёркнутая цена 10000
    1,                                  // Currency (1-монеты ✅ вместо 0-гемы)
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_7_normalultratopppppvigodno", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);



GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,  // редкость 1 = обычный
    50,                               // Cost - 5000 монет ✅
    10000,                              // OldCost - зачёркнутая цена 10000
    1,                                  // Currency (1-монеты ✅ вместо 0-гемы)
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_8_normalultratopppppvigodno", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,  // редкость 1 = обычный
    50,                               // Cost - 5000 монет ✅
    10000,                              // OldCost - зачёркнутая цена 10000
    1,                                  // Currency (1-монеты ✅ вместо 0-гемы)
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_9_normalultratopppppvigodno", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);



GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,  // редкость 1 = обычный
    50,                               // Cost - 5000 монет ✅
    10000,                              // OldCost - зачёркнутая цена 10000
    1,                                  // Currency (1-монеты ✅ вместо 0-гемы)
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_10_normalultratopppppvigodno", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,  // редкость 1 = обычный
    50,                               // Cost - 5000 монет ✅
    10000,                              // OldCost - зачёркнутая цена 10000
    1,                                  // Currency (1-монеты ✅ вместо 0-гемы)
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_2_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,  // редкость 1 = обычный
    50,                               // Cost - 5000 монет ✅
    10000,                              // OldCost - зачёркнутая цена 10000
    1,                                  // Currency (1-монеты ✅ вместо 0-гемы)
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_3_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,  // редкость 1 = обычный
    50,                               // Cost - 5000 монет ✅
    10000,                              // OldCost - зачёркнутая цена 10000
    1,                                  // Currency (1-монеты ✅ вместо 0-гемы)
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_4_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_5_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_6_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_7_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_8_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_9_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_10_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_11_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_12_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_13_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_14_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_15_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_16_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_17_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_18_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_19_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    100, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_20_normalultratopp10182828", "Выгодно!", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);








GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    0, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_21_normalultratopp10182828", "Выгодно! (X2)", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    0, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_22_normalultratopp10182828", "Выгодно! (X2)", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    0, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_23_normalultratopp10182828", "Выгодно! (X2)", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    0, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_24_normalultratopp10182828", "Выгодно! (X2)", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    0, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_25_normalultratopp10182828", "Выгодно! (X2)", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    0, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_26_normalultratopp10182828", "Выгодно! (X2)", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    0, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_27_normalultratopp10182828", "Выгодно! (X2)", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    0, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_28_normalultratopp10182828", "Выгодно! (X2)", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);


GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    0, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_29_normalultratopp10182828", "Выгодно! (X2)", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);



GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),
    new DateTime(2026, 04, 15, 11, 0, 0),
    10, 0, 1, ShopItem.RarityStarrDrop,
    0, 10000, 1,
    "starrdrop8822228i7i71h88338283838882929298282882288288338833252526526383882929298282882288288338833252526526282929298282882288288338833939393339399929298292929androidandiosstaar9998ak52ak1newfon2_30_normalultratopp10182828", "Выгодно! (X2)", "offer_bgr_retro2023",
    0, 0, true, false, false, 0, 0, 0, 0, 0, 0
);

GenerateOffer(
    new DateTime(2024, 5, 9, 12, 0, 0),   // Start time
    new DateTime(2025, 10, 01, 11, 0, 0), // End time
    1,                                     // Count основного предмета
    36,                                    // BrawlerID (35 = Леон ✅)
    0,                                     // Extra
    ShopItem.GuaranteedHero,               // Основной предмет - Леон
    90,                                    // Cost - 90 гемов
    180,                                   // OldCost - зачёркнутая цена
    0,                                     // Currency (0-гемы)
    "leon_megabox_pack",                   // Claim ID
    "Леон",                  // Title
    "offer_bgr_legendary",                 // Background
    0,                                     // IsTID
    0,                                     // DailyOfferType
    true,                                  // OneTimeOffer
    true,                                  // LoadOnStartup
    false,                                 // Processed
    0,                                     // TypeBenefit
    0,                                     // Benefit
    0,                                     // panelClass
    0,                                     // panelType
    0,                                     // styleClass
    0                                      // styleType
);









            AddCustomOffers();

            // Новый вход в игру: неоткрытые стардропы прошлой сессии клиенту уже не покажем (награды за них начислены)
            lock (_pendingStarrDrops) _pendingStarrDrops.Clear();

            RotateShopContent(DateTime.UtcNow, OfferBundles.Count == 0);
            if (Quests == null)
            {
                 if (HomeMode.Avatar.Heroes != null && HomeMode.Avatar.Heroes.Count > 0 && TrophyRoadProgress >= 7)
                 {
                       Quests = new Quests();
                       Quests.AddRandomQuests(HomeMode.Avatar.Heroes, 8);                        
                       Debugger.Print($"[Quests] Сгенерировано 8 квестов.");
                 }
                 else
                {
                     Debugger.Print($"[Quests] Не могу создать квесты: TrophyRoad={TrophyRoadProgress}, Heroes={(HomeMode.Avatar.Heroes?.Count ?? 0)}");
                }
              }
              else if (Quests.QuestList.Count < 8)
              {
                  int count = 8 - Quests.QuestList.Count;
                  if (HomeMode.Avatar.Heroes != null && HomeMode.Avatar.Heroes.Count > 0 && TrophyRoadProgress >= 7)
                  {
                      Quests.AddRandomQuests(HomeMode.Avatar.Heroes, count);
                      Debugger.Print($"[Quests] Дополнено {count} квестов.");
                  }
              }
        }

        // Акции, созданные через админ-панель
        [JsonIgnore] private readonly List<OfferBundle> _customBundles = new List<OfferBundle>();

        private void AddCustomOffers()
        {
            _customBundles.Clear();
            foreach (CustomOffer custom in CustomOffers.GetActive())
            {
                List<CustomOfferItem> items = custom.GetItems();
                GenerateOffer(
                    custom.Start, custom.End,
                    items[0].Count, items[0].BrawlerId, items[0].ExtraId, (ShopItem)items[0].Item,
                    custom.Cost, custom.OldCost, custom.Currency,
                    custom.Id, custom.Title, custom.Background,
                    0, 0, false,
                    false, false, 0,
                    0, 0, 0, 0, 0
                );

                // Остальные товары набора
                OfferBundle customBundle = OfferBundles[OfferBundles.Count - 1];
                _customBundles.Add(customBundle);
                for (int i = 1; i < items.Count; i++)
                {
                    customBundle.Items.Add(new Offer((ShopItem)items[i].Item, items[i].Count, 16000000 + items[i].BrawlerId, items[i].ExtraId));
                }
            }
        }

        // ---------- Стардропы: двухшаговая схема ----------
        // Сервер сообщает клиенту "есть ожидающий стардроп" (команда 228). Клиент сам открывает окно с анимацией
        // и присылает команду 571 - только тогда ему отправляется награда (команда 203).
        private class PendingStarrDrop
        {
            public LogicGiveDeliveryItemsCommand Delivery;
            public int Rarity;
            public int Track, Rank, Season;   // ячейка пути славы/пропуска (0 - дроп не из ячейки)
            public bool IsTile => Track != 0 || Rank != 0;
        }

        [JsonIgnore] private readonly Queue<PendingStarrDrop> _pendingStarrDrops = new Queue<PendingStarrDrop>();

        private void SendPendingStarrState()
        {
            LogicRefreshRandomRewardsCommand refresh = new LogicRefreshRandomRewardsCommand();
            lock (_pendingStarrDrops)
            {
                if (_pendingStarrDrops.Count > 0)
                {
                    refresh.Количество = 1;
                    refresh.Rarity = _pendingStarrDrops.Peek().Rarity;
                    refresh.Lab = StarrDropLab.For(HomeMode);
                }
                else
                {
                    refresh.CleanEmpty = true;
                }
            }
            HomeMode.GameListener.SendMessage(new AvailableServerCommandMessage { Command = refresh });
        }

        /// <summary>
        /// Создаёт стардроп и ставит его в очередь на открытие. Награда начисляется на сервере сразу
        /// (не потеряется при обрыве связи), а клиенту показывается, когда он откроет дроп.
        /// </summary>
        public void QueueStarrDrop(int track, int rank, int season)
        {
            StarrDrop.GenerateDrop(HomeMode);

            LogicGiveDeliveryItemsCommand delivery = new LogicGiveDeliveryItemsCommand();
            DeliveryUnit unit = new DeliveryUnit(100);
            GatchaDrop drop = new GatchaDrop(StarrDrop.data.Type);
            drop.DataGlobalId = StarrDrop.data.DataGlobalID;
            if (StarrDrop.data.Type != 1) drop.SkinGlobalId = StarrDrop.data.SkinGlobalID;
            drop.Count = StarrDrop.data.Ammount;
            unit.AddDrop(drop);
            delivery.StarrDropExecute = true;
            delivery.StarrTwoPhase = true;
            delivery.Lab = StarrDropLab.For(HomeMode);
            delivery.LabRarity = StarrDrop.Rarity;
            delivery.DeliveryUnits.Add(unit);
            delivery.Execute(HomeMode);

            lock (_pendingStarrDrops)
            {
                _pendingStarrDrops.Enqueue(new PendingStarrDrop { Delivery = delivery, Rarity = StarrDrop.Rarity, Track = track, Rank = rank, Season = season });
            }
            SendPendingStarrState();
        }

        /// <summary>
        /// Клиент подтвердил выполнение команды 228. popupOpened = в том же ходе за подтверждением шла ещё команда
        /// (это запрос 571: клиент открыл окно с анимацией).
        /// Окно не открылось и дроп из ячейки пути славы/пропуска (клиент без правки, экран не лобби): клиент ждёт
        /// награду в ответ на нажатие, поэтому отдаём её сразу обычным окном.
        /// Окно не открылось и дроп не из ячейки: остаётся в очереди, откроется, когда игрок вернётся в лобби.
        /// </summary>
        public void OnStarrStateAcknowledged(bool popupOpened)
        {
            if (!(StarrDropLab.For(HomeMode) is StarrDropLabConfig lab) || !lab.TwoPhase) return;

            if (popupOpened)
            {
                OpenPendingStarrDrop();
                return;
            }

            PendingStarrDrop pending;
            lock (_pendingStarrDrops)
            {
                if (_pendingStarrDrops.Count == 0 || !_pendingStarrDrops.Peek().IsTile) return;
                pending = _pendingStarrDrops.Dequeue();
            }
            pending.Delivery.RewardTrackType = pending.Track;
            pending.Delivery.RewardForRank = pending.Rank;
            pending.Delivery.BrawlPassSeason = pending.Season;
            HomeMode.GameListener.SendMessage(new AvailableServerCommandMessage { Command = pending.Delivery });
            SendPendingStarrState();
        }

        /// <summary>
        /// Клиент открыл окно стардропа (команда 571): отправляем награду в это окно. Для дропа из ячейки
        /// пути славы/пропуска следом тихо отмечаем ячейку полученной. Возвращает false, если очереди нет.
        /// </summary>
        public bool OpenPendingStarrDrop()
        {
            PendingStarrDrop pending;
            lock (_pendingStarrDrops)
            {
                if (_pendingStarrDrops.Count == 0) return false;
                pending = _pendingStarrDrops.Dequeue();
            }
            HomeMode.GameListener.SendMessage(new AvailableServerCommandMessage { Command = pending.Delivery });

            if (pending.IsTile)
            {
                LogicGiveDeliveryItemsCommand ack = new LogicGiveDeliveryItemsCommand();
                ack.RewardTrackType = pending.Track;
                ack.RewardForRank = pending.Rank;
                ack.BrawlPassSeason = pending.Season;
                ack.BrawlPassExecute = true;
                ack.Presentation = StarrDropLab.For(HomeMode)?.TileAckPresentation ?? 2;
                HomeMode.GameListener.SendMessage(new AvailableServerCommandMessage { Command = ack });
            }

            SendPendingStarrState();
            return true;
        }

        /// <summary>
        /// Выдаёт игроку в сети один стардроп
        /// </summary>
        public void GiveStarrDrop()
        {
            if (HomeMode?.Avatar == null || HomeMode.GameListener == null) return;

            StarrDropLabConfig lab = StarrDropLab.For(HomeMode);
            if (lab != null && lab.TwoPhase)
            {
                QueueStarrDrop(0, 0, 0);
                return;
            }

            StarrDrop.GenerateDrop(HomeMode);

            LogicRefreshRandomRewardsCommand refresh = new LogicRefreshRandomRewardsCommand();
            refresh.Rarity = 4;
            refresh.Количество = lab != null && lab.ClaimCount >= 0 ? lab.ClaimCount : 4;
            refresh.Execute(HomeMode);
            HomeMode.GameListener.SendMessage(new AvailableServerCommandMessage { Command = refresh });

            LogicGiveDeliveryItemsCommand delivery = new LogicGiveDeliveryItemsCommand();
            DeliveryUnit unit = new DeliveryUnit(100);
            GatchaDrop drop = new GatchaDrop(StarrDrop.data.Type);
            drop.DataGlobalId = StarrDrop.data.DataGlobalID;
            if (StarrDrop.data.Type != 1) drop.SkinGlobalId = StarrDrop.data.SkinGlobalID;
            drop.Count = StarrDrop.data.Ammount;
            unit.AddDrop(drop);
            delivery.StarrDropExecute = true;
            delivery.Lab = lab;
            delivery.LabRarity = StarrDrop.Rarity;
            delivery.DeliveryUnits.Add(unit);
            delivery.Execute(HomeMode);
            HomeMode.GameListener.SendMessage(new AvailableServerCommandMessage { Command = delivery });

            LogicRefreshRandomRewardsCommand disable = new LogicRefreshRandomRewardsCommand();
            disable.Disable = true;
            disable.Execute(HomeMode);
            HomeMode.GameListener.SendMessage(new AvailableServerCommandMessage { Command = disable });
        }

        /// <summary>
        /// Пересобирает акции из админ-панели (вызывается перед отправкой данных дома,
        /// чтобы новые акции появлялись без повторного входа в игру)
        /// </summary>
        public void RefreshCustomOffers()
        {
            if (HomeMode?.Avatar == null || OfferBundles == null) return;
            OfferBundles.RemoveAll(bundle => _customBundles.Contains(bundle));
            AddCustomOffers();
        }

        public void Tick()
        {
            LastVisitHomeTime = DateTime.UtcNow;
            TokenReward = 0;
            TrophiesReward = 0;
            StarTokenReward = 0;
        }
        
        
       public void DebugNewBrawlers()
{
    if (UnlockStarRoad == null || UnlockStarRoad.Length == 0)
    {
        Debugger.Print("[DEBUG] UnlockStarRoad пуст!");
        return;
    }
    
    // Проверить, какие бойцы еще не разблокированы
    var lockedBrawlers = new List<int>();
    foreach (int globalId in UnlockStarRoad)
    {
        if (!HomeMode.Avatar.HasHero(globalId))
        {
            lockedBrawlers.Add(globalId);
        }
    }
    
    Debugger.Print($"[DEBUG] Заблокировано бойцев: {lockedBrawlers.Count}");
    foreach (int globalId in lockedBrawlers)
    {
        int brawlerId = globalId - 16000000;
        CharacterData charData = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(globalId);
        string name = charData?.Name ?? "Unknown";
        Debugger.Print($"  - ID: {brawlerId}, GlobalID: {globalId}, Имя: {name}");
    }
} 

        
        public async void GenerateOffers()
        {

        }

        public async void PurchaseOffer(int index)
        {
            if (index < 0 || index >= OfferBundles.Count) return;

            OfferBundle bundle = OfferBundles[index];
            if (bundle.Purchased) return;

            if (bundle.Currency == 0)
            {
                HomeMode.Avatar.UseDiamonds(bundle.Cost);
            }
            else if (bundle.Currency == 1)
            {
                HomeMode.Avatar.UseGold(bundle.Cost);
            }
            else if (bundle.Currency == 6)
            {
                HomeMode.Avatar.UseBlings(bundle.Cost);
            }

            bundle.Purchased = true;

            if (bundle.Claim == "debug")
            {
                ;
            }
            else
            {
                OffersClaimed.Add(bundle.Claim);
            }

            Random rand = new Random();
            foreach (Offer offer in bundle.Items)
                if (offer.Type == ShopItem.RarityStarrDrop && StarrDropLab.For(HomeMode) is StarrDropLabConfig shopLab && shopLab.TwoPhase)
                {
                    QueueStarrDrop(0, 0, 0);
                }
                else if (offer.Type == ShopItem.RarityStarrDrop)
                {
                    StarrDrop.GenerateDrop(HomeMode);
                    LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand = new LogicRefreshRandomRewardsCommand();

                    logicRefreshRandomRewardsCommand.Rarity = 4;
                    logicRefreshRandomRewardsCommand.Количество = 4;
                    logicRefreshRandomRewardsCommand.Execute(HomeMode);

                    AvailableServerCommandMessage message25 = new AvailableServerCommandMessage();
                    message25.Command = logicRefreshRandomRewardsCommand;
                    HomeMode.GameListener.SendMessage(message25);

                    LogicGiveDeliveryItemsCommand command123 = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit = new DeliveryUnit(100);

                    GatchaDrop drop = new GatchaDrop(HomeMode.Home.StarrDrop.data.Type);

                    drop.DataGlobalId = HomeMode.Home.StarrDrop.data.DataGlobalID;
                    if (HomeMode.Home.StarrDrop.data.Type != 1) drop.SkinGlobalId = HomeMode.Home.StarrDrop.data.SkinGlobalID;
                    drop.Count = HomeMode.Home.StarrDrop.data.Ammount;
                    unit.AddDrop(drop);

                    command123.StarrDropExecute = true;
                    command123.Lab = StarrDropLab.For(HomeMode);
                    command123.LabRarity = HomeMode.Home.StarrDrop.Rarity;
                    command123.DeliveryUnits.Add(unit);
                    command123.Execute(HomeMode);
                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command123;
                    HomeMode.GameListener.SendMessage(message);

                    LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand1 = new LogicRefreshRandomRewardsCommand();

                    logicRefreshRandomRewardsCommand1.Disable = true;
                    logicRefreshRandomRewardsCommand1.Execute(HomeMode);

                    AvailableServerCommandMessage message251 = new AvailableServerCommandMessage();
                    message251.Command = logicRefreshRandomRewardsCommand1;
                    HomeMode.GameListener.SendMessage(message251);

                }
                else if (offer.Type == ShopItem.Coin)
                {
                    LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(7);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                    command.Execute(HomeMode);
                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command;
                    HomeMode.GameListener.SendMessage(message);
                }
                else if (offer.Type == ShopItem.Gems)
                {
                    LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(8);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                    command.Execute(HomeMode);
                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command;
                    HomeMode.GameListener.SendMessage(message);
                }
                else if (offer.Type == ShopItem.Bling)
                {
                    LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(25);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                    command.Execute(HomeMode);
                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command;
                    HomeMode.GameListener.SendMessage(message);
                }
                else if (offer.Type == ShopItem.PowerPoint)
                {
                    LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(24);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                    command.Execute(HomeMode);
                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command;
                    HomeMode.GameListener.SendMessage(message);
                }
                else if (offer.Type == ShopItem.GuaranteedHero)
                {
                    LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(1);
                    reward.DataGlobalId = offer.ItemDataId;
                    reward.Count = 1;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                    command.Execute(HomeMode);
                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command;
                    HomeMode.GameListener.SendMessage(message);
                }
                else if (offer.Type == ShopItem.Titul)
                {
                    LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.DataGlobalId = GlobalId.CreateGlobalId(76, offer.SkinDataId);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                    command.Execute(HomeMode);
                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command;
                    HomeMode.GameListener.SendMessage(message);
                }
                else if (offer.Type == ShopItem.PlayerThumbnail)
                {
                    LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.DataGlobalId = GlobalId.CreateGlobalId(28, offer.SkinDataId);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                    command.Execute(HomeMode);
                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command;
                    HomeMode.GameListener.SendMessage(message);
                }
                else if (offer.Type == ShopItem.Skin)
                {
                    LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(9);
                    reward.SkinGlobalId = GlobalId.CreateGlobalId(29, offer.SkinDataId);
                    reward.Count = 1;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                    command.Execute(HomeMode);
                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command;
                    HomeMode.GameListener.SendMessage(message);
                    NewCommand(DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(offer.SkinDataId), command);
                }
                else if (offer.Type == ShopItem.Emote)
                {
                    LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.DataGlobalId = GlobalId.CreateGlobalId(52, offer.SkinDataId);
                    reward.Count = 1;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                    command.Execute(HomeMode);
                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command;
                    HomeMode.GameListener.SendMessage(message);
                }
                else
                {
                    LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                    logicAddNotificationCommand.Notification = new FloaterTextNotification("ShopItem not initialized\nPlease contact the developers with this problem.");
                    AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                    availableServerCommandMessage.Command = logicAddNotificationCommand;
                    HomeMode.GameListener.SendMessage(availableServerCommandMessage);

                    LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();

                    DeliveryUnit unit = new DeliveryUnit(100);

                    command.DeliveryUnits.Add(unit);
                    command.Execute(HomeMode);
                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command;
                    HomeMode.GameListener.SendMessage(message);

                }

            void NewCommand(SkinData skinData, LogicGiveDeliveryItemsCommand command)
            {
                if (skinData == null) return;
                DeliveryUnit unit = new DeliveryUnit(100);
                GatchaDrop reward = new GatchaDrop(9);
                foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
                {

                    if (emoteData.Skin == skinData.Name)
                    {
                        GatchaDrop reward1 = new GatchaDrop(11);
                        reward1.DataGlobalId = DataTables.Get(DataType.Emote).GetData<EmoteData>(emoteData.Name).GetGlobalId();
                        reward1.Count = 1;
                        unit.AddDrop(reward1);
                    }

                }
                foreach (PlayerThumbnailData playerThumbnailData in DataTables.Get(DataType.PlayerThumbnail).GetDatas())
                {
                    if (playerThumbnailData.CatalogPreRequirementSkin == skinData.Name)
                    {
                        GatchaDrop reward1 = new GatchaDrop(11);
                        reward1.DataGlobalId = DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(playerThumbnailData.Name).GetGlobalId();
                        reward1.Count = 1;
                        unit.AddDrop(reward1);
                    }

                }
                command.DeliveryUnits.Add(unit);
                command.Execute(HomeMode);

                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command;
                HomeMode.GameListener.SendMessage(message);

            }
        } 



private void RotateShopContent(DateTime time, bool isNewAcc)
        {
            //OfferBundles.RemoveAll(bundle => true);
            //if (OfferBundles.Select(bundle => bundle.IsDailyDeals).ToArray().Length > 6)
            //{
            //    OfferBundles.RemoveAll(bundle => bundle.IsDailyDeals);
            //}
            //OfferBundles.RemoveAll(offer => offer.EndTime <= time);

            //if (isNewAcc || DateTime.UtcNow >= DateTime.UtcNow.Date.AddHours(8)) // Daily deals refresh at 08:00 AM UTC
            //{
            //    if (LastVisitHomeTime < DateTime.UtcNow.Date.AddHours(8)||true)
            //    {
            //        UpdateDailyOfferBundles();
            //    }
            //}
            if (true || isNewAcc)
            {
                OfferBundle uab = new OfferBundle();
                uab.Title = "j";
                uab.EndTime = DateTime.UtcNow.Date.AddDays(18141); // tomorrow at 8:00 utc (11:00 MSK)
                uab.Cost = 0;
                Offer offer = new Offer(ShopItem.BrawlBox, 1);
                uab.Items.Add(offer);
                //OfferBundles.Add(uab);
            }

        }



        public void GenerateOffer(
    DateTime OfferStart,
    DateTime OfferEnd,
    int Count,
    int BrawlerID,
    int Extra,
    ShopItem Item,
    int Cost,
    int OldCost,
    int Currency,
    string Claim,
    string Title,
    string BGR,
    int IsTID,
    int DailyOfferType,
    bool OneTimeOffer,
    bool LoadOnStartup,
    bool Processed,
    int TypeBenefit,
    int Benefit,

    int panelClass,
    int panelType,

    int styleClass,
    int styleType

    )
        {

            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = false;
            bundle.IsTrue = true;
            bundle.EndTime = OfferEnd;
            bundle.Cost = Cost;
            bundle.OldCost = OldCost;
            bundle.Currency = Currency;
            bundle.Claim = Claim;
            bundle.Title = Title;
            bundle.BackgroundExportName = BGR;
            bundle.IsTID = IsTID;
            bundle.OfferType = DailyOfferType;
            bundle.OneTimeOffer = OneTimeOffer;
            bundle.LoadOnStartup = LoadOnStartup;
            bundle.Processed = Processed;
            bundle.TypeBenefit = TypeBenefit;
            bundle.Benefit = Benefit;
            bundle.ShopPanelLayoutClass = panelClass;
            bundle.ShopPanelLayoutType = panelType;
            bundle.ShopStyleSetClass = styleClass;
            bundle.ShopStyleSetType = styleType;




              if (OffersClaimed.Contains(bundle.Claim))
              {
                 bundle.Purchased = true;
              }
              if (TimerMath(OfferStart, OfferEnd) == -1)
              {
                 bundle.Purchased = true;
              }
            if (HomeMode.Avatar.HasHero((16000000 + BrawlerID)) && Item == ShopItem.GuaranteedHero)
              {
                 bundle.Purchased = true;
              }

            Offer offer = new Offer(Item, Count, (16000000 + BrawlerID), Extra);
            bundle.Items.Add(offer);

            OfferBundles.Add(bundle);
        }

        private bool IsGearUnlocked(Hero hero, int gearId)
        {
               return hero.GearData.Exists(g => g.GetInstanceId() == gearId);
        }
        public void Encode(ByteStream stream)
        {//64 VInt 32 Boolean 28 String 16 String 36 Int


            stream.WriteVInt(2000000);
            stream.WriteVInt(0);

            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(HomeMode.Avatar.Trophies); // Trophies
            stream.WriteVInt(HomeMode.Avatar.HighestTrophies); // Highest Trophies
            stream.WriteVInt(HomeMode.Avatar.HighestTrophies);
            stream.WriteVInt(TrophyRoadProgress);
            stream.WriteVInt(50000); // Experience
            ByteStreamHelper.WriteDataReference(stream, ThumbnailId);
            ByteStreamHelper.WriteDataReference(stream, NameColorId);


            stream.WriteVInt(27);//played gamemodes(dont shouw battle hint)
            for (int i = 0; i < 27; i++) stream.WriteVInt(i);
            int skins = 0;
            foreach (Hero hero in HomeMode.Avatar.Heroes)
            {
                if (hero.SelectedSkinId != 0) skins++;
            }
            stream.WriteVInt(skins); // Selected Skins
            foreach (Hero hero in HomeMode.Avatar.Heroes)
            {
                if (hero.SelectedSkinId != 0) ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(29, hero.SelectedSkinId));
            }
            //foreach (Hero hero in HomeMode.Avatar.Heroes)
            //{
            //    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(52, 1301));
            //}
            stream.WriteVInt(0); // Randomizer Skin Selected

            stream.WriteVInt(0); // Current Random Skin

            stream.WriteVInt(UnlockedSkins.Count); // Played game modes
            foreach (int s in UnlockedSkins)
            {
                ByteStreamHelper.WriteDataReference(stream, s);
            }

            stream.WriteVInt(0); // Unlocked Skin Purchase Option

            stream.WriteVInt(0); // New Item State

            stream.WriteVInt(0);
            stream.WriteVInt(0);//highest trophies
            stream.WriteVInt(0);
            stream.WriteVInt(2);//control mode
            stream.WriteBoolean(false);//battle hints
            stream.WriteVInt(TokenDoublers);//token doubler
            stream.WriteVInt(0);//maybe starr drop timer ? #v50 --risporce(bsds)
            stream.WriteVInt((int)(DateTime.Parse("2026-04-07 12:00:00") - DateTime.Now).TotalSeconds);//trophy league timer --risporce(bsds)
            stream.WriteVInt(0);//power play timer --risporce(bsds)
            stream.WriteVInt((int)(DateTime.Parse("2026-04-07 12:00:00") - DateTime.Now).TotalSeconds);//Brawl pass season time --risporce(bsds)

            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);

            stream.WriteBoolean(true); // Token Doubler Enabled
            stream.WriteVInt(2);  // Token Doubler New Tag State
            stream.WriteVInt(2);  // Event Tickets New Tag State
            stream.WriteVInt(2);  // Coin Packs New Tag State
            stream.WriteVInt(30);  // Change Name Cost
            stream.WriteVInt(0);  // Timer For the Next Name Change

            stream.WriteVInt(OfferBundles.Count); // Shop offers at 0x78e0c4
            foreach (OfferBundle offerBundle in OfferBundles)
            {
                offerBundle.Encode(stream);        // ес че эта хуйня крашит
            }
            ;

            //v51
            //stream.WriteBoolean(false);
            //stream.WriteBoolean(false);
            //stream.WriteVInt(0);
            //stream.WriteVInt(0);
            //stream.WriteBoolean(false);

            stream.WriteVInt(200); // это количество токенов для игры в боях
            stream.WriteVInt(0); // время до доабвления 20 токенов для игры в боях в секундах

            stream.WriteVInt(0);

            stream.WriteVInt(1);
            stream.WriteVInt(30);

            stream.WriteByte(3);
            ByteStreamHelper.WriteDataReference(stream, CharacterIds[0]);
            ByteStreamHelper.WriteDataReference(stream, CharacterIds[1]);
            ByteStreamHelper.WriteDataReference(stream, CharacterIds[2]);

            stream.WriteString(HomeMode.Avatar.Region);
            stream.WriteString(HomeMode.Avatar.SupportedCreator);

            stream.WriteVInt(6);
            stream.WriteVInt(2147483647); stream.WriteVInt(28);  // Have already watched starrdrop stupid animation
            //stream.WriteVInt(2147483647); stream.WriteVInt(27);
            stream.WriteVInt(TokenReward); stream.WriteVInt(3);
            stream.WriteVInt(TrophiesReward); stream.WriteVInt(4);
            stream.WriteLogicLong(HomeMode.Avatar.WinStreak, 37);
            stream.WriteLogicLong(HomeMode.Avatar.DoNotDisturb, 7); // invite block
            stream.WriteLogicLong(1, 17); // chat block
            //stream.WriteLong(2, 1);  // Unknown
            //stream.WriteLong(3, 0);  // Tokens Gained
            //stream.WriteLong(4, 0);  // Trophies Gained
            //stream.WriteLong(6, 0);  // Demo Account
            //stream.WriteLong(7, 0);  // Invites Blocked

            //stream.WriteLong(8, 0);  // Star Points Gained
            //stream.WriteLong(9, 1);  // Show Star Points
            //stream.WriteLong(10, 0);  // Power Play Trophies Gained
            //stream.WriteLong(12, 1);  // Unknown
            //stream.WriteLong(14, 0);  // Coins Gained
            //stream.WriteLong(15, 0);  // AgeScreen | 3 = underage (disable social media); | 1 = age popup
            //stream.WriteLong(16, 1);
            //stream.WriteLong(17, 0);  // Team Chat Muted
            //stream.WriteLong(18, 1);  // Esport Button
            //stream.WriteLong(19, 0);  // Champion Ship Lives Buy Popup
            //stream.WriteLong(20, 0);  // Gems Gained
            //stream.WriteLong(21, 0);  // Looking For Team State
            //stream.WriteLong(22, 1);
            //stream.WriteLong(23, 0);  // Club Trophies Gained
            //stream.WriteLong(24, 1);  // Have already watched club league stupid animation
            //stream.WriteLogicLong(1488, 27);
            // 26 - Club League Point
            // 27 - PowerPoints Gained
            // 30 - FUCKING FROGS
            // 32 - blings gained

            TokenReward = 0;
            TrophiesReward = 0;

            stream.WriteVInt(0);

            stream.WriteVInt(1); // count brawl pass seasons
            for (int i = 0; i < 1; i++)
            {
                stream.WriteVInt(22);
                stream.WriteVInt(BrawlPassTokens);
                stream.WriteBoolean(HasPremiumPass);

                stream.WriteVInt(131);
                stream.WriteBoolean(false);

                if (stream.WriteBoolean(true)) // Track 9
                {
                    stream.WriteLongLong128(PremiumPassProgress);
                }
                if (stream.WriteBoolean(true)) // Track 10
                {
                    stream.WriteLongLong128(BrawlPassProgress);
                }
                //v53
                stream.WriteBoolean(HasPremiumPassPlus); // BrawlPassPlus
                if (stream.WriteBoolean(true)) // Track ?
                {
                    stream.WriteLongLong128(BrawlPassPlusProgress);
                }
            }
            stream.WriteVInt(0);

            if (Quests != null)
            {
                stream.WriteBoolean(true); ;
                Quests.Encode(stream); ;
            }
            else
            {
                stream.WriteBoolean(true); ;
                stream.WriteVInt(0); ;
            }
            stream.WriteVInt(1);
            stream.WriteVInt(2);
            stream.WriteVInt(0);

            stream.WriteBoolean(true);
            stream.WriteVInt(UnlockedEmotes.Count + UnlockedTituls.Count + UnlockedThumbnails.Count); // count of cosmetic
            foreach (int Emote in UnlockedEmotes)
            {
                ByteStreamHelper.WriteDataReference(stream, Emote);
                stream.WriteVInt(1);
                stream.WriteVInt(Emote);
                stream.WriteVInt(4);
            }
            foreach (int Thumbnail in UnlockedThumbnails)
            {
                ByteStreamHelper.WriteDataReference(stream, Thumbnail);
                stream.WriteVInt(0);
            }
            foreach (int Titul in UnlockedTituls)
            {
                ByteStreamHelper.WriteDataReference(stream, Titul);
                stream.WriteVInt(0);
            }
            //    ByteStreamHelper.WriteDataReference(stream, Titul);
            //    stream.WriteVInt(0);
            if(stream.WriteBoolean(true))
            {
                stream.WriteVInt(GeneralStaticLogic.RankedSeason); // Season
                stream.WriteVInt(RankedSoloRank); // Rank Solo League
                stream.WriteVInt(RankedSoloProgress); // Total team league progress
                stream.WriteVInt(RankedTrioRank); // Rank Team League
                stream.WriteVInt(RankedTrioProgress); // Total team league progress
                stream.WriteVInt(RankedSoloMaxRank); // High Rank Solo League
                stream.WriteVInt(RankedSoloMaxProgress); // max solo progress
                stream.WriteVInt(RankedTrioMaxRank); // High Rank Team League
                stream.WriteVInt(RankedTrioMaxProgress);  // max team progress
                stream.WriteVInt(0); // чет для лидербордов
                stream.WriteVInt(1);
                stream.WriteVInt(1); // reward count???
                if (stream.WriteBoolean(true)) //LogicPlayerRewardData::encode
                {
                    if (stream.WriteBoolean(true))
                    {
                        stream.WriteVInt(1);
                        stream.WriteVInt(1);
                    }
                    if (stream.WriteBoolean(true))
                        new GemOffer(25, 1, 0, 63 - 3).Encode(stream);
                }
                stream.WriteVInt(3);
                stream.WriteBoolean(true);
            }

            stream.WriteInt(0);
            stream.WriteVInt(502052);
            ByteStreamHelper.WriteDataReference(stream, FavouriteCharacter);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);
            //v53
            stream.WriteVInt(0);
            stream.WriteVInt(2023189);


            stream.WriteVInt(35); // event slot id
            stream.WriteVInt(1);
            stream.WriteVInt(2);
            stream.WriteVInt(3);
            stream.WriteVInt(4);
            stream.WriteVInt(5);
            stream.WriteVInt(6);
            stream.WriteVInt(7);
            stream.WriteVInt(8);
            stream.WriteVInt(9);
            stream.WriteVInt(10);
            stream.WriteVInt(11);
            stream.WriteVInt(12);
            stream.WriteVInt(13);
            stream.WriteVInt(14);
            stream.WriteVInt(15);
            stream.WriteVInt(16);
            stream.WriteVInt(17);
            stream.WriteVInt(18);
            stream.WriteVInt(19);
            stream.WriteVInt(20);
            stream.WriteVInt(21);
            stream.WriteVInt(22);
            stream.WriteVInt(23);
            stream.WriteVInt(24);
            stream.WriteVInt(25);
            stream.WriteVInt(26);
            stream.WriteVInt(27);
            stream.WriteVInt(28);
            stream.WriteVInt(29);
            stream.WriteVInt(30);
            stream.WriteVInt(31);
            stream.WriteVInt(32);
            stream.WriteVInt(33);
            stream.WriteVInt(34);
            stream.WriteVInt(35);

            stream.WriteVInt(Events.Length + 1);
for (int i = 0; i < Events.Length; i++)
{
    if (Events[i] != null)
    {
        Events[i].Encode(stream);
    }
    else
    {
        Events[i] = new EventData(); // инициализируем null-элемент
        Events[i].Encode(stream);
    }
}



            stream.WriteVInt(9);
            stream.WriteVInt(9);
            stream.WriteVInt(9);
            stream.WriteVInt(1213);//v53
            stream.WriteVInt(1123);
            stream.WriteVInt(1123);

            stream.WriteDataReference(15, 0);



            stream.WriteVInt(2); // GameModeVaridation
            stream.WriteVInt(2);

            stream.WriteString(null); // 0xacecac
            stream.WriteVInt(0); // 0xacecc0
            stream.WriteVInt(0); // defeates
            stream.WriteVInt(3); // wins needed

            stream.WriteVInt(0); // modifier

            stream.WriteVInt(1); // wins
            stream.WriteVInt(1); // challenge variation
            //encoder.WriteBoolean(false);
            //encoder.WriteVInt(0); // 0xacee6c

            stream.WriteBoolean(false);

            stream.WriteVInt(1);
            stream.WriteBoolean(false);//LogicRankedSeason
            stream.WriteVInt(1);
            stream.WriteVInt(1);

            stream.WriteBoolean(false); // chromos text entry

            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
            //encoder.WriteBoolean(false);
            stream.WriteBoolean(false);

            stream.WriteVInt(-1);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
            stream.WriteVInt(-1);

            stream.WriteVInt(0);//v51
            stream.WriteVInt(0);//v51
            stream.WriteVInt(0);//v51
            stream.WriteBoolean(false);//v53



            stream.WriteVInt(0); // Comming Events


            ByteStreamHelper.WriteIntList(stream, new List<int> { 20, 35, 75, 140, 290, 480, 800, 1250, 1875, 2800 });
            ByteStreamHelper.WriteIntList(stream, new List<int> { 10, 25, 50, 99 });
            ByteStreamHelper.WriteIntList(stream, new List<int> { 150, 400, 1200, 2600 });


            //stream.WriteBoolean(true);  // Show Offers Packs


            //stream.WriteVInt(ReleaseEntry.LogicReleaaseEntry.Length / 2);  // IntValueEntry
            //for (int i = 0; i < ReleaseEntry.LogicReleaaseEntry.Length / 2; i++)
            //{

            //    stream.WriteDataReference(16, ReleaseEntry.LogicReleaaseEntry[i * 2]);
            //    stream.WriteInt(ReleaseEntry.LogicReleaaseEntry[i * 2 + 1]);
            //    stream.WriteInt(0);
            //    stream.WriteInt(0);
            //    stream.WriteBoolean(false);
            //}
            stream.WriteVInt(1);
            stream.WriteDataReference(16, 39);
            stream.WriteInt(1000);
            stream.WriteInt(0);
            stream.WriteInt(0);
            stream.WriteBoolean(true);

            int[] LogicConfData = new int[]
            {
                1,41000000,
                16000007,0, // джеси
                16000027,0,  // 8 бит
                16000061,0, // гас
                16000047,1,// скуик
                16000059,1,// отис
                16000016,0,// пэм
                16000022,0, //тик
                16000031,0,// мр. пи
                16000019,0,// пенни
                //16000050,1,// грифф
                16000071,0,// даг
                16000074,0,// чарли
                16000069,0,// хэнк
                16000056,0,// ева
                16000067,0,// даг
                16000075,1,// мико
                16000073,0,// чак
                16000077,1,// ларри и лори
                10027,0,
                10029,20,
                10018,1,
                48, 99999,
                79, 99999,
                80, 99999,
                65,2,
                66,0,
                47,41381,
                50,1,
                1100, 500,
                1101, 500,
                1003, 1,
                36,0,
                74,1,
                78,1,
                17,4,
                100046,1,
                87,1,
                63,1//dont show scid button
            };
       //////////    LogicConfData[1] = 41000000 + 85; // 87 - это футбольный фон (основной индус бравл) | 86 - Хеллоуин (синий фон с китом) | 85 - тигр тэтэтэ | 84 - зимний новогодний с спайком носок
       LogicConfData[1] = 41000000 + 87; // 87 - это футбольный фон (основной индус бравл) | 86 - Хеллоуин (синий фон с китом) | 85 - тигр тэтэтэ | 84 - зимний новогодний с спайком носок
            // Флаг 10056 = 1 включает в клиенте окно открытия стардропов (без него награда показывается сразу, без анимации).
            // Пока включается только вместе с двухшаговой схемой (см. StarrDropLab).
            if (StarrDropLab.For(HomeMode) is StarrDropLabConfig confLab && confLab.TwoPhase)
            {
                LogicConfData = LogicConfData.Concat(new int[] { 10056, 1 }).ToArray();
            }
    stream.WriteVInt(LogicConfData.Length / 2);  // IntValueEntry
            for (int i = 0; i < LogicConfData.Length / 2; i++)
            {
                stream.WriteVInt(LogicConfData[i * 2 + 1]);
                stream.WriteVInt(LogicConfData[i * 2]);
            }

            stream.WriteVInt(0); // Timed Int Value Entry

            stream.WriteVInt(0); ; //Custom Event

            stream.WriteVInt(0);

            stream.WriteVInt(0);

            stream.WriteVInt(2);
            stream.WriteVInt(1);
            stream.WriteVInt(2);
            stream.WriteVInt(2);
            stream.WriteVInt(1);
            stream.WriteVInt(-1);
            stream.WriteVInt(2);
            stream.WriteVInt(1);
            stream.WriteVInt(4);

            stream.WriteVInt(0);
            stream.WriteVInt(0);

            stream.WriteLong(HomeId);  // PlayerID

            NotificationFactory.Encode(stream);

            stream.WriteVInt(-1);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);
            stream.WriteVInt(0);//+108
            stream.WriteVInt(0);//+124

            stream.WriteBoolean(false);
            GearEncoder.Encode(stream, HomeMode.Avatar.Heroes);
// === Starr Road: используем UnlockStarRoad как источник порядка ===
stream.WriteBoolean(true); // Starr Road активна
stream.WriteVInt(0); // event_index
stream.WriteVInt(0); // end timestamp
stream.WriteVInt(0); // season duration

// Фильтруем только тех, кого ещё нет у игрока
var availableBrawlers = new List<int>();
if (HomeMode?.Home?.UnlockStarRoad != null)
{
    foreach (int globalId in HomeMode.Home.UnlockStarRoad)
    {
        if (!HomeMode.Avatar.HasHero(globalId))
        {
            int brawlerId = globalId - 16000000; // ✅ Безопасная замена GetGlobalIdNumber
            availableBrawlers.Add(brawlerId);
        }
    }
}

// Если нет доступных бравлеров — пропускаем
if (availableBrawlers.Count == 0)
{
    stream.WriteVInt(0);
    stream.WriteVInt(0);
    stream.WriteVInt(0);
    stream.WriteVInt(0);
    goto AfterStarrRoad;
}

// === Первый и второй бравлеры ===
int firstBrawlerId = availableBrawlers[0];
int secondBrawlerId = availableBrawlers.Count > 1 ? availableBrawlers[1] : firstBrawlerId;
var futureBrawlers = availableBrawlers.Skip(2).ToList();

// === Функция: получить стоимость в (Rare Tokens, Gems) ===
(int tokens, int gems) GetCost(int brawlerId)
{
    CharacterData characterData = DataTables.Get(16).GetData<CharacterData>(brawlerId);
    if (characterData == null) return (160, 29);

    CardData cardData = DataTables.Get(23).GetData<CardData>(characterData.Name + "_unlock");
    if (cardData == null) return (160, 29);

    return cardData.Rarity switch
    {
        "rare" => (160, 29),
        "super_rare" => (430, 79),
        "epic" => (925, 169),
        "mega_epic" => (1900, 349),
        "legendary" => (3800, 699),
        _ => (160, 29)
    };
}

// === Получаем количество собранных Rare Tokens ===
int collectedTokens = HomeMode.Avatar.RareTokens;

// === Записываем Starr Road ===
stream.WriteVInt(2); // Всегда 2 активных выбора

// --- ПЕРВЫЙ БОЕЦ ---
stream.WriteDataReference(16, firstBrawlerId);
var (tokens1, gems1) = GetCost(firstBrawlerId);
stream.WriteVInt(tokens1);                        // Rare Tokens цена
stream.WriteVInt(gems1);                          // ✅ Gems цена
stream.WriteVInt(0);                              // 0x0
stream.WriteVInt(collectedTokens);                // ✅ Прогресс: собранные Rare Tokens
stream.WriteVInt(1);                              // Позиция: 1
stream.WriteVInt(0);                              // 0x0

// --- ВТОРОЙ БОЕЦ ---
stream.WriteDataReference(16, secondBrawlerId);
var (tokens2, gems2) = GetCost(secondBrawlerId);
stream.WriteVInt(tokens2);                        // Rare Tokens
stream.WriteVInt(gems2);                          // ✅ Gems цена
stream.WriteVInt(0);                              // 0x0
stream.WriteVInt(0);                              // ✅ 0 — не у первого
stream.WriteVInt(2);                              // Позиция: 2
stream.WriteVInt(0);                              // 0x0

// === Количество будущих бравлеров ===
stream.WriteVInt(futureBrawlers.Count);

// --- Записываем будущих бравлеров ---
for (int i = 0; i < futureBrawlers.Count; i++)
{
    int brawlerId = futureBrawlers[i];
    var (tokens, gems) = GetCost(brawlerId);

    stream.WriteDataReference(16, brawlerId);
    stream.WriteVInt(tokens);           // Rare Tokens
    stream.WriteVInt(gems);             // Gems
    stream.WriteVInt(0);                // 0x0
    stream.WriteVInt(0);                // ✅ 0 — у будущих не показывается прогресс
    stream.WriteVInt(i + 3);            // Позиция: 3, 4, 5...
    stream.WriteVInt(0);                // 0x0
}

// === Конец ===
stream.WriteVInt(0); // Unknown
stream.WriteVInt(0); // Unknown

AfterStarrRoad:
// Продолжение кода...

            stream.WriteVInt(76);
            for (int i = 0; i < 76; i++)
            {
                Hero hero = HomeMode.Avatar.GetHero(GlobalId.CreateGlobalId(16, i));
                int mastery = 0;
                int claimMastery = 0;
                if (hero != null) mastery = hero.MasteryPoints;
                if (hero != null) claimMastery = hero.ClaimedMasteryLVL;

                stream.WriteVInt(mastery);
                stream.WriteVInt(claimMastery);
                stream.WriteDataReference(16, i);
                Console.WriteLine(i);
            }

            DefaultBattleCard.Encode(stream);

            stream.WriteVInt(0); //sub_D5C83C

            StarrDrop.Encode(stream, HomeMode);

            stream.WriteBoolean(false);//v53
        }

        public int TimerMath(DateTime timer_start, DateTime timer_end)
        {
            {
                DateTime timer_now = DateTime.Now;
                if (timer_now > timer_start)
                {
                    if (timer_now < timer_end)
                    {
                        int time_sec = (int)(timer_end - timer_now).TotalSeconds;
                        return time_sec;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    return -1;
                }
            }
        }
    }
}
