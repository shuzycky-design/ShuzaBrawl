using Masuda.Net;
using IndusBrawl.Laser.Logic.Battle;
using IndusBrawl.Laser.Logic.Battle.Structures;
using IndusBrawl.Laser.Logic.Command.Home;
using IndusBrawl.Laser.Logic.Data;
using IndusBrawl.Laser.Logic.Home.Items;
using IndusBrawl.Laser.Logic.Listener;
using IndusBrawl.Laser.Logic.Message.Battle;
using IndusBrawl.Laser.Logic.Message.Home;
using IndusBrawl.Laser.Logic.Notification;
using IndusBrawl.Laser.Logic.Ranked;
using IndusBrawl.Laser.Logic.Team;
using IndusBrawl.Laser.Logic.Util;
using IndusBrawl.Laser.Server.Networking;
using IndusBrawl.Laser.Server.Networking.Session;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
// УДАЛЕНО: using RankedMatch = IndusBrawl.Laser.Logic.Ranked.RankedMatch;
// УДАЛЕНО: using RankedMatchPlayer = IndusBrawl.Laser.Logic.Ranked.RankedMatchPlayer;

namespace IndusBrawl.Laser.Server.Logic.Game
{
    public static class Matchmaking
    {
        private static Dictionary<int, MatchmakingSlot> Slots;
        private static Thread UpdateThread;

        public static void Init()
        {
            Slots = new Dictionary<int, MatchmakingSlot>();

            foreach (EventData e in Events.GetEvents())
            {
                if (e.Location == null) continue;
                int mode = GameModeUtil.GetGameModeVariation(e.Location.GameModeVariation);
                
                // Исключаем нежелательные режимы для силовой лиги (слоты 14 и 15)
                if ((e.Slot == 14 || e.Slot == 15) && IsBannedRankedMode(mode))
                {
                    Console.WriteLine($"[Matchmaking] Skipping banned ranked mode {mode} for slot {e.Slot}");
                    continue;
                }
                
                MatchmakingSlot slot = new(e, GamePlayUtil.GetPlayerCountWithGameModeVariation(mode));
                slot.Slot = e.Slot;
                Slots.Add(e.Slot, slot);
            }

            Console.WriteLine($"[Matchmaking] Init: slots=[{string.Join(",", Slots.Keys.OrderBy(x => x))}]");
            Console.WriteLine($"[Matchmaking] Init: hasRanked14={Slots.ContainsKey(14)} hasRanked15={Slots.ContainsKey(15)}");

            UpdateThread = new Thread(Update);
            UpdateThread.Start();
        }

        // Проверка на запрещенные режимы для силовой лиги
        public static bool IsBannedRankedMode(int gameModeVariation)
        {
            // Исключаем: HotZone (17), BrawlBall (5), Knockout (20)
            return gameModeVariation == 0 || gameModeVariation == 0 || gameModeVariation == 0;
        }

        private static void Update()
        {
            while (true)
            {
                foreach (MatchmakingSlot slot in Slots.Values)
                {
                    slot.Update();
                }
                Thread.Sleep(250);
            }
        }

        public static void RequestMatchmake(Connection connection, int slot, long team = -1)
        {
            if (!Slots.ContainsKey(slot)) return;

            // Получаем EventData для текущего слота
            var eventData = Events.GetEvent(slot);
            int gameModeVariation = 0;
            if (eventData != null && eventData.Location != null)
                gameModeVariation = GameModeUtil.GetGameModeVariation(eventData.Location.GameModeVariation);

            // Если Solo Showdown и игрок в команде — разделяем команду на соло-игроков
            if (gameModeVariation == 6 && team > 0)
            {
                TeamEntry teamEntry = Teams.Get(team);
                if (teamEntry != null)
                {
                    long groupId = DateTime.UtcNow.Ticks + teamEntry.Id;
                    foreach (var member in teamEntry.Members)
                    {
                        Session session = Sessions.GetSession(member.AccountId);
                        if (session != null && session.Connection != null)
                        {
                            session.Connection.MatchmakeSlot = slot;
                            MatchmakingEntry soloEntry = new MatchmakingEntry(session.Connection);
                            soloEntry.PlayerTeamId = 0; // Соло!
                            soloEntry.GroupId = groupId;
                            session.Connection.MatchmakingEntry = soloEntry;
                            Slots[slot].Add(soloEntry);
                        }
                    }
                }
                return;
            }

            // Обычная логика для других режимов
            connection.MatchmakeSlot = slot;
            MatchmakingEntry entry = new MatchmakingEntry(connection);
            entry.PlayerTeamId = team;
            connection.MatchmakingEntry = entry;
            Slots[slot].Add(entry);
        }
        
