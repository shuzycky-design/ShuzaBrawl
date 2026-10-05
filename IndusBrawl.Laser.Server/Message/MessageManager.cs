using IndusBrawl.Laser.Logic.Message.Ranked;
using IndusBrawl.Laser.Logic.Ranked;

namespace IndusBrawl.Laser.Server.Message
{
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Avatar.Structures;
    using IndusBrawl.Laser.Logic.Club;
    using IndusBrawl.Laser.Logic.Command.Avatar;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Friends;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Logic.Listener;
    using IndusBrawl.Laser.Logic.Message.Account;
    using IndusBrawl.Laser.Logic.Message.Club;
    using IndusBrawl.Laser.Logic.Message.Friends;
    using IndusBrawl.Laser.Logic.Message.Home;
    using IndusBrawl.Laser.Logic.Message.Ranking;
    using IndusBrawl.Laser.Logic.Message.Security;
    using IndusBrawl.Laser.Logic.Message.Team;
    using IndusBrawl.Laser.Logic.Message.Udp;
    using IndusBrawl.Laser.Logic.Stream.Entry;
    using IndusBrawl.Laser.Logic.Team;
    using IndusBrawl.Laser.Server.Database;
    using IndusBrawl.Laser.Server.Database.Models;
    using IndusBrawl.Laser.Server.Logic.Game;
    using IndusBrawl.Laser.Server.Networking;
    using IndusBrawl.Laser.Server.Networking.Session;
    using IndusBrawl.Laser.Server.Settings;
    using IndusBrawl.Laser.Server.Logic;
    using IndusBrawl.Laser.Server.Utils;
    using System.Diagnostics;
    using IndusBrawl.Laser.Server.Database.Cache;
    using IndusBrawl.Laser.Logic.Util;
    using IndusBrawl.Laser.Logic.Battle;
    using IndusBrawl.Laser.Logic.Message.Battle;
    using IndusBrawl.Laser.Server.Networking.UDP.Game;
    using IndusBrawl.Laser.Server.Networking.Security;
    using IndusBrawl.Laser.Logic.Home.Items;
    using IndusBrawl.Laser.Logic.Team.Stream;
    using IndusBrawl.Laser.Logic.Message.Team.Stream;
    using IndusBrawl.Laser.Logic.Message;
    using IndusBrawl.Laser.Logic.Message.Account.Auth;
    using IndusBrawl.Laser.Titan.Debug;
    using IndusBrawl.Laser.Logic.Battle.Structures;
    using System.Reflection.Metadata.Ecma335;
    using Debugger = Titan.Debug.Debugger;
    using IndusBrawl.Laser.Logic.Command.Home;
    using IndusBrawl.Laser.Logic.Notification;
    using Org.BouncyCastle.Crypto;
    using Google.Protobuf.WellKnownTypes;
    using K4os.Compression.LZ4.Internal;
    using Masuda.Net.Models;
    using Newtonsoft.Json;
    using IndusBrawl.Laser.Titan.Json;
    using Org.BouncyCastle.Math;
    using IndusBrawl.Laser.Logic.Battle.Component;
    using System.Security.Cryptography;
    using System.Security.Policy;
    using System.Threading;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Titan.Math;
    using IndusBrawl.Laser.Logic.Battle.Level;
    using IndusBrawl.Laser.Server.Handler;
    using System.Buffers;
    using System.Runtime.InteropServices;
    using IndusBrawl.Laser.Logic.Home.Gatcha;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System;
    using System.Text.RegularExpressions;
    using IndusBrawl.Laser.Logic.Battle.Structures;
    
    public class MessageManager
    {
        public Connection Connection { get; }

        public HomeMode HomeMode;

        public DateTime LastKeepAlive;

        private Dictionary<int, int> accrelay;
        public MessageManager(Connection connection)
        {
            Connection = connection;
            LastKeepAlive = DateTime.UtcNow;
        }

        public bool IsAlive()
        {
            return (int)(DateTime.UtcNow - LastKeepAlive).TotalSeconds < 15;
            //return true;
        }

        public void ReceiveMessage(GameMessage message)
{
    // Отправляем LobbyInfoMessage для всех сообщений кроме 10100
    if (message.GetMessageType() != 10100)
    {
        string vipDate = "";
        
        // Получаем VIP дату если есть
        if (HomeMode != null && HomeMode.Avatar != null && HomeMode.Avatar.HasVIP())
        {
            vipDate = HomeMode.Avatar.VIPExpire.ToString("dd.MM.yyyy");
 /////           Console.WriteLine($"VIP дата для игрока {HomeMode.Avatar.AccountId}: {vipDate}");
        }
        
        // Создаем сообщение с количеством онлайн и датой VIP
        LobbyInfoMessage lim = new LobbyInfoMessage(Sessions.Count, vipDate);
        Connection.Send(lim);
    }
    
    // Обработка конкретных типов сообщений
    switch (message.GetMessageType())
    {
                case 10100:
                    ClientHelloReceived((ClientHelloMessage)message);
                    break;
                case 10101:
                    LoginReceived((AuthenticationMessage)message);
                    break;
                case 10107:
                    ClientInfoReceived((ClientInfoMessage)message);
                    break;
                case 10108:
                    LastKeepAlive = DateTime.UtcNow;
                    //Connection.Send(new KeepAliveServerMessage());
                    break;
                case 10110:
                    AnalyticEventsReceived((AnalyticEventMessage)message);
                    break;
                case 10212:
                    ChangeName((ChangeAvatarNameMessage)message);
                    break;
                case 10177:
                    ClientInfoReceived((ClientInfoMessage)message);
                    break;
                case 10501:
                    AcceptFriendReceived((AcceptFriendMessage)message);
                    break;
                case 10502:
                    AddFriendReceived((AddFriendMessage)message);
                    break;
                case 10504:
                    AskForFriendListReceived((AskForFriendListMessage)message);
                    break;
                case 10506:
                    RemoveFriendReceived((RemoveFriendMessage)message);
                    break;
                case 12100:
                    CreatePlayerMapReceived((CreatePlayerMapMessage)message);
                    break;
                case 12101:
                    DeletePlayerMapReceived((DeletePlayerMapMessage)message);
                    break;
                case 12102:
                    GetPlayerMapsReceived((GetPlayerMapsMessage)message);
                    break;
                case 12103:
                    UpdatePlayerMapReceived((UpdatePlayerMapMessage)message);
                    break;
                case 12108:
                    GoHomeFromMapEditorReceived((GoHomeFromMapEditorMessage)message);
                    break;
                case 12110:
                    TeamSetPlayerMapReceived((TeamSetPlayerMapMessage)message);
                    break;
                case 14456:
                    GoHomeReceived((GoHomeMessage)message);
                    break;
                case 14102:
                    EndClientTurnReceived((EndClientTurnMessage)message);
                    break;
                case 18977:
                    MatchmakeRequestReceived((MatchmakeRequestMessage)message);
                    break;
                case 14104:
                    StartSpectateReceived((StartSpectateMessage)message);
                    break;
                case 14106:
                    CancelMatchMaking((CancelMatchmakingMessage)message);
                    break;
                case 14107:
                    StopSpectateReceived((StopSpectateMessage)message);
                    break;
                case 14109:
                    GoHomeFromOfflinePractiseReceived((GoHomeFromOfflinePractiseMessage)message);
                    break;
                case 15081:
                    GetPlayerProfile((GetPlayerProfileMessage)message);
                    break;
                case 14118:
                    SinglePlayerMatchRequestReceived((SinglePlayerMatchRequestMessage)message);
                    break;
                case 14166:
                    break;
                case 12938:
                    GetMapsChampionshipMessageReceived((GetMapsChampionshipMessage)message);
                    break;
                case 14301:
                    CreateAllianceReceived((CreateAllianceMessage)message);
                    break;
                case 14302:
                    AskForAllianceDataReceived((AskForAllianceDataMessage)message);
                    break;
                case 14303:
                    AskForJoinableAllianceListReceived((AskForJoinableAllianceListMessage)message);
                    break;
                case 14305:
                    JoinAllianceReceived((JoinAllianceMessage)message);
                    break;
                case 14307:
                    KickAllianceMemberReceived((KickAllianceMemberMessage)message);
                    break;
                case 14308:
                    LeaveAllianceReceived((LeaveAllianceMessage)message);
                    break;
                case 14315:
                    ChatToAllianceStreamReceived((ChatToAllianceStreamMessage)message);
                    break;
                case 14316:
                    ChangeAllianceSettingsReceived((ChangeAllianceSettingsMessage)message);
                    break;
                case 12541:
                    TeamCreateReceived((TeamCreateMessage)message);
                    break;
                case 14353:
                    TeamLeaveReceived((TeamLeaveMessage)message);
                    break;
                case 14354:
                    TeamChangeMemberSettingsReceived((TeamChangeMemberSettingsMessage)message);
                    break;
                case 14355:
                    TeamSetMemberReadyReceived((TeamSetMemberReadyMessage)message);
                    break;
                case 14357:
                    TeamToggleMemberSideReceived((TeamToggleMemberSideMessage)message);
                    break;
                case 14358:
                    TeamSpectateMessageReceived((TeamSpectateMessage)message);
                    break;
                case 14049:
                    TeamChatReceived((TeamChatMessage)message);
                    break;
                case 14361:
                    TeamMemberStatusReceived((TeamMemberStatusMessage)message);
                    break;
                case 14362:
                    TeamSetEventReceived((TeamSetEventMessage)message);
                    break;
                case 14363:
                    TeamSetLocationReceived((TeamSetLocationMessage)message);
                    break;
                case 14365:
                    TeamInviteReceived((TeamInviteMessage)message);
                    break;
                case 14366:
                    PlayerStatusReceived((PlayerStatusMessage)message);
                    break;
                case 14369:
                    TeamPremadeChatReceived((TeamPremadeChatMessage)message);
                    break;
                case 14469:
                    AlliancePremadeChatReceived((AlliancePremadeChatMessage)message);
                    break;
                case 14373:
                    TeamBotSlotDisableReceived((TeamBotSlotDisableMessage)message);
                    break;
                case 14403:
                    GetLeaderboardReceived((GetLeaderboardMessage)message);
                    break;
                case 14479:
                    TeamInvitationResponseReceived((TeamInvitationResponseMessage)message);
                    break;
                case 14600:
                    AvatarNameCheckRequestReceived((AvatarNameCheckRequestMessage)message);
                    break;
                case 14881:
                    TeamRequestJoinReceived((TeamRequestJoinMessage)message);
                    break;
                case 14114:
                    GetBattleLogReceived((GetBattleLogMessage)message);
                    break;
                case 12998:
                    SetCountryReceived((SetCountryMessage)message);
                    break;
                case 14777:
                    DoNotDisturbReceived((DoNotDisturbMessage)message);
                    break;
                case 14700:
                    StartBrawlTVReceived((StartBrawlTVMessage)message);
                    break;
                  case 18686:
                    SetSupportedCreator((SetSupportedCreatorMessage)message);
                    break;
                                    case 12155:
                    RankedMatchPickHeroReceived((RankedMatchPickHeroMessage)message);
                    break;
                case 12152:
                    RankedMatchBanHero((RankedMatchBanHeroMessage)message);
                    break;
                default:
        //////            Logger.Print($"MessageManager::ReceiveMessage - no case for {message.GetType().Name} ({message.GetMessageType()})");
                    break;
            }
            //Logger.Print($"MessageManager::ReceiveMessage - received {message.GetType().Name} ({message.GetMessageType()})");
        }
        private void RankedMatchPickHeroReceived(RankedMatchPickHeroMessage message)
{
    RankedMatch match = RankedMatchRegulator.Get(HomeMode.Avatar.RanledId);
    if (match == null) return;

    mp_thestealdev player = match.GetPlayer(HomeMode.Avatar.AccountId);
    if (player == null) return;
    
    int g = GlobalId.CreateGlobalId(16, message.BrawlerId);
    var characterData = DataTables.Get(16).GetDataByGlobalId<CharacterData>(g);
    if (characterData == null) g = GlobalId.CreateGlobalId(16, 0);
    
    if (characterData != null && (characterData.LockedForChronos || characterData.Disabled || !characterData.IsHero())) 
        g = GlobalId.CreateGlobalId(16, 0);
        
    if (!HomeMode.Avatar.HasHero(g))
    {
        Connection.Send(new OutOfSyncMessage());
        return;
    }
    
    player.ca_thestealdev = g;
    
    if (message.PickType == 1)
    {
        match.SendHeroDataUpdated(player);
    }
    else if (message.PickType == 0)
    {
        player.ps_thestlealdeveEVEVE = 0;
        player.pie_thestealdev = true;
        match.SendHeroPicked(player);
        match.NextTurn();
    }
}
        private void RankedMatchBanHero(RankedMatchBanHeroMessage message)
{
    RankedMatch match = RankedMatchRegulator.Get(HomeMode.Avatar.RanledId);
    if (match == null) return;
    
    mp_thestealdev player = match.GetPlayer(HomeMode.Avatar.AccountId);
    if (player == null) return;
    
    int g = GlobalId.CreateGlobalId(16, message.BrawlerId);
    var characterData = DataTables.Get(16).GetDataByGlobalId<CharacterData>(g);
    if (characterData == null) g = GlobalId.CreateGlobalId(16, 0);
    
    if (characterData != null && (characterData.LockedForChronos || characterData.Disabled || !characterData.IsHero())) 
        g = GlobalId.CreateGlobalId(16, 0);
        
    player.ca_thestealdev = g;
    
    if (message.PickType == 1)
    {
        match.SendHeroDataUpdated(player);
    }
    else if (message.PickType == 0)
    {
        player.banne_thestealdev = GlobalId.CreateGlobalId(16, message.BrawlerId);
        player.bs_stealdev = 0;
        player.hasb_thestealdev = true;
        match.Send(new RankedMatchBanHeroResponseMessage { BannedCharacter = GlobalId.CreateGlobalId(16, message.BrawlerId) });
    }
}
private void SetSupportedCreator(SetSupportedCreatorMessage message)
{
    string CreatorsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "creators.json");

