using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using IndusBrawl.Laser.Logic.Avatar.Structures;
using IndusBrawl.Laser.Logic.Helper;
using IndusBrawl.Laser.Logic.Ranked;

namespace IndusBrawl.Laser.Logic.Message.Ranked
{
    public class RankedMatchHeroDataUpdatedMessage : GameMessage
    {
        public mp_thestealdev Player;
        /// <summary>Команда зрителя: для отображения слота игрока (свои 0,1,2, чужие 3,4,5).</summary>
        public int ViewerTeam;
        public override void Encode()
        {
            Stream.WriteBoolean(true);
            int? displaySlot = ViewerTeam == 1 ? (Player.piIternnal_thestealdev + 3) % 6 : null;
            Player.Encode(Stream, true, displaySlot);
        }

        public override int GetMessageType()
        {
            return 22157;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
