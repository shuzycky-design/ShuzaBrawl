namespace IndusBrawl.Laser.Server
{
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Listener;
    using IndusBrawl.Laser.Server.Database;
    using IndusBrawl.Laser.Server.Logic;
    using IndusBrawl.Laser.Server.Logic.Game;
    using IndusBrawl.Laser.Server.Message;
    using IndusBrawl.Laser.Server.Networking;
    using IndusBrawl.Laser.Server.Networking.Session;
    using IndusBrawl.Laser.Server.Settings;
    using IndusBrawl.Laser.Server.Fingerprint;

    internal static class Resources
    {
        /// <summary>
        /// Initializes the databases
        /// </summary>
        public static void InitDatabase()
        {
            Accounts.Init(Configuration.Instance.DatabaseUsername, Configuration.Instance.DatabasePassword);
            Alliances.Init(Configuration.Instance.DatabaseUsername, Configuration.Instance.DatabasePassword);
        }

        /// <summary>
        /// Initializes the logic part of server
        /// </summary>
        public static void InitLogic()
        {
            Fingerprint.Fingerprint.Load();
            DataTables.Load();
            Events.Init();
            Sessions.Init();
            Leaderboards.Init();
            Battles.Init();
            Matchmaking.Init();
            Teams.Init();
        }

        /// <summary>
        /// Initializes the network part of server
        /// </summary>
        public static void InitNetwork()
        {
            Processor.Init();
            Connections.Init();
            LogicServerListener.Instance = new ServerListener();
            UDPGateway.Init("0.0.0.0", Configuration.Instance.UdpPort);
            TCPGateway.Init("0.0.0.0", Configuration.Instance.TcpPort > 0 ? Configuration.Instance.TcpPort : 9339);
        }
    }
}
