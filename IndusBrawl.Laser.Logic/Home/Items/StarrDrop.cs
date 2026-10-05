using System.IO;
using IndusBrawl.Laser.Logic.Home;
using System;
using System.Collections.Generic;
using System.Linq;
using IndusBrawl.Laser.Titan.DataStream;
using Newtonsoft.Json;
using System.Reflection.Metadata.Ecma335;
using System.Data;
using IndusBrawl.Laser.Logic.Data;
using IndusBrawl.Laser.Logic.Home.Gatcha;
using IndusBrawl.Laser.Logic.Message.Home;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Reflection;
using IndusBrawl.Laser.Logic.Home.Items;

namespace StarrDrop
{
    [JsonObject(MemberSerialization.OptOut)]
    public class Possibility
    {
        public int Type { get; set; }
        public int Ammount { get; set; }
        public int DataGlobalID { get; set; }
        public int SkinGlobalID { get; set; }
    }


    public class StarrDrop
    {
        public Random random = new Random();
        public static int GetRandomSuperRareSkin()
        {
            List<int> globalIds = new List<int>();

            foreach (SkinData emoteData in DataTables.Get(DataType.Skin).GetDatas())
            {
                if (emoteData.PriceGems == 79)
                {
                    globalIds.Add(DataTables.Get(DataType.Skin).GetData<SkinData>(emoteData.Name).GetGlobalId());
                }
            }

            if (globalIds.Count > 0)
            {
                Random random = new Random();
                return globalIds[random.Next(globalIds.Count)];
            }

            return -1;
        }

        public static int GetRandomEpicSkin()
        {
            List<int> globalIds = new List<int>();

            foreach (SkinData emoteData in DataTables.Get(DataType.Skin).GetDatas())
            {
                if (emoteData.PriceGems == 149)
                {
                    globalIds.Add(DataTables.Get(DataType.Skin).GetData<SkinData>(emoteData.Name).GetGlobalId());
                }
            }

            if (globalIds.Count > 0)
            {
                Random random = new Random();
                return globalIds[random.Next(globalIds.Count)];
            }

            return -1;
        }

        public static int GetRandomRareSkin()
        {
            List<int> globalIds = new List<int>();

            foreach (SkinData emoteData in DataTables.Get(DataType.Skin).GetDatas())
            {
                if (emoteData.PriceGems == 29)
                {
                    globalIds.Add(DataTables.Get(DataType.Skin).GetData<SkinData>(emoteData.Name).GetGlobalId());
                }
            }

            if (globalIds.Count > 0)
            {
                Random random = new Random();
                return globalIds[random.Next(globalIds.Count)];
            }

            return -1;
        }

        public static int GetRandomDefPin()
        {
            List<int> globalIds = new List<int>();

            foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
            {
                if (emoteData.Rarity == "COMMON" && emoteData.Skin == null)
                {
                    globalIds.Add(DataTables.Get(DataType.Emote).GetData<EmoteData>(emoteData.Name).GetGlobalId());
                }
            }

            if (globalIds.Count > 0)
            {
                Random random = new Random();
                return globalIds[random.Next(globalIds.Count)];
            }

            return -1;
        }

        public static int GetRandomRarefPin()
        {
            List<int> globalIds = new List<int>();

            foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
            {
                if (emoteData.Rarity == "RARE" && emoteData.Skin == null)
                {
                    globalIds.Add(DataTables.Get(DataType.Emote).GetData<EmoteData>(emoteData.Name).GetGlobalId());
                }
            }

            if (globalIds.Count > 0)
            {
                Random random = new Random();
                return globalIds[random.Next(globalIds.Count)];
            }

            return -1;
        }

        public static int GetRandomEpicPin()
        {
            List<int> globalIds = new List<int>();

            foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
            {
                if (emoteData.Rarity == "EPIC" && emoteData.Skin == null)
                {
                    globalIds.Add(DataTables.Get(DataType.Emote).GetData<EmoteData>(emoteData.Name).GetGlobalId());
                }
            }

            if (globalIds.Count > 0)
            {
                Random random = new Random();
                return globalIds[random.Next(globalIds.Count)];
            }

            return -1;
        }

        public static int GetRandomThumbnail()
        {
            List<int> globalIds = new List<int>();

            foreach (PlayerThumbnailData emoteData in DataTables.Get(DataType.PlayerThumbnail).GetDatas())
            {
                if (emoteData.IsAvailableForOffers)
                {
                    globalIds.Add(DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(emoteData.Name).GetGlobalId());
                }
            }

            if (globalIds.Count > 0)
            {
                Random random = new Random();
                return globalIds[random.Next(globalIds.Count)];

            }

            return -1;
        }

