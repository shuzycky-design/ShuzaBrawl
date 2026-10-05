namespace IndusBrawl.Laser.Logic.Message.Battle
{
    using IndusBrawl.Laser.Logic.Battle.Structures;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Titan.Util;

    public class StartBrawlTVMessage : GameMessage
    {
        public override void Encode()
        {
            Stream.WriteString(null);
        }

        public override int GetMessageType()
        {
            return 14700;
        }

        public override int GetServiceNodeType()
        {
            return 4;
        }
    }
}
