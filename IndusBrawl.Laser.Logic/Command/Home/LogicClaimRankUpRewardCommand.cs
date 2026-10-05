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
    using IndusBrawl.Laser.Logic.Message.Account.Auth;
    using StarrDrop;
    using IndusBrawl.Laser.Logic.Home.Items;

    public class LogicClaimRankUpRewardCommand : Command
    {
        public int MilestoneId { get; set; }
        public int UnknownDataId { get; set; }
        public int Unk2 { get; set; }
        public int Unk3 { get; set; }
        public int RequieredMilestone { get; set; }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            MilestoneId = stream.ReadVInt();
            UnknownDataId = ByteStreamHelper.ReadDataReference(stream);
            Unk2 = stream.ReadVInt();
            RequieredMilestone = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            Debugger.Print($"Claim rankup reward: milestone: {MilestoneId}, data: {UnknownDataId}, unk2: {Unk2}, unk3: {Unk3}");

            if (MilestoneId == 6)
            {
                string name = $"goal_6_{homeMode.Home.TrophyRoadProgress - 1}";


                MilestoneData milestoneData = DataTables.Get(DataType.Milestone).GetData<MilestoneData>(name);
                if (milestoneData == null)
                {
                    Debugger.Error($"Milestone data is NULL - {name}");
                    return -111;
                }

                if (homeMode.Avatar.HighestTrophies < milestoneData.Progress + milestoneData.ProgressStart)
                {
                    Debugger.Warning($"current progress: {homeMode.Avatar.HighestTrophies}, required progress: {milestoneData.Progress + milestoneData.ProgressStart}");
                    return -2;
                }

                if (ProcessReward(homeMode, milestoneData, false, 6, homeMode.Home.TrophyRoadProgress + 1, 0) != 0) return -3;

                homeMode.Home.TrophyRoadProgress++;
            }
            else if (MilestoneId == 9 || MilestoneId == 10 || MilestoneId == 12)
            {
                string name = $"Goal_{MilestoneId}_22_{(MilestoneId == 10 ? RequieredMilestone : RequieredMilestone)}";
                if (MilestoneId == 9 && !homeMode.Home.HasPremiumPass)
                {
                    return 0;
                }
                if (MilestoneId == 12 && !homeMode.Home.HasPremiumPassPlus)
                {
                    return 0;
                }

                MilestoneData milestoneData = DataTables.Get(DataType.Milestone).GetData<MilestoneData>(name);
                if (milestoneData == null)
                {
                    Debugger.Error($"Milestone data is NULL - {name}");
                    return 0;
                }

                if (homeMode.Home.BrawlPassTokens < milestoneData.Progress + milestoneData.ProgressStart)
                {
                    Debugger.Warning($"current progress: {homeMode.Home.BrawlPassTokens}, required progress: {milestoneData.Progress + milestoneData.ProgressStart}");
                    return 0;
                }
                if (MilestoneId == 9 && LogicBitHelper.Get(homeMode.Home.PremiumPassProgress, RequieredMilestone + 2))
                    return 0;
                if (MilestoneId == 10 && LogicBitHelper.Get(homeMode.Home.BrawlPassProgress, RequieredMilestone + 2))
                    return 0;
                if (MilestoneId == 12 && LogicBitHelper.Get(homeMode.Home.BrawlPassPlusProgress, RequieredMilestone + 2))
                    return 0;

                if (ProcessReward(homeMode, milestoneData, false, MilestoneId, MilestoneId == 10 ? RequieredMilestone + 2 : RequieredMilestone + 2, 22) != 0) return 0;

                if (MilestoneId == 9)
                    homeMode.Home.PremiumPassProgress = LogicBitHelper.Set(homeMode.Home.PremiumPassProgress, RequieredMilestone + 2, true);
                if (MilestoneId == 10)
                    homeMode.Home.BrawlPassProgress = LogicBitHelper.Set(homeMode.Home.BrawlPassProgress, RequieredMilestone + 2, true);
                if (MilestoneId == 12)
                    homeMode.Home.BrawlPassPlusProgress = LogicBitHelper.Set(homeMode.Home.BrawlPassPlusProgress, RequieredMilestone + 2, true);
            }
            else
            {
                return -1;
            }

            return 0;
        }

        private int ProcessReward(HomeMode homeMode, MilestoneData milestoneData, bool useSecondaryReward, int track, int idx, int bpSeason)
        {
            int type = useSecondaryReward ? milestoneData.SecondaryLvlUpRewardType : milestoneData.PrimaryLvlUpRewardType;
            string data = !useSecondaryReward ? milestoneData.PrimaryLvlUpRewardData : milestoneData.SecondaryLvlUpRewardData;
            int count = useSecondaryReward ? milestoneData.SecondaryLvlUpRewardCount : milestoneData.PrimaryLvlUpRewardCount;
            Console.WriteLine(type);
            switch (type)
            {
                case 1:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(7);
                        drop.Count = count;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 3:
                    {
                        CharacterData character = DataTables.Get(DataType.Character).GetData<CharacterData>(data);
                        if (homeMode.Avatar.HasHero(character.GetGlobalId()))
                        {
                            return ProcessReward(homeMode, milestoneData, true, track, idx, bpSeason);
                        }

                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(1);
                        drop.DataGlobalId = character.GetGlobalId();
                        drop.Count = 1;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);
                        Debugger.Print("3 stardrop");

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 6:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(10);
                        homeMode.SimulateGatcha(unit);
                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);
                        break;
                    }
                case 9:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(10);
                        homeMode.SimulateGatcha(unit);
                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);
                        break;
                    }
                case 4: // Skin
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(9);
                        drop.SkinGlobalId = DataTables.Get(DataType.Skin).GetData<SkinData>(data).GetGlobalId();
                        drop.Count = 1;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);
                    }
                    break;
                case 10:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(11);
                        homeMode.SimulateGatcha(unit);
                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);
                        break;
                    }
                case 12:
                    {
                        CharacterData character = DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(UnknownDataId);

                        if (character == null)
                        {
                            return ProcessReward(homeMode, milestoneData, true, track, idx, bpSeason);
                        }

                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(6);
                        drop.DataGlobalId = character.GetGlobalId();
                        drop.Count = count;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 13:
                    break;
                case 14:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(12);
                        homeMode.SimulateGatcha(unit);
                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);
                        break;
                    }
                case 16:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(8);
                        drop.Count = count;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 18: // Quests unlocked!
                    {
                        homeMode.Home.Quests = new Quests();
                        homeMode.Home.Quests.AddRandomQuests(homeMode.Avatar.Heroes, 8);

                        LogicHeroWinQuestsChangedCommand cmd = new LogicHeroWinQuestsChangedCommand();
                        cmd.Quests = homeMode.Home.Quests;

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = cmd;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 19: // Pins
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(11);
                        drop.DataGlobalId = DataTables.Get(DataType.Emote).GetData<EmoteData>(data).GetGlobalId();
                        drop.Count = 1;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);
                    }
                    break;
                case 25:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(11);
                        drop.DataGlobalId = DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(data).GetGlobalId();
                        Console.WriteLine(drop.DataGlobalId);
                        drop.Count = 1;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 29: // Clubs unlocked!
                    {
                        homeMode.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                        {
                           Message = "Клубы разблокированы!"
                        });
                        break;
                    }
                case 35: // unlock spray from bp
                    {
                       // Создаем уведомление с типом 89 (нотиф с гемами)
                        var notification = new Notification
                        {
                            Id = 89,
                            IsViewed = false
                        };
                        
                        // Устанавливаем количество гемов
                        notification.DonationCount = 15;
                        
                        // Добавляем уведомление в NotificationFactory игрока
                        if (homeMode.Home.NotificationFactory == null)
                        {
                            homeMode.Home.NotificationFactory = new NotificationFactory();
                        }
                        homeMode.Home.NotificationFactory.Add(notification);
                        homeMode.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                        {
                           Message = "Получена компенсация, посмотреть можно во входящих."
                        });
                        // нужно сделать так что бы игрок получил нотиф с 15 гемами
                        break;
                    }
                case 37: // Sprays unlocked!
                    {
                        break;
                    }
                case 38:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(22);
                        homeMode.Home.RecruitTokens = homeMode.Home.RecruitTokens + count;
                        drop.Count = count;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 41:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(24);
                        drop.Count = count;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 43:
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(11);
                        drop.DataGlobalId = DataTables.Get(DataType.Titul).GetData<TitlesData>(data).GetGlobalId();
                        Console.WriteLine(drop.DataGlobalId);
                        drop.Count = 1;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 44: // Dayli Quest
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(8);
                        drop.Count = count;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 45: // Blings
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(25);
                        drop.Count = count;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                case 49: // Stardrop
                    {
                        Debugger.Error("Used starr drop 49");
                        if (StarrDropLab.For(homeMode) is StarrDropLabConfig twoPhaseLab && twoPhaseLab.TwoPhase)
                        {
                            homeMode.Home.QueueStarrDrop(track, idx, bpSeason);
                            break;
                        }
                        var starrDrop = new StarrDrop(homeMode);
                        starrDrop.GenerateDrop(homeMode);
                        LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand = new LogicRefreshRandomRewardsCommand();
    
                        logicRefreshRandomRewardsCommand.Rarity = 4;
                        logicRefreshRandomRewardsCommand.Количество = 4;
                        StarrDropLabConfig lab = StarrDropLab.For(homeMode);
                        if (lab != null && lab.ClaimCount >= 0) logicRefreshRandomRewardsCommand.Количество = lab.ClaimCount;
                        logicRefreshRandomRewardsCommand.Execute(homeMode);
    
                        AvailableServerCommandMessage message25 = new AvailableServerCommandMessage();
                        message25.Command = logicRefreshRandomRewardsCommand;
                        homeMode.GameListener.SendMessage(message25);
    
                        LogicGiveDeliveryItemsCommand command123 = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);
    
                        GatchaDrop drop = new GatchaDrop(homeMode.Home.StarrDrop.data.Type);
    
                        drop.DataGlobalId = homeMode.Home.StarrDrop.data.DataGlobalID;
                        if (homeMode.Home.StarrDrop.data.Type != 1) drop.SkinGlobalId = homeMode.Home.StarrDrop.data.SkinGlobalID;
                        drop.Count = homeMode.Home.StarrDrop.data.Ammount;
                        unit.AddDrop(drop);
    
                        command123.StarrDropExecute = true;
                        command123.Lab = lab;
                        command123.LabRarity = homeMode.Home.StarrDrop.Rarity;
                        command123.RewardTrackType = track;
                        command123.RewardForRank = idx;
                        command123.BrawlPassSeason = bpSeason;
                        command123.BrawlPassExecute = true;
                        command123.DeliveryUnits.Add(unit);
                        command123.Execute(homeMode);
                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command123;
                        homeMode.GameListener.SendMessage(message);
    
                        LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand1 = new LogicRefreshRandomRewardsCommand();
    
                        logicRefreshRandomRewardsCommand1.Disable = true;
                        logicRefreshRandomRewardsCommand1.Execute(homeMode);
    
                        AvailableServerCommandMessage message251 = new AvailableServerCommandMessage();
                        message251.Command = logicRefreshRandomRewardsCommand1;
                        homeMode.GameListener.SendMessage(message251);
                        break;
                    }
                case 50: // random Stardrop
                    {
                        goto case 49;
                        homeMode.Home.StarrDrop.GenerateDrop(homeMode);
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);
                        GatchaDrop drop = new GatchaDrop(7);

                        command.StarrDropExecute = true;
                        command.DeliveryUnits.Add(unit);
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand4 = new LogicRefreshRandomRewardsCommand();
                        logicRefreshRandomRewardsCommand4.guaranteed = true;
                        logicRefreshRandomRewardsCommand4.Rarity = 4;
                        logicRefreshRandomRewardsCommand4.Количество = 4;
                        logicRefreshRandomRewardsCommand4.Execute(homeMode);

                        AvailableServerCommandMessage message25 = new AvailableServerCommandMessage();
                        message25.Command = logicRefreshRandomRewardsCommand4;
                        homeMode.GameListener.SendMessage(message25);

                        LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand1 = new LogicRefreshRandomRewardsCommand();

                        logicRefreshRandomRewardsCommand1.Disable = true;
                        logicRefreshRandomRewardsCommand1.Execute(homeMode);

                        AvailableServerCommandMessage message251 = new AvailableServerCommandMessage();
                        message251.Command = logicRefreshRandomRewardsCommand1;
                        homeMode.GameListener.SendMessage(message251);

                        break;
                    }
                case 51: // 1000 tokens
                    {
                        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                        DeliveryUnit unit = new DeliveryUnit(100);

                        GatchaDrop drop = new GatchaDrop(22);
                        drop.Count = 1000;
                        homeMode.Home.RecruitTokens = homeMode.Home.RecruitTokens + 1000;
                        unit.AddDrop(drop);

                        command.DeliveryUnits.Add(unit);
                        command.RewardTrackType = track;
                        command.RewardForRank = idx;
                        command.BrawlPassSeason = bpSeason;
                        command.BrawlPassExecute = true;
                        command.Execute(homeMode);

                        AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                        message.Command = command;
                        homeMode.GameListener.SendMessage(message);

                        break;
                    }
                default:
                    {
                        Debugger.Error("Unknown reward type: " + type);
                        return -3;
                    }
            }

            return 0;
        }

        public override int GetCommandType()
        {
            return 517;
        }
    }
}
