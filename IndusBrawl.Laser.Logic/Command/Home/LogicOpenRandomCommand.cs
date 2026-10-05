namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Battle.Objects;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Gatcha;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Logic.Message.Home;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;

    public class LogicOpenRandomCommand : Command
    {
        public int Unk;
        public int RewardType;
        public int RewardAmount;
        public int Unk2;
        public int Unk3;
        public override void Decode(ByteStream stream)
        {
            
        }

        public override int Execute(HomeMode homeMode)
        {
            // HARDCODE!!!
            StarrDropLabConfig lab = StarrDropLab.For(homeMode);
            if (lab != null && lab.TwoPhase)
            {
                // Повторный запрос без ожидающего дропа просто игнорируем (старая схема сюда больше не идёт)
                homeMode.Home.OpenPendingStarrDrop();
                return 0;
            }

            LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand = new LogicRefreshRandomRewardsCommand();
            if (lab != null && lab.OpenCount >= 0) logicRefreshRandomRewardsCommand.Количество = lab.OpenCount;
            logicRefreshRandomRewardsCommand.Execute(homeMode);

            AvailableServerCommandMessage message2 = new AvailableServerCommandMessage();
            message2.Command = logicRefreshRandomRewardsCommand;
            homeMode.GameListener.SendMessage(message2);
            Random rand = new Random();
            
            int minecraft = rand.Next(10000);
            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            DeliveryUnit unit = new DeliveryUnit(100);

            GatchaDrop drop = new GatchaDrop(homeMode.Home.StarrDrop.data.Type);

            drop.DataGlobalId = homeMode.Home.StarrDrop.data.DataGlobalID;
            if(homeMode.Home.StarrDrop.data.Type != 1)drop.SkinGlobalId = homeMode.Home.StarrDrop.data.SkinGlobalID;
            drop.Count = homeMode.Home.StarrDrop.data.Ammount;
            unit.AddDrop(drop);

            command.StarrDropExecute = true;
            command.Lab = lab;
            command.LabRarity = homeMode.Home.StarrDrop.Rarity;
            command.DeliveryUnits.Add(unit);
            command.Execute(homeMode);

            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            homeMode.GameListener.SendMessage(message);

            return 0;
        }

        public override int GetCommandType()
        {
            return 571;
        }
    }
}
