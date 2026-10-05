namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Gatcha;
    using IndusBrawl.Laser.Logic.Message.Home;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;

    public class LogicGatchaCommand : Command
    {
        public int BoxIndex;

        public LogicGatchaCommand() : base()
        {
            ;
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            BoxIndex = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 500;
        }
    }
}
