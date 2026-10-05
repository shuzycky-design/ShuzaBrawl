namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Gatcha;
    using IndusBrawl.Laser.Titan.DataStream;

    public class LogicSetDoNotDisturb : Command
    {
        public int DoNotDistrub;

        public override void Encode(ByteStream stream)
        {

            stream.WriteVInt(DoNotDistrub);
            stream.WriteVInt(0);
            base.Encode(stream);

        }

        public override int Execute(HomeMode homeMode)
        {
            homeMode.Avatar.DoNotDisturb = DoNotDistrub;
            return 0;
        }

        public override int GetCommandType()
        {
            return 213;
        }
    }
}