        public static int GetRandomBrawlerByRarity(string rare)
        {
            List<int> globalIds = new List<int>();

            foreach (CharacterData emoteData in DataTables.Get(DataType.Character).GetDatas())
            {
                CardData card = DataTables.Get(DataType.Card).GetData<CardData>(emoteData.Name + "_unlock");
                if (card!=null  && card.Rarity == rare && !emoteData.Disabled && !ReleaseEntry.LogicReleaaseEntry.Contains(emoteData.GetInstanceId()))
                {
                    globalIds.Add(DataTables.Get(DataType.Character).GetData<CharacterData>(emoteData.Name).GetGlobalId());
                }
            }

            if (globalIds.Count > 0)
            {
                Random random = new Random();
                return globalIds[random.Next(globalIds.Count)];

            }

            return -1;
        }

        public int Rarity;

        /* Explanation of the rarity of stardrops
         * 0 - Rare
         * 1 - SuperRare
         * 2 - Epic
         * 3 - Mythic
         * 4 - Legendary
         * 5 - Hypercharged
        */

        public Possibility data = new Possibility();
        /* Possibility - crate with drop data
         *  меня в дестве били 
        */

        
        public StarrDrop(HomeMode mode)
        {
            if (mode == null) return;
            GenerateDrop(mode);
        }

        public void GenerateDropByRarity(HomeMode init, int rarity)
        {
            ClientHome home = init.Home;
            if (home.StarrDrop == null) home.StarrDrop = new StarrDrop(init);

            if (rarity == 0)
            {
                home.StarrDrop.Rarity = 0;
                GenerateRareDrop(init);
            }
            else if (rarity == 1)
            {
                home.StarrDrop.Rarity = 1;
                GenerateSuperRareDrop(init);
            }
            else if (rarity == 2)
            {
                home.StarrDrop.Rarity = 2;
                GenerateEpicDrop(init);
            }
            else if (rarity == 3)
            {
                home.StarrDrop.Rarity = 3;
                GenerateMythicDrop(init);
            }
            else if (rarity == 4)
            {
                home.StarrDrop.Rarity = 4;
                GenerateLegendaryDrop(init);
            }
        }
        // ---------- Содержимое стардропов ----------
        // Шансы и награды читаются из starrdrop_loot.json (перечитывается на лету, без перезапуска сервера).
        // Если файла нет или он повреждён, используются значения по умолчанию ниже.
        public class LootEntry
        {
            public string Kind { get; set; }        // Coins, PowerPoints, TokenDoubler, Credits, Bling, Pin, Thumbnail, Brawler, Skin
            public int Weight { get; set; }         // вес (шанс = вес / сумма весов таблицы)
            public int Amount { get; set; }         // количество ресурса
            public string Sub { get; set; }         // Pin: COMMON/RARE/EPIC; Brawler: rare/super_rare/epic/mega_epic/legendary
            public int[] Prices { get; set; }       // Skin: цены скинов в гемах, из которых выбирать (29, 79, 149, 199, 299)
            public int Fallback { get; set; }       // блинги, если предмет уже есть у игрока
        }

        public class LootConfig
        {
            public int[] RarityWeights { get; set; }                 // редкий, сверхредкий, эпический, мифический, легендарный
            public Dictionary<string, List<LootEntry>> Tables { get; set; }
        }

        private const string LOOT_PATH = "starrdrop_loot.json";
        private static readonly object _lootLock = new object();
        private static LootConfig _loot;
        private static DateTime _lootLoadedAt;

        private static LootEntry E(string kind, int weight, int amount = 1, string sub = null, int[] prices = null, int fallback = 0)
        {
            return new LootEntry { Kind = kind, Weight = weight, Amount = amount, Sub = sub, Prices = prices, Fallback = fallback };
        }

