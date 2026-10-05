using IndusBrawl.Laser.Logic.Avatar.Structures;
using IndusBrawl.Laser.Logic.Helper;
using IndusBrawl.Laser.Logic.Ranked;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace IndusBrawl.Laser.Logic.Message.Ranked
{
    public class RankedMatchPickStartedMessage : GameMessage
    {
        public mp_thestealdev Player;
        public int Next;
        /// <summary>Команда зрителя (0 = синие, 1 = красные). Для кодирования слота игрока как у зрителя: свои 0,1,2, чужие 3,4,5.</summary>
        public int ViewerTeam;

        public override void Encode()
        {
            
            Stream.WriteVInt(15);
            Stream.WriteBoolean(true);
            if (Player != null)
            {
                int? displaySlot = ViewerTeam == 1 ? (Player.piIternnal_thestealdev + 3) % 6 : null;
                Player.Encode(Stream, true, displaySlot);
                ByteStreamHelper.EncodeLogicLong(Stream, Player.i_thestealdev);
            }
            else
            {
                // Создаем пустого игрока если Player null
                ByteStreamHelper.EncodeLogicLong(Stream, 0);
                Stream.WriteBoolean(true);
                new PlayerDisplayData().Encode(Stream);
                Stream.WriteVInt(0);
                Stream.WriteVInt(0);
                Stream.WriteVInt(0);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                Stream.WriteVInt(11);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                Stream.WriteBoolean(false);
                Stream.WriteVInt(1);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                Stream.WriteVInt(1);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                Stream.WriteVInt(0);
                ByteStreamHelper.EncodeLogicLong(Stream, 0);
            }

            Stream.WriteVInt(0);
            Stream.WriteVInt(Next); // next who ban
        }

        public override int GetMessageType()
        {
            return 22154;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
