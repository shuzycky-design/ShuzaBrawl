using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndusBrawl.Laser.Logic.Message.Club
{
    public class AlliancePremadeChatMessage : GameMessage
    {
        public int EmojiGlobalId;
        public int DefEmojiSlot;

        public override void Decode()
        {
            Stream.ReadVInt();
            DefEmojiSlot = Stream.ReadVInt(); // def emoji type
            EmojiGlobalId = Stream.ReadVInt(); // emoji global id(not work for def)
            /*
             * 27 - 160 - 3
             * 29 - 85 - 3 
             * 37 - 96-3
             * 33 - 52000081
             * 40 - 52000064
             * 32 - 52000022
             * 41 - 52000158
             * 28 - 148 - 3
             * 38 - 52000169
             * 35 - 52000156
             * 39 - 28 
             * 31 - 52000006
             * 30 - 52000005
             * 34 - 137 - 3
             * 42 - 52000075
             * 25 - 179-3
             * 26 - 178-3
             */

        }
        public override void Encode()
        {
            // Stream.WriteVInt(ResponseType);
        }

        public override int GetMessageType()
        {
            return 14469;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}