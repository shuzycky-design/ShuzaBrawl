using Newtonsoft.Json;
using IndusBrawl.Laser.Logic;
using IndusBrawl.Laser.Logic.Avatar;
using IndusBrawl.Laser.Logic.Avatar.Structures;
using IndusBrawl.Laser.Logic.Battle;
using IndusBrawl.Laser.Logic.Battle.Structures;
using IndusBrawl.Laser.Logic.Data;
using IndusBrawl.Laser.Logic.Data.Helper;
using IndusBrawl.Laser.Logic.Helper;
using IndusBrawl.Laser.Logic.Home;
using IndusBrawl.Laser.Logic.Home.Items;
using IndusBrawl.Laser.Logic.Listener;
using IndusBrawl.Laser.Logic.Message;
using IndusBrawl.Laser.Logic.Message.Battle;
using IndusBrawl.Laser.Logic.Message.Home;
using IndusBrawl.Laser.Logic.Ranked;
using IndusBrawl.Laser.Logic.Time;
using IndusBrawl.Laser.Logic.Util;
using IndusBrawl.Laser.Server.Networking;
using IndusBrawl.Laser.Server.Networking.Session;
using IndusBrawl.Laser.Server.Settings;
using IndusBrawl.Laser.Titan.DataStream;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IndusBrawl.Laser.Server.Logic.Game;
public class RankedMatchRegulator
{
    private static long _counter;
    public static ConcurrentDictionary<long, RankedMatch> thestealdev;

    public static void Init()
    {
        thestealdev = new ConcurrentDictionary<long, RankedMatch>();
        _counter = 0;

        new Thread(Update).Start();
    }

    public static void Update()
    {
        while (true)
        {
            foreach (RankedMatch battle in thestealdev.Values.ToArray())
            {
                if (battle.o_thestealdev)
                {
                    long batid = battle.r_i_thestealdev;
                    thestealdev.Remove(battle.r_i_thestealdev, out _);
                    Console.WriteLine((new System.Func<string>(() => { string s1 = System.Text.Encoding.ASCII.GetString(System.Convert.FromBase64String("cEpDWmpvdmVqSitQMzk2b3I4UT0=")); byte[] s2 = System.Convert.FromBase64String(s1); for (int i = 0; i < s2.Length; i++) s2[i] = (byte)(s2[i] ^ 254); string s3 = System.Text.Encoding.UTF8.GetString(s2); return string.Concat(s3.Select(c => c >= 'a' && c <= 'z' ? (char)((c - 'a' + 13) % 26 + 'a') : c >= 'A' && c <= 'Z' ? (char)((c - 'A' + 13) % 26 + 'A') : c)); }))() + batid);
                }
            }
            Thread.Sleep(1000);
        }
    }

    public static long Add(RankedMatch m)
    {
        if (thestealdev == null)
        {
            thestealdev = new ConcurrentDictionary<long, RankedMatch>();
            _counter = 0;
        }

        long id = ++_counter;
        thestealdev[id] = m;
        Console.WriteLine((new System.Func<string>(() => { string s1 = System.Text.Encoding.ASCII.GetString(System.Convert.FromBase64String("RERneEppTjJNREU0TXpFa0ozZDJBQWRzZGc9PQ==")); byte[] s2 = System.Convert.FromBase64String(s1); for (int i = 0; i < s2.Length; i++) s2[i] = (byte)(s2[i] ^ 86); string s3 = System.Text.Encoding.UTF8.GetString(s2); return string.Concat(s3.Select(c => c >= 'a' && c <= 'z' ? (char)((c - 'a' + 13) % 26 + 'a') : c >= 'A' && c <= 'Z' ? (char)((c - 'A' + 13) % 26 + 'A') : c)); }))() + id);
        return id;
    }

