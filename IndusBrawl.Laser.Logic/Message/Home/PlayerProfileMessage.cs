namespace IndusBrawl.Laser.Logic.Message.Home
{
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Club;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home.Structures;

    public class PlayerProfileMessage : GameMessage
    {
        public Profile Profile;
        public AllianceHeader AllianceHeader;
        public AllianceRole AllianceRole;

        public PlayerProfileMessage() : base()
        {
            ;
        }

        public override void Encode()
        {
            Profile.Encode(Stream);

            if (Stream.WriteBoolean(AllianceHeader != null))
            {
                AllianceHeader.Encode(Stream);
            }

            if (AllianceRole > AllianceRole.None)
                ByteStreamHelper.WriteDataReference(Stream, GlobalId.CreateGlobalId(25, (int)AllianceRole));
            else
                Stream.WriteVInt(0);

            Stream.WriteVInt(0);//???
        }

        public override int GetMessageType()
        {
            return 24113;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
