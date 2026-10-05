using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using IndusBrawl.Laser.Logic.Avatar.Structures;
using IndusBrawl.Laser.Logic.Helper;
using IndusBrawl.Laser.Logic.Message.Club;

namespace IndusBrawl.Laser.Logic.Message.Ranked
{
    public class RankedMatchBanHeroResponseMessage : GameMessage
    {
        public int BannedCharacter;
        public long BannerAccountId; // <-- ид того, кто забанил

        public override void Encode()
        {
            ByteStreamHelper.EncodeLogicLong(Stream, BannerAccountId); // <-- реальный ид
            Stream.WriteVInt(0);
            Stream.WriteDataReference(BannedCharacter);
            Stream.WriteBoolean(false);
            Stream.WriteVInt(1);
        }

        public override int GetMessageType()
        {
            return 22152;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