    try
    {
        string code = message.Creator?.Trim();

        // 🔹 Случай 1: Пустой код — удаляем
        if (string.IsNullOrWhiteSpace(code))
        {
            HomeMode.Avatar.SupportedCreator = null;

            var response = new LogicSetSupportedCreatorCommand { Name = string.Empty };
            Connection.Send(new AvailableServerCommandMessage { Command = response });

            Console.WriteLine($"User {LogicLongCodeGenerator.ToCode(HomeMode.Avatar.AccountId)} cleared creator code.");
            return;
        }

        // 🔹 Проверяем, существует ли файл с кодами
        if (!File.Exists(CreatorsFilePath))
        {
            Console.WriteLine($"Error: creators.json not found at {CreatorsFilePath}");
            Connection.Send(new SetSupportedCreatorResponse());
            return;
        }

        // 🔹 Читаем и парсим JSON
        string json = File.ReadAllText(CreatorsFilePath);
        var creatorData = JsonConvert.DeserializeObject<Dictionary<string, long>>(json);

        if (creatorData == null)
        {
            Console.WriteLine("Error: Failed to parse creators.json");
            Connection.Send(new SetSupportedCreatorResponse());
            return;
        }

        // 🔹 Проверяем, есть ли такой код
        if (creatorData.TryGetValue(code, out long creatorAccountId))
        {
            // ✅ Код валиден — сохраняем только имя кода в аватаре
            HomeMode.Avatar.SupportedCreator = code;

            // Отправляем подтверждение клиенту
            var command = new LogicSetSupportedCreatorCommand { Name = code };
            var msg = new AvailableServerCommandMessage { Command = command };
            Connection.Send(msg);

            Console.WriteLine($"User {LogicLongCodeGenerator.ToCode(HomeMode.Avatar.AccountId)} set creator '{code}' (Account ID: {creatorAccountId})");
        }
        else
        {
            // ❌ Код не найден
            Console.WriteLine($"Invalid creator code: '{code}'");
            Connection.Send(new SetSupportedCreatorResponse());
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in SetSupportedCreator: {ex.Message}\n{ex.StackTrace}");
        Connection.Send(new SetSupportedCreatorResponse());
    }
}




