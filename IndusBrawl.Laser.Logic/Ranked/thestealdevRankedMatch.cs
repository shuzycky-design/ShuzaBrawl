using Newtonsoft.Json;
using IndusBrawl.Laser.Logic.Avatar;
using IndusBrawl.Laser.Logic.Avatar.Structures;
using IndusBrawl.Laser.Logic.Battle;
using IndusBrawl.Laser.Logic.Data;
using IndusBrawl.Laser.Logic.Data.Helper;
using IndusBrawl.Laser.Logic.Helper;
using IndusBrawl.Laser.Logic.Home;
using IndusBrawl.Laser.Logic.Home.Items;
using IndusBrawl.Laser.Logic.Listener;
using IndusBrawl.Laser.Logic.Message;
using IndusBrawl.Laser.Logic.Message.Home;
using IndusBrawl.Laser.Logic.Message.Ranked;
using IndusBrawl.Laser.Logic.Time;
using IndusBrawl.Laser.Titan.DataStream;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IndusBrawl.Laser.Logic.Ranked
{
    public struct TickAction
    {
        public bool ActionWill;
        public int AtWhenTick;
    }

    public class RankedMatch
    {
        public long r_i_thestealdev;
        public List<mp_thestealdev> plist_thestealdev;
        public Timer t_thestealdev;
        public bool d_thestealdev;
        public bool o_thestealdev;
        public GameTime ti_thestealdev;
        public bool mwb_thestealdev = true;
        private bool asdjfhsdjfhasdkf;
        private int ssaihfshdofasodf;
        private bool sdfioasghoadfgut;
        private int hdihgoodsfgosduhguti;
        private int subscribeToMiopark;
        private bool _isMatchEnded = false;

        public TickAction AllPlayersBannedAction;
        public TickAction PickupAction;
        public bool ily;
        public int thesteeeeel;
        public List<int> stealjev;
        public int TG_TheSteallllDev;
        public mp_thestealdev currtur_thestealdev;
        private int loc_stealdev;
        private int team_iiStealdev;
        public int rdns_thestealdev;
        public int TTW_thestealdev;
        public int TRW_thestealdev;
        public bool s_thestealdev;
        public bool BanDone;
        
        public bool MatchOver => TRW_thestealdev >= 2 || TTW_thestealdev >= 2;
        public bool FastPickup;
        public List<int> CurrentPickOrder;
        public int CurrentPickIndex;

        private int _banIndex = 0;
        private readonly int[] _banOrder = new[] { 3, 0, 4, 1, 5, 2 };
        private bool _banPhase = true;

        private static readonly Random _random = new Random();

        public RankedMatch(bool ispiggy)
        {
            TG_TheSteallllDev = 0;
            stealjev = new();
            CurrentPickOrder = new();
            CurrentPickIndex = 0;
            team_iiStealdev = _random.Next(2);
            r_i_thestealdev = 0;
            ti_thestealdev = new();
            plist_thestealdev = new();
            rdns_thestealdev = 0;
            TTW_thestealdev = 0;
            TRW_thestealdev = 0;
            _isMatchEnded = false;
            
            if (ispiggy)
            {
                mwb_thestealdev = false;
                FastPickup = true;
            }
        }

        public void Start() => t_thestealdev = new Timer(new TimerCallback(Update), null, 0, 1000 / 20);
        
        public void Stop()
        {
            t_thestealdev?.Dispose();
            t_thestealdev = null;
        }

        public void AddMatchPlayer(mp_thestealdev plr) => plist_thestealdev.Add(plr);

        public static int CalculatePlayerIndex(int playerPosition, int teamIndex)
        {
            if (teamIndex == 0)
            {
                return playerPosition % 3;
            }
            else
            {
                return (playerPosition % 3) + 3;
            }
        }

        public void Update(object stateInfo) => Tick();

        private void BuildPickOrder()
        {
            stealjev = new List<int>();

            var team0Players = plist_thestealdev
                .Where(p => p.titi_thestealdev == 0)
                .OrderBy(p => p.slot_thestealdev)
                .ToList();

            var team1Players = plist_thestealdev
                .Where(p => p.titi_thestealdev == 1)
                .OrderBy(p => p.slot_thestealdev)
                .ToList();

            var firstTeam = team_iiStealdev == 0 ? team0Players : team1Players;
            var secondTeam = team_iiStealdev == 0 ? team1Players : team0Players;

            int maxCount = Math.Max(firstTeam.Count, secondTeam.Count);
            for (int i = 0; i < maxCount; i++)
            {
                if (i < firstTeam.Count) stealjev.Add(firstTeam[i].piIternnal_thestealdev);
                if (i < secondTeam.Count) stealjev.Add(secondTeam[i].piIternnal_thestealdev);
            }

            CurrentPickOrder = new List<int>(stealjev);
            CurrentPickIndex = TG_TheSteallllDev;

            Console.WriteLine($"[RankedMatch] Pick order built: [{string.Join(",", stealjev)}] (firstTeam={team_iiStealdev})");
        }

        public void Tick()
        {
            if (_isMatchEnded) return;
            
            ti_thestealdev.IncreaseTick();
            if (d_thestealdev) return;
            HandleTerminate();

            if (GetTicks() == 10)
            {
                loc_stealdev = CalculateRandomMap();
                BuildPickOrder();

                var team0First = plist_thestealdev.OrderBy(x => x.titi_thestealdev).ToList();
                var team1First = plist_thestealdev.OrderByDescending(x => x.titi_thestealdev).ToList();
                
                foreach (mp_thestealdev p in plist_thestealdev)
                {
                    try
                    {
                        if (p?.Home?.HomeMode?.GameListener == null)
                            continue;
                        var ordered = p.titi_thestealdev == 0 ? team0First : team1First;
                        p.Home.HomeMode.GameListener.SendMessage(new RankedMatchStartedMessage
                        {
                            Players = ordered,
                            SelectedMap = GlobalId.GetInstanceId(loc_stealdev),
                            Team = 0,
                            ViewerTeam = p.titi_thestealdev,
                            MatchId = r_i_thestealdev
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RankedMatch] Error sending start message to {p.i_thestealdev}: {ex.Message}");
                    }
                }
            }

            if (GetTicks() == 20 * 9)
            {
                if (mwb_thestealdev)
                {
                    foreach (mp_thestealdev p in plist_thestealdev)
                        p.bs_stealdev = 1;
                    int banSlot = GetCurrentBanSlot();
                    foreach (mp_thestealdev p in plist_thestealdev)
                    {
                        try
                        {
                            if (p?.Home?.HomeMode?.GameListener == null) continue;
                            p.Home.HomeMode.GameListener.SendMessage(new RankedMatchBanStartedMessage
                            {
                                Time = 15,
                                QueueIndexer = ToDisplayIndex(banSlot, p.titi_thestealdev)
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[RankedMatch] Error sending ban started to {p.i_thestealdev}: {ex.Message}");
                        }
                    }

                    sdfioasghoadfgut = false;
                    hdihgoodsfgosduhguti = GetTicks();
                }
                else
                {
                    StartPickPhase();
                }
            }

            if (plist_thestealdev.All(p => p.pie_thestealdev) && !asdjfhsdjfhasdkf)
            {
                Send(new RankedMatchFinalPreparationStartedMessage());
                asdjfhsdjfhasdkf = true;
                ssaihfshdofasodf = GetTicks();
            }

            if (plist_thestealdev.All(p => p.hasb_thestealdev) && !sdfioasghoadfgut && !BanDone)
            {
                EndBanPhaseAndStartPicks();
                sdfioasghoadfgut = true;
                BanDone = true;
            }

            if (ily && currtur_thestealdev != null && thesteeeeel + (20 * 16) < GetTicks())
            {
                Send(new RankedMatchTerminatedMessage { Name = currtur_thestealdev.dd_thestealdev.Name, Reason = 2 });
                d_thestealdev = true;
            }

            if (asdjfhsdjfhasdkf && ssaihfshdofasodf + (20 * 10) < GetTicks())
            {
                StartBattle();
            }

            if (!BanDone && hdihgoodsfgosduhguti > 0 && hdihgoodsfgosduhguti + (20 * 15) <= GetTicks())
            {
                EndBanPhaseAndStartPicks();
                sdfioasghoadfgut = true;
                BanDone = true;
            }

            TickTimers();
        }

        private void StartBattle()
        {
            Console.WriteLine($"[RankedMatch] Starting battle round {rdns_thestealdev + 1} for match {r_i_thestealdev}");
            LogicServerListener.Instance.StartMatchBattle(plist_thestealdev, null, loc_stealdev, (int)r_i_thestealdev, rdns_thestealdev);
            d_thestealdev = true;
        }

        private void EndBanPhaseAndStartPicks()
        {
            Dictionary<long, int> bannedPersons = new();
            foreach (mp_thestealdev p in plist_thestealdev)
            {
                bannedPersons[p.i_thestealdev] = p.banne_thestealdev;
            }
            Send(new RankedMatchBanEndedMessage { BannedBrawlers = bannedPersons });

            foreach (mp_thestealdev p in plist_thestealdev)
            {
                p.ca_thestealdev = 0;
                p.bs_stealdev = 0;
            }

            StartPickPhase();
        }

        private void StartPickPhase()
        {
            if (stealjev.Count == 0 || TG_TheSteallllDev >= stealjev.Count)
            {
                Console.WriteLine("[RankedMatch] StartPickPhase: no players in pick order!");
                return;
            }

            currtur_thestealdev = plist_thestealdev.Find(p => p.piIternnal_thestealdev == stealjev[TG_TheSteallllDev]);

            if (currtur_thestealdev != null)
            {
                int nextIndex = (TG_TheSteallllDev + 1 < stealjev.Count) ? stealjev[TG_TheSteallllDev + 1] : -1;
                foreach (mp_thestealdev p in plist_thestealdev)
                {
                    try
                    {
                        if (p?.Home?.HomeMode?.GameListener == null) continue;
                        p.Home.HomeMode.GameListener.SendMessage(new RankedMatchPickStartedMessage
                        {
                            Player = currtur_thestealdev,
                            Next = ToDisplayIndex(nextIndex, p.titi_thestealdev),
                            ViewerTeam = p.titi_thestealdev
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RankedMatch] Error sending pick started to {p.i_thestealdev}: {ex.Message}");
                    }
                }
                currtur_thestealdev.ps_thestlealdeveEVEVE = 0;
                ily = true;
                thesteeeeel = GetTicks();

                Console.WriteLine($"[RankedMatch] Pick phase started: player={currtur_thestealdev.i_thestealdev} idx={currtur_thestealdev.piIternnal_thestealdev} turn={TG_TheSteallllDev}");
            }
            else
            {
                Console.WriteLine($"[RankedMatch] StartPickPhase: player not found for index {stealjev[TG_TheSteallllDev]}!");
            }
        }

        public void NextTurn()
        {
            TG_TheSteallllDev++;

            if (TG_TheSteallllDev >= stealjev.Count)
            {
                Console.WriteLine("[RankedMatch] All picks complete.");
                return;
            }

            currtur_thestealdev = plist_thestealdev.Find(p => p.piIternnal_thestealdev == stealjev[TG_TheSteallllDev]);
            if (currtur_thestealdev != null)
            {
                ily = true;
                currtur_thestealdev.ps_thestlealdeveEVEVE = 0;
                int nextIndex = (TG_TheSteallllDev + 1 < stealjev.Count) ? stealjev[TG_TheSteallllDev + 1] : -1;
                foreach (mp_thestealdev p in plist_thestealdev)
                {
                    try
                    {
                        if (p?.Home?.HomeMode?.GameListener == null) continue;
                        p.Home.HomeMode.GameListener.SendMessage(new RankedMatchPickStartedMessage
                        {
                            Player = currtur_thestealdev,
                            Next = ToDisplayIndex(nextIndex, p.titi_thestealdev),
                            ViewerTeam = p.titi_thestealdev
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RankedMatch] Error sending pick turn to {p.i_thestealdev}: {ex.Message}");
                    }
                }
                thesteeeeel = GetTicks();

                Console.WriteLine($"[RankedMatch] NextTurn: player={currtur_thestealdev.i_thestealdev} idx={currtur_thestealdev.piIternnal_thestealdev} turn={TG_TheSteallllDev}");
            }
            else
            {
                Console.WriteLine($"[RankedMatch] NextTurn: player not found for index {stealjev[TG_TheSteallllDev]}!");
            }
        }

        public void TickTimers()
        {
            if (subscribeToMiopark > 0) subscribeToMiopark--;
        }

        public void TerminateError(string name)
        {
            Send(new RankedMatchTerminatedMessage { Name = name });
            d_thestealdev = true;
        }

        public void HandleRoundEnd(int winnerTeam)
        {
            if (_isMatchEnded) return;
            
            if (winnerTeam == 0)
                TTW_thestealdev++;
            else if (winnerTeam == 1)
                TRW_thestealdev++;
            
            Console.WriteLine($"[RankedMatch] Round {rdns_thestealdev + 1} ended. Winner: Team {winnerTeam}. Score: Team0={TTW_thestealdev} Team1={TRW_thestealdev}");
            
            if (MatchOver)
            {
                EndMatch();
                return;
            }
            
            d_thestealdev = false;
            asdjfhsdjfhasdkf = false;
            sdfioasghoadfgut = false;
            BanDone = false;
            ily = false;
            _banPhase = true;
            _banIndex = plist_thestealdev.Count(p => p.hasb_thestealdev);
            
            foreach (var p in plist_thestealdev)
            {
                p.ca_thestealdev = 0;
                p.bs_stealdev = 0;
                p.ps_thestlealdeveEVEVE = 0;
                p.pie_thestealdev = false;
            }
            
            rdns_thestealdev++;
            Console.WriteLine($"[RankedMatch] Starting round {rdns_thestealdev + 1} of match {r_i_thestealdev}");
            
            TG_TheSteallllDev = 0;
            CurrentPickIndex = 0;
            
            StartBattle();
        }

        public void EndMatch()
        {
            if (_isMatchEnded) return;
            _isMatchEnded = true;
            
            int winnerTeam = TTW_thestealdev >= 2 ? 0 : (TRW_thestealdev >= 2 ? 1 : -1);
            
            if (winnerTeam != -1)
            {
                Console.WriteLine($"[RankedMatch] MATCH ENDED! Match {r_i_thestealdev} finished. Winner: Team {winnerTeam}. Final score: {TTW_thestealdev}-{TRW_thestealdev}");
                
                foreach (var p in plist_thestealdev)
                {
                    try
                    {
                        if (p?.Home?.HomeMode?.GameListener == null) continue;
                        
                        var endMsg = new RankedMatchTerminatedMessage 
                        { 
                            Name = $"Match completed. Winner: Team {(winnerTeam == p.titi_thestealdev ? "YOUR" : "ENEMY")}",
                            Reason = 3
                        };
                        p.Home.HomeMode.GameListener.SendMessage(endMsg);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RankedMatch] Error sending end message to {p.i_thestealdev}: {ex.Message}");
                    }
                }
            }
            
            StopMatch();
        }

        public int RestartRound()
        {
            if (MatchOver || _isMatchEnded)
            {
                EndMatch();
                return -1;
            }
            
            d_thestealdev = false;
            asdjfhsdjfhasdkf = false;
            sdfioasghoadfgut = false;
            BanDone = false;
            ily = false;
            _banPhase = true;
            _banIndex = plist_thestealdev.Count(p => p.hasb_thestealdev);
            
            foreach (var p in plist_thestealdev)
            {
                p.ca_thestealdev = 0;
                p.bs_stealdev = 0;
                p.ps_thestlealdeveEVEVE = 0;
                p.pie_thestealdev = false;
            }
            
            rdns_thestealdev++;
            Console.WriteLine($"[RankedMatch] Restarting round {rdns_thestealdev + 1}");
            
            LogicServerListener.Instance.StartMatchBattle(
                plist_thestealdev, 
                null, 
                loc_stealdev, 
                (int)r_i_thestealdev, 
                rdns_thestealdev
            );
            return 0;
        }

        public void StopMatch()
        {
            o_thestealdev = true;
            Stop();
        }

        public void HandleTerminate()
        {
            if (_isMatchEnded) return;
            
            foreach (mp_thestealdev p in plist_thestealdev)
            {
                if (!LogicServerListener.Instance.IsPlayerOnline(p.i_thestealdev))
                {
                    Send(new RankedMatchTerminatedMessage { Name = p.dd_thestealdev.Name, Reason = 2 });
                    d_thestealdev = true;
                    EndMatch();
                    return;
                }
            }
        }

        public void Send(GameMessage message)
        {
            foreach (mp_thestealdev conn in plist_thestealdev)
            {
                try
                {
                    if (conn?.Home?.HomeMode?.GameListener == null)
                        continue;
                    conn.Home.HomeMode.GameListener.SendMessage(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RankedMatch] Error sending message to {conn.i_thestealdev}: {ex.Message}");
                }
            }
        }

        public void SendHeroPicked(mp_thestealdev player)
        {
            foreach (mp_thestealdev p in plist_thestealdev)
            {
                try
                {
                    if (p?.Home?.HomeMode?.GameListener == null) continue;
                    p.Home.HomeMode.GameListener.SendMessage(new RankedMatchHeroPickedMessage { Player = player, ViewerTeam = p.titi_thestealdev });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RankedMatch] Error sending HeroPicked to {p.i_thestealdev}: {ex.Message}");
                }
            }
        }

        public void SendHeroDataUpdated(mp_thestealdev player)
        {
            foreach (mp_thestealdev p in plist_thestealdev)
            {
                try
                {
                    if (p?.Home?.HomeMode?.GameListener == null) continue;
                    p.Home.HomeMode.GameListener.SendMessage(new RankedMatchHeroDataUpdatedMessage { Player = player, ViewerTeam = p.titi_thestealdev });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RankedMatch] Error sending HeroDataUpdated to {p.i_thestealdev}: {ex.Message}");
                }
            }
        }

        private static int ToDisplayIndex(int internalIndex, int viewerTeam)
        {
            if (internalIndex < 0) return -1;
            return viewerTeam == 0 ? internalIndex : (internalIndex + 3) % 6;
        }

        public int CalculateRandomMap()
        {
            List<string> Names = new();

            var allowedModes = new List<string> { 
                "Heist", "GemGrab", "Knockout", "KingOfHill", 
                "Deathmatch", "BrawlBall", "Bounty", "Invasion", "ProtectKing"
            };

            try
            {
                var rankedData = DataTables.Get(DataType.RankedE);
                if (rankedData != null)
                {
                    foreach (RankedLocationsData data in rankedData.GetDatas())
                    {
                        var locationData = DataTables.Get(DataType.Location).GetData<LocationData>(data.Name);
                        if (locationData != null && IsModeAllowed(locationData, allowedModes))
                        {
                            Names.Add(data.Name);
                        }
                    }
                }
            }
            catch { }

            if (Names.Count == 0)
            {
                foreach (LocationData locationData in DataTables.Get(DataType.Location).GetDatas())
                {
                    if (locationData != null && IsModeAllowed(locationData, allowedModes))
                    {
                        Names.Add(locationData.Name);
                    }
                }
            }

            if (Names.Count == 0)
            {
                Console.WriteLine("[RankedMatch] No maps available! Using default.");
                return GlobalId.CreateGlobalId(15, 2);
            }

            string random = Names[_random.Next(Names.Count)];
            var selectedLocation = DataTables.Get(DataType.Location).GetData<LocationData>(random);
            return selectedLocation?.GetGlobalId() ?? GlobalId.CreateGlobalId(15, 2);
        }

        private bool IsModeAllowed(LocationData locationData, List<string> allowedModes)
        {
            if (locationData == null) return false;
            string modeName = locationData.GameModeVariation;
            return allowedModes.Contains(modeName);
        }

        public mp_thestealdev GetPlayer(long id) => plist_thestealdev.Find(p => p.i_thestealdev == id);
        public int GetTicks() => ti_thestealdev.GetTick();

        private void BroadcastPickUpdate()
        {
            int nextInternal = CurrentPickOrder.Count > CurrentPickIndex + 1 ? CurrentPickOrder[CurrentPickIndex + 1] : -1;
            var currentPlayer = CurrentPickIndex >= 0 && CurrentPickIndex < CurrentPickOrder.Count
                ? plist_thestealdev.Find(x => x.piIternnal_thestealdev == CurrentPickOrder[CurrentPickIndex])
                : null;
            foreach (var p in plist_thestealdev)
            {
                try
                {
                    if (p?.Home?.HomeMode?.GameListener == null) continue;
                    p.Home.HomeMode.GameListener.SendMessage(new RankedMatchPickStartedMessage
                    {
                        Player = currentPlayer,
                        Next = ToDisplayIndex(nextInternal, p.titi_thestealdev),
                        ViewerTeam = p.titi_thestealdev
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RankedMatch] Error sending pick update to {p.i_thestealdev}: {ex.Message}");
                }
            }
        }

        private int GetCurrentBanSlot()
        {
            if (_banIndex < 0 || _banIndex >= _banOrder.Length) return -1;
            return _banOrder[_banIndex];
        }

        public void HandleBan(mp_thestealdev sender, int brawlerId)
        {
            if (!_banPhase) return;
            if (sender == null) return;
            if (sender.hasb_thestealdev)
            {
                Console.WriteLine($"[Ranked] IGNORE BAN: player {sender.i_thestealdev} already banned.");
                return;
            }

            ApplyBan(sender, brawlerId);
            _banIndex = plist_thestealdev.Count(p => p.hasb_thestealdev);

            if (plist_thestealdev.All(p => p.hasb_thestealdev))
            {
                _banPhase = false;
                if (!BanDone)
                {
                    EndBanPhaseAndStartPicks();
                    sdfioasghoadfgut = true;
                    BanDone = true;
                }
                return;
            }

            int nextSlot = GetNextUnbannedSlot();
            BroadcastBanState(nextSlot);
        }

        private int GetNextUnbannedSlot()
        {
            foreach (int slot in _banOrder)
            {
                var player = plist_thestealdev.Find(p => p.piIternnal_thestealdev == slot);
                if (player != null && !player.hasb_thestealdev)
                {
                    return slot;
                }
            }

            var anyUnbanned = plist_thestealdev.FirstOrDefault(p => !p.hasb_thestealdev);
            return anyUnbanned?.piIternnal_thestealdev ?? -1;
        }

        private void ApplyBan(mp_thestealdev player, int brawlerId)
        {
            player.banne_thestealdev = GlobalId.CreateGlobalId(16, brawlerId);
            player.bs_stealdev = 1;
            player.hasb_thestealdev = true;
            player.ca_thestealdev = 0;

            Console.WriteLine($"[RankedMatch] Player {player.i_thestealdev} ({player.dd_thestealdev.Name}) banned brawler: {brawlerId}");
        }

        private void BroadcastBanState(int nextSlot)
        {
            foreach (var p in plist_thestealdev)
            {
                try
                {
                    if (p?.Home?.HomeMode?.GameListener == null) continue;
                    p.Home.HomeMode.GameListener.SendMessage(new RankedMatchBanStartedMessage
                    {
                        Time = 15,
                        QueueIndexer = ToDisplayIndex(nextSlot, p.titi_thestealdev)
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RankedMatch] Error sending ban state to {p.i_thestealdev}: {ex.Message}");
                }
            }

            Console.WriteLine($"[RankedMatch] Next ban slot: {nextSlot}");
        }
    }

    public class mp_thestealdev
    {
        public long i_thestealdev;
        public PlayerDisplayData dd_thestealdev;
        public int ca_thestealdev;
        public int bs_stealdev;
        public int ps_thestlealdeveEVEVE;
        public bool pie_thestealdev;
        public bool hasb_thestealdev;
        public object hda_thestealdev;
        public int piIn_thestealdev;
        public int piIternnal_thestealdev;
        public int titi_thestealdev;
        public int slot_thestealdev;
        public int banne_thestealdev;
        [JsonIgnore] public readonly ClientHome Home;
        [JsonIgnore] public readonly ClientAvatar Avatar;

        public mp_thestealdev()
        {
            dd_thestealdev = new PlayerDisplayData();
            i_thestealdev = -1;
        }

        public mp_thestealdev(ClientAvatar a, ClientHome h) : this()
        {
            Home = h;
            Avatar = a;
        }

        public static mp_thestealdev CreateRankedPlayer(
            ClientAvatar avatar,
            ClientHome home,
            int teamIndex,
            int globalIndex)
        {
            mp_thestealdev player = new(avatar, home);

            player.titi_thestealdev = teamIndex;
            player.piIternnal_thestealdev = globalIndex;
            player.piIn_thestealdev = globalIndex;

            player.slot_thestealdev = teamIndex == 0
                ? globalIndex
                : globalIndex - 3;

            player.dd_thestealdev = new(home.ThumbnailId, home.NameColorId, avatar.Name);
            player.i_thestealdev = avatar.AccountId;

            return player;
        }

        public void ChangeHeroData(int character, int state)
        {
            if (state >= 0 && state <= 1) bs_stealdev = state;
            if (state >= 2 && state <= 3)
            {
                switch (state)
                {
                    case 2:
                        ps_thestlealdeveEVEVE = 0;
                        break;
                    case 3:
                        ps_thestlealdeveEVEVE = 1;
                        break;
                }
            }
        }

        public void SetBanQueue(int i) => bs_stealdev = i;
        public void SetPickUpQueue(int q) => ps_thestlealdeveEVEVE = q;

        public void Encode(ByteStream Stream)
        {
            Encode(Stream, true);
        }

        public void Encode(ByteStream Stream, bool useGlobalSlot, int? draftDisplaySlot = null)
        {
            ByteStreamHelper.EncodeLogicLong(Stream, i_thestealdev);
            Stream.WriteBoolean(true);
            (dd_thestealdev ?? new PlayerDisplayData()).Encode(Stream);

            Stream.WriteVInt(bs_stealdev);
            Stream.WriteVInt(ps_thestlealdeveEVEVE);
            int slotToWrite = draftDisplaySlot ?? (useGlobalSlot ? piIternnal_thestealdev : slot_thestealdev);
            Stream.WriteVInt(slotToWrite);
            ByteStreamHelper.WriteDataReference(Stream, ca_thestealdev);

            if (ca_thestealdev == 0)
            {
                Stream.WriteVInt(11);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                Stream.WriteBoolean(false);
                Stream.WriteVInt(1);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                Stream.WriteVInt(1);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                Stream.WriteVInt(0);
            }
            else
            {
                int _heroLVL = 1;
                int _starpowerId = 0;
                int _gadgetId = 0;
                int _hyperCharge = 0;
                int _gear1GlobalId = 0;
                int _gear2GlobalId = 0;
                
                try
                {
                    if (Avatar != null)
                    {
                        var heroMethod = Avatar.GetType().GetMethod("GetHero");
                        if (heroMethod != null)
                        {
                            var hero = heroMethod.Invoke(Avatar, new object[] { ca_thestealdev });
                            if (hero != null)
                            {
                                var powerLevelProp = hero.GetType().GetProperty("PowerLevel");
                                if (powerLevelProp != null)
                                    _heroLVL = (int)powerLevelProp.GetValue(hero);
                            }
                        }
                    }
                }
                catch { }
                
                _starpowerId = GlobalId.CreateGlobalId(23, 0);
                _gadgetId = GlobalId.CreateGlobalId(23, 0);
                _hyperCharge = GlobalId.CreateGlobalId(23, 0);
                _gear1GlobalId = GlobalId.CreateGlobalId(62, 0);
                _gear2GlobalId = GlobalId.CreateGlobalId(62, 0);
                
                Stream.WriteVInt(_heroLVL);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                ByteStreamHelper.WriteDataReference(Stream, _starpowerId);
                ByteStreamHelper.WriteDataReference(Stream, _gadgetId);
                ByteStreamHelper.WriteDataReference(Stream, _hyperCharge);
                Stream.WriteBoolean(pie_thestealdev);
                Stream.WriteVInt(1);
                ByteStreamHelper.WriteDataReference(Stream, _gear1GlobalId);
                Stream.WriteVInt(1);
                ByteStreamHelper.WriteDataReference(Stream, _gear2GlobalId);
                Stream.WriteVInt(0);
            }
        }
    }
}