        public static void RequestRankedMatchmake(Connection connection, bool solo, int prefferedTeam = -1)
        {
            connection.MatchmakeSlot = solo ? 14 : 15;
            MatchmakingEntry entry = new MatchmakingEntry(connection);
            if (prefferedTeam != -1) entry.PrefferedTeam = prefferedTeam;
            connection.MatchmakingEntry = entry;
            Slots[solo ? 14 : 15].Add(entry);
        }
        
        public static void CancelMatchmake(Connection connection)
        {
            if (connection == null)
                return;

            int slot = connection.MatchmakeSlot;
            if (Slots != null && Slots.ContainsKey(slot))
            {
                connection.MatchmakeSlot = -1;
                if (connection.MatchmakingEntry != null)
                    Slots[slot].Remove(connection.MatchmakingEntry);

                if (connection.MatchmakingEntry != null && connection.MatchmakingEntry.PlayerTeamId > 0)
                {
                    TeamEntry team = Teams.Get(connection.MatchmakingEntry.PlayerTeamId);
                    if (team != null)
                    {
                        foreach (TeamMember member in team.Members)
                        {
                            member.IsReady = false;
                            Session session = Sessions.GetSession(member.AccountId);
                            if (session != null && session.Connection != null && session.Connection.MatchmakingEntry != null)
                            {
                                session.Connection.MatchmakingEntry.PlayerTeamId = -1;
                                CancelMatchmake(session.Connection);
                                MatchMakingCancelledMessage cancelled = new MatchMakingCancelledMessage();
                                session.Connection.Send(cancelled);
                            }
                        }
                        team.TeamUpdated();
                    }
                }
            }
        }
    }

    public class MatchmakingSlot
    {
        private Queue<MatchmakingEntry> RequestQueue;
        private Queue<MatchmakingEntry> RemoveQueue;

        private List<MatchmakingEntry> Queue;

        private int PlayersRequired;
        private EventData EventData;

        public int SecondsLeft;
        private int Turns;

        public const int SEARCH_TIMEOUT = 7;
        public const int RANKED_SEARCH_TIMEOUT = 30;
        public int Slot { get; set; }
        public bool IsRanked => Slot == 14 || Slot == 15;

        public MatchmakingSlot(EventData eventData, int playersRequired)
        {
            PlayersRequired = playersRequired;
            if (GameModeUtil.GetGameModeVariation(eventData.Location.GameModeVariation) == 6)
            {
                PlayersRequired = 10;
            }
            EventData = eventData;
            Queue = new List<MatchmakingEntry>();

            RequestQueue = new Queue<MatchmakingEntry>();
            RemoveQueue = new Queue<MatchmakingEntry>();

            SecondsLeft = IsRanked ? RANKED_SEARCH_TIMEOUT : SEARCH_TIMEOUT;
        }

