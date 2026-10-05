namespace IndusBrawl.Laser.Logic.Message.Team
{
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Logic.Helper;

    public class TeamSetPlayerMapMessage : GameMessage
    {
        public long mapid;
        public override int GetMessageType()
        {
            return 12110;
        }
        public override void Decode()
        {
            mapid = Stream.ReadVLong();
        }
        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
