namespace IndusBrawl.Laser.Logic.Message.Home
{
    using Newtonsoft.Json.Linq;
    using System.Text;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home.Items;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Util;
    using IndusBrawl.Laser.Titan.Debug;

    public class UpdatePlayerMapResponseMessage : GameMessage
    {
        public int MapId;
        public int ErrorCode;
        public override void Encode()
        {
            Stream.WriteVInt(ErrorCode);
            ByteStreamHelper.EncodeLogicLong(Stream, MapId);
        }

        public override int GetMessageType()
        {
            return 22103;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
