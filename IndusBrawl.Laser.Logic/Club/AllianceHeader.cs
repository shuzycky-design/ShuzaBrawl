namespace IndusBrawl.Laser.Logic.Club
{
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Titan.DataStream;

    public class AllianceHeader
    {
        private long Id;

        private string Name;
        private int Badge;
        private int PlayersCount;
        private int Trophies;
        private int RequiredTrophies;

        private int Type;

        public AllianceHeader(long id, string name, int badge, int playersCount, int trophies, int requiredTrophies, int type)
        {
            Id = id;
            Name = name;
            Badge = badge;
            PlayersCount = playersCount;
            Trophies = trophies;
            RequiredTrophies = requiredTrophies;

            Type = type;
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteLong(Id);
            stream.WriteString(Name);
            ByteStreamHelper.WriteDataReference(stream, Badge);
            stream.WriteVInt(Type); // Type
            stream.WriteVInt(PlayersCount);
            stream.WriteVInt(Trophies);
            stream.WriteVInt(RequiredTrophies); // trophies required
            ByteStreamHelper.WriteDataReference(stream, null);
            stream.WriteString("RU");
            stream.WriteVInt(0);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);

            stream.WriteVInt(0);//v53
        }
    }
}