        private void StartBrawlTVReceived(StartBrawlTVMessage message)
        {
            HomeMode.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
            {
                Message = "BrawlTV недоступен!\nНо зато есть Telegram-канал:\n@shuzabrawl"
            });
            LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
            logicAddNotificationCommand.Notification = new FloaterTextNotification("BrawlTV недоступен!\nНо зато есть Telegram-канал:\n@shuzabrawl");
            AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
            availableServerCommandMessage.Command = logicAddNotificationCommand;
            HomeMode.GameListener.SendMessage(availableServerCommandMessage);
            logicAddNotificationCommand.Execute(HomeMode);
            AvailableServerCommandMessage server = new AvailableServerCommandMessage();
            server.Command = logicAddNotificationCommand;
            HomeMode.GameListener.SendMessage(server);
        }
        private void DoNotDisturbReceived(DoNotDisturbMessage message)
        {
            LogicSetDoNotDisturb command = new LogicSetDoNotDisturb();
            command.DoNotDistrub = message.state;
            command.Execute(HomeMode);
            AvailableServerCommandMessage serverCommand = new AvailableServerCommandMessage();
            serverCommand.Command = command;
            HomeMode.GameListener.SendMessage(serverCommand);
        }
        private void SetCountryReceived(SetCountryMessage message)
        {
            SetCountryResponseMessage response = new SetCountryResponseMessage();
            response.CountryID = message.CountryID;
            HomeMode.Avatar.Region = DataTables.Get(14).GetDataWithId<RegionData>(message.CountryID).Name;
            Console.WriteLine(HomeMode.Avatar.Region);
            Connection.Send(response);
        }
        private void GetBattleLogReceived(GetBattleLogMessage message)
        {
            BattleLogMessage log = new BattleLogMessage();
            log.homeMode = HomeMode;
            Connection.Send(log);
        }
        private void AlliancePremadeChatReceived(AlliancePremadeChatMessage message)
        {
            // skip
        }
        private void TeamSpectateMessageReceived(TeamSpectateMessage message)
        {
            TeamEntry team = Teams.Get(message.TeamId);
            if (team == null) return;
            HomeMode.Avatar.TeamId = team.Id;
            TeamMember member = new TeamMember();
            member.AccountId = HomeMode.Avatar.AccountId;
            member.CharacterId = HomeMode.Home.CharacterId;
            member.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name);
            member.homeMode = HomeMode;
            Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
            member.HeroTrophies = hero.Trophies;
            member.HeroHighestTrophies = hero.HighestTrophies;
            member.HeroLevel = hero.PowerLevel;
            member.IsOwner = false;
            member.State = 0;
            team.Members.Add(member);
            team.TeamUpdated();
        }

        private void TeamPremadeChatReceived(TeamPremadeChatMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            QuickChatStreamEntry entry = new QuickChatStreamEntry();
            entry.AccountId = HomeMode.Avatar.AccountId;
            entry.TargetId = message.TargetId;
            entry.Name = HomeMode.Avatar.Name;

            if (message.TargetId > 0)
            {
                TeamMember member = team.GetMember(message.TargetId);
                if (member != null)
                {
                    entry.TargetPlayerName = member.DisplayData.Name;
                }
            }

            entry.MessageDataId = message.MessageDataId;
            entry.Unknown1 = message.Unknown1;
            entry.Unknown2 = message.Unknown2;

            team.AddStreamEntry(entry);
        }

        private void TeamBotSlotDisableReceived(TeamBotSlotDisableMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            if (message.Disable)
            {
                team.DisabledBots.Add(message.BotSlot);
            }
            else team.DisabledBots.Remove(message.BotSlot);
            team.TeamUpdated();
        }

        private void TeamToggleMemberSideReceived(TeamToggleMemberSideMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;
            team.GetMember(message.Longs[0]).TeamIndex = message.Unk1 >= 3 ? 1 : 0;
            team.TeamUpdated();
        }

        private void TeamChatReceived(TeamChatMessage message)
        {
            if (message.Message.StartsWith("/"))
            {
                  // Передаём команду в общий обработчик
                  ChatCommandSystem.HandleCommand(message.Message, HomeMode.Avatar.AccountId);
            }
            else Sessions.SendGlobalMessage(HomeMode.Avatar.AccountId, HomeMode.Avatar.Name, message.Message);
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            ChatStreamEntry entry = new ChatStreamEntry();
            entry.AccountId = HomeMode.Avatar.AccountId;
            entry.Name = HomeMode.Avatar.Name;
            entry.Message = message.Message;

            team.AddStreamEntry(entry);
            //AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
            //loginFailed.ErrorCode = 11;
            //loginFailed.Message = message.Message;
            //Connection.Send(loginFailed);
        }

        private void AvatarNameCheckRequestReceived(AvatarNameCheckRequestMessage message)
        {
            LogicChangeAvatarNameCommand command = new LogicChangeAvatarNameCommand();
            command.Name = message.Name;
            command.ChangeNameCost = 0;
            command.Execute(HomeMode);
            AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
            serverCommandMessage.Command = command;
            Connection.Send(serverCommandMessage);
        }
        private void TeamRequestJoinReceived(TeamRequestJoinMessage message)
        {
            TeamEntry team = Teams.Get(message.TeamId);
            if (team == null) return;

            TeamEntry team1 = Teams.Get(HomeMode.Avatar.TeamId);
            if (team1 != null)
            {
                TeamMember entry = team1.GetMember(HomeMode.Avatar.AccountId);

                if (entry == null) return;
                HomeMode.Avatar.TeamId = -1;

                team1.Members.Remove(entry);

                Connection.Send(new TeamLeftMessage());
                team1.TeamUpdated();

                if (team1.Members.Count == 0)
                {
                    Teams.Remove(team1.Id);
                }
            }

            TeamMember member = new TeamMember();
            member.AccountId = HomeMode.Avatar.AccountId;
            member.CharacterId = HomeMode.Home.CharacterId;
            member.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name);
            member.homeMode = HomeMode;
            Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
            member.HeroTrophies = hero.Trophies;
            member.HeroHighestTrophies = hero.HighestTrophies;
            member.HeroLevel = hero.PowerLevel;
            member.IsOwner = false;
            member.State = 0;
            team.Members.Add(member);

            HomeMode.Avatar.TeamId = team.Id;

            team.TeamUpdated();
        }
        private void AnalyticEventsReceived(AnalyticEventMessage message)
        {
            //Debugger.Print(message.str1 +" "+ message.str2);
            ;
        }

        private void TeamSetEventReceived(TeamSetEventMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            EventData data = Events.GetEvent(message.EventSlot);
            if (data == null) return;

            team.EventSlot = message.EventSlot;
            team.LocationId = data.LocationId;
            team.Type = 0;
            HomeMode.Home.EventId = message.EventSlot;
            team.TeamUpdated();
            FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
            entryMessage.AvatarId = HomeMode.Avatar.AccountId;
            entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;
            entryMessage.AllianceTeamEntry = team;

            foreach (Friend friend in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                }
            }

        }
        private void GetPlayerMapsReceived(GetPlayerMapsMessage message)
        {
            Connection.Send(new PlayerMapsMessage()
            {
                maps = Connection.Home.PlayerMaps.ToArray()
            });
        }
        private void CreatePlayerMapReceived(CreatePlayerMapMessage message)
        {
            PlayerMap map = new PlayerMap()
            {
                MapEnvironmentData = message.PMED,
                MapName = message.name,
                GMV = message.GMV,
                MapId = RandomNumberGenerator.GetInt32(0, 2147483647),
                AccountId = Connection.Avatar.AccountId,
                AvatarName = Connection.Avatar.Name
            };
            Connection.Home.PlayerMaps.Add(map);
            Connection.Send(new CreatePlayerMapResponseMessage()
            {
                map = map
            });
        }
        private void UpdatePlayerMapReceived(UpdatePlayerMapMessage message)
        {
            PlayerMap map = Connection.Home.PlayerMaps.Find(map => map.MapId == message.MapId);
            if (map == null)
            {
                Connection.Send(new UpdatePlayerMapResponseMessage()
                {
                    ErrorCode = 1
                });
                return;
            }
            map.MapData = message.MapData;
            Connection.Send(new UpdatePlayerMapResponseMessage()
            {
                MapId = message.MapId
            });
        }
        private void DeletePlayerMapReceived(DeletePlayerMapMessage message)
        {
            PlayerMap map = Connection.Home.PlayerMaps.Find(map => map.MapId == message.MapId);
            if (map == null)
            {
                Connection.Send(new DeletePlayerMapResponseMessage()
                {
                    ErrorCode = 1
                });
                return;
            }
            Connection.Home.PlayerMaps.Remove(map);
            Connection.Send(new DeletePlayerMapResponseMessage()
            {
                MapId = message.MapId
            });
        }
        private void TeamSetPlayerMapReceived(TeamSetPlayerMapMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            PlayerMap map = Connection.Home.PlayerMaps.Find(map => map.MapId == message.mapid);
            if (map == null) return;

            team.BattlePlayerMap = new BattlePlayerMap(map);
            team.Type = 1;
            team.TeamUpdated();

        }
        private BattleMode SpectatedBattle;
        private void StopSpectateReceived(StopSpectateMessage message)
        {
            if (SpectatedBattle != null)
            {
                SpectatedBattle.RemoveSpectator(Connection.UdpSessionId);
                SpectatedBattle = null;
            }

            if (Connection.Home != null && Connection.Avatar != null)
            {
                HomeMode.Home.RefreshCustomOffers();
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
              Connection.Send(ohd);
            }
        }
        
        
        

                private void StartSpectateReceived(StartSpectateMessage message)
{
    Account data = Accounts.Load(message.AccountId);
    if (data == null)
        return;

    ClientAvatar avatar = data.Avatar;
    long battleId = avatar.BattleId;

    BattleMode battle = Battles.Get(battleId);
    if (battle == null)
        return;
    SpectatedBattle = battle;

    // Исправленная строка - используем UDPGateway вместо UPD
    UDPSocket socket = UDPGateway.CreateSocket();
    socket.Battle = battle;
    socket.IsSpectator = true;
    socket.TCPConnection = Connection;
    Connection.UdpSessionId = socket.SessionId;
    battle.AddSpectator(socket.SessionId, new UDPGameListener(socket, Connection));
    StartLoadingMessage startLoading = new StartLoadingMessage();
    startLoading.TeamIndex = 0; 
    startLoading.OwnIndex = 0;
    startLoading.SpectateMode = 1;
    startLoading.LocationId = battle.m_locationId;
    startLoading.GameMode = battle.GetGameModeVariation(); 
    startLoading.GameType = 0;
    startLoading.MapMode = 1;
    startLoading.Modifiers = new List<int>();
    startLoading.LeaveButton = true;
    startLoading.Players.AddRange(battle.GetPlayers());
    if (battle.BattlePlayerMap != null)
    {
        startLoading.map = battle.BattlePlayerMap;
    }

    Connection.Avatar.UdpSessionId = Connection.UdpSessionId;
    Connection.Send(startLoading);

    UdpConnectionInfoMessage info = new UdpConnectionInfoMessage();
    info.SessionId = Connection.UdpSessionId;
    info.ServerAddress = Configuration.Instance.UdpHost;
    info.ServerPort = Configuration.Instance.UdpPort;
    Connection.Send(info);
}



