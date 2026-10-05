namespace IndusBrawl.Laser.Server.Logic
{
    using IndusBrawl.Laser.Logic.Listener;
    using IndusBrawl.Laser.Server.Networking;
    using IndusBrawl.Laser.Logic.Message;

    public class HomeGameListener : LogicGameListener
    {
        private Connection Connection;

        public HomeGameListener(Connection connection)
        {
            Connection = connection;
        }

        public override void SendMessage(GameMessage message)
        {
            Connection.Send(message);
        }

        public override void SendTCPMessage(GameMessage message)
        {
            Connection.Send(message);
        }
    }
}
