using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using IndusBrawl.Laser.Logic.Avatar.Structures;
using IndusBrawl.Laser.Logic.Helper;

namespace IndusBrawl.Laser.Logic.Message.Ranked
{
    public class RankedMatchPickHeroMessage : GameMessage
    {
        public int BrawlerId;
        public int PickType;
        public override void Decode()
        {
            Stream.ReadVInt();
            Stream.ReadVInt();
            BrawlerId = Stream.ReadVInt();
            PickType = Stream.ReadVInt();
        }

        public override int GetMessageType()
        {
            return 12155;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
