namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Gatcha;
    using IndusBrawl.Laser.Logic.Home.Quest;
    using IndusBrawl.Laser.Logic.Message.Home;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;
    using IndusBrawl.Laser.Logic.Util;
    using System.Runtime.InteropServices;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using System.Collections.Generic;

    public class LogicClaimMasteriesCommand : Command
    {
        public static bool EnableAnticheatBan = true;

        private static readonly Dictionary<int, int> RequiredMasteryPointsForLevel = new Dictionary<int, int>
        {
            { 1, 300 },
            { 2, 800 },
            { 3, 1500 },
            { 4, 2600 },
            { 5, 4000 },
            { 6, 5800 },
            { 7, 10300 },
            { 8, 16800 },
            { 9, 24800 }
        };

        private int Tick1;
        private int Brawler;
        private int MasteryIndex;

        public override void Decode(ByteStream stream)
        {
            Tick1 = stream.ReadVInt();
            stream.ReadVInt();
            stream.ReadVInt();
            stream.ReadVInt();
            stream.ReadVInt();
            Brawler = stream.ReadVInt();
            MasteryIndex = stream.ReadVInt() - 1;
            Console.WriteLine(MasteryIndex);
        }

        public override int Execute(HomeMode homeMode)
        {
            Debugger.Print($"[Mastery] Player '{homeMode.Avatar?.Name}' ({homeMode.Avatar?.AccountId}) attempting to claim mastery level {MasteryIndex} for hero instance {Brawler}.");

            if (!IsValidClaimRequest(homeMode, out string errorMessage))
            {
                Debugger.Print($"[ANTICHEAT] Invalid mastery claim attempt by {homeMode.Avatar?.Name} ({homeMode.Avatar?.AccountId}): {errorMessage}");
                if (EnableAnticheatBan)
                {
                    BanUtil.BanPlayer(homeMode, BanUtil.ViolationType.ModifiedClient);
                }
                return -1;
            }

            int globalCharacterId = GlobalId.CreateGlobalId(16, Brawler);
            Hero hero = homeMode.Avatar.GetHero(globalCharacterId);

            if (hero == null)
            {
                string err = $"[Mastery] Hero not found for GlobalId {globalCharacterId} (Instance ID {Brawler}) during claim for player {homeMode.Avatar?.Name}";
                Debugger.Error(err);
                return -1;
            }

            if (RequiredMasteryPointsForLevel.ContainsKey(MasteryIndex))
            {
                int requiredPoints = RequiredMasteryPointsForLevel[MasteryIndex];
                if (hero.MasteryPoints < requiredPoints)
                {
                    string cheatReason = $"Insufficient MasteryPoints for hero instance {Brawler} (GlobalId {globalCharacterId}) at mastery level {MasteryIndex}. Required: {requiredPoints}, Has: {hero.MasteryPoints}";
                    Debugger.Print($"[ANTICHEAT] {cheatReason}");
                    if (EnableAnticheatBan)
                    {
                        BanUtil.BanPlayer(homeMode, BanUtil.ViolationType.ModifiedClient);
                    }
                    return -1;
                }
            }
            else
            {
                string cheatReason = $"MasteryIndex {MasteryIndex} is out of defined range (1-{RequiredMasteryPointsForLevel.Count}) for hero instance {Brawler}.";
                Debugger.Print($"[ANTICHEAT] {cheatReason}");
                if (EnableAnticheatBan)
                {
                    BanUtil.BanPlayer(homeMode, BanUtil.ViolationType.ModifiedClient);
                }
                return -1;
            }

            if (hero.ClaimedMasteryLVL >= MasteryIndex)
            {
                string cheatReason = $"Attempting to claim already claimed Mastery level {MasteryIndex} for hero instance {Brawler} (GlobalId {globalCharacterId}). Already claimed up to level {hero.ClaimedMasteryLVL}.";
                Debugger.Print($"[ANTICHEAT] {cheatReason}");
                if (EnableAnticheatBan)
                {
                    BanUtil.BanPlayer(homeMode, BanUtil.ViolationType.ModifiedClient);
                }
                return -1;
            }

            int oldClaimedLevel = hero.ClaimedMasteryLVL;
            Debugger.Print($"[Mastery] Hero {Brawler} has {hero.MasteryPoints} MasteryPoints. ClaimedMasteryLVL before: {oldClaimedLevel}.");

            hero.ClaimedMasteryLVL += 1;
            int newClaimedLevel = hero.ClaimedMasteryLVL;

            Debugger.Print($"[Mastery] Successfully claimed mastery level {MasteryIndex} for hero instance {Brawler}. ClaimedMasteryLVL updated from {oldClaimedLevel} to {newClaimedLevel}.");

            MasteryData masteryData = DataTables.Get(DataType.Mastery).GetData<MasteryData>(MasteryIndex);
            if (masteryData == null)
            {
                Debugger.Error($"[Mastery] MasteryData not found for mastery level {MasteryIndex}");
                return -1;
            }

            MasteryVanityData masteryVanityData = DataTables.Get(DataType.MasteryVanity).GetData<MasteryVanityData>(Brawler);
            if (masteryVanityData == null)
            {
                Debugger.Error($"[Mastery] MasteryVanityData not found for brawler instance {Brawler}");
                return -1;
            }

            CharacterData characterData = DataTables.Get(DataType.Character).GetData<CharacterData>(Brawler);
            if (characterData == null)
            {
                Debugger.Error($"[Mastery] CharacterData not found for brawler instance {Brawler}");
                return -1;
            }

            CardData cardData = DataTables.Get(DataType.Card).GetData<CardData>(characterData.Name + "_unlock");
            if (cardData == null)
            {
                Debugger.Error($"[Mastery] CardData not found for {characterData.Name}_unlock");
                return -1;
            }

            TitlesData titlesData = DataTables.Get(DataType.Titul).GetData<TitlesData>(masteryVanityData.RewardTitles);
            PlayerThumbnailData playerThumbnailData = DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(masteryVanityData.RewardPlayerIcons);
            EmoteData emoteData = DataTables.Get(DataType.Emote).GetData<EmoteData>(masteryVanityData.RewardEmotes);

            int rewardCount;
            string rewardType;

            if (cardData.Rarity == "common" || cardData.Rarity == "Common")
            {
                rewardCount = masteryData.RewardCount_Common;
                rewardType = masteryData.RewardType_Common;
            }
            else if (cardData.Rarity == "rare" || cardData.Rarity == "Rare")
            {
                rewardCount = masteryData.RewardCount_Rare;
                rewardType = masteryData.RewardType_Rare;
            }
            else if (cardData.Rarity == "super_rare")
            {
                rewardCount = masteryData.RewardCount_SuperRare;
                rewardType = masteryData.RewardType_SuperRare;
            }
            else if (cardData.Rarity == "epic" || cardData.Rarity == "Epic")
            {
                rewardCount = masteryData.RewardCount_Epic;
                rewardType = masteryData.RewardType_Epic;
            }
            else if (cardData.Rarity == "mega_epic")
            {
                rewardCount = masteryData.RewardCount_Mythic;
                rewardType = masteryData.RewardType_Mythic;
            }
            else if (cardData.Rarity == "legendary" || cardData.Rarity == "Legendary")
            {
                rewardCount = masteryData.RewardCount_Legendary;
                rewardType = masteryData.RewardType_Legendary;
            }
            else
            {
                Debugger.Error($"[Mastery] Unknown rarity '{cardData.Rarity}' for card {cardData.GetName()}");
                return -1;
            }

            switch (rewardType)
            {
                case "Coins":
                    GiveReward(homeMode, 7, rewardCount);
                    break;
                case "PowerPoints":
                    GiveReward(homeMode, 24, rewardCount);
                    break;
                case "Credits":
                    GiveReward(homeMode, 22, rewardCount);
                    break;
                case "UniqueEmote":
                    if (emoteData != null)
                    {
                        GiveUniqueReward(homeMode, 11, emoteData.GetGlobalId());
                    }
                    else
                    {
                        Debugger.Warning($"[Mastery] EmoteData is null for RewardEmotes '{masteryVanityData.RewardEmotes}'");
                    }
                    break;
                case "UniquePlayerIcon":
                    if (playerThumbnailData != null)
                    {
                        GiveUniqueReward(homeMode, 11, playerThumbnailData.GetGlobalId());
                    }
                    else
                    {
                        Debugger.Warning($"[Mastery] PlayerThumbnailData is null for RewardPlayerIcons '{masteryVanityData.RewardPlayerIcons}'");
                    }
                    break;
                case "UniqueTitle":
                    if (titlesData != null)
                    {
                        GiveUniqueReward(homeMode, 11, titlesData.GetGlobalId());
                    }
                    else
                    {
                        Debugger.Warning($"[Mastery] TitlesData is null for RewardTitles '{masteryVanityData.RewardTitles}'");
                    }
                    break;
                default:
                    Debugger.Warning($"[Mastery] Unknown reward type '{rewardType}' for MasteryIndex {MasteryIndex}");
                    break;
            }

            return 0;
        }

        private bool IsValidClaimRequest(HomeMode homeMode, out string errorMessage)
        {
            errorMessage = "";

            if (Brawler < 0 || Brawler > 75)
            {
                errorMessage = $"Invalid brawler instance ID: {Brawler}. Must be between 0 and 75.";
                return false;
            }

            if (MasteryIndex <= 0 || !RequiredMasteryPointsForLevel.ContainsKey(MasteryIndex))
            {
                errorMessage = $"Invalid mastery level: {MasteryIndex}. Must be between 1 and {RequiredMasteryPointsForLevel.Count}.";
                return false;
            }

            int globalCharacterId = GlobalId.CreateGlobalId(16, Brawler);
            var hero = homeMode.Avatar.GetHero(globalCharacterId);
            if (hero == null)
            {
                errorMessage = $"Player doesn't own hero with instance ID {Brawler} (GlobalId: {globalCharacterId})";
                return false;
            }

            return true;
        }

        private void GiveReward(HomeMode homeMode, int dropType, int count)
        {
            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            DeliveryUnit unit = new DeliveryUnit(100);

            GatchaDrop drop = new GatchaDrop(dropType);
            drop.Count = count;
            unit.AddDrop(drop);

            command.DeliveryUnits.Add(unit);
            command.Execute(homeMode);

            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            homeMode.GameListener.SendMessage(message);
        }

        private void GiveUniqueReward(HomeMode homeMode, int dropType, int globalId)
        {
            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            DeliveryUnit unit = new DeliveryUnit(100);

            GatchaDrop drop = new GatchaDrop(dropType);
            drop.DataGlobalId = globalId;
            drop.Count = 1;
            unit.AddDrop(drop);

            command.DeliveryUnits.Add(unit);
            command.Execute(homeMode);

            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            homeMode.GameListener.SendMessage(message);
        }

        public override int GetCommandType()
        {
            return 569;
        }
    }
}