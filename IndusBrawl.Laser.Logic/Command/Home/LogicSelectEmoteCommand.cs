namespace IndusBrawl.Laser.Logic.Command.Home
{
    using System.Runtime.InteropServices;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Logic.Util;
    using System.Collections.Generic;

    public class LogicSelectEmoteCommand : Command
    {
        private int EmoteID;
        private int EmoteSlot;
        private int idk;

        // Таблица для пинов (ID без префикса 52000000)
        private static readonly Dictionary<int, PinType> PinRules = new Dictionary<int, PinType>
        {
            { 5, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 6, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 22, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 28, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 64, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 75, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 81, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 82, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 93, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 134, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 145, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 156, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 157, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 158, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 169, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 175, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 176, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 910, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 780, new PinType { RequirementType = Requirement.Free, HeroGlobalId = 0 } },
            { 151, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000000 } }, // Шелли
            { 44, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000001 } }, // Кольт
            { 34, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000002 } },
            { 29, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000003 } },
            { 104, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000008 } },
            { 129, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000010 } },
            { 7, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000006 } },
            { 124, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000013 } },
            { 140, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000024 } },
            { 88, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000007 } },
            { 59, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000009 } },
            { 177, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000022 } },
            { 0, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000027 } },
            { 135, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000004 } },
            { 54, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000018 } },
            { 114, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000019 } },
            { 39, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000025 } },
            { 83, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000034 } },
            { 845, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000061 } },
            { 23, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000014 } },
            { 65, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000030 } },
            { 407, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000045 } },
            { 119, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000015 } },
            { 109, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000016 } },
            { 70, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000020 } },
            { 17, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000026 } },
            { 12, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000029 } },
            { 227, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000036 } },
            { 369, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000043 } },
            { 447, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000050 } },
            { 594, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000048 } },
            { 698, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000058 } },
            { 99, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000011 } },
            { 215, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000035 } },
            { 1131, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000069 } },
            { 305, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000039 } },
            { 415, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000046 } },
            { 524, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000051 } },
            { 567, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000053 } },
            { 781, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000060 } },
            { 912, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000065 } },
            { 1111, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000068 } },
            { 1250, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000072 } },
            { 1474, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000077 } },
            { 170, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000017 } },
            { 76, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000021 } },
            { 277, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000032 } },
            { 283, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000031 } },
            { 164, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000037 } },
            { 377, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000042 } },
            { 431, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000047 } },
            { 949, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000064 } },
            { 1063, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000066 } },
            { 1073, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000067 } },
            { 1155, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000071 } },
            { 1260, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000073 } },
            { 1308, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000074 } }, // charlie
            { 1394, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000075 } }, // miko
            { 159, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000005 } },
            { 49, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000012 } },
            { 94, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000023 } },
            { 146, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000028 } },
            { 320, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000040 } },
            { 552, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000052 } },
            { 289, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000038 } },
            { 921, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000063 } },
            { 1174, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000070 } },
            { 1419, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000076 } }, // kit
            { 344, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000041 } },
            { 385, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000044 } },
            { 439, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000049 } },
            { 627, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000054 } },
            { 658, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000056 } },
            { 688, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000057 } },
            { 738, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000059 } },
            { 874, new PinType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000062 } },
            // ... остальные пины ...
        };

        public enum Requirement
        {
            Free,
            HeroOwnership,
            PinOwnership
        }

        public class PinType
        {
            public Requirement RequirementType { get; set; }
            public int HeroGlobalId { get; set; } = 0;
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            idk = stream.ReadVInt();
            EmoteID = stream.ReadVInt();
            EmoteSlot = stream.ReadVInt();
            Console.WriteLine($"1: {EmoteID} 2: {EmoteSlot} 3: {idk}");
        }

        public override int Execute(HomeMode homeMode)
        {
            // Проверяем, является ли это пином (слот 5)
            if (EmoteSlot == 5)
            {
                return HandlePinSelection(homeMode, EmoteID);
            }

            EmoteData emoteData = DataTables.Get(DataType.Emote).GetDataWithId<EmoteData>(EmoteID);
            if (EmoteSlot < 0 || EmoteSlot > 6) return -1;
            
            switch (EmoteSlot)
            {
                case 4:
                    homeMode.Home.PlayerSelectedEmotes[4] = EmoteID;
                    break;
                case 5:
                    homeMode.Home.PlayerSelectedEmotes[5] = EmoteID;
                    break;
            }

            CharacterData characterData = DataTables.Get(DataType.Character).GetData<CharacterData>(emoteData.Character);
            if (characterData == null) return 0;
            Hero hero = homeMode.Avatar.GetHero(characterData.GetGlobalId()) ?? null;
            
            if (emoteData.Character != null && characterData != null) 
                homeMode.Avatar.SetEmoteForBrawler(characterData.GetGlobalId(), EmoteSlot, EmoteID);
                
            if (hero != null)
            {
                if (hero.emote[1] == hero.emote[2] || hero.emote[1] == hero.emote[3]) 
                    homeMode.Avatar.GetHero(characterData.GetGlobalId()).emote[1] = 160 - 3;
                if (hero.emote[2] == hero.emote[1] || hero.emote[3] == hero.emote[1]) 
                    homeMode.Avatar.GetHero(characterData.GetGlobalId()).emote[2] = 148 - 3;
                if (hero.emote[3] == hero.emote[1] || hero.emote[3] == hero.emote[2]) 
                    homeMode.Avatar.GetHero(characterData.GetGlobalId()).emote[3] = 137 - 3;
            }
            
            return 0;
        }

        private int HandlePinSelection(HomeMode homeMode, int pinId)
        {
            // Для пустого пина
            if (pinId == 0)
            {
                homeMode.Home.PlayerSelectedEmotes[5] = 0;
                return 0;
            }

            // Проверяем правила для пина
            if (!PinRules.TryGetValue(pinId, out var pinType))
            {
                Console.WriteLine($"LogicSelectEmoteCommand.HandlePinSelection - No rule for pin ID {pinId}, requiring PinOwnership.");
                pinType = new PinType { RequirementType = Requirement.PinOwnership, HeroGlobalId = 0 };
            }

            switch (pinType.RequirementType)
            {
                case Requirement.Free:
                    break;

                case Requirement.HeroOwnership:
                    if (!homeMode.Avatar.HasHero(pinType.HeroGlobalId))
                    {
                        var heroData = DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(pinType.HeroGlobalId);
                        string heroName = heroData?.GetName() ?? "Unknown Hero";
                        Console.WriteLine($"LogicSelectEmoteCommand.HandlePinSelection - Need hero '{heroName}' (ID: {pinType.HeroGlobalId}) for pin {pinId}");
                        BanUtil.BanPlayer(homeMode, BanUtil.ViolationType.ModifiedClient);
                        return -1;
                    }
                    break;

                case Requirement.PinOwnership:
                    // Генерируем глобальный ID для проверки владения
                    int globalPinId = 52000000 + pinId;
                    if (!homeMode.Home.UnlockedEmotes.Contains(globalPinId))
                    {
                        Console.WriteLine($"LogicSelectEmoteCommand.HandlePinSelection - Player does not own pin ID {pinId} (Global ID: {globalPinId})");
                        BanUtil.BanPlayer(homeMode, BanUtil.ViolationType.ModifiedClient);
                        return -1;
                    }
                    break;

                default:
                    Console.WriteLine($"LogicSelectEmoteCommand.HandlePinSelection - Invalid requirement type for pin {pinId}");
                    return -1;
            }

            // Устанавливаем пин
            homeMode.Home.PlayerSelectedEmotes[5] = pinId;
            return 0;
        }

        public override int GetCommandType()
        {
            return 538;
        }
    }
}