namespace IndusBrawl.Laser.Logic.Avatar
{
    using Newtonsoft.Json;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Logic.Friends;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;
    using System.IO;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Battle.Objects;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Security.Principal;
    using IndusBrawl.Laser.Logic.Avatar.Structures;

    public enum AllianceRole
    {
        None = 0,
        Member = 1,
        Leader = 2,
        Elder = 3,
        CoLeader = 4
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ClientAvatar
    {
        [JsonProperty] public long AccountId;
        [JsonProperty] public string PassToken;
        
        [JsonProperty] public bool IsVIP;
[JsonProperty] public DateTime VIPExpire;

        [JsonProperty] public string Name;
        [JsonProperty] public bool NameSetByUser;
        [JsonProperty] public int TutorialsCompletedCount = 2;

        [JsonIgnore] public HomeMode HomeMode;

        [JsonProperty] public int Gold;
        [JsonProperty] public int Diamonds;

        [JsonProperty] public List<Hero> Heroes;

        [JsonProperty] public int TrioWins;
        [JsonProperty] public int DuoWins;
        [JsonProperty] public int SoloWins;
        [JsonProperty] public int MaxWinstreak;

        [JsonProperty] public int Tokens;
        [JsonProperty] public int StarTokens;
        [JsonProperty] public int StarPoints;
        [JsonProperty] public int Blings;
        [JsonProperty] public int PowerPoints;
        [JsonProperty] public int RareTokens;

        [JsonProperty] public bool IsDev;
        [JsonProperty] public bool IsPremium;
        [JsonIgnore] public long RanledId;
        [JsonProperty] public long AllianceId;
        [JsonProperty] public AllianceRole AllianceRole;

        [JsonProperty] public DateTime LastOnline;

        [JsonProperty] public List<Friend> Friends;
        [JsonProperty] public bool Banned;

        [JsonProperty] public int ChatBanCount;
        [JsonProperty] public int BanCount;
        [JsonProperty] public int BanType;
        [JsonProperty] public int BanID;
        [JsonProperty] public string TextReason;
        [JsonProperty] public DateTime BanEndTime;

        [JsonProperty] public DateTime PremiumTime;
        

        [JsonProperty] public List<string> ClaimedRewards;

        [JsonIgnore] public int PlayerStatus;
        [JsonIgnore] public long TeamId;

        [JsonIgnore] public long BattleId;
        [JsonIgnore] public long LastRejoinBattleId;   // бой, в который игрока уже возвращали при входе
        [JsonIgnore] public long UdpSessionId;
        [JsonIgnore] public int TeamIndex;
        [JsonIgnore] public int OwnIndex;

        [JsonProperty] public int HighestTrophies;

        [JsonProperty] public int RollsSinceGoodDrop;

        [JsonProperty] public List<int> SPGS;
        [JsonProperty] public List<int> SelectedSPGS;

        [JsonProperty] public string Region;
        [JsonProperty] public string SupportedCreator;
        [JsonProperty] public int WinStreak;
        [JsonProperty] public int DoNotDisturb;
        [JsonProperty] public bool HasUsedFreeGear;
        [JsonProperty] public int FamePoints;
[JsonProperty] public string NamePrefix;
[JsonProperty] public int RankedRank = 0; // Начальный ранг (Bronze I)
[JsonProperty] public int RankedSoloProgress = 0;
[JsonProperty] public int RankedSoloRank = 0;
[JsonProperty] public int RankedSoloMaxProgress = 0;
[JsonProperty] public int RankedSoloMaxRank = 0;
[JsonProperty] public int RankedTrioProgress = 0;
[JsonProperty] public int RankedTrioRank = 0;
[JsonProperty] public int RankedTrioMaxProgress = 0;
[JsonProperty] public int RankedTrioMaxRank = 0;

[JsonProperty]
public bool HasDoubleTrophyVIP { get; set; }

[JsonProperty] 
public DateTime DoubleTrophyVIPEndTime { get; set; }

[JsonProperty]
public int NameColor { get; set; } = 0xFFFFFF; // по умолчанию белый

public int Gems { get; set; } = 0;
public bool IsPremiumUltra;
public DateTime PremiumUltraTime;
public List<int> UnlockedTitles { get; set; } = new List<int>();



        public int Trophies
        {
            get
            {
                int result = 0;
                foreach (Hero hero in Heroes.ToArray())
                {
                    result += hero.Trophies;
                }
                return result;
            }
        }
        
        public bool HasVIP()
{
    return IsVIP && VIPExpire > DateTime.UtcNow;
}


public string GetFullName()
{
    return string.IsNullOrEmpty(NamePrefix) ? Name : NamePrefix + "\n" + Name;
}

public void SetDevPrefix(bool enable)
{
    if (enable)
        NamePrefix = "[DEV]";
    else
        NamePrefix = null;
}



public void GiveVIP(int days)
{
    IsVIP = true;
    VIPExpire = DateTime.UtcNow.AddDays(days);
}

// --- VIP из маркета: x2 кубки + 100 гемов раз в неделю ---
public const int VipWeeklyGems = 100;
[JsonProperty] public DateTime VIPWeeklyGemsNext;

// Активирует VIP на days дней (если VIP уже активен - продлевает от даты окончания)
public void ActivateVip(int days)
{
    bool wasActive = HasVIP();
    DateTime from = wasActive ? VIPExpire : DateTime.UtcNow;
    IsVIP = true;
    VIPExpire = from.AddDays(days);
    IsPremium = true;
    PremiumTime = VIPExpire;
    HasDoubleTrophyVIP = true;
    DoubleTrophyVIPEndTime = VIPExpire;
    if (!wasActive) VIPWeeklyGemsNext = DateTime.UtcNow; // первая выдача сразу
}

// Начисляет накопившиеся еженедельные гемы VIP, возвращает сколько начислено
public int CollectVipWeeklyGems()
{
    if (!IsVIP || VIPWeeklyGemsNext == default) return 0;
    int total = 0;
    DateTime now = DateTime.UtcNow;
    while (VIPWeeklyGemsNext <= now && VIPWeeklyGemsNext < VIPExpire)
    {
        total += VipWeeklyGems;
        VIPWeeklyGemsNext = VIPWeeklyGemsNext.AddDays(7);
    }
    Diamonds += total;
    return total;
}

        public void AddTrophies(int t)
        {
            HighestTrophies = Math.Max(HighestTrophies, Trophies + t);
        }

        public int GetUnlockedBrawlersCountWithRarity(string rarity)
        {
            return Heroes.Count(x => x.CardData.Rarity == rarity);
        }

        public void ResetTrophies()
        {
            foreach (Hero hero in Heroes.ToArray())
            {
                hero.Trophies = 0;
                hero.HighestTrophies = 0;
            }
        }

        public int GetUnlockedHeroesCount()
        {
            return Heroes.Count;
        }

        public void UnlockHero(int characterId)
        {
            Hero heroEntry = new Hero(characterId);
            Heroes.Add(heroEntry);
        }

        public void UpgradeHero(int characterId)
        {
            Hero heroEntry = GetHero(characterId);
            if (heroEntry.SelectedOverChargeId == 0)
            {
                CardData o = heroEntry.GetDefaultMetaForHero(6);
                if (o != null) heroEntry.SelectedOverChargeId = o.GetInstanceId();
            }
        }

        public void RemoveHero(int characterId)
        {
            Heroes.RemoveAll(x => x.CharacterId == characterId);
        }

        public bool HasHero(int characterId)
        {
            return Heroes.Find(x => x.CharacterId == characterId) != null;
        }
        public Hero GetHero(int characterId)
        {
            return Heroes.Find(x => x.CharacterId == characterId);
        }
        public Hero GetHeroForCard(CardData cardData)
        {
            //Debugger.Print(DataTables.Get(16).GetData<CharacterData>(cardData.Target).GetInstanceId() + "");
            return GetHero(DataTables.Get(16).GetData<CharacterData>(cardData.Target).GetInstanceId() + 16000000);
        }


        public void SetEmoteForBrawler(int characterId, int slot, int pin)
        {
            Hero heroEntry = GetHero(characterId);
            heroEntry.emote[slot] = pin;
        }

        public bool UseDiamonds(int count)
        {
            if (count > Diamonds) return false;

            Diamonds -= count;
            return true;
        }

        public bool UseRareTokens(int count)
        {
            if (count > RareTokens) return false;

            RareTokens -= count;
            return true;
        }

        public bool UseBlings(int count)
        {
            if (count > Blings) return false;

            Blings -= count;
            return true;
        }

        public bool UseGold(int count)
        {
            if (count > Gold) return false;

            Gold -= count;
            return true;
        }

        public void AddDiamonds(int count)
        {
            Diamonds += count;
        }

        public void AddGold(int count)
        {
            Gold += count;
        }

        public bool UseTokens(int count)
        {
            if (count > Tokens) return false;

            Tokens -= count;
            return true;
        }

        public void AddTokens(int count)
        {
            HomeMode.Home.BrawlPassTokens += count;
        }

        public bool UseStarTokens(int count)
        {
            if (count > StarTokens) return false;

            StarTokens -= count;
            return true;
        }

        public void AddStarTokens(int count)
        {
            StarTokens += count;
        }

        public void AddPowerPoints(int count)
        {
            PowerPoints += count;
        }

        public void AddRareTokens(int count)
        {
            RareTokens += count;
        }

        public void AddBlings(int count)
        {
            Blings += count;
        }
        public void ConvertRecruitTokensToFame(int recruitTokens)
        {
            if (recruitTokens > 0)
            {
                FamePoints += recruitTokens;
                Console.WriteLine($"[ClientAvatar] Конвертировано {recruitTokens} Recruit Tokens в Fame Points. Всего Fame: {FamePoints}");
            }
        }
        public bool HasGearEquipped(int brawlerGlobalId, int gearId)
        {
           if (GetHero(brawlerGlobalId)?.GearData != null)
           {
               return GetHero(brawlerGlobalId).GearData.Exists(g => g.GetGlobalId() == GlobalId.CreateGlobalId(50, gearId));
           }
           return false;
        }
        public void MarkFreeGearAsUsed()
        {
           HasUsedFreeGear = true;
        }
        public List<GearData> GetGearsForBrawler(int brawlerGlobalId)
        {
            return GetHero(brawlerGlobalId)?.GearData ?? new List<GearData>();
        }
        public ClientAvatar()
        {
            Name = "ShuzaBrawl";
            Gold = 0;
            Diamonds = 0;

            Heroes = new List<Hero>();

            ClaimedRewards = new List<string>();
            SPGS = new List<int>();
            SelectedSPGS = new List<int>();
            IsDev = false;
            IsPremium = false;
            Region = "RU";
            HasUsedFreeGear = false;

            AllianceRole = AllianceRole.None;
            AllianceId = -1;

            LastOnline = DateTime.UtcNow;
            Friends = new List<Friend>();
            
    HasDoubleTrophyVIP = false;
    DoubleTrophyVIPEndTime = DateTime.MinValue;
            if (HighestTrophies == 0 && Trophies != 0)
            {
                HighestTrophies = Trophies;
            }
        }

        public void SkipTutorial()
        {
            TutorialsCompletedCount = 2;
        }

        public bool IsTutorialState()
        {
            return TutorialsCompletedCount < 2;
        }

        public Friend GetRequestFriendById(long id)
        {
            return Friends.Find(friend => friend.AccountId == id && friend.FriendState != 4);
        }

        public Friend GetAcceptedFriendById(long id)
        {
            return Friends.Find(friend => friend.AccountId == id && friend.FriendState == 4);
        }
        public EmoteData GetDefaultEmoteForCharacter(string Character, string Type)
        {
            foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
            {
                if (emoteData.Character == Character && emoteData.EmoteType == Type) return emoteData;
            }
            return null;
        }
        public Friend GetFriendById(long id)
        {
            return Friends.Find(friend => friend.AccountId == id);
        }
        public void Refresh()
        {
            for (int i = 0; ; i++)
            {
                CharacterData character = DataTables.Get(16).GetDataWithId<CharacterData>(i);
                if (character.Disabled && !character.LockedForChronos)
                {
                    RemoveHero(character.GetGlobalId());
                    continue;
                }
                if (!HasHero(16000000 + i))
                {
                    if (!character.IsHero()) break;
                    UnlockHero(character.GetGlobalId());
                }
            }
        }
        public int Checksum
        {
            get
            {
                ChecksumEncoder encoder = new ChecksumEncoder();
                Encode(encoder);
                return encoder.GetCheckSum();
            }
        }
        //64 vint 36 int 32 boolean
        //124 VInt 108 Int 104 Boolean
        public void Encode(ChecksumEncoder stream)
        {

            stream.WriteVLong(AccountId);
            stream.WriteVLong(AccountId);
            stream.WriteVLong(AccountId);
            stream.WriteString(Name);
            stream.WriteBoolean(NameSetByUser);
            stream.WriteInt(-1);

            stream.WriteVInt(17);
            int baseCount = 3 + Heroes.Count;
            if (HomeMode?.Home?.UnlockStarRoad == null || HomeMode.Home.UnlockStarRoad.Length == 0)
            {
                baseCount += 1;
            }
            stream.WriteVInt(baseCount);//1
            {
                ByteStreamHelper.WriteDataReference(stream, 5000008);
                stream.WriteVInt(-1);
                stream.WriteVInt(Gold);

                if (HomeMode?.Home?.UnlockStarRoad == null || HomeMode.Home.UnlockStarRoad.Length == 0)
                {
                    ByteStreamHelper.WriteDataReference(stream, 5000021);
                    stream.WriteVInt(-1);
                    stream.WriteVInt(FamePoints);
                }

                ByteStreamHelper.WriteDataReference(stream, 5000022);
                stream.WriteVInt(-1);
                stream.WriteVInt(PowerPoints);

                ByteStreamHelper.WriteDataReference(stream, 5000023);
                stream.WriteVInt(-1);
                stream.WriteVInt(Blings);


                foreach (Hero hero in Heroes)
                {
                    ByteStreamHelper.WriteDataReference(stream, hero.CardData);
                    stream.WriteVInt(-1);
                    stream.WriteVInt(1);
                }
            }

            stream.WriteVInt(Heroes.Count);//2
            foreach (Hero hero in Heroes)
            {
                ByteStreamHelper.WriteDataReference(stream, hero.CharacterData);
                stream.WriteVInt(-1);
                stream.WriteVInt(hero.Trophies);
            }

            stream.WriteVInt(Heroes.Count);//3
            foreach (Hero hero in Heroes)
            {
                ByteStreamHelper.WriteDataReference(stream, hero.CharacterData);
                stream.WriteVInt(-1);
                stream.WriteVInt(hero.HighestTrophies);
            }

            stream.WriteVInt(Heroes.Count);//4
            foreach (Hero hero in Heroes)
            {
                ByteStreamHelper.WriteDataReference(stream, hero.CharacterData);
                stream.WriteVInt(-1);
                stream.WriteVInt(0);
            }

            stream.WriteVInt(Heroes.Count);//5
            foreach (Hero hero in Heroes)
            {
                ByteStreamHelper.WriteDataReference(stream, hero.CharacterData);
                stream.WriteVInt(-1);
                stream.WriteVInt(hero.PowerPoints);
            }

            stream.WriteVInt(Heroes.Count);//6
            foreach (Hero hero in Heroes)
            {
                ByteStreamHelper.WriteDataReference(stream, hero.CharacterData);
                stream.WriteVInt(-1);
                stream.WriteVInt(hero.PowerLevel - 1);
            }

            stream.WriteVInt(SPGS.Count);//7
            foreach (int s in SPGS) 
            {
                ByteStreamHelper.WriteDataReference(stream, s);
                stream.WriteVInt(-1);
                stream.WriteVInt(SelectedSPGS.Contains(s) ? 2 : 1);//0 lock 1 unlock 2 chosen
            }

            
            stream.WriteVInt(0);//8 (if not 0 ohd error)
            stream.WriteVInt(0);//9 (if not 0 ohd error)
            stream.WriteVInt(0);//10 (if not 0 ohd error)
            stream.WriteVInt(0);//11 (if not 0 ohd error)
            stream.WriteVInt(0);//12 (if not 0 ohd error)
            stream.WriteVInt(0);//13 (if not 0 ohd error)
            stream.WriteVInt(0);//14 (if not 0 ohd error)
            stream.WriteVInt(0);//15 (запускает меню выбора бойца который получит очки силы если не 0)
            stream.WriteVInt(0);//16(PowerPoints) (not PowerPoints)
            stream.WriteVInt(0);//17 (idk but craSh if not 0)

            stream.WriteVInt(Diamonds); // Diamonds
            stream.WriteVInt(0); // CumulativePurchasedDiamonds

            stream.WriteVInt(0);//player level
            stream.WriteVInt(0); // do nothing
            stream.WriteVInt(0);//Cumulative
            stream.WriteVInt(2);//battle count
            stream.WriteVInt(0);// idk (do nothing)
            stream.WriteVInt(0);// idk (do nothing)
            stream.WriteVInt(0);// idk (do nothing)
            stream.WriteVInt(0);// idk (do nothing)
            stream.WriteVInt(0);// idk (do nothing)
            stream.WriteVInt(2);//tutorial count
            stream.WriteVInt(19);//LogicDataTables::GetRankedRankDataByRank
            stream.WriteVInt(1000);//IsOldPlayer????
            stream.WriteVInt(15);//????
            stream.WriteString(null);
            stream.WriteVInt(0);// idk (do nothing)
            stream.WriteVInt(0);// idk (do nothing)
            stream.WriteVInt(1);
        }
    }

}