private void SendNotification(string message)
{
    try
    {
        LogicAddNotificationCommand notification = new LogicAddNotificationCommand();
        notification.Notification = new FloaterTextNotification(message);
        
        AvailableServerCommandMessage serverMsg = new AvailableServerCommandMessage();
        serverMsg.Command = notification;
        Connection.Send(serverMsg);
    }
    catch { }
}

        private void GoHomeFromOfflinePractiseReceived(GoHomeFromOfflinePractiseMessage message)
        {
            if (Connection.Home != null && Connection.Avatar != null)
            {
                if (Connection.Avatar.IsTutorialState())
                {
                    Connection.Avatar.SkipTutorial();
                }

                HomeMode.Home.RefreshCustomOffers();
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
              Connection.Send(ohd);
                ////////// SendWelcomeDialog();
            }
        }

        private void TeamSetLocationReceived(TeamSetLocationMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;
            team.LocationId = message.locid;
            team.CustomModifiers = message.CustomModifiers.ToList();
            team.Type = 1;
            team.TeamUpdated();
            FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
            entryMessage.AvatarId = HomeMode.Avatar.AccountId;
            entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;
            entryMessage.AllianceTeamEntry = team;

            foreach (Friend friend in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                }
            }
            //HomeMode.Home.EventId = message.locid;

        }

        private void ChangeAllianceSettingsReceived(ChangeAllianceSettingsMessage message)
        {
            if (HomeMode.Avatar.AllianceId <= 0) return;

            if (HomeMode.Avatar.AllianceRole != AllianceRole.Leader) return;

            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            if (message.BadgeId >= 8000000 && message.BadgeId < 8000000 + DataTables.Get(DataType.AllianceBadge).Count)
            {
                alliance.AllianceBadgeId = message.BadgeId;
            }
            else
            {
                alliance.AllianceBadgeId = 8000000;
            }

            alliance.Description = message.Description;
            alliance.RequiredTrophies = message.RequiredTrophies;

            Connection.Send(new AllianceResponseMessage()
            {
                ResponseType = 10
            });

            MyAllianceMessage myAlliance = new MyAllianceMessage();
            myAlliance.Role = HomeMode.Avatar.AllianceRole;
            myAlliance.OnlineMembers = alliance.OnlinePlayers;
            myAlliance.AllianceHeader = alliance.Header;
            Connection.Send(myAlliance);
        }

        private void KickAllianceMemberReceived(KickAllianceMemberMessage message)
        {
            if (HomeMode.Avatar.AllianceId <= 0) return;

            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            AllianceMember member = alliance.GetMemberById(message.AccountId);
            if (member == null) return;

            ClientAvatar avatar = Accounts.Load(message.AccountId).Avatar;

            if (HomeMode.Avatar.AllianceRole <= avatar.AllianceRole) return;

            alliance.Members.Remove(member);
            avatar.AllianceId = -1;

            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = HomeMode.Avatar.AccountId;
            entry.AuthorName = HomeMode.Avatar.Name;
            entry.Id = ++alliance.Stream.EntryIdCounter;
            entry.PlayerId = avatar.AccountId;
            entry.PlayerName = avatar.Name;
            entry.Type = 4;
            entry.Event = 1; // kicked
            entry.AuthorRole = HomeMode.Avatar.AllianceRole;
            alliance.AddStreamEntry(entry);

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 70;
            Connection.Send(response);

            if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
            {
                LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(new AllianceResponseMessage()
                {
                    ResponseType = 100
                });
                LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(new MyAllianceMessage());
            }
        }

        private void TeamSetMemberReadyReceived(TeamSetMemberReadyMessage message)
        {
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;
            /*
            if (HomeMode.Home.Character.Disabled)
            {
                Connection.Send(new OutOfSyncMessage());
                return;
            }
            */

            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;

            member.IsReady = message.IsReady;

            team.TeamUpdated();

            if (team.IsEveryoneReady())
            {
                Teams.StartGame(team);
            }
        }

        private void TeamChangeMemberSettingsReceived(TeamChangeMemberSettingsMessage message)
        {
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;

            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;

            team.TeamUpdated();
        }

        private void TeamMemberStatusReceived(TeamMemberStatusMessage message)
        {
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;

            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;

            member.State = message.Status;
            team.TeamUpdated();
        }

        private void TeamInvitationResponseReceived(TeamInvitationResponseMessage message)
        {
            bool isAccept = message.Response == 1;

            TeamEntry team = Teams.Get(message.TeamId);
            if (team == null) return;

            TeamInviteEntry invite = team.GetInviteById(HomeMode.Avatar.AccountId);
            if (invite == null) return;

            team.Invites.Remove(invite);

            if (isAccept)
            {
                TeamMember member = new TeamMember();
                member.AccountId = HomeMode.Avatar.AccountId;
                member.CharacterId = HomeMode.Home.CharacterId;
                member.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name);
                member.homeMode = HomeMode;
                Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
                member.HeroTrophies = hero.Trophies;
                member.HeroHighestTrophies = hero.HighestTrophies;
                member.HeroLevel = hero.PowerLevel;
                member.IsOwner = false;
                member.State = 0;
                team.Members.Add(member);

                HomeMode.Avatar.TeamId = team.Id;
            }

            team.TeamUpdated();
        }

        private TeamEntry GetTeam()
        {
            return Teams.Get(HomeMode.Avatar.TeamId);
        }

        private void TeamInviteReceived(TeamInviteMessage message)
        {
            TeamEntry team = GetTeam();
            if (team == null) return;

            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            TeamInviteEntry entry = new TeamInviteEntry();
            entry.Slot = message.Team;
            entry.Name = data.Avatar.Name;
            entry.Id = message.AvatarId;
            entry.InviterId = HomeMode.Avatar.AccountId;

            team.Invites.Add(entry);

            team.TeamUpdated();

            LogicGameListener gameListener = LogicServerListener.Instance.GetGameListener(message.AvatarId);
            if (gameListener != null)
            {
                TeamInvitationMessage teamInvitationMessage = new TeamInvitationMessage();
                teamInvitationMessage.TeamId = team.Id;

                Friend friendEntry = new Friend();
                friendEntry.AccountId = HomeMode.Avatar.AccountId;
                friendEntry.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name);
                friendEntry.Trophies = HomeMode.Avatar.Trophies;
                teamInvitationMessage.Unknown = 1;
                teamInvitationMessage.FriendEntry = friendEntry;

                gameListener.SendTCPMessage(teamInvitationMessage);
            }
        }

        private void TeamLeaveReceived(TeamLeaveMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);

            if (team == null)
            {
                Logger.Print("TeamLeave - Team is NULL!");
                HomeMode.Avatar.TeamId = -1;
                Connection.Send(new TeamLeftMessage());
                return;
            }

            TeamMember entry = team.GetMember(HomeMode.Avatar.AccountId);

            if (entry == null) return;
            HomeMode.Avatar.TeamId = -1;

            team.Members.Remove(entry);

            Connection.Send(new TeamLeftMessage());
            team.TeamUpdated();

            if (team.Members.Count == 0)
            {
                Teams.Remove(team.Id);
            }
            FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
            entryMessage.AvatarId = HomeMode.Avatar.AccountId;
            entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;

            foreach (Friend friend in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                }
            }
            //TeamCreateReceived(new TeamCreateMessage());
        }

        private void TeamCreateReceived(TeamCreateMessage message)
        {
            //return;
            TeamEntry team = Teams.Create();

            team.Type = message.TeamType;
            team.LocationId = Events.GetEvents()[0].LocationId;
            team.EventSlot = 1;

            TeamMember member = new TeamMember();
            member.AccountId = HomeMode.Avatar.AccountId;
            member.CharacterId = HomeMode.Home.CharacterId;
            member.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name);
            member.homeMode = HomeMode;
            Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
            member.SkinId = GlobalId.CreateGlobalId(29, hero.SelectedSkinId);
            member.HeroTrophies = hero.Trophies;
            member.HeroHighestTrophies = hero.HighestTrophies;
            member.HeroLevel = hero.PowerLevel;
            member.IsOwner = true;
            member.State = 0;
            team.Members.Add(member);

            TeamMessage teamMessage = new TeamMessage();
            teamMessage.Team = team;
            HomeMode.Avatar.TeamId = team.Id;
            Connection.Send(teamMessage);
            FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
            entryMessage.AvatarId = HomeMode.Avatar.AccountId;
            entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;
            entryMessage.AllianceTeamEntry = team;

            foreach (Friend friend in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                }
            }
        }

        private void AcceptFriendReceived(AcceptFriendMessage message)
        {
            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            {
                Friend entry = HomeMode.Avatar.GetRequestFriendById(message.AvatarId);
                if (entry == null) return;

                Friend oldFriend = HomeMode.Avatar.GetAcceptedFriendById(message.AvatarId);
                if (oldFriend != null)
                {
                    HomeMode.Avatar.Friends.Remove(entry);
                    Connection.Send(new OutOfSyncMessage());
                    return;
                }

                entry.FriendReason = 0;
                entry.FriendState = 4;

                FriendListUpdateMessage update = new FriendListUpdateMessage();
                update.Entry = entry;
                Connection.Send(update);
            }

            {
                ClientAvatar avatar = data.Avatar;
                Friend entry = avatar.GetFriendById(HomeMode.Avatar.AccountId);
                if (entry == null) return;

                entry.FriendState = 4;
                entry.FriendReason = 0;

                if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
                {
                    FriendListUpdateMessage update = new FriendListUpdateMessage();
                    update.Entry = entry;
                    LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(update);
                }
            }
        }

        private void RemoveFriendReceived(RemoveFriendMessage message)
        {
            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            ClientAvatar avatar = data.Avatar;

            Friend MyEntry = HomeMode.Avatar.GetFriendById(message.AvatarId);
            if (MyEntry == null) return;

            MyEntry.FriendState = 0;

            HomeMode.Avatar.Friends.Remove(MyEntry);

            FriendListUpdateMessage update = new FriendListUpdateMessage();
            update.Entry = MyEntry;
            Connection.Send(update);

            Friend OtherEntry = avatar.GetFriendById(HomeMode.Avatar.AccountId);

            if (OtherEntry == null) return;

            OtherEntry.FriendState = 0;

            avatar.Friends.Remove(OtherEntry);

            if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
            {
                FriendListUpdateMessage update2 = new FriendListUpdateMessage();
                update2.Entry = OtherEntry;
                LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(update2);
            }
        }

        private void AddFriendReceived(AddFriendMessage message)
        {
            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            ClientAvatar avatar = data.Avatar;

            Friend requestEntry = HomeMode.Avatar.GetFriendById(message.AvatarId);
            if (requestEntry != null)
            {
                AcceptFriendReceived(new AcceptFriendMessage()
                {
                    AvatarId = message.AvatarId
                });
                return;
            }
            else
            {
                Friend friendEntry = new Friend();
                friendEntry.AccountId = HomeMode.Avatar.AccountId;
                friendEntry.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name);
                friendEntry.FriendReason = message.Reason;
                friendEntry.FriendState = 3;
                avatar.Friends.Add(friendEntry);

                Friend request = new Friend();
                request.AccountId = avatar.AccountId;
                request.DisplayData = new PlayerDisplayData(data.Home.ThumbnailId, data.Home.NameColorId, data.Avatar.Name);
                request.FriendReason = 0;
                request.FriendState = 2;
                HomeMode.Avatar.Friends.Add(request);

                if (LogicServerListener.Instance.IsPlayerOnline(message.AvatarId))
                {
                    var gameListener = LogicServerListener.Instance.GetGameListener(message.AvatarId);

                    FriendListUpdateMessage update = new FriendListUpdateMessage();
                    update.Entry = friendEntry;

                    gameListener.SendTCPMessage(update);

                    FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
                    entryMessage.AvatarId = HomeMode.Avatar.AccountId;
                    entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;

                    gameListener.SendTCPMessage(entryMessage);
                }

                FriendListUpdateMessage update2 = new FriendListUpdateMessage();
                update2.Entry = request;
                Connection.Send(update2);
                FriendOnlineStatusEntryMessage entryMessage2 = new FriendOnlineStatusEntryMessage();
                entryMessage2.AvatarId = avatar.AccountId;
                entryMessage2.PlayerStatus = avatar.PlayerStatus;
                Connection.Send(entryMessage2);
            }
        }

        private void AskForFriendListReceived(AskForFriendListMessage message)
        {
            FriendListMessage friendList = new FriendListMessage();
            friendList.Friends = HomeMode.Avatar.Friends.ToArray();
            Connection.Send(friendList);
        }

        private void PlayerStatusReceived(PlayerStatusMessage message)
        {
            if (HomeMode == null) return;

            HomeMode.Avatar.PlayerStatus = message.Status;

            FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
            entryMessage.AvatarId = HomeMode.Avatar.AccountId;
            entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;

            foreach (Friend friend in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                }
            }

            if (HomeMode.Avatar.AllianceRole != AllianceRole.None && HomeMode.Avatar.AllianceId > 0)
            {
                Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);

                if (alliance != null)
                {
                    AllianceOnlineStatusUpdatedMessage allianceOnlineStatusUpdatedMessage = new AllianceOnlineStatusUpdatedMessage()
                    {
                        AvatarId = HomeMode.Avatar.AccountId,
                        Members = alliance.Members.Count,
                        PlayerStatus = HomeMode.Avatar.PlayerStatus
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
        }

        private void SendMyAllianceData(Alliance alliance)
        {
            MyAllianceMessage myAlliance = new MyAllianceMessage();
            myAlliance.Role = HomeMode.Avatar.AllianceRole;
            myAlliance.OnlineMembers = alliance.OnlinePlayers;
            myAlliance.AllianceHeader = alliance.Header;
            Connection.Send(myAlliance);

            AllianceStreamMessage stream = new AllianceStreamMessage();
            stream.Entries = alliance.Stream.GetEntries();
            Connection.Send(stream);
        }

        private int BotIdCounter;


        private string Profanity(string input)
        {
            var profanity = new HashSet<string>(
                File.ReadAllLines("Assets/profanity.txt")
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrEmpty(line)),
                StringComparer.OrdinalIgnoreCase);
            Regex wordRegex = new Regex(@"\b[\p{L}']+\b", RegexOptions.IgnoreCase);
            string result = wordRegex.Replace(input, match =>
            {
                string word = match.Value;
                foreach (string badWord in profanity)
                {
                    if (word.StartsWith(badWord, StringComparison.OrdinalIgnoreCase) &&
                        word.Length <= badWord.Length + 3)
                    {
                        return new string('*', word.Length);
                    }
                }
                return word;
            });

            return result;
        }


        private void ChatToAllianceStreamReceived(ChatToAllianceStreamMessage message)
        {
            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            if (message.Message.StartsWith("/"))
            {
                string[] cmd = message.Message.Substring(1).Split(' ');
                if (cmd.Length == 0) return;

                AllianceStreamEntryMessage response = new AllianceStreamEntryMessage();
                response.Entry = new AllianceStreamEntry();
                response.Entry.AuthorName = "ShuzaBrawl";
                response.Entry.AuthorId = 1;
                response.Entry.Id = alliance.Stream.EntryIdCounter + 667 + BotIdCounter++;
                response.Entry.AuthorRole = AllianceRole.Member;
                response.Entry.Type = 2;

                long accountId = HomeMode.Avatar.AccountId;

                switch (cmd[0])
                {
                                            case "online":
                        response.Entry.Message = $"Онлайн: {Sessions.Count}";
                        Connection.Send(response);
                        break;
                    case "help":
                        response.Entry.Message = $"Доступные команды:\n/online - показать список игроков в сети.\n\nЧтобы привязать аккаунт, воспользуйтесь нашим ботом в Telegram:\n@shuzabrawl_bot";
                        Connection.Send(response);
                        break;
                    default:
                        response.Entry.Message = $"Неизвестная команда '{cmd[0]}'. Для просмотра доступных команд напишите: /help";
                        Connection.Send(response);
                        break;
                }

                return;
            }

            alliance.SendChatMessage(HomeMode.Avatar.AccountId, Profanity(message.Message));
        }

        private void JoinAllianceReceived(JoinAllianceMessage message)
        {
            Alliance alliance = Alliances.Load(message.AllianceId);
            if (HomeMode.Avatar.AllianceId > 0) return;
            if (alliance == null) return;
            if (alliance.Members.Count >= 5000) return;

            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = HomeMode.Avatar.AccountId;
            entry.AuthorName = HomeMode.Avatar.Name;
            entry.Id = ++alliance.Stream.EntryIdCounter;
            entry.PlayerId = HomeMode.Avatar.AccountId;
            entry.PlayerName = HomeMode.Avatar.Name;
            entry.Type = 4;
            entry.Event = 3;
            entry.AuthorRole = HomeMode.Avatar.AllianceRole;
            alliance.AddStreamEntry(entry);

            HomeMode.Avatar.AllianceRole = AllianceRole.Member;
            HomeMode.Avatar.AllianceId = alliance.Id;
            alliance.Members.Add(new AllianceMember(HomeMode.Avatar));

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 40;
            Connection.Send(response);

            SendMyAllianceData(alliance);
        }

        private void LeaveAllianceReceived(LeaveAllianceMessage message)
        {
            if (HomeMode.Avatar.AllianceId < 0 || HomeMode.Avatar.AllianceRole == AllianceRole.None) return;

            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            alliance.RemoveMemberById(HomeMode.Avatar.AccountId);
            HomeMode.Avatar.AllianceId = -1;
            HomeMode.Avatar.AllianceRole = AllianceRole.None;

            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = HomeMode.Avatar.AccountId;
            entry.AuthorName = HomeMode.Avatar.Name;
            entry.Id = ++alliance.Stream.EntryIdCounter;
            entry.PlayerId = HomeMode.Avatar.AccountId;
            entry.PlayerName = HomeMode.Avatar.Name;
            entry.Type = 4;
            entry.Event = 4;
            entry.AuthorRole = HomeMode.Avatar.AllianceRole;
            alliance.AddStreamEntry(entry);

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 80;
            Connection.Send(response);

            MyAllianceMessage myAlliance = new MyAllianceMessage();
            Connection.Send(myAlliance);
        }

        private void GetMapsChampionshipMessageReceived(GetMapsChampionshipMessage message)
        {
            MapsChampionshipMessage mc = new MapsChampionshipMessage();
            mc.Type = message.Type;
            Console.WriteLine(message.Type);
            Connection.Send(mc);
        }
        private void CreateAllianceReceived(CreateAllianceMessage message)
        {
            if (HomeMode.Avatar.AllianceId >= 0) return;

            Alliance alliance = new Alliance();
            alliance.Name = message.Name;
            alliance.Description = message.Description;
            alliance.RequiredTrophies = message.RequiredTrophies;

            if (message.BadgeId >= 8000000 && message.BadgeId < 8000000 + DataTables.Get(DataType.AllianceBadge).Count)
            {
                alliance.AllianceBadgeId = message.BadgeId;
            }
            else
            {
                alliance.AllianceBadgeId = 8000000;
            }

            HomeMode.Avatar.AllianceRole = AllianceRole.Leader;
            alliance.Members.Add(new AllianceMember(HomeMode.Avatar));

            Alliances.Create(alliance);

            HomeMode.Avatar.AllianceId = alliance.Id;

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 20;
            Connection.Send(response);

            SendMyAllianceData(alliance);
        }

        private void AskForAllianceDataReceived(AskForAllianceDataMessage message)
        {
            Alliance alliance = Alliances.Load(message.AllianceId);
            if (alliance == null) return;

            AllianceDataMessage data = new AllianceDataMessage();
            data.Alliance = alliance;
            data.IsMyAlliance = message.AllianceId == HomeMode.Avatar.AllianceId;
            Connection.Send(data);
        }

        private void AskForJoinableAllianceListReceived(AskForJoinableAllianceListMessage message)
        {
    //if (HomeMode.Home.TrophyRoadProgress <= 14)
    //{
    //        HomeMode.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
    //        {
    //            Message = "Античит задетектил csv чит."
    //        });
    //}
    //else
    //{
        // Этот код выполняется, если TrophyRoadProgress < 14
        JoinableAllianceListMessage list = new JoinableAllianceListMessage();
        List<Alliance> alliances = Alliances.GetRandomAlliances(10);
        foreach (Alliance alliance in alliances)
        {
            list.JoinableAlliances.Add(alliance.Header);
        }
        Connection.Send(list);
    //}
        }

        private void ClientCapabilitesReceived(ClientCapabilitiesMessage message)
        {
            Connection.PingUpdated(message.Ping);
        }