        public void Update()
        {
            if (IsRanked)
            {
                int gameModeVariation = GameModeUtil.GetGameModeVariation(EventData.Location.GameModeVariation);
                
                // Пропускаем запрещенные режимы в силовой лиге
                if (Matchmaking.IsBannedRankedMode(gameModeVariation))
                {
                    Console.WriteLine($"[Matchmaking] Skipping banned ranked mode {gameModeVariation} in update");
                    return;
                }
                
                PlayersRequired = GamePlayUtil.GetPlayerCountWithGameModeVariation(gameModeVariation);
            }
            else
            {
                int gameModeVariation = GameModeUtil.GetGameModeVariation(EventData.Location.GameModeVariation);
                PlayersRequired = gameModeVariation == 6 ? 10 : 6;
            }

            if (Sessions.Maintenance) return;

            // удаляем отключённых игроков
            foreach (MatchmakingEntry entry in Queue)
            {
                if (!entry.Connection.IsOpen) Remove(entry);
            }

            if (EventData.Slot == 5)
            {
                LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                logicAddNotificationCommand.Notification = new FloaterTextNotification("Sorry, this gamemode unavailable\nTry another gamemode");
                AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                availableServerCommandMessage.Command = logicAddNotificationCommand;
                foreach (MatchmakingEntry member in Queue)
                {
                    member.Connection.Home.HomeMode.GameListener.SendMessage(availableServerCommandMessage);
                    member.Connection.Home.HomeMode.GameListener.SendMessage(new MatchMakingCancelledMessage());
                    Remove(member);
                }
            }

            while (RemoveQueue.Count > 0)
            {
                Queue.Remove(RemoveQueue.Dequeue());
            }

            while (RequestQueue.Count > 0)
            {
                MatchmakingEntry entry = RequestQueue.Dequeue();
                MatchmakingEntry check = Queue.Find(x => x.Connection == entry.Connection);
                if (check != null) continue;

                Queue.Add(entry);
            }

            // достаточно игроков для старта
            if (Queue.Count >= PlayersRequired)
            {
                Console.WriteLine($"[Matchmaking] Queue has {Queue.Count} players, required {PlayersRequired} for slot {EventData.Slot} (IsRanked={IsRanked})");
                List<MatchmakingEntry> playersToStart = new List<MatchmakingEntry>();

                if (GameModeUtil.HasTwoTeams(GameModeUtil.GetGameModeVariation(EventData.Location.GameModeVariation)))
                {
                    Dictionary<long, List<MatchmakingEntry>> teamGroups = new Dictionary<long, List<MatchmakingEntry>>();
                    List<MatchmakingEntry> soloPlayers = new List<MatchmakingEntry>();

                    foreach (MatchmakingEntry entry in Queue)
                    {
                        if (entry.PlayerTeamId > 0)
                        {
                            if (!teamGroups.ContainsKey(entry.PlayerTeamId))
                                teamGroups[entry.PlayerTeamId] = new List<MatchmakingEntry>();
                            teamGroups[entry.PlayerTeamId].Add(entry);
                        }
                        else
                        {
                            soloPlayers.Add(entry);
                        }
                    }

                    int totalTeamPlayers = teamGroups.Values.Sum(team => team.Count);
                    int totalPlayers = totalTeamPlayers + soloPlayers.Count;

                    if (totalPlayers >= 1)
                    {
                        List<MatchmakingEntry> team1 = new List<MatchmakingEntry>();
                        List<MatchmakingEntry> team2 = new List<MatchmakingEntry>();

                        // распределяем группы
                        foreach (var teamGroup in teamGroups.Values)
                        {
                            if (team1.Count + teamGroup.Count <= 3)
                            {
                                foreach (var p in teamGroup) p.PrefferedTeam = 0;
                                team1.AddRange(teamGroup);
                            }
                            else if (team2.Count + teamGroup.Count <= 3)
                            {
                                foreach (var p in teamGroup) p.PrefferedTeam = 1;
                                team2.AddRange(teamGroup);
                            }
                        }

                        // распределяем соло игроков
                        foreach (var soloPlayer in soloPlayers)
                        {
                            if (team1.Count == 0 && team2.Count == 0)
                            {
                                soloPlayer.PrefferedTeam = 0;
                                team1.Add(soloPlayer);
                            }
                            else if (team1.Count == 1 && team2.Count == 0 && soloPlayers.Count == 2)
                            {
                                soloPlayer.PrefferedTeam = 1;
                                team2.Add(soloPlayer);
                            }
                            else if (team1.Count < team2.Count)
                            {
                                soloPlayer.PrefferedTeam = 0;
                                team1.Add(soloPlayer);
                            }
                            else if (team2.Count < team1.Count)
                            {
                                soloPlayer.PrefferedTeam = 1;
                                team2.Add(soloPlayer);
                            }
                            else
                            {
                                if (team1.Count < 3)
                                {
                                    soloPlayer.PrefferedTeam = 0;
                                    team1.Add(soloPlayer);
                                }
                                else if (team2.Count < 3)
                                {
                                    soloPlayer.PrefferedTeam = 1;
                                    team2.Add(soloPlayer);
                                }
                            }
                        }

                        if (team1.Count > 0 && team2.Count > 0)
                        {
                            Console.WriteLine($"[Matchmaking] Teams: Team1={team1.Count}, Team2={team2.Count}");
                            Console.WriteLine($"[Matchmaking] Team1: {string.Join(", ", team1.Select(p => p.Connection?.Avatar?.AccountId ?? 0))}");
                            Console.WriteLine($"[Matchmaking] Team2: {string.Join(", ", team2.Select(p => p.Connection?.Avatar?.AccountId ?? 0))}");

                            playersToStart.AddRange(team1);
                            playersToStart.AddRange(team2);

                            foreach (var player in playersToStart)
                                Queue.Remove(player);

                            if (IsRanked) StartRankedMatch(playersToStart);
                            else StartGame(playersToStart);
                        }
                        else
                        {
                            Console.WriteLine($"[Matchmaking] Failed teams: T1={team1.Count}, T2={team2.Count}");
                        }
                    }
                }
                else
                {
                    if (Queue.Count >= 1)
                    {
                        playersToStart = Queue.Take(Math.Min(PlayersRequired, Queue.Count)).ToList();
                        Queue.RemoveRange(0, playersToStart.Count);

                        if (IsRanked) StartRankedMatch(playersToStart);
                        else StartGame(playersToStart);
                    }
                }
            }

            // таймаут
            if (Queue.Count > 0 && Queue.Count < PlayersRequired && SecondsLeft <= 0)
            {
                SecondsLeft = IsRanked ? RANKED_SEARCH_TIMEOUT : SEARCH_TIMEOUT;
                Console.WriteLine($"[Matchmaking] Timeout: {Queue.Count} players for slot {EventData.Slot}");

                if (Queue.Count >= 1)
                {
                    var toStart = Queue.Take(Queue.Count).ToList();
                    if (IsRanked) StartRankedMatch(toStart);
                    else StartGame(toStart);
                    Queue.RemoveRange(0, toStart.Count);
                }
            }

            // таймер
            if (Queue.Count > 0)
            {
                Turns++;
                if (Turns >= 4)
                {
                    Turns = 0;
                    SecondsLeft--;
                    if (IsRanked && Queue.Count > 0 && Queue.Count < PlayersRequired)
                    {
                        Console.WriteLine($"[Matchmaking] Ranked: {Queue.Count}/{PlayersRequired}, {SecondsLeft}s left");
                    }
                }
            }
            else
            {
                Turns = 0;
                SecondsLeft = IsRanked ? RANKED_SEARCH_TIMEOUT : SEARCH_TIMEOUT;
            }

            // статус поиска
            if (Queue.Count > 0)
            {
                MatchMakingStatusMessage message = new MatchMakingStatusMessage();

                message.Seconds = SecondsLeft;
                message.Found = Queue.Count;
                message.Max = PlayersRequired;
                message.ShowTips = true;

                foreach (MatchmakingEntry entry in Queue)
                {
                    entry.Connection.Messaging.Send(message);
                }
            }
        }

