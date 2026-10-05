namespace IndusBrawl.Laser.Logic.Message.Club
{
    using IndusBrawl.Laser.Logic.Club;

    public class AllianceMemberMessage : GameMessage
    {
        public long AvatarId;
        public AllianceMember Entry;

        public override void Encode()
        {
            Stream.WriteLong(AvatarId);
            Entry.Encode(Stream);
        }

        public override int GetMessageType()
        {
            return 24308;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
