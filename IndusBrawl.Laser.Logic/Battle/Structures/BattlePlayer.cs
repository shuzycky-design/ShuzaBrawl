﻿namespace IndusBrawl.Laser.Logic.Battle.Structures
{
    using Newtonsoft.Json;
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Avatar.Structures;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Logic.Battle.Objects;
    using IndusBrawl.Laser.Logic.Listener;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Math;
    using IndusBrawl.Laser.Titan.Util;
    using IndusBrawl.Laser.Logic.Data;
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using IndusBrawl.Laser.Logic.Home.Items;

    public class BattlePlayer
    {
        public long AccountId;
        public int PlayerIndex;
        public int TeamIndex;
        public Dictionary<int, int> Emotes = new Dictionary<int, int>();
        public long TeamId = -1;
        public int HeroIndex;
        public int HeroIndexMax;
        public PlayerDisplayData DisplayData;
        public int CharacterId => CharacterIds[HeroIndex];
        public int SkinId => SkinIds[HeroIndex];
        public long SessionId;
        public LogicGameListener GameListener;
        public int[] CharacterIds = new int[3];
        public int[] SkinIds = new int[3];
        public int OwnObjectId;
        public int PreviousObjectId;
        public int ControlTicksLeft;
        public int LastHandledInput;
        public int Trophies, HighestTrophies;
        public int HeroPowerLevel;
        private int Score;
        private LogicVector2 SpawnPoint;
        public int StartUsingPinTicks;
        public int Likes;   // нажатия "палец вверх" на экране результатов
        public int PinIndex;
        public int LastPinUseTicks;

        // Поля для переподключения (ДОБАВЬТЕ ИХ ЗДЕСЬ!)
        public bool IsConnected { get; set; } = true;
        public bool NeedsFullState { get; set; } = false;
        public DateTime LastReconnectTime { get; set; }
        public int ReconnectAttempts { get; set; }
                public Dictionary<int, int> Spray = new Dictionary<int, int>();

        // Data references
        public SkinConfData SkinConfData => SkinId == 0 ? null : DataTables.Get(DataType.SkinConf).GetData<SkinConfData>(
            DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(SkinId).Conf);
        public SkinData SkinData => SkinId == 0 ? null : DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(SkinId);
        public CardData AccessoryCardData => AccessoryCardDatas[HeroIndex];
        public CardData[] AccessoryCardDatas;
        public AccessoryData AccessoryData => AccessoryDatas[HeroIndex];
        public AccessoryData[] AccessoryDatas;
        public CardData StarPowerData => StarPowerDatas[HeroIndex];
        public CardData[] StarPowerDatas;
        public Accessory Accessory;
        public CharacterData CharacterData => CharacterDatas[HeroIndex];
        public CharacterData[] CharacterDatas;
        public GearData Gear1;
        public GearData Gear2;
        public CardData[] OverChargeDatas;
        public CardData OverChargeData => OverChargeDatas[HeroIndex];
        public int PowerLevel;

        public int Bot;
        public bool IsAdmin;

        private int UltiCharge;
        private int OverCharge;

        public bool OverCharging;
        public bool OverChargeActivated;
        public bool OverChargeStarted;
        public bool OverChargeEnded;
        public bool IsAlive;
        public int BattleRoyaleRank;

        public List<PlayerKillEntry> KillList;
        public int Kills;
        public int Damage;
        public int Heals;
        public int DamagePerSec; // Новое поле
        public int Deaths;       // Новое поле
        public int DeathTick;
                public int SprayIndex;
                        public Item CharacterSpray;

        public int T1, T2, E, Ti;

        public BattlePlayer()
        {
            DisplayData = new PlayerDisplayData();
            SpawnPoint = new LogicVector2();
            StartUsingPinTicks = -9999;
            BattleRoyaleRank = -1;
            KillList = new List<PlayerKillEntry>();
            UltiCharge = 0;
            OverCharge = 0;
            DeathTick = 1;
            
            // Инициализация полей переподключения
            IsConnected = true;
            NeedsFullState = false;
            LastReconnectTime = DateTime.MinValue;
            ReconnectAttempts = 0;
        }

        public void Healed(int heals) => Heals += heals;
        public void DamageDealed(int damage) => Damage += damage;

        public void KilledPlayer(int index, int bountyStars)
        {
            KillList.Add(new PlayerKillEntry { PlayerIndex = index, BountyStarsEarned = bountyStars });
            Kills++;
        }

        public bool HasUlti() => UltiCharge >= 4000;
        public bool OverChargeReady() => OverCharge >= 4000;
        public int GetUltiCharge() => UltiCharge;
        public int GetOverCharge() => OverCharge;

        public int GetCardValueForPassive(string type, int valueIndex)
        {
            if (StarPowerData?.Type == type)
            {
                return valueIndex switch
                {
                    0 => StarPowerData.Value,
                    1 => StarPowerData.Value2,
                    2 => StarPowerData.Value3,
                    _ => -1
                };
            }
            return -1;
        }

        public void AddUltiCharge(int amount)
        {
            UltiCharge = LogicMath.Clamp(UltiCharge + amount, 0, 4000);
        }

        public int GetGearBoost(int logicType)
        {
            if (Gear1?.LogicType == logicType) return Gear1.ModifierValue;
            if (Gear2?.LogicType == logicType) return Gear2.ModifierValue;
            return 0;
        }

        public void ChargeUlti(int amount, bool isUlti, bool isPassive)
        {
            int chargeMul = isPassive ? CharacterData.UltiChargeUltiMul : CharacterData.UltiChargeMul;
            int result = UltiCharge + (chargeMul * amount / 100);
            UltiCharge = LogicMath.Min(4000, result);

            int overChargeDiv = CharacterData.Name == "Roller" ? 20 : 2;
            if (HasOverCharge())
            {
                OverCharge = LogicMath.Min(4000, OverCharge + chargeMul * amount / 100 / overChargeDiv);
            }
        }

        public void UseUlti()
        {
            if (!IsAdmin) UltiCharge = 0;
        }

        public void UseOverCharge(Character character)
        {
            if (OverChargeReady() && !OverCharging) OverCharging = true;
        }

        public void UpdateOverCharge()
        {
            OverChargeStarted = OverCharging && !OverChargeActivated;
            OverChargeActivated = OverCharging;
            OverChargeEnded = false;

            if (OverCharging)
            {
                OverCharge -= 40;
                if (!IsAlive || OverCharge <= 0)
                {
                    OverChargeEnded = true;
                    OverCharge = 0;
                    OverCharging = false;
                }
            }
        }

        public int IsBot() => Bot;

        public BattlePlayer(ClientHome home, ClientAvatar avatar) : this()
        {
            Home = home;
            Avatar = avatar;
        }

        public void AddScore(int amount) => Score += amount;
        public void ResetScore() => Score = 0;

        public EmoteData GetDefaultEmoteForCharacter(string character, string type)
        {
            foreach (EmoteData data in DataTables.Get(DataType.Emote).GetDatas())
            {
                if (data.Character == character && data.EmoteType == type) return data;
            }
            return null;
        }

        public void UsePin(int index, int ticks)
        {
            StartUsingPinTicks = ticks;
            PinIndex = index;
            LastPinUseTicks = ticks;
        }

        public bool IsUsingPin(int ticks) => ticks - StartUsingPinTicks < 80;

        public void SetHeroIndex(int index)
        {
            HeroIndex = index;
            Accessory = AccessoryDatas[index] == null ? null : new Accessory(AccessoryDatas[index]);
        }

        public int GetScore() => Score;

        public void SetSpawnPoint(int x, int y) => SpawnPoint.Set(x, y);
        public LogicVector2 GetSpawnPoint() => SpawnPoint.Clone();

        public bool HasOverCharge() => OverChargeData != null;

        public void Encode(ByteStream stream)
        {
            stream.WriteLong(AccountId);
            stream.WriteVInt(PlayerIndex);
            stream.WriteVInt(TeamIndex);
            stream.WriteVInt(0);
            stream.WriteInt(0);

            stream.WriteVInt(HeroIndexMax + 1);
            for (int i = 0; i <= HeroIndexMax; i++)
            {
                ByteStreamHelper.WriteDataReference(stream, CharacterIds[i] > 16000080 ? 16000000 : CharacterIds[i]);

                if (stream.WriteBoolean(true))
                {
                    // Logic Hero Upgrades
                    stream.WriteVInt(HeroPowerLevel);
                    ByteStreamHelper.WriteDataReference(stream, StarPowerDatas[i]);
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    ByteStreamHelper.WriteDataReference(stream, OverChargeDatas[i]);
                }

                if (stream.WriteBoolean(true))
                {
                    stream.WriteVInt(5); // ← Поменял обратно на 5 эмодзи

                    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(52, Emotes.TryGetValue(1, out var value) ? value : 134));
                    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(52, Emotes.TryGetValue(2, out var value2) ? value2 : 134));
                    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(52, Emotes.TryGetValue(3, out var value3) ? value3 : 134));
                    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(52, Emotes.TryGetValue(4, out var value4) ? value4 : 134));
                    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(52, Emotes.TryGetValue(5, out var value5) ? value5 : 134));
                    stream.WriteVInt(5);
                }

               if (stream.WriteBoolean(true))
{
    int vanityCount = 0; 
    if (false) vanityCount = 3;
    else if (Spray.TryGetValue(4, out int value123) && value123 != -1) vanityCount = 5;
    else vanityCount = 5;

    stream.WriteVInt(vanityCount);
        ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(68, Spray.TryGetValue(0, out var value) ? value : 7 - 3));
    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(68, Spray.TryGetValue(1, out var value2) ? value2 : 8-3));
    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(68, Spray.TryGetValue(2, out var value3) ? value3 : 18-3));
    if (vanityCount > 3) ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(68, Spray.TryGetValue(3, out var value4) ? value4 : 4));
    if(vanityCount == 5) ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(68, Spray.TryGetValue(4, out var value5) ? value5 : 5));

}

                string tmp = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(CharacterIds[i]).Name;
                if (tmp == "MechaDude" || tmp == "CannonGirl") ByteStreamHelper.WriteDataReference(stream, null);
                else ByteStreamHelper.WriteDataReference(stream, SkinIds[i]);

                ByteStreamHelper.WriteDataReference(stream, null);
                stream.WriteVInt(0);
            }

            DisplayData.Encode(stream);
            stream.WriteBoolean(false);
            if (stream.WriteBoolean(false))
            {
                stream.WriteVLong(AccountId);
                stream.WriteString("SB");
                ByteStreamHelper.WriteDataReference(stream, null);
            }

            // New fields
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            ByteStreamHelper.WriteDataReference(stream, T1);
            ByteStreamHelper.WriteDataReference(stream, T2);
            ByteStreamHelper.WriteDataReference(stream, E);
            ByteStreamHelper.WriteDataReference(stream, Ti);
            stream.WriteVInt(0);
        }

        [JsonIgnore] public readonly ClientHome Home;
        [JsonIgnore] public readonly ClientAvatar Avatar;

     ///////////   public static BattlePlayer Create(ClientHome home, ClientAvatar avatar, int playerIndex, int teamIndex)
     public static BattlePlayer Create(ClientHome home, ClientAvatar avatar, int playerIndex, int teamIndex, EventData e = null, int iw = -1)
        {
            if (home == null || avatar == null)
            {
                Console.WriteLine($"[BattlePlayer] Create failed: home={home != null}, avatar={avatar != null}");
                return null;
            }

            if (home.CharacterIds == null || home.CharacterIds.Length == 0 || home.CharacterIds[0] <= 0)
            {
                Console.WriteLine($"[BattlePlayer] Invalid CharacterIds[0]: {home.CharacterIds?[0] ?? -1}");
                return null;
            }

            int brawlerId = home.CharacterIds[0];
            Hero hero = avatar.GetHero(brawlerId);

            if (hero == null)
            {
                Console.WriteLine($"[BattlePlayer] Hero not found for ID: {brawlerId}");
                return null;
            }

            CharacterData characterData = DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(brawlerId);
            if (characterData == null)
            {
                Console.WriteLine($"[BattlePlayer] CharacterData not found for ID: {brawlerId}");
                return null;
            }

            BattlePlayer player = new BattlePlayer(home, avatar);
            player.DisplayData.Name = avatar.Name ?? "Player";
            player.AccountId = avatar.AccountId;
            player.PlayerIndex = playerIndex;
            player.TeamIndex = teamIndex;
            player.DisplayData.ThumbnailId = home.ThumbnailId;
            player.DisplayData.NameColorId = home.NameColorId;

            player.AccessoryCardDatas = new CardData[3];
            player.AccessoryDatas = new AccessoryData[3];
            player.StarPowerDatas = new CardData[3];
            player.CharacterDatas = new CharacterData[3];
            player.OverChargeDatas = new CardData[3];

            // HeroIndexMax зависит от количества бравлеров у игрока (максимум 3)
            player.HeroIndexMax = Math.Min(home.CharacterIds.Length - 1, 2);

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (i < home.CharacterIds.Length)
                    {
                        int charId = home.CharacterIds[i];
                        player.CharacterIds[i] = charId;
                        player.CharacterDatas[i] = DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(charId);

                        Hero heroForBrawler = avatar.GetHero(charId);
                        if (heroForBrawler != null)
                        {
                            if (heroForBrawler.SelectedSkinId > 0)
                            {
                                int skinId = GlobalId.CreateGlobalId(29, heroForBrawler.SelectedSkinId);
                                player.SkinIds[i] = skinId;
                            }

                            if (heroForBrawler.SelectedStarPowerId > 0)
                            {
                                player.StarPowerDatas[i] = DataTables.Get(23).GetData<CardData>(heroForBrawler.SelectedStarPowerId);
                            }

                            if (heroForBrawler.SelectedGadgetId > 0)
                            {
                                int accessoryId = GlobalId.CreateGlobalId(50, heroForBrawler.SelectedGadgetId);
                                Console.WriteLine($"Gadget id (global): {accessoryId}");
                                AccessoryData accessoryData = DataTables.Get(DataType.Accessory).GetDataByGlobalId<AccessoryData>(accessoryId);
                                if (accessoryData != null)
                                {
                                    player.AccessoryDatas[i] = accessoryData;
                                    player.AccessoryCardDatas[i] = DataTables.Get(23).GetDataByGlobalId<CardData>(GlobalId.CreateGlobalId(23, heroForBrawler.SelectedGadgetId));
                                }
                                else
                                {
                                    Console.WriteLine("ACCESSORY DATA IS NULL!!!");
                                }
                            }

                            if (player.CharacterDatas[i] != null)
                            {
                                CardData overCharge = DataTables.Get(23).GetData<CardData>(player.CharacterDatas[i].Name + "_overcharge");
                                if (overCharge != null && avatar.SPGS != null && avatar.SPGS.Contains(overCharge.GetGlobalId()))
                                {
                                    player.OverChargeDatas[i] = overCharge;
                                }
                                else
                                {
                                    player.OverChargeDatas[i] = null;
                                }
                            }
                        }
                        else
                        {
                            player.SkinIds[i] = 0;
                            player.StarPowerDatas[i] = null;
                            player.AccessoryDatas[i] = null;
                            player.AccessoryCardDatas[i] = null;
                            player.OverChargeDatas[i] = null;
                        }
                    }
                    else
                    {
                        // Если нет бравлера с таким индексом — ставим 0 или null
                        player.CharacterIds[i] = 0;
                        player.CharacterDatas[i] = null;
                        player.SkinIds[i] = 0;
                        player.StarPowerDatas[i] = null;
                        player.AccessoryDatas[i] = null;
                        player.AccessoryCardDatas[i] = null;
                        player.OverChargeDatas[i] = null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to load character slot {i} for account {avatar.AccountId}: {ex.Message}");
                    // Заполняем дефолтными значениями при ошибке
                    player.CharacterIds[i] = 0;
                    player.CharacterDatas[i] = null;
                    player.SkinIds[i] = 0;
                    player.StarPowerDatas[i] = null;
                    player.AccessoryDatas[i] = null;
                    player.AccessoryCardDatas[i] = null;
                    player.OverChargeDatas[i] = null;
                }
            }

            player.Trophies = hero.Trophies;
            player.HighestTrophies = hero.HighestTrophies;
            player.HeroPowerLevel = hero.PowerLevel;

            try
            {
                player.Emotes.Add(1, hero.emote != null && hero.emote.ContainsKey(1) ? hero.emote[1] : 0);
                player.Emotes.Add(2, hero.emote != null && hero.emote.ContainsKey(2) ? hero.emote[2] : 0);
                player.Emotes.Add(3, hero.emote != null && hero.emote.ContainsKey(3) ? hero.emote[3] : 0);
                player.Emotes.Add(4, home.PlayerSelectedEmotes != null ? home.PlayerSelectedEmotes.GetValueOrDefault(4, 28) : 28);
                player.Emotes.Add(5, home.PlayerSelectedEmotes != null ? home.PlayerSelectedEmotes.GetValueOrDefault(5, 82) : 82);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load emotes for account {avatar.AccountId}: {ex.Message}");
                // Загружаем дефолтные эмоты при ошибке
                if (!player.Emotes.ContainsKey(1)) player.Emotes.Add(1, 0);
                if (!player.Emotes.ContainsKey(2)) player.Emotes.Add(2, 0);
                if (!player.Emotes.ContainsKey(3)) player.Emotes.Add(3, 0);
                if (!player.Emotes.ContainsKey(4)) player.Emotes.Add(4, 28);
                if (!player.Emotes.ContainsKey(5)) player.Emotes.Add(5, 82);
            }

            try
            {
                player.T1 = home.DefaultBattleCard != null ? home.DefaultBattleCard.Thumbnail1 : 0;
                player.T2 = home.DefaultBattleCard != null ? home.DefaultBattleCard.Thumbnail2 : 0;
                player.E = home.DefaultBattleCard != null ? home.DefaultBattleCard.Emote : 0;
                player.Ti = home.DefaultBattleCard != null ? home.DefaultBattleCard.Title : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load battle card for account {avatar.AccountId}: {ex.Message}");
                player.T1 = 0;
                player.T2 = 0;
                player.E = 0;
                player.Ti = 0;
            }
            player.Spray.Add(0,1);
            player.Spray.Add(1, 3);
            player.Spray.Add(2, 2);
            player.Spray.Add(3, 4);
            player.Spray.Add(4, 5);
            return player;
        }

        public static BattlePlayer CreateBotInfo(string name, int playerIndex, int teamIndex, int character = 16000000)
        {
            BattlePlayer player = new BattlePlayer();
            // Боту даём случайный ник, аватарку и цвет ника, чтобы он выглядел как живой игрок
            player.DisplayData.Name = IndusBrawl.Laser.Logic.Util.BotNames.Next();
            player.DisplayData.ThumbnailId = GlobalId.CreateGlobalId(28, IndusBrawl.Laser.Logic.Util.BotNames.NextInt(0, 30));
            player.DisplayData.NameColorId = GlobalId.CreateGlobalId(43, IndusBrawl.Laser.Logic.Util.BotNames.NextInt(0, 12));
            player.AccountId = 100000 + playerIndex;
            player.PlayerIndex = playerIndex;
            player.TeamIndex = teamIndex;
            player.SessionId = -1;
            player.Bot = 1;

            player.CharacterIds[0] = character;
            player.CharacterDatas = new CharacterData[3];
            player.CharacterDatas[0] = DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(character);

            player.AccessoryCardDatas = new CardData[3];
            player.AccessoryDatas = new AccessoryData[3];
            player.StarPowerDatas = new CardData[3];
            player.OverChargeDatas = new CardData[3];

            player.AccessoryCardDatas[0] = DataTables.Get(23).GetData<CardData>(255);
            if (player.AccessoryCardDatas[0] != null)
            {
                player.AccessoryDatas[0] = DataTables.Get(DataType.Accessory).GetData<AccessoryData>(player.AccessoryCardDatas[0].Name);
            }

            player.HeroIndexMax = 0;
            return player;
        }
        
        public void RefreshSlotFromPickedHero(int slot, int characterId, ClientAvatar avatar)
        {
            if (slot < 0 || slot > 2 || avatar == null || characterId <= 0) return;
            if (CharacterDatas == null) CharacterDatas = new CharacterData[3];
            if (AccessoryCardDatas == null) AccessoryCardDatas = new CardData[3];
            if (AccessoryDatas == null) AccessoryDatas = new AccessoryData[3];
            if (StarPowerDatas == null) StarPowerDatas = new CardData[3];
            if (OverChargeDatas == null) OverChargeDatas = new CardData[3];

            CharacterIds[slot] = characterId;
            CharacterDatas[slot] = DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(characterId);
            Hero heroForBrawler = avatar.GetHero(characterId);
            if (heroForBrawler != null)
            {
                if (heroForBrawler.SelectedSkinId > 0)
                    SkinIds[slot] = GlobalId.CreateGlobalId(29, heroForBrawler.SelectedSkinId);
                else
                    SkinIds[slot] = 0;

                if (heroForBrawler.SelectedStarPowerId > 0)
                {
                    CardData selectedSp = DataTables.Get(DataType.Card).GetData<CardData>(heroForBrawler.SelectedStarPowerId);
                    int selectedSpGlobalId = selectedSp?.GetGlobalId() ?? 0;
                    bool isSpOwned = selectedSp != null && selectedSp.MetaType == 4 && avatar.SPGS != null && avatar.SPGS.Contains(selectedSpGlobalId);
                    bool hasRequiredLevel = heroForBrawler.PowerLevel >= 9;
                    StarPowerDatas[slot] = (isSpOwned && hasRequiredLevel) ? selectedSp : null;
                }
                else
                    StarPowerDatas[slot] = null;

                if (heroForBrawler.SelectedGadgetId > 0)
                {
                    CardData selectedGadgetCard = DataTables.Get(DataType.Card).GetData<CardData>(heroForBrawler.SelectedGadgetId);
                    int selectedGadgetGlobalId = selectedGadgetCard?.GetGlobalId() ?? 0;
                    bool isGadgetOwned = selectedGadgetCard != null && selectedGadgetCard.MetaType == 5 && avatar.SPGS != null && avatar.SPGS.Contains(selectedGadgetGlobalId);
                    bool hasRequiredLevel = heroForBrawler.PowerLevel >= 7;
                    if (isGadgetOwned && hasRequiredLevel)
                    {
                        int accessoryId = GlobalId.CreateGlobalId(50, heroForBrawler.SelectedGadgetId);
                        AccessoryData accessoryData = DataTables.Get(DataType.Accessory).GetDataByGlobalId<AccessoryData>(accessoryId);
                        if (accessoryData != null)
                        {
                            AccessoryDatas[slot] = accessoryData;
                            AccessoryCardDatas[slot] = selectedGadgetCard;
                        }
                    }
                    else
                    {
                        AccessoryDatas[slot] = null;
                        AccessoryCardDatas[slot] = null;
                    }
                }
                else
                {
                    AccessoryDatas[slot] = null;
                    AccessoryCardDatas[slot] = null;
                }

                if (CharacterDatas[slot] != null)
                {
                    CardData overCharge = DataTables.Get(23).GetData<CardData>(CharacterDatas[slot].Name + "_overcharge");
                    OverChargeDatas[slot] = (overCharge != null && avatar.SPGS != null && avatar.SPGS.Contains(overCharge.GetGlobalId())) ? overCharge : null;
                }
                else
                    OverChargeDatas[slot] = null;

                if (slot == 0)
                {
                    Trophies = heroForBrawler.Trophies;
                    HighestTrophies = heroForBrawler.HighestTrophies;
                    HeroPowerLevel = heroForBrawler.PowerLevel;
                }
            }
            else
            {
                SkinIds[slot] = 0;
                StarPowerDatas[slot] = null;
                AccessoryDatas[slot] = null;
                AccessoryCardDatas[slot] = null;
                OverChargeDatas[slot] = null;
            }
        }

        public int GetSprayIDBySprayIndex(int i)
        {
            return Spray[i];
        }

        public static BattlePlayer CreateStoryModeDummy(string name, int playerIndex, int teamIndex, int characterId = 0, int skinId = 0, int accessoryId = 0)
        {
            BattlePlayer player = new BattlePlayer();
            player.DisplayData.Name = name;
            player.DisplayData.ThumbnailId = GlobalId.CreateGlobalId(28, 0);
            player.DisplayData.NameColorId = GlobalId.CreateGlobalId(43, 0);
            player.AccountId = 100000 + playerIndex;
            player.PlayerIndex = playerIndex;
            player.TeamIndex = teamIndex;
            player.SessionId = -1;

            int globalId = GlobalId.CreateGlobalId(16, characterId);
            player.CharacterIds[0] = globalId;
            player.CharacterDatas = new CharacterData[3];
            player.CharacterDatas[0] = DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(globalId);

            if (skinId != 0)
            {
                player.SkinIds[0] = GlobalId.CreateGlobalId(29, skinId);
            }

            player.AccessoryCardDatas = new CardData[3];
            player.AccessoryDatas = new AccessoryData[3];
            player.StarPowerDatas = new CardData[3];
            player.OverChargeDatas = new CardData[3];

            player.HeroIndexMax = 0;
            return player;
        }
    }
}