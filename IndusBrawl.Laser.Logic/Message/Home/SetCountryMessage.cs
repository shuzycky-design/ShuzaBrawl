using IndusBrawl.Laser.Logic.Helper;

namespace IndusBrawl.Laser.Logic.Message.Home
{
    public class SetCountryMessage : GameMessage
    {
        public int CountryID;
        public override void Decode()
        {
            Stream.ReadVInt();
            CountryID = Stream.ReadVInt();
        }
        public override int GetMessageType()
        {
            return 12998;
        }
        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
