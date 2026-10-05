namespace IndusBrawl.Laser.Logic.Message.Home
{
    public class SetCountryResponseMessage : GameMessage
    {
        public int CountryID;
        public override int GetMessageType()
        {
            return 24178;
        }
        public override void Encode()
        {
            Stream.WriteVInt(0);
            Stream.WriteDataReference(14, CountryID);
        }
        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
