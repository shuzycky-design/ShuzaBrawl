namespace IndusBrawl.Laser.Logic.Message
{
    using IndusBrawl.Laser.Logic.Message;
    using IndusBrawl.Laser.Logic.Message.Account;
    using IndusBrawl.Laser.Logic.Message.Club;
    using IndusBrawl.Laser.Logic.Message.Battle;
    using IndusBrawl.Laser.Logic.Message.Friends;
    using IndusBrawl.Laser.Logic.Message.Home;
    using IndusBrawl.Laser.Logic.Message.Ranking;
    using IndusBrawl.Laser.Logic.Message.Security;
    using IndusBrawl.Laser.Logic.Message.Team;
    using IndusBrawl.Laser.Logic.Message.Team.Stream;
    using IndusBrawl.Laser.Logic.Message.Udp;
    using IndusBrawl.Laser.Logic.Message.Account.Auth;
using IndusBrawl.Laser.Logic.Message.Ranked;


    public class MessageFactory
    {
        public static MessageFactory Instance;

        static MessageFactory()
        {
            Instance = new MessageFactory();
        }

        private Dictionary<int, Type> Definitions;

        public MessageFactory()
        {
            Definitions = new Dictionary<int, Type>()
            {
                {10100, typeof(ClientHelloMessage)},
                {10101, typeof(AuthenticationMessage)},
                {10107, typeof(ClientCapabilitiesMessage)},
                {10108, typeof(KeepAliveMessage)},
                {10110, typeof(AnalyticEventMessage)},

                {10177, typeof(ClientInfoMessage)},
                {10212, typeof(ChangeAvatarNameMessage)},

                {10501, typeof(AcceptFriendMessage)},
                {10502, typeof(AddFriendMessage)},
                {10504, typeof(AskForFriendListMessage)},
                {10506, typeof(RemoveFriendMessage)},

                {10555, typeof(ClientInputMessage)},

                {12100,typeof(CreatePlayerMapMessage)},
                {12101,typeof(DeletePlayerMapMessage)},
                {12102,typeof(GetPlayerMapsMessage)},
                {12103,typeof(UpdatePlayerMapMessage)},
                {12108,typeof(GoHomeFromMapEditorMessage)},
                {12110,typeof(TeamSetPlayerMapMessage)},
                {14456, typeof(GoHomeMessage)},
                {14102, typeof(EndClientTurnMessage)},
                {18977, typeof(MatchmakeRequestMessage)},
                {14104, typeof(StartSpectateMessage)},
                {14106, typeof(CancelMatchmakingMessage)},
                {14107, typeof(StopSpectateMessage)},
                {14109, typeof(GoHomeFromOfflinePractiseMessage)},
                {15081, typeof(GetPlayerProfileMessage)},
                {14118, typeof(SinglePlayerMatchRequestMessage)},
                {14166, typeof(ChronosEventSeenMessage)},

                {14301, typeof(CreateAllianceMessage)},
                {14302, typeof(AskForAllianceDataMessage)},
                {14303, typeof(AskForJoinableAllianceListMessage)},
                {14305, typeof(JoinAllianceMessage)},
                {14307, typeof(KickAllianceMemberMessage)},
                {14308, typeof(LeaveAllianceMessage)},
                {14315, typeof(ChatToAllianceStreamMessage)},
                {14316, typeof(ChangeAllianceSettingsMessage)},

                {12541, typeof(TeamCreateMessage)},
                {14353, typeof(TeamLeaveMessage)},
                {14354, typeof(TeamChangeMemberSettingsMessage)},
                {14355, typeof(TeamSetMemberReadyMessage)},
                {14357, typeof(TeamToggleMemberSideMessage)},
                {14358, typeof(TeamSpectateMessage)},
                {14049, typeof(TeamChatMessage)},
                {14361, typeof(TeamMemberStatusMessage)},
                {14362, typeof(TeamSetEventMessage)},
                {14363, typeof(TeamSetLocationMessage)},
                {14365, typeof(TeamInviteMessage)},
                {14366, typeof(PlayerStatusMessage)},
                {14369, typeof(TeamPremadeChatMessage)},
                {14373, typeof(TeamBotSlotDisableMessage)},
                {12938, typeof(GetMapsChampionshipMessage)},

                {14403, typeof(GetLeaderboardMessage)},

                {14479, typeof(TeamInvitationResponseMessage)},

                {14600, typeof(AvatarNameCheckRequestMessage)},

                {14881, typeof(TeamRequestJoinMessage)},
                {14469, typeof(AlliancePremadeChatMessage) },
                {14114, typeof(GetBattleLogMessage) },
                {12998, typeof(SetCountryMessage) },
                {14777, typeof(DoNotDisturbMessage) },
                {14700, typeof(StartBrawlTVMessage) },
                {18686, typeof(SetSupportedCreatorMessage)},
                {new RankedMatchPickHeroMessage().GetMessageType(), typeof(RankedMatchPickHeroMessage)},
                                {new RankedMatchBanHeroMessage().GetMessageType(), typeof(RankedMatchBanHeroMessage)},
            };
        }

        public GameMessage CreateMessageByType(int type)
        {
            if (Definitions.ContainsKey(type))
            {
                return (GameMessage)Activator.CreateInstance(Definitions[type]);
            }
            return null;
        }
    }
}
