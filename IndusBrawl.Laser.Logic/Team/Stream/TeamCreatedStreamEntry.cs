namespace IndusBrawl.Laser.Logic.Team.Stream
{
    using IndusBrawl.Laser.Titan.DataStream;

    public class TeamCreatedStreamEntry : TeamStreamEntry
    {
        public long TeamId;

        public override void Encode(ByteStream encoder)
        {
            base.Encode(encoder);
            encoder.WriteLong(TeamId);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
        }

        public override int GetStreamEntryType()
        {
            return 77;
        }
    }
}