private void GetLeaderboardReceived(GetLeaderboardMessage message)
{
    if (message.LeaderboardType == 0)
    {
        Account[] rankingList = Leaderboards.GetBrawlerRankingList(message.HeroDataId);

        LeaderboardMessage leaderboard = new LeaderboardMessage();
        leaderboard.LeaderboardType = 0;
        leaderboard.Region = message.IsRegional ? HomeMode.Avatar.Region : null;
        
        // Собираем уникальные AllianceId для загрузки имен клубов
        var allianceIds = new HashSet<long>();
        
        foreach (Account data in rankingList)
        {
            if (!message.IsRegional) 
            {
                leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                if (data.Avatar.AllianceId > 0)
                    allianceIds.Add(data.Avatar.AllianceId);
            }
            else if (message.IsRegional && HomeMode.Avatar.Region == data.Avatar.Region) 
            {
                leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                if (data.Avatar.AllianceId > 0)
                    allianceIds.Add(data.Avatar.AllianceId);
            }
        }
        
        // Загружаем имена клубов
        foreach (long allianceId in allianceIds)
        {
            var alliance = Alliances.Load(allianceId);
            if (alliance != null)
            {
                leaderboard.AllianceNames[allianceId] = alliance.Name;
            }
        }
        
        leaderboard.OwnAvatarId = Connection.Avatar.AccountId;
        leaderboard.AvatarRegion = HomeMode.Avatar.Region;
        leaderboard.HeroDataId = message.HeroDataId;
        Connection.Send(leaderboard);
    }
    else if (message.LeaderboardType == 1)
    {
        Account[] rankingList = Leaderboards.GetAvatarRankingList();

        LeaderboardMessage leaderboard = new LeaderboardMessage();
        leaderboard.LeaderboardType = 1;
        leaderboard.Region = message.IsRegional ? HomeMode.Avatar.Region : null;
        leaderboard.AvatarRegion = HomeMode.Avatar.Region;
        
        // Собираем уникальные AllianceId для загрузки имен клубов
        var allianceIds = new HashSet<long>();
        
        foreach (Account data in rankingList)
        {
            if (!message.IsRegional) 
            {
                leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                if (data.Avatar.AllianceId > 0)
                    allianceIds.Add(data.Avatar.AllianceId);
            }
            else if (message.IsRegional && HomeMode.Avatar.Region == data.Avatar.Region) 
            {
                leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                if (data.Avatar.AllianceId > 0)
                    allianceIds.Add(data.Avatar.AllianceId);
            }
        }
        
        // Загружаем имена клубов
        foreach (long allianceId in allianceIds)
        {
            var alliance = Alliances.Load(allianceId);
            if (alliance != null)
            {
                leaderboard.AllianceNames[allianceId] = alliance.Name;
            }
        }
        
        leaderboard.OwnAvatarId = Connection.Avatar.AccountId;
        Connection.Send(leaderboard);
    }
    else if (message.LeaderboardType == 2)
    {
        Alliance[] rankingList = Leaderboards.GetAllianceRankingList();

        LeaderboardMessage leaderboard = new LeaderboardMessage();
        leaderboard.LeaderboardType = 2;
        leaderboard.Region = message.IsRegional ? HomeMode.Avatar.Region : null;
        leaderboard.AvatarRegion = HomeMode.Avatar.Region;
        if (!message.IsRegional) leaderboard.AllianceList.AddRange(rankingList);
        
        Connection.Send(leaderboard);
    }
    else if (message.LeaderboardType == 4) // solo
{
    Account[] rankingList = Leaderboards.GetRankedSoloAccountsList();

    // 🟢 ОТЛАДКА
    Console.WriteLine($"[DEBUG] SOLO: Получено {rankingList.Length} аккаунтов из Leaderboards");
    foreach (var acc in rankingList)
    {
        Console.WriteLine($"[DEBUG] SOLO: ID={acc.AccountId}, Rank={acc.Home?.RankedSoloRank}, Progress={acc.Home?.RankedSoloProgress}");
    }

    LeaderboardMessage leaderboard = new LeaderboardMessage();
    leaderboard.LeaderboardType = 4;
    leaderboard.Region = message.IsRegional ? HomeMode.Avatar.Region : null;
    leaderboard.AvatarRegion = HomeMode.Avatar.Region;
    leaderboard.idk = -2;
    
    // 🟢 ОТЛАДКА: сколько добавится в Avatars
    int addedCount = 0;
    
    // Собираем уникальные AllianceId для загрузки имен клубов
    var allianceIds = new HashSet<long>();
    
    foreach (Account data in rankingList)
    {
        if (!message.IsRegional) 
        {
            leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
            addedCount++;
            if (data.Avatar.AllianceId > 0)
                allianceIds.Add(data.Avatar.AllianceId);
        }
        else if (message.IsRegional && HomeMode.Avatar.Region == data.Avatar.Region) 
        {
            leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
            addedCount++;
            if (data.Avatar.AllianceId > 0)
                allianceIds.Add(data.Avatar.AllianceId);
        }
    }
    
    // 🟢 ОТЛАДКА
    Console.WriteLine($"[DEBUG] SOLO: В Avatars добавлено {addedCount} аккаунтов");
    Console.WriteLine($"[DEBUG] SOLO: Avatars.Count = {leaderboard.Avatars.Count}");
    
    // Загружаем имена клубов
    foreach (long allianceId in allianceIds)
    {
        var alliance = Alliances.Load(allianceId);
        if (alliance != null)
        {
            leaderboard.AllianceNames[allianceId] = alliance.Name;
        }
    }
    
    leaderboard.OwnAvatarId = Connection.Avatar.AccountId;
    leaderboard.idk = -2;
    
    // 🟢 ОТЛАДКА перед отправкой
    Console.WriteLine($"[DEBUG] SOLO: Отправка сообщения с {leaderboard.Avatars.Count} аватарами");
    
    Connection.Send(leaderboard);
    
    // 🟢 ОТЛАДКА после отправки
    Console.WriteLine($"[DEBUG] SOLO: Сообщение отправлено");
}
    else if (message.LeaderboardType == 5) // team
    {
        Account[] rankingList = Leaderboards.GetRankedTeamAccountsList();

        LeaderboardMessage leaderboard = new LeaderboardMessage();
        leaderboard.LeaderboardType = 5;
        leaderboard.Region = message.IsRegional ? HomeMode.Avatar.Region : null;
        leaderboard.AvatarRegion = HomeMode.Avatar.Region;
        
        // Собираем уникальные AllianceId для загрузки имен клубов
        var allianceIds = new HashSet<long>();
        
        foreach (Account data in rankingList)
        {
            if (!message.IsRegional) 
            {
                leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                if (data.Avatar.AllianceId > 0)
                    allianceIds.Add(data.Avatar.AllianceId);
            }
            else if (message.IsRegional && HomeMode.Avatar.Region == data.Avatar.Region) 
            {
                leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                if (data.Avatar.AllianceId > 0)
                    allianceIds.Add(data.Avatar.AllianceId);
            }
        }
        
        // Загружаем имена клубов
        foreach (long allianceId in allianceIds)
        {
            var alliance = Alliances.Load(allianceId);
            if (alliance != null)
            {
                leaderboard.AllianceNames[allianceId] = alliance.Name;
            }
        }
        
        leaderboard.OwnAvatarId = Connection.Avatar.AccountId;
        leaderboard.idk = -2;
        Connection.Send(leaderboard);
    }
}


        private void GoHomeReceived(GoHomeMessage message)
        {
            //return;
            if (Connection.Home != null && Connection.Avatar != null)
            {
                HomeMode.Home.RefreshCustomOffers();
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
              Connection.Send(ohd);
                //if (HomeMode.Avatar.TeamId <= 0)
                //{
                //    TeamEntry team = Teams.Create();

                //    team.Type = 0;
                //    EventData data = Events.GetEvent(HomeMode.Home.EventId);
                //    if (data == null) data = Events.GetEvent(1);
                //    team.LocationId = data.LocationId;

                //    TeamMember member = new TeamMember();
                //    member.AccountId = HomeMode.Avatar.AccountId;
                //    member.CharacterId = HomeMode.Home.CharacterId;
                //    member.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Avatar.Name);
                //    member.homeMode = HomeMode;
                //    Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
                //    member.HeroTrophies = hero.Trophies;
                //    member.HeroHighestTrophies = hero.HighestTrophies;
                //    member.HeroLevel = hero.PowerLevel;
                //    member.IsOwner = true;
                //    member.State = 0;
                //    team.Members.Add(member);

                //    TeamMessage teamMessage = new TeamMessage();
                //    teamMessage.Team = team;
                //    HomeMode.Avatar.TeamId = team.Id;
                //    Connection.Send(teamMessage);
                //}
                ////////// SendWelcomeDialog();
            }
        }
        private void GoHomeFromMapEditorReceived(GoHomeFromMapEditorMessage message)
        {
            if (Connection.Home != null && Connection.Avatar != null)
            {
                HomeMode.Home.RefreshCustomOffers();
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
              Connection.Send(ohd);
                ////////// SendWelcomeDialog();
            }
        }

        private void ClientInfoReceived(ClientInfoMessage message)
        {
            UdpConnectionInfoMessage info = new UdpConnectionInfoMessage();
            info.SessionId = Connection.UdpSessionId;
            info.ServerAddress = Configuration.Instance.UdpHost;
            info.ServerPort = Configuration.Instance.UdpPort;
            Connection.Send(info);
        }

        private void CancelMatchMaking(CancelMatchmakingMessage message)
        {
            Matchmaking.CancelMatchmake(Connection);
            Connection.Send(new MatchMakingCancelledMessage());
        }

        private void MatchmakeRequestReceived(MatchmakeRequestMessage message)
        {
            int slot = message.EventSlot;

            //if (HomeMode.Home.Character.Disabled)
            //{
            //    Connection.Send(new OutOfSyncMessage());
            //    return;
            //}

            if (!Events.HasSlot(slot))
            {
                slot = 1;
            }

            Matchmaking.RequestMatchmake(Connection, slot);
        }

        private void SinglePlayerMatchRequestReceived(SinglePlayerMatchRequestMessage message)
        {
            BattleMode battle = new BattleMode(15000121);
            battle.Id = Battles.Add(battle);

            BattlePlayer player = BattlePlayer.Create(Connection.Home, Connection.Avatar, 0, 0);
            player.CharacterIds[0] = message.CharacterId;
            player.SkinIds[0] = message.SkinId;
            player.CharacterDatas[0] = (DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(message.CharacterId));
            player.HeroIndexMax = 0;
            player.Gear1 = null;
            player.Gear2 = null;
            player.Accessory = null;
            Hero hero = Connection.Avatar.GetHero(message.CharacterId);

            if (hero != null)
            {
                player.Trophies = hero.Trophies;
                player.HighestTrophies = hero.HighestTrophies;
            }
            else
            {
                player.Trophies = 0;
                player.HighestTrophies = 0;
            }
            player.HeroPowerLevel = 11;
            if (hero != null)
            {
                player.Emotes.Remove(1);
                player.Emotes.Add(1, hero.emote[1]);
                player.Emotes.Remove(2);
                player.Emotes.Add(2, hero.emote[2]);
                player.Emotes.Remove(3);
                player.Emotes.Add(3, hero.emote[3]);
            }
            UDPSocket socket = UDPGateway.CreateSocket();
            socket.TCPConnection = Connection;
            socket.Battle = battle;
            Connection.UdpSessionId = socket.SessionId;
            battle.AddPlayer(player, Connection.UdpSessionId);


            if (battle.m_players[0].IsAdmin) for (int i = 0; i < 120; i++) battle.m_time.IncreaseTick();
            battle.AddGameObjects();

            StartLoadingMessage startLoading = new StartLoadingMessage();
            startLoading.LocationId = battle.Location.GetGlobalId();
            startLoading.TeamIndex = player.TeamIndex;
            startLoading.OwnIndex = player.PlayerIndex;
            startLoading.GameMode = battle.GetGameModeVariation();
            startLoading.GameType = 8;

            Connection.Avatar.UdpSessionId = Connection.UdpSessionId;
            startLoading.Players.AddRange(battle.GetPlayers());
            Connection.Send(startLoading);
            battle.Dummy = startLoading;

            battle.Start();
        }

        private void EndClientTurnReceived(EndClientTurnMessage message)
        {
            HomeMode.ClientTurnReceived(message.Tick, message.Checksum, message.Commands);

            // Клиент подтвердил, что выполнил команду 228 (состояние стардропов): к этому моменту окно открытия
            // у него уже создано, а его собственный запрос 571 идёт в том же ходе следом и теряется. Выдаём награду сейчас.
            if (message.UnhandledType == 228) HomeMode.Home.OnStarrStateAcknowledged(message.CommandsAfterUnhandled > 0);
            switch (message.type) {
                case 506:
                   if (HomeMode.Avatar.TeamId > 0)
                   {
                    TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
                    if (team == null)
                    {
                        return;
                    }
                    TeamMember m = team.GetMember(HomeMode.Avatar.AccountId);
                    if (m == null)
                    {
                        return;
                    }
                    Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterIds[0]);
                    m.CharacterId = HomeMode.Home.CharacterIds[0];
                    m.SkinId = GlobalId.CreateGlobalId(29, hero.SelectedSkinId);
                    team.TeamUpdated();
                   }
                   break;
            }
        }

        private void GetPlayerProfile(GetPlayerProfileMessage message)
        {
            Account data = Accounts.Load(message.AccountId);
            if (data == null) return;

            Profile profile = Profile.Create(data.Home, data.Avatar);

            PlayerProfileMessage profileMessage = new PlayerProfileMessage();
            profileMessage.Profile = profile;
            if (data.Avatar.AllianceId >= 0)
            {
                Alliance alliance = Alliances.Load(data.Avatar.AllianceId);
                if (alliance != null)
                {
                    profileMessage.AllianceHeader = alliance.Header;
                    profileMessage.AllianceRole = data.Avatar.AllianceRole;
                }
            }
            Connection.Send(profileMessage);
        }

        private void ChangeName(ChangeAvatarNameMessage message)
        {
            LogicChangeAvatarNameCommand command = new LogicChangeAvatarNameCommand();
            command.Name = message.Name;
            command.ChangeNameCost = 0;
            command.Execute(HomeMode);
            AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
            serverCommandMessage.Command = command;
            Connection.Send(serverCommandMessage);
        }

        private void OnChangeCharacter(int characterId)
        {
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;

            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;
            if (characterId == 0) goto LABEL_1;

            Hero hero = HomeMode.Avatar.GetHero(characterId);
            if (hero == null) return;
            member.CharacterId = characterId;
            member.SkinId = GlobalId.CreateGlobalId(29, hero.SelectedSkinId);
            member.HeroTrophies = hero.Trophies;
            member.HeroHighestTrophies = hero.HighestTrophies;
            member.HeroLevel = hero.PowerLevel;

        LABEL_1:
            team.TeamUpdated();
        }
        private void LoginReceived(AuthenticationMessage message)
        {
            if (message.Version != "53.007")
            {
                Connection.Send(new AuthenticationFailedMessage()
                {
                    ErrorCode = 8,
                    UpdateUrl = "https://t.me/shuzabrawl"
                });
                return;
            }
            if (message.ClientMajor < 52)
            {
                Connection.Send(new AuthenticationFailedMessage()
                {
                    ErrorCode = 8,
                    UpdateUrl = "https://t.me/shuzabrawl"
                });
                return;
            }
            if (Sessions.Maintenance)
            {
                Connection.Send(new AuthenticationFailedMessage()
                {
                    ErrorCode = 10,
                    dateTime = DateTime.Parse("2023-02-14 13:30:00")
                });
                return;
            }

            Account account = null;
            if (message.AccountId == 0)
            {
                // 🔹 Создание нового аккаунта
                account = Accounts.Create();
                AccountCache.SaveAll();
            }
            else
            {
                // 🔹 Загрузка существующего аккаунта
                account = Accounts.Load(message.AccountId);
            
                if (account == null)
                {
                    Debugger.Print($"[Login] Аккаунт не найден: {message.AccountId}");
                    // Можно отправить ошибку клиенту
                    return;
                }
            
                // 🔥 ПРОВЕРКА PassToken
                if (account.PassToken != message.PassToken)
                {
                    Debugger.Print($"[Login] Ошибка токена: AccountId={message.AccountId}, Ожидался='{account.PassToken}', Получен='{message.PassToken}'");
                    
                    // ❌ Отклоняем вход
                    // Можно отправить клиенту сообщение об ошибке
                    AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                    loginFailed.ErrorCode = 22;
                    loginFailed.Message = "Wrong token, that fixed)";
                    Connection.Send(loginFailed);
                    return;
                }
            }
            int penis = 124;
            Logger.Print($"Login sha: {message.ResourceSha}");
            if (message.ResourceSha != Fingerprint.Sha && penis == 0)
            {
                Logger.Warning($"Login sha: {message.ResourceSha} Server Sha: {Fingerprint.Sha}");
                {
                    AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                    loginFailed.ErrorCode = 7;
                    loginFailed.FingerprintSha = Fingerprint.Sha;
                    loginFailed.ContentUrl = "http://89.35.130.57:8080/";
                    Connection.Send(loginFailed);
                    return;
                }
            }
            if (account == null)
            {
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 22;
                loginFailed.Message = "Account not loaded. Please clear app data and try again.";
                Connection.Send(loginFailed);
                return;
            }

            Debugger.Print(account.Avatar.Name + "(" + account.AccountId + ") login.");

            if (account.Avatar.BanEndTime > DateTime.UtcNow)
            {
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 11;
                if (account.Avatar.TextReason != null)
                {
                    loginFailed.Message = account.Avatar.TextReason;
                }
                else
                {
                    switch(account.Avatar.BanID)
                    {
                        case 0:
                            loginFailed.Message = "Your account banned.";
                            break;
                        case 1:
                            loginFailed.Message = $"Your account has been temporarily banned for violating server rules. You've been banned until : {account.Avatar.BanEndTime}";
                            break;
                        case 2:
                            loginFailed.Message = $"Access to social features is temporarily blocked for your account. Please honor other players. Access will return in: {account.Avatar.BanEndTime}\nRejoin to remove this message.";
                            // потом еще надо эту хуйню на почту отправить да
                            break;
                        case 3:
                            loginFailed.Message = $"Ваш аккаунт был пермоментно заблокирован, за использование модифицированной версии игры. Конец бана: НИКОГДА >:)";
                            break;
                        default:
                            loginFailed.Message = "Your account has been permanently banned for violating server rules. Please honor the work of developers and other players.";
                            break;
                    }
                }
                
                Connection.Send(loginFailed);
                return;
            }
            AuthenticationOkMessage loginOk = new AuthenticationOkMessage();
            loginOk.AccountId = account.AccountId;
            loginOk.PassToken = account.PassToken;
            loginOk.ServerEnvironment = "dev";
            Connection.Send(loginOk);

            HomeMode = HomeMode.LoadHomeState(new HomeGameListener(Connection), account.Home, account.Avatar, Events.GetEvents());
            HomeMode.CharacterChanged += OnChangeCharacter;
            if (HomeMode.Avatar.HighestTrophies == 0 && HomeMode.Avatar.Trophies != 0)
            {
                HomeMode.Avatar.HighestTrophies = HomeMode.Avatar.Trophies;
            }

            FriendListMessage friendList = new FriendListMessage();
            friendList.Friends = HomeMode.Avatar.Friends.ToArray();
            Connection.Send(friendList);
            BattleMode battle = null;

            if (HomeMode.Avatar.BattleId > 0)
            {
                battle = Battles.Get(HomeMode.Avatar.BattleId);

                // Вернуть игрока в незаконченный бой пробуем только один раз. Если после возврата он снова
                // отвалился (бой завис или клиент в нём падает), в следующий вход пускаем в лобби -
                // иначе игрок не может зайти в игру вообще, пока бой не закончится.
                if (battle != null && (battle.IsGameOver || HomeMode.Avatar.LastRejoinBattleId == HomeMode.Avatar.BattleId))
                {
                    Logger.Print($"[Login] Аккаунт {HomeMode.Avatar.AccountId}: повторный возврат в бой {HomeMode.Avatar.BattleId} отменён, вход в лобби");
                    battle = null;
                    HomeMode.Avatar.BattleId = -1;
                }
                else if (battle != null)
                {
                    HomeMode.Avatar.LastRejoinBattleId = HomeMode.Avatar.BattleId;
                }
            }

            int vipWeeklyGems = HomeMode.Avatar.CollectVipWeeklyGems();
            if (vipWeeklyGems > 0)
            {
                Logger.Print($"[VIP] Еженедельные гемы: +{vipWeeklyGems} аккаунту {HomeMode.Avatar.AccountId}");
            }

            if (battle == null)
            {
                HomeMode.Home.RefreshCustomOffers();
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = HomeMode.Home;
                ohd.Avatar = HomeMode.Avatar;
              Connection.Send(ohd);
                ////////// SendWelcomeDialog();
            }
            else
            {
                StartLoadingMessage startLoading = battle.Dummy;
                startLoading.TeamIndex = HomeMode.Avatar.TeamIndex;
                startLoading.OwnIndex = HomeMode.Avatar.OwnIndex;
                UDPSocket socket = UDPGateway.CreateSocket();
socket.TCPConnection = Connection;
                socket.Battle = battle;
                Connection.UdpSessionId = socket.SessionId;
                battle.ChangePlayerSessionId(HomeMode.Avatar.UdpSessionId, socket.SessionId);
                HomeMode.Avatar.UdpSessionId = socket.SessionId;
                Connection.Send(startLoading);
            }

            Connection.Avatar.LastOnline = DateTime.UtcNow;

            Sessions.Create(HomeMode, Connection);
            if (false)
            {
                TeamInvitationMessage teamInvitationMessage = new TeamInvitationMessage();
                teamInvitationMessage.TeamId = -1;

                Friend friendEntry = new Friend();
                friendEntry.AccountId = new LogicLong(-1, -1);
                friendEntry.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name);
                friendEntry.Trophies = HomeMode.Avatar.Trophies;
                teamInvitationMessage.Unknown = 1;
                teamInvitationMessage.FriendEntry = friendEntry;

                Connection.Send(teamInvitationMessage);
            }
            if (HomeMode.Avatar.AllianceRole != AllianceRole.None && HomeMode.Avatar.AllianceId > 0)
            {
                Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);

                if (alliance != null)
                {
                    SendMyAllianceData(alliance);
                    AllianceDataMessage data = new AllianceDataMessage();
                    data.Alliance = alliance;
                    data.IsMyAlliance = true;
                    Connection.Send(data);

                    foreach (var member in alliance.Members)
                    {
                        if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                        {
                            Connection.Send(new AllianceOnlineStatusUpdatedMessage() { AvatarId = member.AccountId, Members = alliance.Members.Count, PlayerStatus = member.Avatar.PlayerStatus });
                        }

                    }
                }
            }

            foreach (Friend entry in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(entry.AccountId))
                {
                    FriendOnlineStatusEntryMessage statusEntryMessage = new FriendOnlineStatusEntryMessage();
                    statusEntryMessage.AvatarId = entry.AccountId;
                    statusEntryMessage.PlayerStatus = entry.Avatar.PlayerStatus;
                    statusEntryMessage.AllianceTeamEntry = Teams.Get(entry.Avatar.TeamId);
                    Connection.Send(statusEntryMessage);
                }
            }
            if (HomeMode.Avatar.TeamId <= 0)
            {
                //TeamCreateReceived(new TeamCreateMessage());
            }

            if (HomeMode.Avatar.TeamId > 0)
            {
                TeamMessage teamMessage = new TeamMessage();
                teamMessage.Team = Teams.Get(HomeMode.Avatar.TeamId);
                if (teamMessage.Team != null)
                {
                    Connection.Send(teamMessage);
                    TeamMember member = teamMessage.Team.GetMember(HomeMode.Avatar.AccountId);
                    member.State = 0;
                    teamMessage.Team.TeamUpdated();
                }
            }
            //LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
            //string name = GamePlayUtil.GetHeroName(DataTables.Get(16).GetDataByGlobalId<CharacterData>(HomeMode.Home.CharacterId).Name);
            //if (HomeMode.Home.FavouriteCharacter != 16000000 && RandomNumberGenerator.GetInt32(2) == 1) name = GamePlayUtil.GetHeroName(DataTables.Get(16).GetDataByGlobalId<CharacterData>(HomeMode.Home.FavouriteCharacter).Name);
            //if (name != null)
            //{
            //    logicAddNotificationCommand.Notification = new FloaterTextNotification(name + ": 你好。");

            //    AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
            //    availableServerCommandMessage.Command = logicAddNotificationCommand;
            //    Connection.Send(availableServerCommandMessage);
            //}
        }

        private void ClientHelloReceived(ClientHelloMessage message)
        {
            Connection.Messaging.DisableCrypto = false;
            if (Sessions.Maintenance)
            {
                Connection.Send(new AuthenticationFailedMessage()
                {
                    ErrorCode = 10
                });
                return;
            }
            if (message.MajorVersion < 53)
            {
                Connection.Send(new AuthenticationFailedMessage()
                {
                    ErrorCode = 8,
                    UpdateUrl = "https://t.me/shuzabrawl"
                });
                return;
            }
            //Connection.Send(new AuthenticationFailedMessage()
            //{
            //    ErrorCode = 8,
            //    UpdateUrl = "https://t.me/shuzabrawl"
            //});
            //return;
            Connection.Messaging.Seed = message.ClientSeed;
            Connection.Nonce = Helpers.RandomString(32);
            ServerHelloMessage hello = new ServerHelloMessage();
            hello.SetServerHelloToken(Connection.Messaging.SessionToken);
            hello.Nonce = Connection.Nonce;
            Connection.Send(hello);
        }
    }
}
