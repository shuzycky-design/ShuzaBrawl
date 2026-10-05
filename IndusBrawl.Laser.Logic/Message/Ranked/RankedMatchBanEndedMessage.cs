using System.Collections.Generic;
using IndusBrawl.Laser.Logic.Helper;

namespace IndusBrawl.Laser.Logic.Message.Ranked
{
    
    public class RankedMatchBanEndedMessage : GameMessage
    {
        public Dictionary<long, int> BannedBrawlers;
        public override void Encode()
        {
            if (BannedBrawlers == null)
                BannedBrawlers = new Dictionary<long, int>();
            Stream.WriteVInt(BannedBrawlers.Count); // count 
            foreach(var kvp  in BannedBrawlers)
            {
                Stream.WriteLong(kvp.Key);
                Stream.WriteDataReference(kvp.Value);
            }
        }

        public override int GetMessageType()
        {
            return 22153;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
