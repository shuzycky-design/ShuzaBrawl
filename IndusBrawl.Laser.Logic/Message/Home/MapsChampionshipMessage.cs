namespace IndusBrawl.Laser.Logic.Message.Home
{
    public class MapsChampionshipMessage : GameMessage
    {
        public int Type;
        public override void Encode()
        {
            // заглушко
        }

        public override int GetMessageType()
        {
            return 12938;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}