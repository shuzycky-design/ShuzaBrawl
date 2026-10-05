namespace IndusBrawl.Laser.Server.Networking.Session
{
    using Masuda.Net;
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Club;
    using IndusBrawl.Laser.Logic.Friends;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Listener;
    using IndusBrawl.Laser.Logic.Message.Account;
    using IndusBrawl.Laser.Logic.Message.Account.Auth;
    using IndusBrawl.Laser.Logic.Message.Friends;
    using IndusBrawl.Laser.Logic.Message.Team.Stream;
    using IndusBrawl.Laser.Logic.Team.Stream;
    using IndusBrawl.Laser.Logic.Util;
    using IndusBrawl.Laser.Server.Database;
    using IndusBrawl.Laser.Server.Logic.Game;
    using System.Collections.Concurrent;

    public static class Sessions
    {
        public static bool Maintenance;
        private static ConcurrentDictionary<long, Session> ActiveSessions = new ConcurrentDictionary<long, Session>();
        public static long GlobalMessageCount;

        public static int Count
        {
            get
            {
                return ActiveSessions.Count;
            }
        }

        public static void Init()
        {
            ActiveSessions = new ConcurrentDictionary<long, Session>();
            GlobalMessageCount = 0;
        }

        public static void StartShutdown()
        {
            Maintenance = true;
            foreach (var session in ActiveSessions.Values.ToArray())
            {
                session.Connection.Send(new ShutdownStartedMessage());
            }

            Thread.Sleep(1000);

            foreach (var session in ActiveSessions.Values.ToArray())
            {
                session.Connection.Send(new AuthenticationFailedMessage()
                {
                    ErrorCode = 10,
                });
            }

        }

        public static void SendGlobalMessage(long AccountId, string name, string message)
        {
            if (AccountId != -1)
            {
                //MasudaBot masudaBot = new MasudaBot(102038674, "ElazeGW3722wbRMI9StXcSJbvsRLitBm", "ElazeGW3722wbRMI9StXcSJbvsRLitBm", BotType.Public).LogTo(null);
                //masudaBot.SendMessageAsync("638478159", new Masuda.Net.HelpMessage.PlainMessage(name + "(" + LogicLongCodeGenerator.ToCode(AccountId) + "):\n" + message));
            }
            foreach (var session in ActiveSessions.Values.ToArray())
            {
                if (session.Connection.MessageManager.IsAlive())
                {
                    session.Connection.Send(new TeamStreamMessage() { TeamId = -1, Entries = new TeamStreamEntry[] { new ChatStreamEntry() { Id = GlobalMessageCount, AccountId = AccountId, Name = name, Message = message } } });
                }
            }
            GlobalMessageCount++;

        }
        public static void Remove(long id)
        {
            if (ActiveSessions.ContainsKey(id))
            {
                ActiveSessions[id].Home.Avatar.LastOnline = DateTime.UtcNow;
                ActiveSessions[id].Home.Avatar.PlayerStatus = 0;

                FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
                entryMessage.AvatarId = ActiveSessions[id].Home.Avatar.AccountId;
                entryMessage.PlayerStatus = 0;

                foreach (Friend friend in ActiveSessions[id].Home.Avatar.Friends.ToArray())
                {
                    if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                    {
                        LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                    }
                }

                if (ActiveSessions[id].Home.Avatar.AllianceRole != AllianceRole.None && ActiveSessions[id].Home.Avatar.AllianceId > 0)
                {
                    Alliance alliance = Alliances.Load(ActiveSessions[id].Home.Avatar.AllianceId);

                    if (alliance != null)
                    {
                        AllianceOnlineStatusUpdatedMessage allianceOnlineStatusUpdatedMessage = new AllianceOnlineStatusUpdatedMessage()
                        {
                            AvatarId = ActiveSessions[id].Home.Avatar.AccountId,
                            Members = alliance.Members.Count,
                            PlayerStatus = 0
                        };
                        foreach (var member in alliance.Members)
                        {
                            if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                            {
                                LogicServerListener.Instance.GetGameListener(member.AccountId).SendTCPMessage(allianceOnlineStatusUpdatedMessage);
                            }

                        }
                    }
                }

                if (ActiveSessions[id].Home.Avatar.TeamId > 0)
                {
                    var team = Teams.Get(ActiveSessions[id].Home.Avatar.TeamId);
                    if (team != null)
                    {
                        var member = team.GetMember(id);
                        if (member != null)
                        {
                            member.State = 0;
                            team.TeamUpdated();
                        }
                    }
                }
            }
            //return;
            ActiveSessions.Remove(id, out _);
        }

        public static Session Create(HomeMode home, Connection connection)
        {
            if (ActiveSessions.ContainsKey(home.Avatar.AccountId))
            {
                Session oldSession = ActiveSessions[home.Avatar.AccountId];
                //oldSession.Connection.Send(new DisconnectedMessage()
                //{
                //    Reason = 1
                //});

                Session s = new Session(home, connection);
                ActiveSessions[home.Avatar.AccountId] = s;
                return s;
            }

            Session session = new Session(home, connection);
            ActiveSessions[home.Avatar.AccountId] = session;
            return session;
        }

        public static bool IsSessionActive(long id)
        {
            //if (!ActiveSessions.ContainsKey(id))
            //{
            //    ;
            //}
            return ActiveSessions.ContainsKey(id);
        }

        public static Session GetSession(long id)
        {
            if (ActiveSessions.ContainsKey(id))
            {
                return ActiveSessions[id];
            }
            return null;
        }
    }
}