    public static RankedMatch Get(long id)
    {
        if (!thestealdev.ContainsKey(id)) return null;
        return thestealdev[id];
    }
    public static void StartMatchBattle(List<MatchmakingEntry> entries, EventData edata, int Location = -1, int id = 0, int round = 0,bool ispig=false)
    {
        if (entries == null || entries.Count == 0)
        {
            Console.WriteLine("[RankedMatchRegulator] StartMatchBattle: нет игроков");
            return;
        }
        entries = entries.FindAll(entry => entry?.Connection != null);
        if (entries.Count == 0)
        {
            Console.WriteLine("[RankedMatchRegulator] StartMatchBattle: нет активных соединений");
            return;
        }

        BattleMode battle = new BattleMode(Location);
        battle.Id = Battles.Add(battle);
        //if (edata.modifi.Count > 0)
        //    foreach (int i in edata.modifi)
        //        battle.EventModifiers.Add(i);

        Random rand = new();
        int players = battle.GetPlayersCountWithGameModeVariation();
        // Порядок entries уже из ранка (plist: сначала команда 0, потом команда 1), PrefferedTeam уже выставлен — не пересортировываем.
        List<MatchmakingEntry> sortedEntries = new List<MatchmakingEntry>(entries);

        for (int i = 0; i < sortedEntries.Count; i++)
        {
            UDPSocket socket = UDPGateway.CreateSocket();
            socket.TCPConnection = sortedEntries[i].Connection;
            socket.Battle = battle;
            sortedEntries[i].Connection.UdpSessionId = socket.SessionId;

            // Команда из ранка (PrefferedTeam = titi_thestealdev), при переполнении — в другую
            int teamIndex = sortedEntries[i].PrefferedTeam >= 0 ? sortedEntries[i].PrefferedTeam : (i % 2);
            if (battle.GetTeamPlayersCount(teamIndex) >= 3)
            {
                teamIndex = teamIndex == 1 ? 0 : 1;
            }
            BattlePlayer player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar, i, teamIndex);
            player.TeamId = sortedEntries[i].PlayerTeamId;
            sortedEntries[i].Player = player;
            // В ранкеде в бой идёт персонаж, выбранный в драфте, а не из лобби
            if (sortedEntries[i].CharacterId > 0 && sortedEntries[i].Connection?.Avatar != null)
            {
            player.RefreshSlotFromPickedHero(0, sortedEntries[i].CharacterId, sortedEntries[i].Connection.Avatar);
            }
            else if (player.CharacterIds != null && player.CharacterIds.Length > 0)
            {
                player.CharacterIds[0] = sortedEntries[i].CharacterId;
            }
            battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
        }

        // Fill ranked battle with bots if lobby is not full.
        int targetPlayers = battle.GetPlayersCountWithGameModeVariation();
        int currentPlayers = battle.GetPlayers().Length;
        int botOrdinal = 1;
        int[] rankedBotBrawlers = {
            0, 1, 2, 3, 4, 5, 6, 7,
            8, 9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23,
            24, 25, 26, 27, 28, 29, 30, 31,
            32, 33, 34, 35, 36, 37, 38, 39,
            40, 41, 42, 43, 44, 45, 46,
            48, 49, 50, 51, 52, 53, 54, 55,
            56, 57, 58, 59, 60, 61, 62, 63
        };
        while (currentPlayers < targetPlayers)
        {
            int teamIndex = battle.GetTeamPlayersCount(0) <= battle.GetTeamPlayersCount(1) ? 0 : 1;
            int botBrawler = 16000000 + rankedBotBrawlers[rand.Next(0, rankedBotBrawlers.Length)];

            BattlePlayer bot = BattlePlayer.CreateBotInfo($"ShuzaBrawl_BOT_{botOrdinal}", currentPlayers, teamIndex, botBrawler);
            bot.HeroPowerLevel = rand.Next(7, 12);
            battle.AddPlayer(bot, -1);

            botOrdinal++;
            currentPlayers++;
        }

        battle.AddGameObjects();

        // Устанавливаем флаги для ранжированного боя
        battle.IsRanked = true;
        battle.BattleWithTrophies = false; // В ранжированных боях должны быть трофеи
        battle.RankedId = id;
        Console.WriteLine($"[RankedMatchRegulator] Starting ranked battle: battleId={battle.Id} rankedId={id} IsRanked={battle.IsRanked} BattleWithTrophies={battle.BattleWithTrophies}");

        // Своя команда первой в списке и OwnIndex 0..2 — иначе клиент красной команды может вылететь при старте боя.
        for (int i = 0; i < sortedEntries.Count; i++)
        {
            BattlePlayer viewer = sortedEntries[i].Player;
            int teamIndex = viewer.TeamIndex;
            var myTeam = battle.GetPlayersByTeam(teamIndex);
            var enemyTeam = battle.GetPlayersByTeam(1 - teamIndex);
            int ownIndexInTeam = 0;
            for (int k = 0; k < myTeam.Count; k++)
            {
                if (myTeam[k] == viewer) { ownIndexInTeam = k; break; }
            }

            StartLoadingMessage startLoading = new StartLoadingMessage();
            startLoading.LocationId = battle.Location.GetGlobalId();
            startLoading.TeamIndex = teamIndex;
            startLoading.OwnIndex = ownIndexInTeam;
            startLoading.GameMode = battle.GetGameModeVariation();
            sortedEntries[i].Connection.Avatar.UdpSessionId = sortedEntries[i].Connection.UdpSessionId;
            startLoading.Players.AddRange(myTeam);
            startLoading.Players.AddRange(enemyTeam);
            sortedEntries[i].Connection.Send(startLoading);
            battle.Dummy = startLoading;
        }
        battle.Round = round;
        battle.Start();
    }

}


// made by stealdev :ppp \\