        public void Remove(MatchmakingEntry entry)
        {
            RemoveQueue.Enqueue(entry);
        }

        public void Add(MatchmakingEntry entry)
        {
            RequestQueue.Enqueue(entry);
        }

        public static readonly int[] botBrawlers = { 1, 2, 3, 4, 5, 6, 9, 10 };

        public void StartRankedMatch(List<MatchmakingEntry> entries)
        {
            Console.WriteLine($"[Matchmaking] StartRankedMatch: slot={Slot} entries={entries?.Count ?? 0}");

            if (entries == null || entries.Count < 1)
            {
                Console.WriteLine($"[Matchmaking] Not enough players for ranked: {entries?.Count ?? 0}");
                return;
            }

            // фильтруем невалидных игроков
            List<MatchmakingEntry> validEntries = new List<MatchmakingEntry>();
            foreach (var entry in entries)
            {
                try
                {
                    if (entry?.Connection?.IsOpen == true && entry.Connection.Avatar != null)
                    {
                        validEntries.Add(entry);
                    }
                    else
                    {
                        Console.WriteLine($"[Matchmaking] Removing invalid entry: {entry?.Connection?.Avatar?.AccountId ?? 0}");
                        if (entry != null) Remove(entry);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Matchmaking] Error checking entry: {ex.Message}");
                    if (entry != null) Remove(entry);
                }
            }

            if (validEntries.Count < 1)
            {
                Console.WriteLine($"[Matchmaking] No valid players for ranked");
                return;
            }

            entries = validEntries;

            // ИСПРАВЛЕНО: Используем RankedMatch из IndusBrawl.Laser.Logic.Ranked
            RankedMatch ranked = new RankedMatch(Slot == 14); // solo = true для слота 14
            ranked.r_i_thestealdev = RankedMatchRegulator.Add(ranked);

            // сортируем: сначала командные, потом соло
            List<MatchmakingEntry> sortedEntries = new List<MatchmakingEntry>();
            Dictionary<long, List<MatchmakingEntry>> teamEntries = new Dictionary<long, List<MatchmakingEntry>>();

            MatchmakingEntry[] entriesWithTeam = entries.FindAll(entry => entry.PlayerTeamId > 0).ToArray();
            for (int i = 0; i < entriesWithTeam.Length; i++)
            {
                MatchmakingEntry entry = entriesWithTeam[i];
                long teamId = entry.PlayerTeamId;
                if (!teamEntries.ContainsKey(teamId)) teamEntries.Add(teamId, new List<MatchmakingEntry>());
                teamEntries[teamId].Add(entry);
                entries.Remove(entry);
            }

            long[] ids = teamEntries.Keys.ToArray();

            // В ранкед режиме все игроки должны быть в команде 0 (синие)
            for (int i = 0; i < ids.Length; i++)
            {
                List<MatchmakingEntry> teamedEntries = teamEntries[ids[i]];
                for (int j = 0; j < teamedEntries.Count; j++)
                {
                    teamedEntries[j].PrefferedTeam = 0; // Все в синюю команду
                    sortedEntries.Add(teamedEntries[j]);
                }
            }

            // Соло игроки тоже в синюю команду
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].PrefferedTeam = 0;
                sortedEntries.Add(entries[i]);
            }