        public static LootConfig DefaultLoot()
        {
            return new LootConfig
            {
                RarityWeights = new[] { 45, 27, 16, 8, 4 },
                Tables = new Dictionary<string, List<LootEntry>>
                {
                    ["0"] = new List<LootEntry>
                    {
                        E("Coins", 30, 100), E("PowerPoints", 22, 40), E("TokenDoubler", 13, 120), E("Credits", 10, 10), E("Bling", 10, 100),
                        E("Pin", 10, 1, "COMMON", fallback: 100), E("Skin", 5, 1, prices: new[] { 29 }, fallback: 250),
                    },
                    ["1"] = new List<LootEntry>
                    {
                        E("Coins", 26, 200), E("PowerPoints", 20, 80), E("TokenDoubler", 12, 220), E("Credits", 8, 30), E("Bling", 10, 150),
                        E("Pin", 10, 1, "COMMON", fallback: 100), E("Pin", 4, 1, "RARE", fallback: 150), E("Skin", 10, 1, prices: new[] { 29 }, fallback: 250),
                    },
                    ["2"] = new List<LootEntry>
                    {
                        E("Coins", 18, 400), E("PowerPoints", 14, 150), E("TokenDoubler", 10, 500),
                        E("Pin", 10, 1, "COMMON", fallback: 100), E("Pin", 8, 1, "RARE", fallback: 150), E("Brawler", 5, 1, "rare", fallback: 1000),
                        E("Skin", 20, 1, prices: new[] { 29 }, fallback: 250), E("Skin", 15, 1, prices: new[] { 79 }, fallback: 500),
                    },
                    ["3"] = new List<LootEntry>
                    {
                        E("Coins", 12, 800), E("PowerPoints", 8, 300),
                        E("Pin", 6, 1, "COMMON", fallback: 100), E("Pin", 8, 1, "RARE", fallback: 250), E("Pin", 8, 1, "EPIC", fallback: 350),
                        E("Thumbnail", 6, 1, fallback: 250),
                        E("Brawler", 3, 1, "rare", fallback: 1000), E("Brawler", 5, 1, "super_rare", fallback: 1000),
                        E("Brawler", 4, 1, "epic", fallback: 1000), E("Brawler", 5, 1, "mega_epic", fallback: 1000),
                        E("Skin", 10, 1, prices: new[] { 29 }, fallback: 250), E("Skin", 17, 1, prices: new[] { 79 }, fallback: 500),
                        E("Skin", 8, 1, prices: new[] { 149 }, fallback: 2350),
                    },
                    ["4"] = new List<LootEntry>
                    {
                        E("Pin", 15, 1, "EPIC", fallback: 350), E("Thumbnail", 5, 1, fallback: 250),
                        E("Brawler", 10, 1, "epic", fallback: 2000), E("Brawler", 8, 1, "mega_epic", fallback: 3000), E("Brawler", 12, 1, "legendary", fallback: 4000),
                        E("Skin", 15, 1, prices: new[] { 79 }, fallback: 500), E("Skin", 25, 1, prices: new[] { 149 }, fallback: 2350),
                        E("Skin", 10, 1, prices: new[] { 199, 299 }, fallback: 3000),
                    },
                }
            };
        }

        private static LootConfig Loot
        {
            get
            {
                lock (_lootLock)
                {
                    try
                    {
                        if (File.Exists(LOOT_PATH))
                        {
                            DateTime modified = File.GetLastWriteTimeUtc(LOOT_PATH);
                            if (_loot == null || modified != _lootLoadedAt)
                            {
                                LootConfig loaded = JsonConvert.DeserializeObject<LootConfig>(File.ReadAllText(LOOT_PATH));
                                if (loaded?.RarityWeights == null || loaded.RarityWeights.Length != 5 || loaded.Tables == null) throw new Exception("неполный файл");
                                _loot = loaded;
                                _lootLoadedAt = modified;
                                Console.WriteLine("[StarrDrop] Таблица наград загружена");
                            }
                            return _loot;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StarrDrop] Ошибка чтения {LOOT_PATH}: {ex.Message} - используются значения по умолчанию");
                    }
                    return _loot ?? (_loot = DefaultLoot());
                }
            }
        }

        private static T Weighted<T>(Random random, IList<T> items, Func<T, int> weight)
        {
            int total = items.Sum(i => Math.Max(0, weight(i)));
            if (total <= 0) return items[0];
            int roll = random.Next(total);
            foreach (T item in items)
            {
                roll -= Math.Max(0, weight(item));
                if (roll < 0) return item;
            }
            return items[items.Count - 1];
        }

        public void GenerateDrop(HomeMode init)
        {
            ClientHome home = init.Home;
            if (home.StarrDrop == null) home.StarrDrop = new StarrDrop(init);
            int[] weights = Loot.RarityWeights;
            int rarity = Weighted(random, new[] { 0, 1, 2, 3, 4 }, r => weights[r]);
            home.StarrDrop.Rarity = rarity;
            GenerateLoot(init, rarity);
        }

        public void GenerateRareDrop(HomeMode init) => GenerateLoot(init, 0);
        public void GenerateSuperRareDrop(HomeMode init) => GenerateLoot(init, 1);
        public void GenerateEpicDrop(HomeMode init) => GenerateLoot(init, 2);
        public void GenerateMythicDrop(HomeMode init) => GenerateLoot(init, 3);
        public void GenerateLegendaryDrop(HomeMode init) => GenerateLoot(init, 4);

        private static void SetReward(ClientHome home, int type, int amount, int dataId = 0, int skinId = 0)
        {
            home.StarrDrop.data.Type = type;
            home.StarrDrop.data.Ammount = amount;
            home.StarrDrop.data.DataGlobalID = dataId;
            home.StarrDrop.data.SkinGlobalID = skinId;
        }

