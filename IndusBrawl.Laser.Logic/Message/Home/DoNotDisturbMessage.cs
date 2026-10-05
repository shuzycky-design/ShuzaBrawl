namespace IndusBrawl.Laser.Logic.Message.Club
{
    public class DoNotDisturbMessage : GameMessage
    {
        public int state;

        public override void Decode()
        {
            state = Stream.ReadVInt();
        }

        public override int GetMessageType()
        {
            return 14777;
        }

        public override int GetServiceNodeType()
        {
            return 8; // я хз вроде 8 или 9
        }
    }
}
