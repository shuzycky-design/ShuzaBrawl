namespace IndusBrawl.Laser.Logic.Command.Home
{
    using Masuda.Net.Models;
    using IndusBrawl.Laser.Logic.Command.Avatar;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Gatcha;
    using IndusBrawl.Laser.Logic.Message.Home;
    using IndusBrawl.Laser.Titan.DataStream;

    public class LogicStarRoadRewardCommand : Command
    {
        public int BrawlerId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            BrawlerId = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            int globalId = GlobalId.CreateGlobalId(16, BrawlerId);
            int creditsCost = 0;
            if (homeMode.Avatar.HasHero(globalId)) { return -1; }
            else
            {
                CharacterData characterData = DataTables.Get(16).GetDataByGlobalId<CharacterData>(globalId);
                if (characterData == null) return -1;
                CardData cardData = DataTables.Get(23).GetData<CardData>(characterData.Name + "_unlock");
                if (cardData == null) return -1;
                if (cardData.Rarity == "rare") { creditsCost = 160; }
                if (cardData.Rarity == "super_rare") { creditsCost = 430; }
                if (cardData.Rarity == "epic") { creditsCost = 925; }
                if (cardData.Rarity == "mega_epic") { creditsCost = 1900; }
                if (cardData.Rarity == "legendary") { creditsCost = 3800; }
                if (!homeMode.Avatar.UseRareTokens(creditsCost)) { return -2; }
                homeMode.Avatar.UnlockHero(characterData.GetGlobalId());
           
   


    // Выполняем команду награды

    // Отправляем сообщение клиенту о доступной награде
    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
   


    // Уведомляем об изменении в Star Road
    LogicBrawlerRecruitRoadChangedCommand roadChangedCommand = new LogicBrawlerRecruitRoadChangedCommand();
    AvailableServerCommandMessage roadChangedMessage = new AvailableServerCommandMessage();
    roadChangedCommand.homeMode = homeMode;
    roadChangedMessage.Command = roadChangedCommand;
    homeMode.GameListener.SendMessage(roadChangedMessage);
            }
            return 0;
        }

        public override int GetCommandType()
        {
            return 562;
        }
    }
}
