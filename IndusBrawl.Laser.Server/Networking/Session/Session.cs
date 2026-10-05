namespace IndusBrawl.Laser.Server.Networking.Session
{
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Listener;

    public class Session
    {
        public HomeMode Home;
        public Connection Connection;
        public LogicGameListener GameListener => Home.GameListener;

        public Session(HomeMode home, Connection connection)
        {
            Home = home;
            Connection = connection;
        }
    }
}
