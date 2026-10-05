using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using IndusBrawl.Laser.Logic.Avatar.Structures;
using IndusBrawl.Laser.Logic.Helper;

namespace IndusBrawl.Laser.Logic.Message.Ranked
{
    public class RankedMatchFinalPreparationStartedMessage : GameMessage
    {
        public int FinalSeconds = 10;
        public override void Encode()
        {
            Stream.WriteVInt(FinalSeconds); 
        }

        public override int GetMessageType()
        {
            return 22158;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