        private void GenerateLoot(HomeMode init, int rarity)
        {
            ClientHome home = init.Home;
            LootConfig loot = Loot;
            if (!loot.Tables.TryGetValue(rarity.ToString(), out List<LootEntry> table) || table == null || table.Count == 0)
            {
                table = DefaultLoot().Tables[rarity.ToString()];
            }

            LootEntry entry = Weighted(random, table, e => e.Weight);
            int amount = Math.Max(1, entry.Amount);
            int fallback = entry.Fallback > 0 ? entry.Fallback : 100;

            switch (entry.Kind)
            {
                case "Coins": SetReward(home, 7, amount); break;
                case "PowerPoints": SetReward(home, 24, amount); break;
                case "TokenDoubler": SetReward(home, 2, amount); break;
                case "Credits": SetReward(home, 22, amount); break;
                case "Bling": SetReward(home, 25, amount); break;

                case "Skin":
                {
                    // Только скины, которых у игрока ещё нет
                    var prices = entry.Prices ?? new[] { 29 };
                    var pool = new List<int>();
                    foreach (SkinData skin in DataTables.Get(DataType.Skin).GetDatas())
                    {
                        if (skin.Disabled || !prices.Contains(skin.PriceGems)) continue;
                        if (home.UnlockedSkins != null && home.UnlockedSkins.Contains(skin.GetGlobalId())) continue;
                        pool.Add(skin.GetGlobalId());
                    }
                    if (pool.Count == 0) SetReward(home, 25, fallback);
                    else SetReward(home, 9, 1, 0, pool[random.Next(pool.Count)]);
                    break;
                }

                case "Pin":
                {
                    var pool = new List<int>();
                    foreach (EmoteData emote in DataTables.Get(DataType.Emote).GetDatas())
                    {
                        if (emote.Disabled || emote.Skin != null || emote.Rarity != (entry.Sub ?? "COMMON")) continue;
                        if (home.UnlockedEmotes != null && home.UnlockedEmotes.Contains(emote.GetGlobalId())) continue;
                        pool.Add(emote.GetGlobalId());
                    }
                    if (pool.Count == 0) SetReward(home, 25, fallback);
                    else SetReward(home, 11, 1, pool[random.Next(pool.Count)]);
                    break;
                }

                case "Thumbnail":
                {
                    int id = GetRandomThumbnail();
                    if (id == -1 || (home.UnlockedThumbnails != null && home.UnlockedThumbnails.Contains(id))) SetReward(home, 25, fallback);
                    else SetReward(home, 11, 1, id);
                    break;
                }

                case "Brawler":
                {
                    int id = GetRandomBrawlerByRarity(entry.Sub ?? "rare");
                    if (id == -1 || init.Avatar.GetHero(id) != null) SetReward(home, 25, fallback);
                    else SetReward(home, 1, 1, id);
                    break;
                }

                default:
                    SetReward(home, 7, 100);
                    break;
            }
        }

        public void Encode(ByteStream byteStream, HomeMode player)
        {
            byteStream.WriteVInt(5);
            for (int i = 0; i < 5; i++)
            {
                byteStream.WriteDataReference(80, i);
                byteStream.WriteVInt(-1);
                byteStream.WriteVInt(0);
            }

            // Отладочная сборка клиента показала: старый вариант ниже даёт клиенту 0 контейнеров и 1 награду,
            // из-за чего он каждую секунду пишет ошибку "Containers and rewards counts must match".
            // Правильное пустое состояние: 0 контейнеров, 0 наград. Пока только для испытательных аккаунтов.
            if (IndusBrawl.Laser.Logic.Home.StarrDropLab.For(player) is IndusBrawl.Laser.Logic.Home.StarrDropLabConfig lab && lab.TwoPhase)
            {
                byteStream.WriteVInt(0); // ожидающие контейнеры
                byteStream.WriteVInt(0); // ожидающие награды
                byteStream.WriteInt(0);
                byteStream.WriteVInt(0); // wins
                byteStream.WriteVInt(0);
                byteStream.WriteVInt(4); // timer
                byteStream.WriteVInt(0);
                byteStream.WriteVInt(0);
                return;
            }

            byteStream.WriteVInt(0);
            {
                

               // byteStream.WriteDataReference(80, player.Home.StarrDrop.Rarity);
            }
            byteStream.WriteVInt(1);
            { 
                byteStream.WriteByte(1);
                byteStream.WriteVInt(1);
                byteStream.WriteVInt(1);

                byteStream.WriteDataReference(0, 0);
                byteStream.WriteVInt(0);
            }
            byteStream.WriteInt(-1788180018);
            byteStream.WriteVInt(0); // wins
            byteStream.WriteVInt(0);
            byteStream.WriteVInt(4); // timer
            byteStream.WriteVInt(0);
            byteStream.WriteVInt(0);
        }
    }
}
