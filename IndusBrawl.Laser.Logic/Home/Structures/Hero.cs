namespace IndusBrawl.Laser.Logic.Home.Structures
{
    using Newtonsoft.Json;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Math;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Logic.Util;

    [JsonObject(MemberSerialization.OptIn)]
    public class Hero
    {

        public static readonly int[] UpgradeCostTable = new int[]
        {
            20, 35, 75, 140, 290, 480, 800, 1250, 1875, 2800
        };
        public static readonly int[] UpgradeCost1Table = new int[]
        {
            20, 30, 50, 80, 130, 210, 340, 550, 890, 1440
        };

        [JsonProperty] public int CharacterId;
        public int Id;
        [JsonProperty] public int CardId;

        [JsonProperty] public int Trophies;
        [JsonProperty] public int HighestTrophies;

        [JsonProperty] public int PowerPoints;
        [JsonProperty] public int PowerLevel;
        [JsonProperty] public List<GearData> GearData { get; set; } = new List<GearData>();
        [JsonProperty] public List<int> UnlockedGearInstanceIds { get; set; } = new List<int>();

        [JsonProperty] public int SelectedStarPowerId;
        [JsonProperty] public int SelectedGadgetId;

        [JsonProperty] public int SelectedGearId1;
        [JsonProperty] public int SelectedGearId2; 

        [JsonProperty] public int SelectedSkinId;

        [JsonProperty] public Dictionary<int, int> emote;
        [JsonProperty] public int SelectedOverChargeId;
        [JsonProperty] public int MasteryPoints;
        [JsonProperty] public int ClaimedMasteryLVL;

        public CharacterData CharacterData => DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(CharacterId);
        public CardData CardData => DataTables.Get(DataType.Card).GetDataByGlobalId<CardData>(CardId);
        public EmoteData GetDefaultEmoteForCharacter(string Character, string Type)
        {
            foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
            {
                if (emoteData.Character == Character && emoteData.EmoteType == Type) return emoteData;
            }
            return null;
        }
        public Hero(int characterId)
        {
            CharacterId = characterId;
            CardData g = GetDefaultMetaForHero(5);
            CharacterData characterData = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(characterId);
            if (g != null) SelectedGadgetId = g.GetInstanceId();
            CardData s = GetDefaultMetaForHero(4);
            if (s != null) SelectedStarPowerId = s.GetInstanceId();
            CardData o = GetDefaultMetaForHero(6);
            if (o != null) SelectedOverChargeId = o.GetInstanceId();
            CardId = DataTables.GetUnlockCardFor(CharacterData).GetGlobalId();
            PowerLevel = 1;
            SelectedSkinId = 0;
            emote = new Dictionary<int, int>();
            if(GetDefaultEmoteForCharacter(characterData.Name, "DEFAULT") != null) emote.Add(1, GetDefaultEmoteForCharacter(characterData.Name, "DEFAULT").GetInstanceId());
            emote.Add(2, 137 - 3);
            emote.Add(3, 148 - 3);
        }

        // Метод для получения GlobalID
        public int GetGlobalId()
        {
            return GlobalId.CreateGlobalId(16, Id);
        }
        public void AddTrophies(int trophies)
        {
            Trophies += trophies;
            HighestTrophies = LogicMath.Max(HighestTrophies, Trophies);
        }

        public void SetTrophies(int trophies)
        {
            Trophies = trophies;
            HighestTrophies = LogicMath.Max(HighestTrophies, Trophies);
        }
        public CardData GetDefaultMetaForHero(int MetaType)
        {
            foreach (CardData carddata in DataTables.Get(DataType.Card).GetDatas())
            {
                if (carddata.Target == CharacterData.Name && carddata.MetaType == MetaType)
                {
                    return carddata;
                }
            }
            return null;
        }
        public void Encode(ByteStream stream)
        {
            ByteStreamHelper.WriteDataReference(stream, CharacterData);
            ByteStreamHelper.WriteDataReference(stream, null);
            stream.WriteVInt(Trophies);
            stream.WriteVInt(HighestTrophies);
            stream.WriteVInt(PowerLevel);
        }
    }
}
