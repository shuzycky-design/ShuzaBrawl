using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using IndusBrawl.Laser.Logic.Avatar.Structures;
using IndusBrawl.Laser.Logic.Helper;
using IndusBrawl.Laser.Logic.Message.Club;

namespace IndusBrawl.Laser.Logic.Message.Ranked
{
    public class RankedMatchBanHeroMessage : GameMessage
    {
        public int BrawlerId;
        public int PlayerSlot;    // глобальный слот 0..5
        public int NextSlot;      // кто ходит дальше
        public long MatchId;
        public int PickType;

        public override void Encode()
        {
            ByteStreamHelper.EncodeLogicLong(Stream, MatchId);

            Stream.WriteVInt(PlayerSlot); // глобальный слот 0..5
            Stream.WriteVInt(BrawlerId);

            Stream.WriteVInt(NextSlot); // кто ходит дальше
        }

        public override void Decode()
        {
            Stream.ReadVInt();
            BrawlerId = Stream.ReadVInt();
            PickType = Stream.ReadVInt();
        }

        public override int GetMessageType()
        {
            return 12152;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
