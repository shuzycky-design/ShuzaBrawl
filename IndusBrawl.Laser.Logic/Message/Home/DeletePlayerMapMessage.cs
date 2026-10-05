namespace IndusBrawl.Laser.Logic.Message.Home
{
    using Newtonsoft.Json.Linq;
    using System.Text;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home.Items;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Util;
    using IndusBrawl.Laser.Titan.Debug;

    public class DeletePlayerMapMessage : GameMessage
    {
        public int MapId;
        public override void Decode()
        {
            MapId=(int) ByteStreamHelper.DecodeLogicLong(Stream);
        }

        public override int GetMessageType()
        {
            return 12101;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
