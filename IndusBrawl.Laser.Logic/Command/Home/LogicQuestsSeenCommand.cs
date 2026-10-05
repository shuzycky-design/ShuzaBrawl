namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Quest;

    public class LogicQuestsSeenCommand : Command
    {
        public override int Execute(HomeMode homeMode)
        {
            //foreach (Quest quest in homeMode.Home.Quests.QuestList.ToArray())
            //{
            //    quest.QuestSeen = true;
            //}

            return 0;
        }

        public override int GetCommandType()
        {
            return 533;
        }
    }
}
