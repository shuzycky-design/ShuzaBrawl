namespace IndusBrawl.Laser.Logic.Message.Friends
{
    using System.Linq;
    using IndusBrawl.Laser.Logic.Club;
    using Newtonsoft.Json;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Listener;
    using IndusBrawl.Laser.Logic.Message.Club;
    using IndusBrawl.Laser.Logic.Stream;
    using IndusBrawl.Laser.Logic.Stream.Entry;
    using IndusBrawl.Laser.Titan.DataStream;

    public class AllianceOnlineStatusUpdatedMessage : GameMessage
    {
        public long AvatarId { get; set; }
        public int PlayerStatus { get; set; }
        public int Members;

        public override void Encode()
        {
            Stream.WriteVInt(Members); // Общее количество игроков в клане
            Stream.WriteVInt(1);
            Stream.WriteLong(AvatarId);
            Stream.WriteVInt(PlayerStatus);
        }

        public override int GetMessageType()
        {
            return 20207;
        }

        public override int GetServiceNodeType()
        {
            return 3;
        }
    }
}