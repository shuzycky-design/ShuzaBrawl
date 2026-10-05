namespace IndusBrawl.Laser.Server.Networking
{
    using Masuda.Net;
    using IndusBrawl.Laser.Logic.Message.Account;
    using IndusBrawl.Laser.Logic.Message.Team.Stream;
    using IndusBrawl.Laser.Logic.Team.Stream;
    using IndusBrawl.Laser.Server.Networking.Session;

    public static class Connections
    {
        public static int Count => ActiveConnections.Count;
        private static List<Connection> ActiveConnections;
        private static Thread Thread;
        private static long SecondsGone;

        public static void Init()
        {
            ActiveConnections = new List<Connection>();
            Thread = new Thread(Update);
            Thread.Start();
            SecondsGone = 0;
        }

        private static void Update()
        {
            while (true)
            {
                foreach (Connection connection in ActiveConnections.ToArray())
                {
                    if (connection.Avatar != null && !Sessions.IsSessionActive(connection.Avatar.AccountId))
                    {
                        connection.Close();
                        ActiveConnections.Remove(connection);
                    }
                    if (!connection.MessageManager.IsAlive())
                    {
                        //DisconnectedMessage d = new DisconnectedMessage() { Reason = 0 };
                        //connection.Send(d);
                        if (connection.MessageManager.HomeMode != null)
                        {
                            Sessions.Remove(connection.Avatar.AccountId);
                        }
                        connection.Close();
                        ActiveConnections.Remove(connection);
                    }
                }
                //if (SecondsGone % 5 == 0) Sessions.SendGlobalMessage(-1, "System", "test");
                SecondsGone++;
                Thread.Sleep(1000);
            }
        }

        public static void AddConnection(Connection connection)
        {
            ActiveConnections.Add(connection);
        }
    }
}
