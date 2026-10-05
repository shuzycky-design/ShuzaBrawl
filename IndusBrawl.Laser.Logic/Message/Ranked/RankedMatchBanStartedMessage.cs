using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using IndusBrawl.Laser.Logic.Avatar.Structures;
using IndusBrawl.Laser.Logic.Helper;

namespace IndusBrawl.Laser.Logic.Message.Ranked
{
    public class RankedMatchBanStartedMessage : GameMessage
    {
        public int Time;
        public int QueueIndexer;
        public override void Encode()
        {
            Stream.WriteVInt(15); // time
            Stream.WriteVInt(QueueIndexer); // queue index
            Stream.WriteVInt(0);
        }

        public override int GetMessageType()
        {
            return 22151;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