            for (int i = 0; i < sortedEntries.Count; i++)
            {
                try
                {
                    int teamIndex = sortedEntries[i].PrefferedTeam;
                    sortedEntries[i].Connection.Avatar.RanledId = (int)ranked.r_i_thestealdev;

                    // ИСПРАВЛЕНО: RankedMatchPlayer → mp_thestealdev
                    var rankedPlayer = mp_thestealdev.CreateRankedPlayer(
                        sortedEntries[i].Connection.Avatar,
                        sortedEntries[i].Connection.Home,
                        teamIndex,
                        i);

                    ranked.AddMatchPlayer(rankedPlayer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Matchmaking] Error adding ranked player: {ex.Message}");
                }
            }

            // ИСПРАВЛЕНО: используем правильные названия свойств
            foreach (var p in ranked.plist_thestealdev)
            {
                Console.WriteLine($"team={p.titi_thestealdev} global={p.piIternnal_thestealdev}");
            }

            ranked.s_thestealdev = Slot == 14; // solo = true для слота 14
            Console.WriteLine($"[Matchmaking] RankedMatch created: id={ranked.r_i_thestealdev} solo={ranked.s_thestealdev} teamCount={sortedEntries.Count}");

            try
            {
                ranked.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Matchmaking] Error starting ranked: {ex.Message}");
                foreach (var entry in sortedEntries)
                {
                    if (entry != null) Remove(entry);
                }
            }
        }

        public void StartGame(List<MatchmakingEntry> entries)
        {
            // The battle inits here
            BattleMode battle = new BattleMode(EventData.LocationId);
            battle.Id = Battles.Add(battle);

            Random rand = new Random();

            List<MatchmakingEntry> sortedEntries = new List<MatchmakingEntry>();

            if (battle.GetGameModeVariation() == 7)
            {
                sortedEntries = entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    UDPSocket socket = UDPGateway.CreateSocket();
                    socket.TCPConnection = sortedEntries[i].Connection;
                    socket.Battle = battle;
                    sortedEntries[i].Connection.UdpSessionId = socket.SessionId;
                    BattlePlayer player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar, i, 0);
                    player.TeamId = sortedEntries[i].PlayerTeamId;
                    sortedEntries[i].Player = player;
                    var hero = player.Avatar?.GetHero(player.CharacterId);
                    if (hero != null)
                        player.HeroPowerLevel = hero.PowerLevel;
                    else
                        player.HeroPowerLevel = 1;
                    battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
                }

                BattlePlayer bot1 = BattlePlayer.CreateStoryModeDummy("首领", battle.GetPlayersCountWithGameModeVariation(), 1, 32, 310, 254);
                battle.AddPlayer(bot1, -1);
                bot1.Bot = 2;
            }
            else if (GameModeUtil.HasTwoTeams(battle.GetGameModeVariation()))
            {
                Dictionary<long, List<MatchmakingEntry>> teamEntries = new Dictionary<long, List<MatchmakingEntry>>();
                List<MatchmakingEntry> soloPlayers = new List<MatchmakingEntry>();

                foreach (MatchmakingEntry entry in entries)
                {
                    if (entry.PlayerTeamId > 0)
                    {
                        if (!teamEntries.ContainsKey(entry.PlayerTeamId))
                            teamEntries[entry.PlayerTeamId] = new List<MatchmakingEntry>();
                        teamEntries[entry.PlayerTeamId].Add(entry);
                    }
                    else
                    {
                        soloPlayers.Add(entry);
                    }
                }

                List<MatchmakingEntry> team1 = new List<MatchmakingEntry>();
                List<MatchmakingEntry> team2 = new List<MatchmakingEntry>();

                foreach (var teamGroup in teamEntries.Values)
                {
                    if (team1.Count + teamGroup.Count <= 3)
                    {
                        foreach (var player in teamGroup)
                        {
                            player.PrefferedTeam = 0;
                            team1.Add(player);
                        }
                    }
                    else if (team2.Count + teamGroup.Count <= 3)
                    {
                        foreach (var player in teamGroup)
                        {
                            player.PrefferedTeam = 1;
                            team2.Add(player);
                        }
                    }
                }

                foreach (var soloPlayer in soloPlayers)
                {
                    if (team1.Count < 3)
                    {
                        soloPlayer.PrefferedTeam = 0;
                        team1.Add(soloPlayer);
                    }
                    else if (team2.Count < 3)
                    {
                        soloPlayer.PrefferedTeam = 1;
                        team2.Add(soloPlayer);
                    }
                }

                sortedEntries.AddRange(team1);
                sortedEntries.AddRange(team2);

                for (int i = 0; i < sortedEntries.Count; i++)
                {
                    UDPSocket socket = UDPGateway.CreateSocket();
                    socket.TCPConnection = sortedEntries[i].Connection;
                    socket.Battle = battle;
                    sortedEntries[i].Connection.UdpSessionId = socket.SessionId;

                    int teamIndex = sortedEntries[i].PrefferedTeam;

                    if (sortedEntries[i].Connection?.MessageManager?.HomeMode == null ||
                        sortedEntries[i].Connection?.Avatar == null)
                    {
                        var accountId = sortedEntries[i].Connection?.MessageManager?.HomeMode?.Avatar?.AccountId ?? 0;
                        Console.WriteLine($"[Matchmaking] Invalid account data for connection {accountId}");
                        continue;
                    }

                    BattlePlayer player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar, i, teamIndex);
                    player.TeamId = sortedEntries[i].PlayerTeamId;
                    sortedEntries[i].Player = player;
                    var hero = player.Avatar?.GetHero(player.CharacterId);
                    if (hero != null)
                        player.HeroPowerLevel = hero.PowerLevel;
                    else
                        player.HeroPowerLevel = 1;
                    battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
                }

                List<int> team1BotsCharacters = new List<int>();
                List<int> team2BotsCharacters = new List<int>();

                for (int i = sortedEntries.Count; i < battle.GetPlayersCountWithGameModeVariation(); i++)
                {
                    bool isBotValid = false;
                    int botCharacter = -1;
                    while (!isBotValid)
                    {
                        botCharacter = 16000000 + botBrawlers[rand.Next(0, botBrawlers.Length)];
                        if (i % 2 == 0)
                            isBotValid = !team1BotsCharacters.Contains(botCharacter);
                        else
                            isBotValid = !team2BotsCharacters.Contains(botCharacter);
                        if (!isBotValid) isBotValid = !GameModeUtil.HasTwoTeams(battle.GetGameModeVariation());
                    }

                    int teamIndex = i % 2;
                    if (battle.GetTeamPlayersCount(teamIndex) >= 3)
                    {
                        if (teamIndex == 1) teamIndex = 0;
                        else teamIndex = 1;
                    }

                    if (teamIndex == 0) team1BotsCharacters.Add(botCharacter);
                    else team2BotsCharacters.Add(botCharacter);

                    CharacterData data = DataTables.Get(16).GetDataByGlobalId<CharacterData>(botCharacter);
                    BattlePlayer bot = BattlePlayer.CreateBotInfo((i - sortedEntries.Count + 1).ToString(), i, teamIndex, botCharacter);
                    bot.HeroPowerLevel = new Random().Next(5, 12);
                    battle.AddPlayer(bot, -1);
                }
            }
            else
            {
                sortedEntries = entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    UDPSocket socket = UDPGateway.CreateSocket();
                    socket.TCPConnection = sortedEntries[i].Connection;
                    socket.Battle = battle;
                    sortedEntries[i].Connection.UdpSessionId = socket.SessionId;

                    int teamIndex = i;

                    if (sortedEntries[i].Connection?.MessageManager?.HomeMode == null ||
                        sortedEntries[i].Connection?.Avatar == null)
                    {
                        var accountId = sortedEntries[i].Connection?.MessageManager?.HomeMode?.Avatar?.AccountId ?? 0;
                        Console.WriteLine($"[Matchmaking] Invalid account data for connection {accountId}");
                        continue;
                    }

                    BattlePlayer player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar, i, teamIndex);
                    player.TeamId = sortedEntries[i].PlayerTeamId;
                    sortedEntries[i].Player = player;
                    var hero = player.Avatar?.GetHero(player.CharacterId);
                    if (hero != null)
                        player.HeroPowerLevel = hero.PowerLevel;
                    else
                        player.HeroPowerLevel = 1;
                    battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
                }

                for (int i = entries.Count; i < battle.GetPlayersCountWithGameModeVariation(); i++)
                {
                    int botCharacter = 16000000 + botBrawlers[rand.Next(0, botBrawlers.Length)];
                    int teamIndex = i;
                    CharacterData data = DataTables.Get(16).GetDataByGlobalId<CharacterData>(botCharacter);
                    BattlePlayer bot = BattlePlayer.CreateBotInfo(data.ItemName.ToUpper(), i, teamIndex, botCharacter);
                    bot.HeroPowerLevel = new Random().Next(5, 12);
                    battle.AddPlayer(bot, -1);
                }
            }

            if (battle.m_players[0].IsAdmin) for (int i = 0; i < 120; i++) battle.m_time.IncreaseTick();
            if (battle.StoryMode != null) battle.StoryMode.Init();
            battle.AddGameObjects();

            for (int i = 0; i < sortedEntries.Count; i++)
            {
                StartLoadingMessage startLoading = new StartLoadingMessage();
                startLoading.LocationId = battle.Location.GetGlobalId();
                startLoading.TeamIndex = sortedEntries[i].Player.TeamIndex;
                startLoading.OwnIndex = sortedEntries[i].Player.PlayerIndex;
                startLoading.GameMode = battle.GetGameModeVariation();
                sortedEntries[i].Connection.Avatar.UdpSessionId = sortedEntries[i].Connection.UdpSessionId;
                startLoading.Players.AddRange(battle.GetPlayers());
                sortedEntries[i].Connection.Send(startLoading);
                battle.Dummy = startLoading;
                battle.BattleWithTrophies = true;
            }
            battle.Start();
        }
    }

    public class MatchmakingEntry
    {
        public readonly Connection Connection;
        public BattlePlayer Player;
        public long PlayerTeamId;
        public int PrefferedTeam;
        public long GroupId;
        public int CharacterId;

        public MatchmakingEntry(Connection connection)
        {
            PrefferedTeam = -1;
            this.Connection = connection;
            GroupId = 0;
        }
    }
}