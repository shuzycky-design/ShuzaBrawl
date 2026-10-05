namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Quest;
    using IndusBrawl.Laser.Titan.DataStream;

    public class LogicHeroSeenCommand : Command
    {
        public override void Decode(ByteStream stream)
        {
            ByteStreamHelper.ReadDataReference(stream);
            stream.ReadInt();
        }
        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }
        public override int GetCommandType()
        {
            return 522;
        }
    }
}
