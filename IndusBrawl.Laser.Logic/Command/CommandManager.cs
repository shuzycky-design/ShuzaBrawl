namespace IndusBrawl.Laser.Logic.Command
{
    using IndusBrawl.Laser.Logic.Command.Home;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;

    public class CommandManager
    {
        private static Dictionary<int, Type> CommandTypes;

        static CommandManager()
        {
            CommandTypes = new Dictionary<int, Type>()
            {
                {557, typeof(LogicBuyStarpowerCommand) },
                {500, typeof(LogicGatchaCommand)},
                {505, typeof(LogicSetPlayerThumbnailCommand)},
                {506, typeof(LogicSelectSkinCommand)},
                {509, typeof(LogicBuyTokens)},
                {528, typeof(LogicViewInboxNotificationCommand)},
                //514 LogicDeleteNotificationCommand
                {515, typeof(LogicClearShopTickersCommand)},
                {517, typeof(LogicClaimRankUpRewardCommand)},
                {519, typeof(LogicPurchaseOfferCommand)},
                {520, typeof(LogicLevelUpCommand)},
                {521, typeof(LogicPurchaseHeroLvlUpMaterialCommand)},
                {522,typeof(LogicHeroSeenCommand) },
                {525, typeof(LogicSelectCharacterCommand)},
                {527, typeof(LogicSetPlayerNameColorCommand)},
                {529, typeof(LogicSelectStarPowerCommand)},
                {533, typeof(LogicQuestsSeenCommand)},
                {534, typeof(LogicPurchaseBrawlPassCommand)},
                {535, typeof(LogicClaimTailRewardCommand)},
                {536, typeof(LogicPurchaseBrawlPassProgressCommand)},
                {538, typeof(LogicSelectEmoteCommand)},
                {543, typeof(LogicSelectGearCommand)},
                {558, typeof(LogicPuschaseGearCommand)},
                {560, typeof(LogicPurchaseBrawlerCommand)},
                {562, typeof(LogicStarRoadRewardCommand)},
                {567, typeof(LogicEditBattlePassCommand1)},
                {568, typeof(LogicEditBattlePassCommand)},
                {570, typeof(LogicSelectFavouriteBrawlerCommand)},
                {571, typeof(LogicOpenRandomCommand)},
                {569, typeof(LogicClaimMasteriesCommand) },
                {600, typeof(SpectateCommand)}
            };
        }

        public static Command DecodeCommand(ByteStream stream, int type)
        {
            Command command = CommandManager.CreateCommand(type);
            if (command == null)
            {
                Debugger.Warning("Command is unhandled: " + type);
                return null;
            }
            Debugger.Warning("Command: " + type);
            command.Decode(stream);
            return command;
        }

        public static Command CreateCommand(int type)
        {
            if (CommandTypes.ContainsKey(type))
            {
                return (Command)Activator.CreateInstance(CommandTypes[type]);
            }
            return null;
        }
    }
}
