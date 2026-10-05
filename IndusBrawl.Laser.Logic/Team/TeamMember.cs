namespace IndusBrawl.Laser.Logic.Team
{
    using IndusBrawl.Laser.Logic.Avatar.Structures;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Logic.Home;
    public class TeamMember
    {
        public bool IsOwner;
        public long AccountId;

        public int CharacterId;
        public int SkinId;

        public int HeroTrophies;
        public int HeroHighestTrophies;
        public int HeroLevel;

        public int State;
        public bool IsReady;

        public int SelectedStarPowerId;

        public PlayerDisplayData DisplayData;

        public HomeMode homeMode;

        public int TeamIndex;

        public void Encode(ByteStream stream)
        {
            stream.WriteBoolean(IsOwner);
            stream.WriteLong(AccountId);

            ByteStreamHelper.WriteDataReference(stream, CharacterId);
            ByteStreamHelper.WriteDataReference(stream, SkinId);

            stream.WriteVInt(0);
            stream.WriteVInt(HeroTrophies);//tropies
            stream.WriteVInt(HeroHighestTrophies);
            stream.WriteVInt(HeroLevel);//power
            stream.WriteVInt(State);

            stream.WriteBoolean(IsReady);

            stream.WriteVInt(TeamIndex); // team
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk

            DisplayData.Encode(stream);
            
            ByteStreamHelper.WriteDataReference(stream, null); // star power
            ByteStreamHelper.WriteDataReference(stream, null); // star power
            ByteStreamHelper.WriteDataReference(stream, null); // star power
            ByteStreamHelper.WriteDataReference(stream, null); // unk
            ByteStreamHelper.WriteDataReference(stream, null); // OVERCHARGE
            
            stream.WriteVInt(0);
        }
    }
}
