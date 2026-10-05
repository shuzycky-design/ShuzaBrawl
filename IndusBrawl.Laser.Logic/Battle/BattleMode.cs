using System.Threading.Tasks;

namespace IndusBrawl.Laser.Logic.Battle
{
    using BattleLogEntry;
    using Masuda.Net.HelpMessage;
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Battle.Component;
    using IndusBrawl.Laser.Logic.Battle.Input;
    using IndusBrawl.Laser.Logic.Battle.Level;
    using IndusBrawl.Laser.Logic.Battle.Level.Factory;
    using IndusBrawl.Laser.Logic.Battle.Objects;
    using IndusBrawl.Laser.Logic.Battle.Structures;
    using IndusBrawl.Laser.Logic.Command.Home;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Math;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Logic.Listener;
    using IndusBrawl.Laser.Logic.Message.Account;
    using IndusBrawl.Laser.Logic.Message.Battle;
    using IndusBrawl.Laser.Logic.Message.Club;
    using IndusBrawl.Laser.Logic.Message.Home;
    using IndusBrawl.Laser.Logic.Notification;
    using IndusBrawl.Laser.Logic.Time;
    using IndusBrawl.Laser.Logic.Util;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;
    using IndusBrawl.Laser.Titan.Json;
    using IndusBrawl.Laser.Titan.Math;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;
    using System.Threading;
    using IndusBrawl.Laser.Logic.Ranked;

    public struct DiedEntry
    {
        public int DeathTick;
        public BattlePlayer Player;
    }

    public enum MarkerState
    {
        Neutral = 0,
        Team0 = 1,
        Team1 = 2,
        Crossed = 3
    }
///////    public int TmpBrawlballTick;

    public class KnockoutMarker
    {
        public int X { get; set; }
        public int Y { get; set; }
        public MarkerState State { get; set; }
        public Character VisualObject { get; set; }

        public KnockoutMarker(int x, int y)
        {
            X = x;
            Y = y;
            State = MarkerState.Neutral;
        }
    }
    
    public class KingOfHillState
    {
        public int ZoneIndex;
        public int ZonePercentage; 
        public int ZonePercentageTeam0 { get; set; } = 0;
        public int ZonePercentageTeam1 { get; set; } = 0;
        public bool CaptureEffectActiveTeam0 { get; set; }
        public bool CaptureEffectActiveTeam1 { get; set; }
        public AreaEffect CurrentCaptureEffectTeam0 { get; set; }
        public AreaEffect CurrentCaptureEffectTeam1 { get; set; }

        public bool GaugeTeam0 { get; set; }
        public bool GaugeTeam1 { get; set; }
        public AreaEffect GaugeTeam0Effect { get; set; }
        public AreaEffect GaugeTeam1Effect { get; set; }

        public int LastUpTickTeam0 { get; set; }
        public int LastUpTickTeam1 { get; set; }
        public bool HillCompleteEffectSpawnedTeam0 { get; set; }
        public bool CanContinueUpTeam0 = true;

        public bool HillCompleteEffectSpawnedTeam1 { get; set; }
        public bool CanContinueUpTeam1 = true;

        public KingOfHillState(int zoneIndex)
        {
            this.ZoneIndex = zoneIndex;
        }
    }
    
    public class BattleMode
    {
        public const int ORBS_TO_COLLECT_NORMAL = 0xA;
        public const int INTRO_TICKS = 180;
        public const int NO_TIME_TICKS = 16000;
        public const int NORMAL_TICKS = 3180;
        /// <summary>Интервал тика в миллисекундах (50ms = 20 tps).</summary>
        public const int TICK_INTERVAL_MS = 50;
        
        // Константы для режимов
        public const int GAME_MODE_VARIATION_KNOCKOUT = 20;
        public const int GAME_MODE_VARIATION_KNOCKOUT_2 = 24;
        public const int GAME_MODE_VARIATION_BRAWL_BALL = 5;
        public const int GAME_MODE_VARIATION_HOT_ZONE = 17;
        public const int GAME_MODE_VARIATION_BOUNTY = 3;
        public const int GAME_MODE_VARIATION_GEM_GRAB = 0;
        public const int GAME_MODE_VARIATION_SHOWDOWN = 6;
        public const int GAME_MODE_VARIATION_DUO_SHOWDOWN = 7;
        public const int GAME_MODE_VARIATION_HEIST = 2;
        public const int GAME_MODE_VARIATION_SIEGE = 27;
        public const int GAME_MODE_VARIATION_PAYLOAD = 33;
        public const int GAME_MODE_VARIATION_BOSS_FIGHT = 8;
        public const int GAME_MODE_VARIATION_TRAINING = 13;
        public const int GAME_MODE_VARIATION_STORY = 30;
        public const int GAME_MODE_VARIATION_VOLLEYBRAWL = 25;
        public const int GAME_MODE_VARIATION_LAST_STAND = 19;
        
        // Многораундовые режимы для ранкеда
        private bool IsMultiRoundRankedMode()
        {
            return m_gameModeVariation == GAME_MODE_VARIATION_BRAWL_BALL || 
                   m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT ||
                   m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT_2;
        }
        
        public StartLoadingMessage Dummy;
        private bool m_hasSentEndMessages = false;

        public Timer m_updateTimer;
        
        public int TmpBrawlballTick;

        public int m_locationId;
        public int m_gameModeVariation;
        private int m_playersCountWithGameModeVariation;

        public bool BattleWithTrophies;

        private Queue<ClientInput> m_inputQueue;

        List<GameObject> printobjects = new List<GameObject>();

        public List<BattlePlayer> m_players;
        private Dictionary<long, BattlePlayer> m_playersBySessionId;
        private Dictionary<long, LogicGameListener> m_spectators;
        private GameObjectManager m_gameObjectManager;
        private Dictionary<long, BattlePlayer> m_playersByAccountId;

        private Rect m_playArea;
        private TileMap m_tileMap;
        public GameTime m_time;
        private LogicRandom m_random;
        private int m_randomSeed;
        public List<GameObject> StoryModeStuffs;
        public int m_winnerTeam;
        private List<KingOfHillState> _koHStates = new List<KingOfHillState>();
        public BattlePlayerMap BattlePlayerMap;
        private int m_playersAlive;

        private int m_gemGrabCountdown;
        public bool IsStoryMode;
        public bool IsGameOver { get; set; }

        public List<int> EventModifiers;

        // Knockout mode variables
        public List<KnockoutMarker> m_knockoutMarkers;
        private int m_team0AliveCount;
        private int m_team1AliveCount;
        
        // Переменные для нокаута (раунды)
        public int m_knockoutRound = 1;
        public int[] m_knockoutRoundWins = new int[2] { 0, 0 };
        public int RoundTicks;
        public int RoundResetTicks;
        public List<int> TScore = new List<int> { 0, 0 };
        public int RoundCount;
        public int[] RoundWins = { 0, 0, 0 };
        public bool nokbool;
        public int KnockoutTicks;
        private bool _knockoutSmokeSpawnedThisRound;

        public StoryMode StoryMode;
        private int Team1King;
        private int Team2King;
        List<int> PlayerIndexesForTeam1 = new List<int> { 0, 1, 2 };
        List<int> PlayerIndexesForTeam2 = new List<int> { 3, 4, 5 };
        
        // Для Brawl Ball
        public Character Carryable;
        public int ToGoalTeam = -1;
        public int GoalTicks;
        public bool CanReset = true;

        private int _brawlBallLastX;
        private int _brawlBallLastY;
        private int _brawlBallSamePosTicks;
        private int _brawlBallMissingTicks;
        private int _brawlBallAirTicks;
        private int _brawlBallTeam0Goals;
        private int _brawlBallTeam1Goals;
        private const int BRAWLBALL_PICKUP_DELAY_TICKS = 0;

        // Ranked battle flags
        public bool IsRanked;
        public int RankedId;
        public int Round;

        private int _reconnectTimeoutTicks = 600;
        private Dictionary<long, int> _disconnectedTicks = new Dictionary<long, int>();
        
        private int _lastSpectatorUpdateTick = 0;
        private const int SPECTATOR_UPDATE_INTERVAL_TICKS = 4;
        private int _isExecutingTick = 0;
        
        private int Zone1Score;
        private int Zone2Score;

        private bool _hotZoneInitialized;

        // Ранкед переменные
        private bool _tmpb = false;
        private int _tmpi = 0;
        private bool _rankedRestartIssued = false;
        private bool _rankedMatchCompleted = false;
        private const int RANKED_ROUND_DELAY_TICKS = 10 * 20; // 10 секунд

        public BattleMode(int locationId)
        {
            m_winnerTeam = -1;
            m_locationId = locationId;
            try
            {
                var loc = Location;
                m_gameModeVariation = loc != null ? GameModeUtil.GetGameModeVariation(loc.GameModeVariation) : 0;
            }
            catch { m_gameModeVariation = 0; }
            m_playersCountWithGameModeVariation = GamePlayUtil.GetPlayerCountWithGameModeVariation(m_gameModeVariation);

            m_inputQueue = new Queue<ClientInput>();

            m_randomSeed = 0;
            m_random = new LogicRandom(m_randomSeed);

            m_players = new List<BattlePlayer>();
            m_playersBySessionId = new Dictionary<long, BattlePlayer>();
            m_playersByAccountId = new Dictionary<long, BattlePlayer>();

            m_time = new GameTime();
            try
            {
                var loc = Location;
                m_tileMap = loc?.Map != null ? TileMapFactory.CreateTileMap(loc.Map) : null;
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine("[BattleMode] CreateTileMap failed: " + ex.Message);
#endif
                m_tileMap = null;
            }
            if (m_tileMap == null)
                m_tileMap = new TileMap(50, 50, new string('0', 2500));
            m_playArea = new Rect(0, 0, m_tileMap.LogicWidth, m_tileMap.LogicHeight);
            m_gameObjectManager = new GameObjectManager(this);

            m_spectators = new Dictionary<long, LogicGameListener>();

            // Инициализируем нокаут переменные
            if (m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT || m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT_2)
            {
                m_knockoutMarkers = new List<KnockoutMarker>();
                m_team0AliveCount = 0;
                m_team1AliveCount = 0;
                m_knockoutRound = 1;
                m_knockoutRoundWins = new int[2] { 0, 0 };
                
                Carryable = null;
                
                TScore = new List<int> { 0, 0 };
                RoundCount = 0;
                RoundWins = new int[] { 0, 0, 0 };
                nokbool = false;
                KnockoutTicks = 0;
                _knockoutSmokeSpawnedThisRound = false;
                
                Console.WriteLine("[Knockout] Чистый нокаут режим инициализирован (ID 20/24)!");
            }

            Dummy = new StartLoadingMessage();
            Dummy.LocationId = m_locationId;
            Dummy.GameMode = m_gameModeVariation;
            Dummy.GameType = 0;
            
            Dummy.map = BattlePlayerMap;
            Dummy.MapMode = 1;
            Dummy.Modifiers = EventModifiers ?? new List<int>();
            Dummy.LeaveButton = true;

            Console.WriteLine($"[BattleMode] Dummy инициализирован для наблюдения: LocationId={m_locationId}, GameMode={m_gameModeVariation}");

            EventModifiers = new List<int>();
            PlayerIndexesForTeam1.Clear();
            PlayerIndexesForTeam2.Clear();
            if (m_gameModeVariation == 30) StoryMode = new(this);
            _hotZoneInitialized = false;
        }
        
        public int GetKOHZoneCount() => _koHStates.Count;
        public KingOfHillState GetZoneByUID(int a1)
        {
            if (_koHStates == null) return null;
            if (a1 < 0 || a1 >= _koHStates.Count) return null;
            return _koHStates[a1];
        }
        public int GetTeam1King() => Team1King;
        public int GetTeam2King() => Team2King;
        
        public List<BattlePlayer> GetPlayersByTeam(int team)
        {
            List<BattlePlayer> teamCharacters = new List<BattlePlayer>();
            if (m_players == null) return teamCharacters;
            foreach (BattlePlayer bplr in m_players)
            {
                if (bplr != null && bplr.TeamIndex == team)
                    teamCharacters.Add(bplr);
            }
            return teamCharacters;
        }
        
        public void SetPlayerMap(BattlePlayerMap map)
        {
            BattlePlayerMap = map;
            m_tileMap = TileMapFactory.CreatePlayerMap(map.MapData);
            m_gameModeVariation = map.GMV;
            m_playersCountWithGameModeVariation = GamePlayUtil.GetPlayerCountWithGameModeVariation(map.GMV);

            if (Dummy != null)
            {
                Dummy.map = map;
                Dummy.GameMode = map.GMV;
                Dummy.LocationId = 0;
                Console.WriteLine($"[BattleMode] Dummy обновлен для пользовательской карты: GameMode={map.GMV}");
            }
        }
        
        public int GetGoalTeam() => ToGoalTeam;
        public bool IsRoundActionLocked()
        {
            if (m_gameModeVariation == GAME_MODE_VARIATION_BRAWL_BALL)
            {
                if (ToGoalTeam != -1 || GoalTicks > 0) return true;
            }

            if (m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT || m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT_2)
            {
                if (nokbool && KnockoutTicks > 0) return true;
            }

            return false;
        }

        private void FreezeAllHeroesDuringRoundPause()
        {
            if (m_gameObjectManager == null) return;

            foreach (Character character in m_gameObjectManager.GetCharacters())
            {
                if (character?.CharacterData == null || !character.CharacterData.IsHero()) continue;
                if (!character.IsAlive()) continue;

                character.InterruptAllSkills();
                character.StopMovement();
                character.UltiDisabled();
            }
        }
        
        private BattlePlayerMap CreateSimpleMap()
        {
            Console.WriteLine($"[BattleMode] Создание простого PlayerMap...");
            
            try
            {
                PlayerMap playerMap = new PlayerMap();
                BattlePlayerMap battleMap = new BattlePlayerMap(playerMap);
                Console.WriteLine($"[BattleMode] Простой map создан");
                return battleMap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BattleMode ERROR] CreateSimpleMap failed: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

private void NotifyRankedRoundEnd()
{
    if (!IsRanked || RankedId <= 0) return;
    if (m_winnerTeam == -1) return;
    
    var match = LogicServerListener.Instance?.GetRankedMatch(RankedId);
    if (match != null)
    {
        Console.WriteLine($"[Ranked] Notifying round end: Match={RankedId}, Winner={m_winnerTeam}, Score={match.TTW_thestealdev}-{match.TRW_thestealdev}");
        match.HandleRoundEnd(m_winnerTeam);
    }
}

        public void AddWinInLaserBall(int teamIndex)
        {
            try
            {
                if (teamIndex == 0) _brawlBallTeam0Goals++;
                else if (teamIndex == 1) _brawlBallTeam1Goals++;

                if (m_players == null) return;
                BattlePlayer firstPlayer = m_players.FirstOrDefault(p => p != null && p.TeamIndex == teamIndex);
                if (firstPlayer != null)
                {
                    firstPlayer.AddScore(1);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[BrawlBall] AddWinInLaserBall error: {ex?.Message}");
#endif
            }
        }
        
        public void SetGoalLaserBall(int team)
        {
            ToGoalTeam = team;
            GoalTicks = 3 * 20;
        }
        
        public int GetZone2Score() => Zone2Score;
        public int GetZone1Score() => Zone1Score;
        public void AddScoreTo1Zone() { Zone1Score++; }
        public void AddScoreTo2Zone() { Zone2Score++; }
        
        public void ResetRoundLaserBall(bool ballr)
        {
            if (!CanReset && ballr) return;
            if (m_gameObjectManager == null) return;

            if (Carryable != null)
            {
                try
                {
                    Carryable.StopKicking();
                    Carryable.CARRi_CODEBYSTEAL_TG_THESTEALDEV = null;
                    m_gameObjectManager.RemoveGameObject(Carryable);
                }
                catch
                {
                }
                Carryable = null;
            }
            
            try
            {
                var charactersSnapshot = m_gameObjectManager.GetCharacters()?.ToArray() ?? Array.Empty<Character>();
                foreach (Character character in charactersSnapshot)
                {
                    if (character == null) continue;
                    try
                    {
                        if (character.GetPlayer() != null && character.CharacterData != null && character.CharacterData.IsHero())
                        {
                            m_gameObjectManager.RemoveGameObject(character);
                        }
                        else if (character.CharacterData != null && character.CharacterData.IsCarryable())
                        {
                            try { character.StopKicking(); } catch { }
                            m_gameObjectManager.RemoveGameObject(character);
                        }
                        else
                        {
                            try { character.CauseDamage(null, 99999, 99999, false, null, false); } catch (Exception) { }
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }

            if (m_players != null)
            {
                foreach (BattlePlayer playerrrr in m_players.ToArray())
                {
                    try { if (playerrrr != null) InstantRespawn(playerrrr); } catch (Exception) { }
                }
            }
            
            foreach (Item item in m_gameObjectManager.GetItems())
            {
                m_gameObjectManager.RemoveGameObject(item);
            }
            
            foreach (AreaEffect area in m_gameObjectManager.GetAreaEffects())
            {
                m_gameObjectManager.RemoveGameObject(area);
            }
            
            foreach (Projectile proj in m_gameObjectManager.GetProjectiles())
            {
                m_gameObjectManager.RemoveGameObject(proj);
            }
            
            m_gameObjectManager.Petrols.Clear();
            
            if (!ballr)
            {
                nokbool = !nokbool;
                RoundTicks = 0;
                _knockoutSmokeSpawnedThisRound = false;
                return;
            }
            
            Character ball = new(DataTables.Get(16).GetData<CharacterData>("LaserBall"));
            ball.SetPosition(3150, 4950, 0);
            ball.SetIndex(100);
            ball.TeamIndex = -1;
            ball.StopKicking();
            ball.m_maxHitpoints = 1;
            ball.m_hitpoints = 1;
            ball.CARRYABLETICK_CODEBYSTEAL_TG_THESTEALDEV = BRAWLBALL_PICKUP_DELAY_TICKS;
            m_gameObjectManager.AddGameObject(ball);
            Carryable = ball;

            _brawlBallLastX = 3150;
            _brawlBallLastY = 4950;
            _brawlBallSamePosTicks = 0;
            _brawlBallMissingTicks = 0;
            _brawlBallAirTicks = 0;
        }
        
        public void InstantRespawn(BattlePlayer player)
        {
            if (player == null || m_gameObjectManager == null) return;

            foreach (Character TG_THESTEALDEV in m_gameObjectManager.GetCharacters().ToList())
            {
                if (TG_THESTEALDEV.GetPlayer() != null && TG_THESTEALDEV.GetPlayer().PlayerIndex == player.PlayerIndex) 
                    m_gameObjectManager.RemoveGameObject(TG_THESTEALDEV);
            }
            
            Character character = SpawnHero(player.CharacterData, player.HeroPowerLevel, player.TeamIndex * 16 + player.PlayerIndex, 0, false);
            if (character == null)
            {
                Console.WriteLine($"[BattleMode] InstantRespawn failed: SpawnHero returned null for player {player.PlayerIndex}");
                return;
            }
            LogicVector2 spawnPoint = player.GetSpawnPoint();
            character.TriggerBlink(spawnPoint.X, spawnPoint.Y, null, null, 0, 0);
            character.SetBot(player.IsBot());
            character.SpawnTick = GetTicksGone();
            
            player.OwnObjectId = character.GetGlobalID();
            player.IsAlive = true;
        }
        
        public bool ResetCarryableState;
        
        public void ResetCarryable()
        {
            if (Carryable == null) return;
            
            Carryable.CARRi_CODEBYSTEAL_TG_THESTEALDEV = null;
            Carryable.CARRYABLETICK_CODEBYSTEAL_TG_THESTEALDEV = 0;
            Carryable.StopKicking();
            try { Carryable.CauseDamage(null, 99999, 99999, false, null, false, false, -1, -1, false, true); } catch (Exception) { }
            Carryable = null;
            
            foreach (Character cha in m_gameObjectManager.GetCharacters())
            {
                cha.CarryablePersonImunTicks = 0;
            }
        }
        
        private void EnsureBrawlBallNotStuck()
        {
            if (m_gameModeVariation != GAME_MODE_VARIATION_BRAWL_BALL) return;
            if (m_gameObjectManager == null || m_tileMap == null) return;

            if (ToGoalTeam != -1 || GoalTicks > 0) return;

            Character ball = Carryable;

            if (ball == null)
            {
                foreach (Character c in m_gameObjectManager.GetCharacters())
                {
                    if (c?.CharacterData != null && c.CharacterData.IsCarryable() && c.CharacterData.Name == "LaserBall")
                    {
                        ball = c;
                        break;
                    }
                }
                Carryable = ball;
            }

            if (ball == null)
            {
                _brawlBallMissingTicks++;
                _brawlBallAirTicks = 0;
                if (_brawlBallMissingTicks >= 20)
                {
                    RespawnBrawlBall();
                }
                return;
            }

            _brawlBallMissingTicks = 0;

            if (ball.CARRi_CODEBYSTEAL_TG_THESTEALDEV != null && !ball.CARRi_CODEBYSTEAL_TG_THESTEALDEV.IsAlive())
            {
                ball.CARRi_CODEBYSTEAL_TG_THESTEALDEV = null;
                ball.CARRYABLETICK_CODEBYSTEAL_TG_THESTEALDEV = 0;
            }

            int x = ball.GetX();
            int y = ball.GetY();

            if (ball.CARRi_CODEBYSTEAL_TG_THESTEALDEV == null && ball.GetZ() > 0)
            {
                _brawlBallAirTicks++;
                if (!ball.IsKicked || _brawlBallAirTicks >= 20 * 3)
                {
                    ball.StopKicking();
                    ball.SetPosition(x, y, 0);
                }
            }
            else
            {
                _brawlBallAirTicks = 0;
            }

            if (x < 0 || y < 0 || x > m_tileMap.LogicWidth || y > m_tileMap.LogicHeight)
            {
                RespawnBrawlBall();
                return;
            }

            if (ball.CARRi_CODEBYSTEAL_TG_THESTEALDEV == null)
            {
                if (x == _brawlBallLastX && y == _brawlBallLastY)
                {
                    _brawlBallSamePosTicks++;
                }
                else
                {
                    _brawlBallSamePosTicks = 0;
                    _brawlBallLastX = x;
                    _brawlBallLastY = y;
                }

                if (_brawlBallSamePosTicks >= 20 * 8)
                {
                    RespawnBrawlBall();
                }
            }
            else
            {
                _brawlBallSamePosTicks = 0;
                _brawlBallLastX = x;
                _brawlBallLastY = y;
                _brawlBallAirTicks = 0;
            }
        }

        private void RespawnBrawlBall()
        {
            try
            {
                ResetCarryable();
            }
            catch
            {
            }

            try
            {
                foreach (Character c in m_gameObjectManager.GetCharacters())
                {
                    if (c?.CharacterData != null && c.CharacterData.IsCarryable())
                    {
                        try { c.StopKicking(); } catch { }
                        m_gameObjectManager.RemoveGameObject(c);
                    }
                }
            }
            catch
            {
            }

            try
            {
                Character ball = new Character(DataTables.Get(16).GetData<CharacterData>("LaserBall"));
                ball.SetPosition(3150, 4950, 0);
                ball.SetIndex(100);
                ball.TeamIndex = -1;
                ball.StopKicking();
                ball.m_maxHitpoints = 1;
                ball.m_hitpoints = 1;
                ball.CARRYABLETICK_CODEBYSTEAL_TG_THESTEALDEV = BRAWLBALL_PICKUP_DELAY_TICKS;
                m_gameObjectManager.AddGameObject(ball);
                Carryable = ball;

                _brawlBallLastX = 3150;
                _brawlBallLastY = 4950;
                _brawlBallSamePosTicks = 0;
                _brawlBallMissingTicks = 0;
                _brawlBallAirTicks = 0;
            }
            catch
            {
            }
        }
        
        public bool HasEventModifier(int m) => EventModifiers.Contains(m);
        
        public void SetEventModifiers(List<int> ms)
        {
            EventModifiers = ms.ToList();
        }
        
        public int GetGemGrabCountdown()
        {
            return m_gemGrabCountdown;
        }
        
        public LogicRandom GetLogicRandom()
        {
            return m_random;
        }
        
        public GameObjectManager GetGameObjectManager()
        {
            return m_gameObjectManager;
        }
        
        public int GetPlayersAliveCountForBattleRoyale()
        {
            return m_playersAlive;
        }
        
        public Petrol AddPetrol(int a2, int a3, int a4, int a5, int a6, int a7)
        {
            Petrol v32 = new Petrol(a2, a3, a4, a5, a6, a7);
            m_gameObjectManager.Petrols.Add(v32);
            return v32;
        }
        
        private void TickSpawnHeroes()
        {
            foreach (BattlePlayer player in m_players)
            {
                if (player == null) continue;
                if (player.IsAlive) continue;
                if (m_gameModeVariation is 6 or 20 or 24) return;
                
                LogicVector2 spawnPoint = player.GetSpawnPoint();
                Random random = new Random();
                int randomindex = random.Next(0, 3);
                
                if (GetTicksGone() == player.DeathTick + 20 * GameModeUtil.GetRespawnSeconds(m_gameModeVariation))
                {
                    if (player.DeathTick != 1)
                    {
                        AreaEffect v90 = GameObjectFactory.CreateGameObjectByData(DataTables.GetAreaEffectByName("HeroSpawn"));
                        v90.SetPosition(spawnPoint.X, spawnPoint.Y, 0);
                        
                        if (m_gameModeVariation == 5)
                        {
                            if (player.TeamIndex == 0)
                            {
                                var respawnTiles = GetTileMap().LaserBallInstantRespawnTileTeam1;
                                if (respawnTiles != null && respawnTiles.Count > 0)
                                {
                                    randomindex = randomindex % respawnTiles.Count;
                                    v90.SetPosition(
                                        respawnTiles[randomindex].X + 150,
                                        respawnTiles[randomindex].Y + 150,
                                        0);
                                }
                            }
                            else if (player.TeamIndex == 1)
                            {
                                var respawnTiles = GetTileMap().LaserBallInstantRespawnTileTeam2;
                                if (respawnTiles != null && respawnTiles.Count > 0)
                                {
                                    randomindex = randomindex % respawnTiles.Count;
                                    v90.SetPosition(
                                        respawnTiles[randomindex].X + 150,
                                        respawnTiles[randomindex].Y + 150,
                                        0);
                                }
                            }
                        }

                        v90.SetIndex(player.TeamIndex * 16 + player.PlayerIndex);
                        m_gameObjectManager.AddGameObject(v90);
                        v90.Trigger();
                    }
                }
                
                if (GetTicksGone() == player.DeathTick + 20 * GameModeUtil.GetRespawnSeconds(m_gameModeVariation) + 40)
                {
                    Character character = SpawnHero(player.CharacterData, player.HeroPowerLevel, player.TeamIndex * 16 + player.PlayerIndex, 0, false);
                    if (character == null) continue;
                    character.SetPosition(spawnPoint.X, spawnPoint.Y, 0);
                    
                    if (m_gameModeVariation == 5)
                    {
                        if (player.TeamIndex == 0)
                        {
                            var respawnTiles = GetTileMap().LaserBallInstantRespawnTileTeam1;
                            if (respawnTiles != null && respawnTiles.Count > 0)
                            {
                                randomindex = randomindex % respawnTiles.Count;
                                character.SetPosition(
                                    respawnTiles[randomindex].X + 150,
                                    respawnTiles[randomindex].Y + 150,
                                    0);
                            }
                        }
                        else if (player.TeamIndex == 1)
                        {
                            var respawnTiles = GetTileMap().LaserBallInstantRespawnTileTeam2;
                            if (respawnTiles != null && respawnTiles.Count > 0)
                            {
                                randomindex = randomindex % respawnTiles.Count;
                                character.SetPosition(
                                    respawnTiles[randomindex].X + 150,
                                    respawnTiles[randomindex].Y + 150,
                                    0);
                            }
                        }
                    }
                    
                    character.SetBot(player.IsBot());
                    
                    if (player.Gear1 != null) character.Gear1 = new Gear("ConsumableShield");
                    if (player.Gear2 != null) character.Gear2 = null;

                    character.SpawnTick = GetTicksGone();
                    character.IsInvincible = true;
                    player.OwnObjectId = character.GetGlobalID();
                    player.IsAlive = true;
                }
            }
        }
        
        public Character SpawnHero(CharacterData a2, int a3, int a4, int a5, bool a6)
        {
            if (a2 == null)
            {
                Console.WriteLine("[BattleMode] SpawnHero: CharacterData is null");
                return null;
            }

            if (m_gameObjectManager == null)
            {
                Console.WriteLine("[BattleMode] SpawnHero: m_gameObjectManager is null");
                return null;
            }

            try
            {
                Character v9 = new Character(a2);
                m_gameObjectManager.AddGameObject(v9);
                if (a2.AreaEffect != null)
                {
                    v9.AddAreaEffect(0, 0, null, 1, false);
                }
                v9.SetIndex(a4);
                v9.SetUpgrades(a3);
                return v9;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BattleMode] SpawnHero: Exception - {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
        
        public void PlayerDied(BattlePlayer player)
        {
            if (player == null) return;
            try
            {
                player.Deaths += 1;
            }
            catch (Exception) { return; }

            try
            {
                try
                {
                    player.IsAlive = false;
                    player.DeathTick = GetTicksGone();
                }
                catch (Exception) { }

                if (m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT || m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT_2)
                {
                    if (player.TeamIndex == 0)
                    {
                        m_team0AliveCount--;
                        if (m_team0AliveCount < 0) m_team0AliveCount = 0;
                    }
                    else if (player.TeamIndex == 1)
                    {
                        m_team1AliveCount--;
                        if (m_team1AliveCount < 0) m_team1AliveCount = 0;
                    }
                    
                    Console.WriteLine($"[Knockout] Игрок {player.PlayerIndex} умер. Живых: Team0={m_team0AliveCount}, Team1={m_team1AliveCount}");
                }

                if (m_gameModeVariation == 6)
                {
                    try
                    {
                        int rank = m_playersAlive;
                        m_playersAlive--;
                        if (m_playersAlive < 0) m_playersAlive = 0;
                        if (rank <= 0)
                        {
                            rank = Math.Max(1, m_players.Count);
                        }

                        player.BattleRoyaleRank = rank;
                        player.IsAlive = false;

                        if (player.IsBot() == 0 && player.Avatar != null)
                        {
                            var home = player.Home;
                            if (home != null && home.BattleLogs != null)
                            {
                                home.BattleLogs.Add(BuildBattleLog(), 1, 0, GetTicksGone() / 20, !BattleWithTrophies, m_locationId, rank);
                            }

                            Hero hero = player.Avatar.GetHero(player.CharacterId);
                            if (hero != null && hero.Trophies > hero.HighestTrophies)
                                hero.HighestTrophies = hero.Trophies;
                            if (player.Avatar.Trophies > player.Avatar.HighestTrophies)
                                player.Avatar.HighestTrophies = player.Avatar.Trophies;

                            int tokensReward = rank > 0 ? 40 / rank : 0;
                            player.Avatar.AddTokens(tokensReward);
                            if (home != null) home.TokenReward += tokensReward;

                            if (BattleWithTrophies && hero != null)
                            {
                                if (rank > 5)
                                {
                                    int trophiesReward = -(rank - 5);
                                    if (hero.Trophies < -trophiesReward) trophiesReward = -hero.Trophies;
                                    hero.AddTrophies(trophiesReward);
                                }
                                else
                                {
                                    int trophiesReward = (5 - rank) * 2;
                                    hero.AddTrophies(trophiesReward);
                                    if (home != null) home.TrophiesReward += trophiesReward;
                                    if (rank <= 4)
                                    {
                                        player.Avatar.AddStarTokens(1);
                                        if (home != null) home.StarTokenReward += 1;
                                    }
                                }
                            }

                            player.Avatar.BattleId = -1;
                            if (player.GameListener != null)
                            {
                                try
                                {
                                    var safePlayers = new List<BattlePlayer> { player };
                                    BattleEndMessage message = new BattleEndMessage
                                    {
                                        GameMode = 2,
                                        IsPvP = BattleWithTrophies,
                                        BattleWithoutTrophies = !BattleWithTrophies,
                                        Players = safePlayers,
                                        OwnPlayer = player,
                                        Result = rank,
                                        TokensReward = tokensReward,
                                        TrophiesReward = BattleWithTrophies && hero != null ? (rank > 5 ? Math.Max(-(rank - 5), -hero.Trophies) : (5 - rank) * 2) : 0,
                                        StarToken = rank <= 4 && BattleWithTrophies,
                                        Winstreak = player.Avatar.WinStreak,
                                        MasteryPoints = hero?.MasteryPoints ?? 0,
                                        MasteryGained = 0
                                    };
                                    HomeMode homeMode = LogicServerListener.Instance?.GetHomeMode(player.AccountId);
                                    if (homeMode?.Home?.Quests != null && BattleWithTrophies)
                                        message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
                                    player.GameListener.SendTCPMessage(message);
                                }
                                catch (Exception ex)
                                {
#if DEBUG
                                    Console.WriteLine("[Showdown] BattleEndMessage send error: " + ex.Message);
#endif
                                }
                                finally
                                {
                                    player.GameListener = null;
                                    player.IsConnected = false;
                                    try { m_playersBySessionId?.Remove(player.SessionId); } catch { }
                                }
                            }
                            Console.WriteLine($"[Showdown] Игрок {player.PlayerIndex} выбыл с рангом {rank}. Осталось живых: {m_playersAlive}. BattleEnd отправлен.");
                        }
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Console.WriteLine("[Showdown] PlayerDied exception: " + ex.Message + " " + ex.StackTrace);
#endif
                    }
                }
            
                if (m_gameModeVariation == 25)
                {
                    int killedTeam = player.TeamIndex;
                    int enemyTeam = killedTeam == 0 ? 1 : 0;
                    bool scoreAdded = false;
                    foreach (BattlePlayer enemyPlayer in m_players)
                    {
                        if (enemyPlayer == null) continue;
                        if (enemyTeam == enemyPlayer.TeamIndex && !scoreAdded)
                        {
                            try { enemyPlayer.AddScore(1); scoreAdded = true; } catch (Exception) { }
                        }
                    }
                }

                if (m_gameModeVariation == 0)
                {
                    try { player.ResetScore(); } catch (Exception) { }
                }
            }
            catch (Exception)
            {
            }
        }
        
        public BattlePlayer GetPlayerWithObject(int globalId)
        {
            GameObject gameObject = m_gameObjectManager.GetGameObjectByID(globalId);
            if (gameObject == null) return null;
            return gameObject.GetPlayer();
        }

        public TileMap GetTileMap()
        {
            return m_tileMap;
        }

        public void Start()
        {
            m_updateTimer = new Timer(new TimerCallback(Update), null, 0, TICK_INTERVAL_MS);
        }

        public void Update(object stateInfo)
        {
            if (Interlocked.Exchange(ref _isExecutingTick, 1) == 1)
            {
                return;
            }

            try
            {
                this.ExecuteOneTick();
            }
            finally
            {
                Interlocked.Exchange(ref _isExecutingTick, 0);
            }
        }

        public BattlePlayer GetPlayer(int globalId)
        {
            GameObject gameObject = m_gameObjectManager.GetGameObjectByID(globalId);
            return gameObject?.GetPlayer();
        }

        public void ChangePlayerSessionId(long old, long newId)
        {
            if (m_playersBySessionId.ContainsKey(old))
            {
                BattlePlayer player = m_playersBySessionId[old];
                player.SessionId = newId;
                player.LastHandledInput = 0;
                m_playersBySessionId.Remove(old);
                m_playersBySessionId.Add(newId, player);
                Console.WriteLine($"[BattleMode] Changed SessionId for player {player.PlayerIndex}: {old} -> {newId}");
            }
            else
            {
                Console.WriteLine($"[BattleMode] WARNING: Cannot change SessionId - player with old SessionId {old} not found!");
                
                foreach (var player in m_players)
                {
                    if (player.SessionId == old || (player.AccountId > 0 && _disconnectedTicks.ContainsKey(player.AccountId)))
                    {
                        player.SessionId = newId;
                        player.LastHandledInput = 0;
                        m_playersBySessionId.Add(newId, player);
                        Console.WriteLine($"[BattleMode] Found and updated player {player.PlayerIndex} with new SessionId {newId}");
                        return;
                    }
                }
            }
        }

        public void AddSpectator(long sessionId, LogicGameListener gameListener)
        {
            m_spectators.Add(sessionId, gameListener);
        }
        
        public void ReconnectPlayer(long accountId, long newSessionId, LogicGameListener newListener)
        {
            try
            {
                BattlePlayer player = null;
                if (m_playersByAccountId != null && m_playersByAccountId.TryGetValue(accountId, out var p))
                {
                    player = p;
                }
                else
                {
                    player = m_players?.FirstOrDefault(pl => pl != null && pl.AccountId == accountId);
                    if (player != null && m_playersByAccountId != null)
                    {
                        m_playersByAccountId[accountId] = player;
                    }
                }
                if (player == null) return;

                if (player.SessionId != 0 && m_playersBySessionId != null)
                {
                    m_playersBySessionId.Remove(player.SessionId);
                }

                player.SessionId = newSessionId;
                if (m_playersBySessionId != null)
                {
                    m_playersBySessionId[newSessionId] = player;
                }

                player.GameListener = newListener;
                player.IsConnected = true;
                player.NeedsFullState = true;
                player.LastHandledInput = 0;
            }
            catch
            {
            }
        }

        public BattlePlayer GetPlayerBySessionId(long sessionId)
        {
            if (m_playersBySessionId.ContainsKey(sessionId))
            {
                return m_playersBySessionId[sessionId];
            }
            return null;
        }

        private BattlePlayer FindDisconnectedPlayerBySession(long sessionId)
        {
            return null;
        }
        
        private BattlePlayer FindDisconnectedPlayerByAccountId(long accountId)
        {
            foreach (var player in m_players)
            {
                if (player.AccountId == accountId && !player.IsConnected)
                {
                    return player;
                }
            }
            return null;
        }

        public void AddPlayer(BattlePlayer player, long sessionId)
        {
            if (player == null)
            {
                Console.WriteLine("LogicBattle::AddPlayer - player is NULL!");
                return;
            }

            if (m_players == null)
            {
                Console.WriteLine("LogicBattle::AddPlayer - m_players is NULL!");
                return;
            }

            if (m_playersBySessionId == null)
            {
                Console.WriteLine("LogicBattle::AddPlayer - m_playersBySessionId is NULL!");
                return;
            }

            if (m_playersByAccountId == null)
            {
                Console.WriteLine("LogicBattle::AddPlayer - m_playersByAccountId is NULL!");
                return;
            }

            try
            {
                player.SessionId = sessionId;
                player.IsConnected = true;
                m_players.Add(player);

                if (m_players.Count == 1 && m_gameModeVariation == 30 && StoryMode != null)
                {
                    StoryMode.Player = player;
                }

                if (sessionId > 0)
                {
                    if (!m_playersBySessionId.ContainsKey(sessionId))
                    {
                        m_playersBySessionId.Add(sessionId, player);
                    }
                    else
                    {
                        Console.WriteLine($"LogicBattle::AddPlayer - sessionId {sessionId} already exists in m_playersBySessionId!");
                        m_playersBySessionId[sessionId] = player;
                    }
                }

                if (player.AccountId > 0)
                {
                    m_playersByAccountId[player.AccountId] = player;
                }

                if (player.Avatar != null)
                {
                    player.Avatar.BattleId = Id;
                    player.Avatar.TeamIndex = player.TeamIndex;
                    player.Avatar.OwnIndex = player.PlayerIndex;
                }
                
                Console.WriteLine($"[BattleMode] Added player {player.PlayerIndex} (AccountId={player.AccountId}, SessionId={sessionId})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LogicBattle::AddPlayer - Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void DisconnectPlayer(BattlePlayer player)
        {
            if (player != null)
            {
                Console.WriteLine($"[BattleMode] DisconnectPlayer: Player {player.PlayerIndex} (AccountId={player.AccountId}) marked as disconnected (NOT REMOVED)");
                
                player.GameListener = null;
                player.IsConnected = false;
                
                long oldSessionId = player.SessionId;
                player.SessionId = 0;
                
                if (oldSessionId != 0 && m_playersBySessionId.ContainsKey(oldSessionId))
                {
                    m_playersBySessionId.Remove(oldSessionId);
                    Console.WriteLine($"[BattleMode] Removed session {oldSessionId} from m_playersBySessionId");
                }
                
                if (player.AccountId > 0)
                {
                    _disconnectedTicks[player.AccountId] = GetTicksGone();
                    Console.WriteLine($"[BattleMode] Player marked as disconnected at tick {GetTicksGone()}, can reconnect within {_reconnectTimeoutTicks/20} seconds");
                    
                    if (!m_playersByAccountId.ContainsKey(player.AccountId))
                    {
                        m_playersByAccountId[player.AccountId] = player;
                        Console.WriteLine($"[BattleMode] Added player back to m_playersByAccountId");
                    }
                }
                
                Console.WriteLine($"[BattleMode] Player {player.PlayerIndex} remains in battle (m_players count: {m_players.Count})");
            }
            else
            {
                Console.WriteLine("[BattleMode] DisconnectPlayer: player is NULL!");
            }
        }

        public void RemovePlayer(BattlePlayer player)
        {
            if (player == null)
            {
                return;
            }
            try
            {
                Console.WriteLine($"[BattleMode] RemovePlayer: Player {player.PlayerIndex} (AccountId={player.AccountId}) completely removed from battle");
                player.GameListener = null;
                player.IsConnected = false;
                try { m_playersBySessionId?.Remove(player.SessionId); } catch (Exception) { }
                if (player.AccountId > 0)
                {
                    try { m_playersByAccountId?.Remove(player.AccountId); } catch (Exception) { }
                    try { _disconnectedTicks?.Remove(player.AccountId); } catch (Exception) { }
                }
                try { m_players?.Remove(player); } catch (Exception) { }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[BattleMode] RemovePlayer error: {ex.Message}");
#endif
            }
        }
        
        public List<int> GetPlayerIndexesByTeam(int teamIndex)
        {
            List<int> teamCharacters = new List<int>();

            foreach (BattlePlayer player in m_players)
            {
                if (player != null && player.TeamIndex == teamIndex)
                {
                    teamCharacters.Add(player.PlayerIndex);
                }
            }

            return teamCharacters;
        }
        
                
        public void AddGameObjects()
        {
            m_playersAlive = m_players.Count;
            PlayerIndexesForTeam1 = GetPlayerIndexesByTeam(0);
            PlayerIndexesForTeam2 = GetPlayerIndexesByTeam(1);
            
            if (m_gameModeVariation == 19)
            {
                Team1King = PlayerIndexesForTeam1[new Random().Next(PlayerIndexesForTeam1.Count)];
                Team2King = PlayerIndexesForTeam2[new Random().Next(PlayerIndexesForTeam2.Count)];
            }
            
            int team1Indexer = 0;
            int team2Indexer = 0;
            
            if (IsStoryMode)
            {
                StoryModeStuffs = new List<GameObject>();
                foreach (BattlePlayer player in m_players)
                {
                    Character character = new Character(player.CharacterData);
                    character.SetIndex(player.PlayerIndex + (16 * player.TeamIndex));
                    character.SetHeroLevel(0);
                    character.Gear1 = new Gear(player.Gear1);
                    character.Gear2 = null;
                    character.SetPosition(3150, 4950, 0);

                    m_gameObjectManager.AddGameObject(character);
                    player.OwnObjectId = character.GetGlobalID();
                    StoryModeStuffs.Add(character);
                }
                return;
            }
            
            // ========== НОКАУТ (ID 20) - ЧИСТЫЙ НОКАУТ БЕЗ МЯЧА ==========
            if (m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT)
            {
                Console.WriteLine("[Knockout] Запуск ЧИСТОГО нокаут режима - БЕЗ МЯЧА! Только бой до последнего!");
                
                // НОКАУТ: НИКАКОГО МЯЧА!
                Carryable = null;
                
                // Инициализируем счетчики живых игроков
                m_team0AliveCount = 0;
                m_team1AliveCount = 0;
                
                // Создаем персонажей для всех игроков
                foreach (BattlePlayer player in m_players)
                {
                    Character character = SpawnHero(player.CharacterData, player.HeroPowerLevel, player.TeamIndex * 16 + player.PlayerIndex, 0, false);
                    player.IsAlive = true;
                    character.SetBot(player.IsBot());
                    
                    if (player.Gear1 != null) character.Gear1 = new Gear("ConsumableShield");
                    if (player.Gear2 != null) character.Gear2 = null;

                    // Спавн на точках респавна
                    if (player.TeamIndex == 0)
                    {
                        if (m_tileMap.SpawnPointsTeam1 != null && m_tileMap.SpawnPointsTeam1.Count > 0)
                        {
                            Tile tile = m_tileMap.SpawnPointsTeam1[team1Indexer % m_tileMap.SpawnPointsTeam1.Count];
                            team1Indexer++;
                            character.SetPosition(tile.X + 150, tile.Y + 150, 0);
                            player.SetSpawnPoint(tile.X + 150, tile.Y + 150);
                            m_team0AliveCount++;
                        }
                        else
                        {
                            int fallbackX = 1500;
                            int fallbackY = 1500;
                            character.SetPosition(fallbackX, fallbackY, 0);
                            player.SetSpawnPoint(fallbackX, fallbackY);
                            m_team0AliveCount++;
                            Console.WriteLine($"[WARNING] No spawn points for Team1! Using fallback position: ({fallbackX}, {fallbackY})");
                        }
                    }
                    else
                    {
                        if (m_tileMap.SpawnPointsTeam2 != null && m_tileMap.SpawnPointsTeam2.Count > 0)
                        {
                            Tile tile = m_tileMap.SpawnPointsTeam2[team2Indexer % m_tileMap.SpawnPointsTeam2.Count];
                            team2Indexer++;
                            character.SetPosition(tile.X + 150, tile.Y + 150, 0);
                            player.SetSpawnPoint(tile.X + 150, tile.Y + 150);
                            m_team1AliveCount++;
                        }
                        else
                        {
                            int fallbackX = m_tileMap.LogicWidth - 1500;
                            int fallbackY = m_tileMap.LogicHeight - 1500;
                            character.SetPosition(fallbackX, fallbackY, 0);
                            player.SetSpawnPoint(fallbackX, fallbackY);
                            m_team1AliveCount++;
                            Console.WriteLine($"[WARNING] No spawn points for Team2! Using fallback position: ({fallbackX}, {fallbackY})");
                        }
                    }

                    player.OwnObjectId = character.GetGlobalID();
                    character.SetHeroLevel(player.HeroPowerLevel + 10);
                }
                
                Console.WriteLine($"[Knockout] Инициализация завершена. Живых: Team0={m_team0AliveCount}, Team1={m_team1AliveCount}");
                
                // Сбрасываем счет раундов
                TScore = new List<int> { 0, 0 };
                RoundCount = 0;
                RoundWins = new int[] { 0, 0, 0 };
                nokbool = false;
                KnockoutTicks = 0;
                m_knockoutRound = 1;
                m_knockoutRoundWins = new int[2] { 0, 0 };
                
                return; // ВАЖНО: выходим, чтобы не создавать другие объекты
            }
            
            // ========== ДРУГИЕ РЕЖИМЫ ==========
            
            // Обычный код для других режимов (создание персонажей)
            foreach (BattlePlayer player in m_players)
            {
                Character character = SpawnHero(player.CharacterData, player.HeroPowerLevel, player.TeamIndex * 16 + player.PlayerIndex, 0, false);
                player.IsAlive = true;
                character.SetBot(player.IsBot());
                
                if (player.Gear1 != null) character.Gear1 = new Gear("ConsumableShield");
                if (player.Gear2 != null) character.Gear2 = null;

                if (GameModeUtil.HasTwoTeams(m_gameModeVariation) || m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT)
                {
                    if (player.TeamIndex == 0)
                    {
                        if (m_tileMap.SpawnPointsTeam1 != null && m_tileMap.SpawnPointsTeam1.Count > 0)
                        {
                            Tile tile = m_tileMap.SpawnPointsTeam1[team1Indexer % m_tileMap.SpawnPointsTeam1.Count];
                            team1Indexer++;
                            character.SetPosition(tile.X + 150, tile.Y + 150, 0);
                            player.SetSpawnPoint(tile.X + 150, tile.Y + 150);
                        }
                        else
                        {
                            int fallbackX = 1500;
                            int fallbackY = 1500;
                            character.SetPosition(fallbackX, fallbackY, 0);
                            player.SetSpawnPoint(fallbackX, fallbackY);
                            Console.WriteLine($"[WARNING] No spawn points for Team1! Using fallback position: ({fallbackX}, {fallbackY})");
                        }
                    }
                    else
                    {
                        if (m_tileMap.SpawnPointsTeam2 != null && m_tileMap.SpawnPointsTeam2.Count > 0)
                        {
                            Tile tile = m_tileMap.SpawnPointsTeam2[team2Indexer % m_tileMap.SpawnPointsTeam2.Count];
                            team2Indexer++;
                            character.SetPosition(tile.X + 150, tile.Y + 150, 0);
                            player.SetSpawnPoint(tile.X + 150, tile.Y + 150);
                        }
                        else
                        {
                            int fallbackX = m_tileMap.LogicWidth - 1500;
                            int fallbackY = m_tileMap.LogicHeight - 1500;
                            character.SetPosition(fallbackX, fallbackY, 0);
                            player.SetSpawnPoint(fallbackX, fallbackY);
                            Console.WriteLine($"[WARNING] No spawn points for Team2! Using fallback position: ({fallbackX}, {fallbackY})");
                        }
                    }
                }
                else
                {
                    if (m_tileMap.SpawnPointsTeam1 != null && m_tileMap.SpawnPointsTeam1.Count > 0)
                    {
                        Tile tile = m_tileMap.SpawnPointsTeam1[team1Indexer % m_tileMap.SpawnPointsTeam1.Count];
                        team1Indexer++;
                        character.SetPosition(tile.X + 150, tile.Y + 150, 0);
                        player.SetSpawnPoint(tile.X + 150, tile.Y + 150);
                    }
                    else
                    {
                        int fallbackX = m_tileMap.LogicWidth / 2;
                        int fallbackY = m_tileMap.LogicHeight / 2;
                        character.SetPosition(fallbackX, fallbackY, 0);
                        player.SetSpawnPoint(fallbackX, fallbackY);
                        Console.WriteLine($"[WARNING] No spawn points available! Using center position: ({fallbackX}, {fallbackY})");
                    }
                }

                player.OwnObjectId = character.GetGlobalID();
                character.SetHeroLevel(player.HeroPowerLevel + 10);

                if (m_gameModeVariation == 19)
                {
                    if (player.PlayerIndex == Team2King) character.SetKing();
                    if (player.PlayerIndex == Team1King) character.SetKing();
                }
            }

            // Спавн объектов для разных режимов
            if (m_gameModeVariation == 0)
            {
                Item item = new Item(DataTables.GetItemByName("OrbSpawner"));
                item.SetPosition(3150, 4950, 0);
                item.DisableAppearAnimation();
                m_gameObjectManager.AddGameObject(item);
            }

            if (m_gameModeVariation == 3)
            {
                ItemData data = DataTables.Get(18).GetData<ItemData>("Money");
                Item item = new Item(data);
                item.SetPosition(3150, 4950, 0);
                item.DisableAppearAnimation();
                m_gameObjectManager.AddGameObject(item);
            }
            
            // ========== ГОРЯЧАЯ ЗОНА (ID 17) ==========
            if (m_gameModeVariation == GAME_MODE_VARIATION_HOT_ZONE)
            {
                Console.WriteLine("[HotZone] Инициализация горячей зоны!");
                
                // ОЧИЩАЕМ ВСЁ!
                _koHStates.Clear();
                Zone1Score = 0;
                Zone2Score = 0;
                
                // ТОЛЬКО ЗОНЫ, БЕЗ МАРКЕРОВ!
                int[][] zonePositions = new int[][]
                {
                    new int[] { 3150, 4950 }, // Центр
          ///////          new int[] { 1150, 2950 }, // Лево-верх
      /////////              new int[] { 5150, 6950 }, // Право-низ
                };
                
                for (int i = 0; i < zonePositions.Length; i++)
                {
                    int posX = zonePositions[i][0];
                    int posY = zonePositions[i][1];
                    
                    // СОЗДАЕМ ТОЛЬКО ЗОНУ, БЕЗ МАРКЕРА
                    AreaEffect zoneArea = GameObjectFactory.CreateGameObjectByData(DataTables.GetAreaEffectByName("KingOfHillArea"));
                    if (zoneArea != null)
                    {
                        zoneArea.SetPosition(posX, posY, 0);
                        zoneArea.SetIndex(16);
                        zoneArea.KOHUID = i;
                        
                        _koHStates.Add(new KingOfHillState(i));
                        m_gameObjectManager.AddGameObject(zoneArea);
                        zoneArea.Trigger();
                        
                        Console.WriteLine($"[HotZone] Зона {i} создана на ({posX}, {posY})");
                    }
                    else
                    {
                        Console.WriteLine($"[HotZone] ОШИБКА: Эффект KingOfHillArea не найден!");
                        
                        // Если нет эффекта - просто добавляем состояние зоны
                        _koHStates.Add(new KingOfHillState(i));
                    }
                }
                
                Console.WriteLine($"[HotZone] Готово! Создано зон: {_koHStates.Count}");
            }

            if (m_gameModeVariation == GAME_MODE_VARIATION_BRAWL_BALL)
            {
                Character ball = new Character(DataTables.Get(16).GetData<CharacterData>("LaserBall"));
                ball.SetPosition(3150, 4950, 0);
                ball.SetIndex(100);
                ball.TeamIndex = -1;
                
                m_gameObjectManager.AddGameObject(ball);
                Carryable = ball;
                
                Console.WriteLine($"[BrawlBall] Brawl Ball mode initialized!");
            }

            if (m_gameModeVariation == 6)
            {
                CharacterData data = DataTables.Get(16).GetData<CharacterData>("LootBox");
                for (int i = 0; i < m_tileMap.Height; i++)
                {
                    for (int j = 0; j < m_tileMap.Width; j++)
                    {
                        Tile tile = m_tileMap.GetTile(i, j, true);
                        if (tile.Code == '4')
                        {
                            bool shouldSpawnBox = GetRandomInt(0, 120) < 60;

                            if (shouldSpawnBox)
                            {
                                Character box = new Character(data);
                                box.SetPosition(tile.X + 150, tile.Y + 150, 0);
                                box.SetIndex(165);
                                m_gameObjectManager.AddGameObject(box);
                            }
                        }
                    }
                }
            }
            
            if (m_gameModeVariation == 7) { }
            
            if (m_gameModeVariation == 8)
            {
                CharacterData characterData = DataTables.Get(16).GetData<CharacterData>("FleaPet");
                Character boss = new Character(characterData);
                boss.SetPosition(10000, 10000, 0);
                boss.SetIndex(32);
                m_gameObjectManager.AddGameObject(boss);
            }
            
            if (m_gameModeVariation == 2)
            {
                CharacterData b = DataTables.GetCharacterByName("Safe");
                Character base1 = new Character(b);
                base1.SetPosition(m_tileMap.SpawnPointsBases[0].X + 150, m_tileMap.SpawnPointsBases[0].Y + 150, 0);
                base1.SetIndex(22);
                base1.TeamIndex = 1;
                m_gameObjectManager.AddGameObject(base1);
                Character base2 = new Character(b);
                base2.SetPosition(m_tileMap.SpawnPointsBases[1].X + 150, m_tileMap.SpawnPointsBases[1].Y + 150, 0);
                base2.SetIndex(6);
                base2.TeamIndex = 0;
                m_gameObjectManager.AddGameObject(base2);
            }
            
            if (m_gameModeVariation == 33)
            {
                Item item = new Item(DataTables.GetItemByName("OrbSpawner"));
                item.SetPosition(3150, 4950, 0);
                item.DisableAppearAnimation();
                m_gameObjectManager.AddGameObject(item);
            }
            
            // ========== ТРЕНИРОВКА (ID 13) ==========
            if (m_gameModeVariation == 13)
            {
                Console.WriteLine("[Training] Инициализация тренировочной пещеры");
                
                // ЛОГ: Кто зашел и на каком бойце
                foreach (BattlePlayer player in m_players)
                {
                    string brawlerName = player.CharacterData?.Name ?? "Unknown";
                    int brawlerID = player.CharacterId; // АЙДИ БОЙЦА
                    int powerLevel = player.HeroPowerLevel;
                    string playerName = player.Avatar?.Name ?? $"Player {player.PlayerIndex}";
                    
                    Console.WriteLine($"[TRAINING] ИГРОК: {playerName} | БОЕЦ: {brawlerName} | ID БОЙЦА: {brawlerID} | УРОВЕНЬ: {powerLevel}");
                }
                
                Console.WriteLine("[Training] Инициализация тренировочной пещеры");
                
                // СОЗДАЕМ ТОЛЬКО МАНЕКЕНЫ, НЕ СОЗДАЕМ ПЕРСОНАЖА!
                
                // Большой манекен
                Character TrainingDummyBig = new Character(DataTables.Get(16).GetData<CharacterData>("TrainingDummyBig"));
                TrainingDummyBig.SetPosition(m_tileMap.TrainingDummyBigSpawners[0].X + 150, m_tileMap.TrainingDummyBigSpawners[0].Y + 150, 0);
                TrainingDummyBig.SetIndex(-16);
                m_gameObjectManager.AddGameObject(TrainingDummyBig);
                
                // Маленькие манекены
                for (int i = 0; i < m_tileMap.TrainingDummySmallSpawners.Count; i++)
                {
                    Character TrainingDummySmall = new Character(DataTables.Get(16).GetData<CharacterData>("TrainingDummySmall"));
                    Tile tile = m_tileMap.TrainingDummySmallSpawners[i];
                    TrainingDummySmall.SetPosition(tile.X + 150, tile.Y + 150, 0);
                    TrainingDummySmall.SetIndex(-16);
                    m_gameObjectManager.AddGameObject(TrainingDummySmall);
                }
                
                // Стреляющие манекены
                for (int i = 0; i < m_tileMap.TrainingDummyShooting.Count; i++)
                {
                    Character TrainingDummyShooting = new Character(DataTables.Get(16).GetData<CharacterData>("TrainingDummyShooting"));
                    Tile spawner = m_tileMap.TrainingDummyShooting[i];
                    TrainingDummyShooting.SetPosition(spawner.X + 150, spawner.Y + 150, 0);
                    TrainingDummyShooting.SetIndex(-16);
                    m_gameObjectManager.AddGameObject(TrainingDummyShooting);
                }
                
                // Средние манекены
                if (m_tileMap.TrainingDummyMediumSpawners.Count >= 4)
                {
                    Character TrainingDummyMedium = new Character(DataTables.Get(16).GetData<CharacterData>("TrainingDummyMedium"));
                    TrainingDummyMedium.SetPosition(m_tileMap.TrainingDummyMediumSpawners[0].X + 150, m_tileMap.TrainingDummyMediumSpawners[0].Y + 150, 0);
                    TrainingDummyMedium.SetIndex(-16);
                    m_gameObjectManager.AddGameObject(TrainingDummyMedium);
                    
                    Character TrainingDummyMedium1 = new Character(DataTables.Get(16).GetData<CharacterData>("TrainingDummyMedium"));
                    TrainingDummyMedium1.SetPosition(m_tileMap.TrainingDummyMediumSpawners[1].X + 150, m_tileMap.TrainingDummyMediumSpawners[1].Y + 150, 0);
                    TrainingDummyMedium1.SetIndex(-16);
                    m_gameObjectManager.AddGameObject(TrainingDummyMedium1);
                    
                    Character TrainingDummyMedium2 = new Character(DataTables.Get(16).GetData<CharacterData>("TrainingDummyMedium"));
                    TrainingDummyMedium2.SetPosition(m_tileMap.TrainingDummyMediumSpawners[2].X + 150, m_tileMap.TrainingDummyMediumSpawners[2].Y + 150, 0);
                    TrainingDummyMedium2.SetIndex(-16);
                    m_gameObjectManager.AddGameObject(TrainingDummyMedium2);
                    
                    Character TrainingDummyMedium3 = new Character(DataTables.Get(16).GetData<CharacterData>("TrainingDummyMedium"));
                    TrainingDummyMedium3.SetPosition(m_tileMap.TrainingDummyMediumSpawners[3].X + 150, m_tileMap.TrainingDummyMediumSpawners[3].Y + 150, 0);
                    TrainingDummyMedium3.SetIndex(-16);
                    m_gameObjectManager.AddGameObject(TrainingDummyMedium3);
                }
                
                Console.WriteLine("[Training] Манекены созданы, персонаж будет создан в общем коде");
                // НЕТ RETURN! ПРОДОЛЖАЕМ ДАЛЬШЕ!
            }
            if (m_gameModeVariation == 30)
            {
                StoryMode.AddGameObjects();
            }
        }
        
        // ========== МЕТОД ДЛЯ ГОРЯЧЕЙ ЗОНЫ ==========
        private void TickKingOfHill()
        {
            if (m_gameModeVariation != GAME_MODE_VARIATION_HOT_ZONE) return;
            if (_koHStates == null || _koHStates.Count == 0) return;
            
            int currentTick = GetTicksGone();
            
            for (int i = 0; i < _koHStates.Count; i++)
            {
                KingOfHillState zone = _koHStates[i];
                if (zone == null) continue;
                
                AreaEffect zoneArea = null;
                
                // Находим объект зоны
                foreach (AreaEffect area in m_gameObjectManager.GetAreaEffects())
                {
                    if (area.KOHUID == i)
                    {
                        zoneArea = area;
                        break;
                    }
                }
                
                if (zoneArea == null) continue;
                
                // Считаем игроков в зоне
                int playersInZoneTeam0 = 0;
                int playersInZoneTeam1 = 0;
                
                foreach (Character character in m_gameObjectManager.GetCharacters())
                {
                    if (!character.IsAlive() || character.GetPlayer() == null) continue;
                    
                    // ПРОСТАЯ МАТЕМАТИКА - 100% РАБОТАЕТ
                    int deltaX = character.GetX() - zoneArea.GetX();
                    int deltaY = character.GetY() - zoneArea.GetY();
                    int distanceSquared = deltaX * deltaX + deltaY * deltaY;
                        
                    if (distanceSquared < 250000) // 500^2 = 250000
                    {
                        if (character.GetPlayer().TeamIndex == 0)
                            playersInZoneTeam0++;
                        else
                            playersInZoneTeam1++;
                    }
                }
                
                // Логика захвата
                if (playersInZoneTeam0 > 0 && playersInZoneTeam1 == 0)
                {
                    // Только команда 0
                    zone.ZonePercentageTeam0 += 1;
                    
                    if (zone.ZonePercentageTeam0 >= 100)
                    {
                        Zone1Score += 1;
                        zone.ZonePercentageTeam0 = 0;
                        Console.WriteLine($"[HotZone] Team 0 захватила зону {i}! Счет: {Zone1Score}");
                        
                        // Эффект захвата
                        AreaEffect captureEffect = new AreaEffect(DataTables.GetAreaEffectByName("KingOfHillAreaCapture"));
                        captureEffect.SetPosition(zoneArea.GetX(), zoneArea.GetY(), 0);
                        m_gameObjectManager.AddGameObject(captureEffect);
                        captureEffect.Trigger();
                    }
                }
                else if (playersInZoneTeam1 > 0 && playersInZoneTeam0 == 0)
                {
                    // Только команда 1
                    zone.ZonePercentageTeam1 += 1;
                    
                    if (zone.ZonePercentageTeam1 >= 100)
                    {
                        Zone2Score += 1;
                        zone.ZonePercentageTeam1 = 0;
                        Console.WriteLine($"[HotZone] Team 1 захватила зону {i}! Счет: {Zone2Score}");
                        
                        AreaEffect captureEffect = new AreaEffect(DataTables.GetAreaEffectByName("KingOfHillAreaCapture"));
                        captureEffect.SetPosition(zoneArea.GetX(), zoneArea.GetY(), 0);
                        m_gameObjectManager.AddGameObject(captureEffect);
                        captureEffect.Trigger();
                    }
                }
            }
        }
        
        

        public void RemoveSpectator(long id)
        {
            if (m_spectators.ContainsKey(id))
            {
                m_spectators.Remove(id);
            }
        }

        public bool IsInPlayArea(int x, int y)
        {
            return m_playArea.IsInside(x + 150, y + 150);
        }

        public int GetTeamPlayersCount(int teamIndex)
        {
            int result = 0;
            foreach (BattlePlayer player in GetPlayers())
            {
                if (player.TeamIndex == teamIndex) result++;
            }
            return result;
        }

        public void AddClientInput(ClientInput input, long sessionId)
        {
            if (input == null)
            {
                Console.WriteLine($"[BattleMode] AddClientInput: input is null for session {sessionId}");
                return;
            }

            if (IsGameOver && m_postGameInputTypes.Add(1000 + input.Type))
                Console.WriteLine($"[BattleMode] После конца боя {Id} пришёл ввод типа {input.Type} (emote {input.EmoteIndex}, x {input.X}, y {input.Y})");

            BattlePlayer player = GetPlayerBySessionId(sessionId);
            
            if (player == null && m_players.Count > 0)
            {
                player = m_players[0];
                player.SessionId = sessionId;
                player.IsConnected = true;
                player.NeedsFullState = true;
                m_playersBySessionId[sessionId] = player;
                Console.WriteLine($"[BattleMode] Assigned session {sessionId} to player {player.PlayerIndex}");
            }

            if (player == null)
            {
                Console.WriteLine($"[BattleMode] AddClientInput: NO PLAYER FOUND for session {sessionId}");
                return;
            }

            if (!player.IsConnected)
            {
                Console.WriteLine($"[BattleMode] Player {player.PlayerIndex} is not marked as connected, marking as connected");
                player.IsConnected = true;
                player.NeedsFullState = true;
            }

            input.OwnerSessionId = sessionId;
            m_inputQueue.Enqueue(input);
        }

        public void HandleSpectatorInput(ClientInput input, long sessionId)
        {
            if (input == null)
            {
                Console.WriteLine($"[BattleMode] HandleSpectatorInput: input is null for session {sessionId}");
                return;
            }

            if (m_spectators == null)
            {
                Console.WriteLine($"[BattleMode] HandleSpectatorInput: m_spectators is null");
                return;
            }

            if (!m_spectators.ContainsKey(sessionId))
            {
                Console.WriteLine($"[BattleMode] HandleSpectatorInput: session {sessionId} not found in spectators");
                return;
            }

            var spectator = m_spectators[sessionId];
            if (spectator == null)
            {
                Console.WriteLine($"[BattleMode] HandleSpectatorInput: spectator is null for session {sessionId}");
                return;
            }

            spectator.HandledInputs = input.Index;
        }

        private void HandleClientInput(ClientInput input)
        {
            if (input == null)
            {
                Console.WriteLine("[BattleMode] HandleClientInput: input is null");
                return;
            }

            // После конца боя (экран результатов) обрабатываем только реакции; каждый новый тип ввода пишем в лог один раз
            if (IsGameOver)
            {
                if (m_postGameInputTypes.Add(input.Type))
                    Console.WriteLine($"[BattleMode] Post-game input type {input.Type} (emote {input.EmoteIndex}, x {input.X}, y {input.Y}) in battle {Id}");
                if (input.Type != 9 && input.Type != 17) return;
                BattlePlayer reacting = GetPlayerBySessionId(input.OwnerSessionId);
                if (reacting == null || reacting.LastHandledInput >= input.Index) return;
                reacting.LastHandledInput = input.Index;
                if (input.Type == 9) reacting.UsePin(input.EmoteIndex, GetTicksGone());
                else reacting.Likes = Math.Min(3, reacting.Likes + 1);   // "палец вверх" на экране результатов
                return;
            }

            BattlePlayer player = GetPlayerBySessionId(input.OwnerSessionId);

            if (player == null)
            {
                Console.WriteLine($"[BattleMode] HandleClientInput: player is null for session {input.OwnerSessionId}");
                return;
            }
            if (player.LastHandledInput >= input.Index) return;

            player.LastHandledInput = input.Index;

            if (m_gameObjectManager == null)
            {
                Console.WriteLine("[BattleMode] HandleClientInput: m_gameObjectManager is null");
                return;
            }

            Character character1 = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
            if ((character1 == null || !character1.IsAlive()) && input.Type != 4 && input.Type != 9) return;
            if (character1 != null && character1.ViusalChargeType > 0 && input.Type >= 2 && input.Type <= 8) return;
            if (input.Type < 8 && input.Type != 4 && StoryMode != null && StoryMode.Intangible) return;
            if (input.Type != 0 && StoryMode != null && StoryMode.Choosing) return;
            if (IsRoundActionLocked() && input.Type != 4 && input.Type != 9) return;
            
            switch (input.Type)
            {
                case 0:
                    {
                        if (StoryMode != null && StoryMode.Choosing)
                        {
                            StoryMode.ChoiceChosen(input.X, input.Y);
                            return;
                        }

                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (character == null) return;
                        
                        Skill skill = character.GetWeaponSkill();
                        if (skill == null) return;

                        character.UltiDisabled();
                        if (input.AutoAttack)
                        {
                            Character target = (Character)m_gameObjectManager.GetGameObjectByID(input.AutoAttackTarget);
                            if (target != null)
                            {
                                input.X = target.GetX();
                                input.Y = target.GetY();
                            }
                            else
                            {
                                target = character.GetClosestVisibleEnemy();
                                if (target != null)
                                {
                                    input.X = target.GetX();
                                    input.Y = target.GetY();
                                }
                            }
                            character.ActivateSkill(0, input.X, input.Y);
                            break;
                        }
                        if (!skill.SkillData.IsPositionalTargeted())
                        {
                            character.ActivateSkill(0, input.X + character.GetX(), input.Y + character.GetY());
                        }
                        else character.ActivateSkill(0, input.X, input.Y);

                        var counter = character1.GetFadeCounter();
                        character1.IncrementFadeCounter();
                        character.SetFadeCounter(counter);

                        character1.ResetAFKTicks();
                        break;
                    }
                case 1:
                    {
                        if (character1.IsControlled) return;
                        
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);

                        if (character == null) return;
                        character.m_skills[1].SeemToBeActive = false;

                        if (character.DisplayUltiTmp) return;
                        Skill skill = character.GetUltimateSkill();
                        if (skill == null) return;
                        character?.UltiDisabled();
                        if (!player.HasUlti()) return;
                        
                        if (character.CharacterData != null && character.CharacterData.Name.Contains("Dynamike"))
                        {
                            if (Carryable != null)
                            {
                                ResetCarryable();
                            }
                        }
                        
                        character.UltiEnabled();
                        skill.IsFromAutoAttack = input.AutoAttack;
                        if (input.AutoAttack && !skill.SkillData.MovementBasedAutoshoot)
                        {
                            Character target = (Character)m_gameObjectManager.GetGameObjectByID(input.AutoAttackTarget);
                            if (target != null)
                            {
                                input.X = target.GetX();
                                input.Y = target.GetY();
                            }
                            else
                            {
                                target = character.GetClosestVisibleEnemy();
                                if (target != null)
                                {
                                    input.X = target.GetX();
                                    input.Y = target.GetY();
                                }
                            }
                            character.ActivateSkill(1, input.X, input.Y);
                            break;
                        }
                        if (input.AutoAttack && skill.SkillData.MovementBasedAutoshoot)
                        {
                            character.ActivateSkill(1, input.X, input.Y);
                            break;
                        }
                        if (!skill.SkillData.IsPositionalTargeted())
                        {
                            character.ActivateSkill(1, input.X + character.GetX(), input.Y + character.GetY());
                        }
                        else character.ActivateSkill(1, input.X, input.Y);
                        var counter = character1.GetFadeCounter();
                        character1.IncrementFadeCounter();
                        character.SetFadeCounter(counter);

                        character1.ResetAFKTicks();
                        break;
                    }
                case 2:
                    {
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);

                        if (character == null) return;

                        LogicVector2 old = new LogicVector2(character.GetX(), character.GetY());
                        LogicVector2 destin = new LogicVector2(input.X, input.Y);
                        int v15 = old.GetDistanceSquaredTo(input.X, input.Y);
                        
                        character.MoveTo(0, input.X, input.Y, 0, 0, 0, 0);

                        character1.ResetAFKTicks();
                        break;
                    }
                case 4:
                        try { SendBattleEndToPlayer(player); } catch (Exception) { }
                        try { RemovePlayer(player); } catch (Exception) { }
                        if (m_gameModeVariation == 13)
                        {
                            this.IsGameOver = true;
                            try { this.m_updateTimer?.Dispose(); } catch (Exception) { }
                        }
                break;
                case 5:
                    {
                        if (character1.IsControlled) return;
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        character?.UltiEnabled();
                        character1.ResetAFKTicks();
                        break;
                    }
                case 6:
                    {
                        if (character1.IsControlled) return;
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        character?.UltiDisabled();
                        character1.ResetAFKTicks();
                        break;
                    }
                case 7:
                    {
                        break;
                    }
                case 8:
                    {
                        if (StoryMode != null && StoryMode.StartWaitingTick > GetTicksGone())
                        {
                            StoryMode.DoorTest ^= true;
                            StoryMode.WaitSkipped = true;
                            return;
                        }
                        if (character1.IsControlled) return;
                        Accessory accessory = player.Accessory;
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (accessory != null) accessory.TriggerAccessory(character, input.X, input.Y);
                        character1.ResetAFKTicks();
                        break;
                    }
                case 9:
                    {
                        // Пин может прийти и от игрока без персонажа (погиб, бой закончен) - раньше здесь была ошибка
                        if (character1 != null && character1.IsControlled) return;
                        player.UsePin(input.EmoteIndex, GetTicksGone());
                        if (player.LastPinUseTicks + 200 < GetTicksGone()) player.UsePin(input.EmoteIndex, GetTicksGone());
                        character1?.ResetAFKTicks();
                        break;
                    }
                case 10:
                    {
                        if (character1.IsSteerMovementActive())
                        {
                            if ((input.X | input.Y) < 0) character1.SteerAngle = -1;
                            else
                                character1.SteerAngle = LogicMath.GetAngle(-input.X + character1.GetX(), -input.Y + character1.GetY());
                            return;
                        }
                        Projectile projectile = character1.GetControlledProjectile();
                        if (projectile == null) return;
                        int v26;
                        if ((input.X | input.Y) < 0) v26 = -1;
                        else
                        {
                            v26 = LogicMath.GetAngle(input.X - projectile.GetX(), input.Y - projectile.GetY());
                        }
                        projectile.SteerAngle = v26;
                        character1.ResetAFKTicks();
                        break;
                    }
                case 13:
                    input.X = input.X * 150 / 100;
                    if (character1.GetSkillHoldedTicks() <= 0)
                    {
                        character1.HoldSkillStarted();
                        if (character1.m_skills[0].SkillData.AttackPattern == 13) break;
                        AreaEffect areaEffect = GameObjectFactory.CreateGameObjectByData(DataTables.GetAreaEffectByName("Tara005UltiSuck"));
                        areaEffect.SetIndex(character1.GetIndex());
                        areaEffect.SetPosition(character1.GetX(), character1.GetY(), 0);
                        m_gameObjectManager.AddGameObject(areaEffect);
                    }
                    if (character1.m_skills[0].SkillData.AttackPattern == 13) break;
                    character1.SetForcedAngle(LogicMath.NormalizeAngle360(LogicMath.GetAngle(input.X, input.Y) - character1.GetIndex() / 16 * 180));
                    character1.SetVisionOverride(input.X + character1.GetX(), input.Y + character1.GetY(), 2);
                    character1.StopMovement();
                    character1.SetForcedVisible();
                    character1.BlockHealthRegen();
                    character1.m_skills[1].SeemToBeActive = true;
                    character1.ResetAFKTicks();
                    break;
                case 14:
                    character1.SkillHoldTicks = -1;
                    character1.m_skills[1].SeemToBeActive = false;
                    character1.ResetAFKTicks();
                    break;
                case 15:
                    Item Spray = new Item(DataTables.GetItemByName("Spray"));
                    if (input.SprayIndex < 0) { Console.WriteLine("input index dissmatch"); return;}
                    player.SprayIndex = input.SprayIndex;
                    Console.WriteLine($"index: {input.SprayIndex}");
                    Spray.SprayData = DataTables.Get(DataType.Spray).GetDataWithId<SprayData>(player.GetSprayIDBySprayIndex(input.SprayIndex));
                    Spray.Owner = character1;
                    if (player.CharacterSpray != null) m_gameObjectManager.RemoveGameObject(player.CharacterSpray);
                    player.CharacterSpray = Spray;

                    Spray.SetPosition(
                        character1.GetX() + LogicMath.GetRotatedX(500, 0, input.SprayAngle),
                        character1.GetY() + LogicMath.GetRotatedY(500, 0, input.SprayAngle),
                        0
                    );
                   Console.WriteLine("Success !");
                    Spray.SetIndex(character1.GetIndex());
                    m_gameObjectManager.AddGameObject(Spray);
                    break;
                case 16:
                    break;
                case 17:
                    if (!player.HasOverCharge()) return;
                    player.OverCharging = true;
                    character1.ResetAFKTicks();
                    break;
                default:
                    Debugger.Warning("Input is unhandled: " + input.Type);
                    break;
            }
        }

        public bool IsTileOnPoisonArea(int xTile, int yTile)
        {
            if (m_gameModeVariation == GAME_MODE_VARIATION_SHOWDOWN)
            {
                int tick = GetTicksGone();
                if (tick <= 500) return false;
                int poisons = (tick - 500) / 100;
                int maxX = m_tileMap.Width - 1;
                int maxY = m_tileMap.Height - 1;
                return xTile <= poisons || xTile >= maxX - poisons || yTile <= poisons || yTile >= maxY - poisons;
            }

            if (m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT || m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT_2)
            {
                const int smokeStartTicks = 20 * 15;
                const int smokeStepTicks = 100;
                if (RoundTicks < smokeStartTicks) return false;

                int poisons = (RoundTicks - smokeStartTicks) / smokeStepTicks;
                int maxPoison = LogicMath.Max(0, LogicMath.Min((m_tileMap.Width - 2) / 2, (m_tileMap.Height - 2) / 2) - 2);
                poisons = LogicMath.Clamp(poisons, 0, maxPoison);

                int maxX = m_tileMap.Width - 1;
                int maxY = m_tileMap.Height - 1;
                return xTile <= poisons || xTile >= maxX - poisons || yTile <= poisons || yTile >= maxY - poisons;
            }

            return false;
        }

        public int GetPoisonTilesForClient()
        {
            if (m_gameModeVariation == GAME_MODE_VARIATION_SHOWDOWN)
            {
                int tick = GetTicksGone();
                if (tick <= 500) return 0;
                return LogicMath.Clamp((tick - 500) / 100, 0, 15);
            }

            if (m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT || m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT_2)
            {
                return 0;
            }

            return 0;
        }

        private void HandleIncomingInputMessages()
        {
            ClientInput input;
            while (m_inputQueue.TryDequeue(out input))
            {
                if (input != null)
                {
                    try
                    {
                        this.HandleClientInput(input);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error processing input in HandleClientInput: {e.Message}. Input Type: {input.Type}, OwnerSessionId: {input.OwnerSessionId}, Tick: {GetTicksGone()}");
                    }
                }
                else
                {
                    Console.WriteLine("HandleIncomingInputMessages: Dequeued a null input!");
                }
            }
        }

        private void CheckAndRecoverDisconnectedPlayers()
        {
            int currentTick = GetTicksGone();
            List<long> toRemove = new List<long>();
            bool skipTimeoutRemoval = IsRanked;

            foreach (var kvp in _disconnectedTicks)
            {
                long accountId = kvp.Key;
                int disconnectTick = kvp.Value;

                if (!skipTimeoutRemoval && currentTick - disconnectTick > _reconnectTimeoutTicks)
                {
                    Console.WriteLine($"[BattleMode] Player {accountId} exceeded reconnect timeout ({_reconnectTimeoutTicks/20} seconds), removing from battle");
                    BattlePlayer player = GetPlayerByAccountId(accountId);
                    if (player != null)
                    {
                        if (!player.IsConnected)
                        {
                            Console.WriteLine($"[BattleMode] Removing player {player.PlayerIndex} due to timeout");
                            RemovePlayer(player);
                        }
                        else
                        {
                            Console.WriteLine($"[BattleMode] Player {player.PlayerIndex} reconnected, keeping in battle");
                            toRemove.Add(accountId);
                        }
                    }
                    toRemove.Add(accountId);
                }
                else if (skipTimeoutRemoval)
                {
                    BattlePlayer player = GetPlayerByAccountId(accountId);
                    if (player != null && player.IsConnected)
                        toRemove.Add(accountId);
                }
            }

            foreach (var accountId in toRemove)
            {
                _disconnectedTicks.Remove(accountId);
            }
        }

        public void ExecuteOneTick()
        {
            try
            {
                this.HandleIncomingInputMessages();

                if (m_postGameTicksLeft > 0)
                {
                    m_postGameTicksLeft--;
                    try { this.SendVisionUpdateToPlayers(); } catch (Exception) { }
                    m_time.IncreaseTick();
                    if (m_postGameTicksLeft == 0)
                    {
                        try { this.m_updateTimer?.Dispose(); } catch (Exception) { }
                    }
                    return;
                }

                if ((GetTicksGone() & 7) == 0)
                {
                    CheckAndRecoverDisconnectedPlayers();
                }

                if (CalculateIsGameOver())
                {
                    if (IsRanked)
                    {
                        if (!_tmpb)
                        {
                            RankedMatch r = LogicServerListener.Instance.GetRankedMatch(RankedId);
                            if (r == null)
                            {
                                Console.WriteLine($"[BattleMode] Ranked match not found! RankedId={RankedId}");
                                GameOver();
                                this.IsGameOver = true;
                                try { this.m_updateTimer.Dispose(); } catch (Exception) { }
                                return;
                            }
                            
                            if (m_winnerTeam == 0) r.TTW_thestealdev++;
                            if (m_winnerTeam == 1) r.TRW_thestealdev++;
                            r.rdns_thestealdev++;
                            _tmpi = GetTicksGone();
                            _tmpb = true;
                            
                            Console.WriteLine($"[Ranked] Round ended. Winner Team={m_winnerTeam}, Score: {r.TTW_thestealdev}-{r.TRW_thestealdev}");
                            
                            if (r.TTW_thestealdev >= 2 || r.TRW_thestealdev >= 2)
                            {
                                _rankedMatchCompleted = true;
                                Console.WriteLine($"[Ranked] Match fully completed after {r.rdns_thestealdev} rounds! Final score: {r.TTW_thestealdev}-{r.TRW_thestealdev}");
                                GameOver();
                                try { this.m_updateTimer.Dispose(); } catch (Exception) { }
                                return;
                            }
                            
                            SendRoundEndToPlayers();
                            
                            Console.WriteLine($"[Ranked] Waiting for next round. Current score: {r.TTW_thestealdev}-{r.TRW_thestealdev}");
                        }
                        
                        if (!_rankedRestartIssued && GetTicksGone() >= _tmpi + 60)
                        {
                            _rankedRestartIssued = true;
                            RankedMatch ranked = LogicServerListener.Instance.GetRankedMatch(RankedId);
                            if (ranked == null)
                            {
                                try { this.m_updateTimer.Dispose(); } catch (Exception) { }
                                return;
                            }
                            
                            if (ranked.TTW_thestealdev >= 2 || ranked.TRW_thestealdev >= 2)
                            {
                                if (!_rankedMatchCompleted)
                                {
                                    Console.WriteLine($"[Ranked] Match completed (double-check)! Final score: {ranked.TTW_thestealdev}-{ranked.TRW_thestealdev}");
                                    _rankedMatchCompleted = true;
                                    GameOver();
                                }
                                try { this.m_updateTimer.Dispose(); } catch (Exception) { }
                                return;
                            }
                            
                            Console.WriteLine($"[Ranked] Starting next round. Current score: {ranked.TTW_thestealdev}-{ranked.TRW_thestealdev}");
                            ranked.RestartRound();
                            
                            try { this.m_updateTimer.Dispose(); } catch (Exception) { }
                            return;
                        }
                        
                        m_time.IncreaseTick();
                        return;
                    }
                    
                    try { this.GameOver(); } catch (Exception) { }
                    this.IsGameOver = true;

                    // Экран результатов: если рядом с сервером лежит файл postgame_on, бой ещё 20 секунд принимает
                    // реакции игроков (пины, "палец вверх") и рассылает их остальным. Иначе - как раньше, сразу стоп.
                    if (IsPostGameEnabled())
                    {
                        m_postGameTicksLeft = 20 * 20;
                        return;
                    }

                    try { this.m_updateTimer?.Dispose(); } catch (Exception) { }
                    return;
                }

                this.SendVisionUpdateToPlayers();

                foreach (BattlePlayer player in GetPlayers())
                {
                    if (player?.KillList != null)
                        player.KillList.Clear();
                }

                if (StoryMode != null) StoryMode.Tick();

                this.m_gameObjectManager.PreTick();
                this.Tick();
                this.m_time.IncreaseTick();
            }
            catch (Exception e)
            {
                // Ошибка в логике боя останавливает его для всех игроков (клиент видит зависший бой и отваливается),
                // поэтому пишем её в лог всегда: первые 3 раза за бой с местом ошибки, дальше - каждую 200-ю.
                m_tickErrorCount++;
                if (m_tickErrorCount <= 3 || m_tickErrorCount % 200 == 0)
                {
                    string who = "";
                    try { who = string.Join(", ", m_players.Where(p => p != null && p.CharacterDatas != null).Select(p => p.CharacterDatas[0]?.Name)); } catch (Exception) { }
                    Console.WriteLine($"[BattleMode] Tick error #{m_tickErrorCount} (bой {Id}, режим {m_gameModeVariation}, бойцы: {who}): {e?.GetType().Name}: {e?.Message}");
                    if (m_tickErrorCount <= 3) Console.WriteLine(e?.StackTrace ?? "");
                }

                // Бой, который падает 20 секунд подряд, закрываем: иначе он висит вечно, а игроки не могут из него выйти
                if (m_tickErrorCount >= 400 && !IsGameOver)
                {
                    Console.WriteLine($"[BattleMode] Бой {Id} принудительно завершён после {m_tickErrorCount} ошибок");
                    try { this.GameOver(); } catch (Exception) { }
                    this.IsGameOver = true;
                    try { this.m_updateTimer?.Dispose(); } catch (Exception) { }
                }
            }
        }

        private int m_tickErrorCount;
        private int m_postGameTicksLeft;

        // Файл postgame_on: пустой - включено для всех боёв; с номерами аккаунтов - только для боёв с этими игроками
        private bool IsPostGameEnabled()
        {
            try
            {
                if (!System.IO.File.Exists("postgame_on")) return false;
                string[] ids = System.IO.File.ReadAllText("postgame_on").Split(new[] { ' ', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (ids.Length == 0) return true;
                return m_players.Any(p => p != null && p.IsBot() == 0 && Array.IndexOf(ids, p.AccountId.ToString()) >= 0);
            }
            catch (Exception)
            {
                return false;
            }
        }
        private readonly HashSet<int> m_postGameInputTypes = new HashSet<int>();

        /// <summary>
        /// Запись истории боёв со списком участников (без него клиент показывает пустую запись)
        /// </summary>
        private BattleLogStructure BuildBattleLog()
        {
            BattleLogStructure log = new BattleLogStructure();
            try
            {
                foreach (BattlePlayer p in m_players.ToArray())
                {
                    if (p == null) continue;
                    int brawler = p.CharacterIds != null && p.CharacterIds.Length > 0 ? p.CharacterIds[0] : 16000000;
                    int trophies = 0;
                    try { trophies = p.Avatar?.GetHero(brawler)?.Trophies ?? BotNames.NextInt(50, 600); } catch (Exception) { }
                    log.AddPlayer(new PlayerBattleInfo
                    {
                        Index = p.PlayerIndex,
                        AccountId = p.AccountId,
                        Name = p.DisplayData?.Name ?? "Player",
                        Team = p.TeamIndex,
                        BrawlerId = brawler,
                        BrawlerTrophies = trophies,
                        BrawlerLevel = Math.Max(1, p.HeroPowerLevel),
                        IsStarPlayer = false,
                        PlayerExperience = 1000,
                        ThumbnailId = p.DisplayData?.ThumbnailId ?? 28000000,
                        NameColorId = p.DisplayData?.NameColorId ?? 43000000
                    });
                }
            }
            catch (Exception) { }
            return log;
        }
        
        private void TickSpawnEventStuffDelayed() { }

        public void GameOver()
{
    // ДОБАВЬТЕ ЭТУ СТРОКУ В САМОМ НАЧАЛЕ
    NotifyRankedRoundEnd();
    
    if (IsGameOver) return;
    IsGameOver = true;
    // Если включены реакции на экране результатов, бой ещё 20 секунд принимает и рассылает их (см. ExecuteOneTick)
    if (IsPostGameEnabled()) m_postGameTicksLeft = 20 * 20;
    else try { m_updateTimer?.Dispose(); } catch (Exception) { }
    try
    {
        SendBattleEndToPlayers();
    }
    catch (Exception ex)
    {
#if DEBUG
        Console.WriteLine("[GameOver] SendBattleEndToPlayers error: " + ex.Message + " " + ex.StackTrace);
#endif
    }
}

        public void SendBattleEndToPlayers()
        {
            if (IsRanked && !_rankedMatchCompleted)
            {
                Console.WriteLine($"[Ranked] Not sending battle end yet - match not completed (Score: {GetTeamScore(0)}-{GetTeamScore(1)})");
                return;
            }
            
            if (m_hasSentEndMessages) return;
            m_hasSentEndMessages = true;

            try
            {
            RankedMatch rankedMatch = null;
            bool shouldStartNextRankedRound = false;
            bool rankedMatchFinished = false;
            int rankedWinnerTeam = m_winnerTeam;

            if (IsRanked && RankedId > 0 && LogicServerListener.Instance != null)
            {
                rankedMatch = LogicServerListener.Instance.GetRankedMatch(RankedId);
                if (rankedMatch != null)
                {
                    if (rankedWinnerTeam == 0)
                    {
                        rankedMatch.TTW_thestealdev++;
                    }
                    else if (rankedWinnerTeam == 1)
                    {
                        rankedMatch.TRW_thestealdev++;
                    }

                    rankedMatch.rdns_thestealdev++;
                    rankedMatchFinished = rankedMatch.MatchOver;
                    shouldStartNextRankedRound = !rankedMatchFinished;

                    if (rankedMatchFinished)
                    {
                        if (rankedMatch.TTW_thestealdev > rankedMatch.TRW_thestealdev)
                            rankedWinnerTeam = 0;
                        else if (rankedMatch.TRW_thestealdev > rankedMatch.TTW_thestealdev)
                            rankedWinnerTeam = 1;
                        else
                            rankedWinnerTeam = -1;
                    }
                }
            }

            if (m_players == null) return;

            foreach (BattlePlayer player in m_players)
            {
                try
                {
                if (player == null) continue;
                if (player.SessionId < 0) continue;

                if (m_gameModeVariation == 6 && player.BattleRoyaleRank == -1)
                {
                    if (player.IsAlive)
                    {
                        player.BattleRoyaleRank = m_playersAlive;
                    }
                    else
                    {
                        player.BattleRoyaleRank = 1;
                    }
                }
                if (player.BattleRoyaleRank == -1)
                {
                    player.BattleRoyaleRank = m_players.Count;
                }

                if (player.Avatar == null) continue;

                int rank = player.BattleRoyaleRank;
                player.Avatar.BattleId = -1;
                bool isVictory = m_winnerTeam == player.TeamIndex;
                bool isDraw = m_winnerTeam == -1;

                bool winForStreak = false;
                if (BattleWithTrophies && player.IsBot() == 0 && player.Avatar != null)
                {
                    if (m_gameModeVariation == 6)
                    {
                        winForStreak = rank <= 5;
                    }
                    else
                    {
                        winForStreak = isVictory;
                    }

                    if (winForStreak)
                    {
                        player.Avatar.WinStreak += 1;
                        if (player.Avatar.WinStreak > player.Avatar.MaxWinstreak)
                        {
                            player.Avatar.MaxWinstreak = player.Avatar.WinStreak;
                        }
                    }
                    else
                    {
                        player.Avatar.WinStreak = 0;
                    }
                }

                BattleEndMessage message = new BattleEndMessage();
                Hero hero = null;
                try { hero = player.Avatar.GetHero(player.CharacterId); } catch (Exception) { }

                int oldMasteryPoints = hero?.MasteryPoints ?? 0;
                message.MasteryPoints = oldMasteryPoints;
                message.MasteryGained = 0;

                if (player.IsBot() == 0)
                {
                    try
                    {
                    HomeMode homeMode = LogicServerListener.Instance?.GetHomeMode(player.AccountId);
                    if (homeMode?.Home?.Quests != null)
                    {
                        if (m_gameModeVariation != 13 && BattleWithTrophies)
                        {
                            message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(
                                m_gameModeVariation, 
                                player.CharacterId, 
                                player.Kills, 
                                player.Damage, 
                                player.Heals, 
                                homeMode.Home);
                        }
                    }
                    }
                    catch (Exception) { }
                }

                try
                {
                    BattlePlayer best = null;
                    int bestScore = int.MinValue;
                    foreach (var p in m_players)
                    {
                        if (p == null) continue;
                        int score = p.Kills * 300 + p.Damage / 100 + p.Heals / 100;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = p;
                        }
                    }
                    if (best != null)
                    {
                        message.StarPlayerAccountId = best.AccountId;
                    }
                }
                catch { }

                if (m_gameModeVariation != 6)
                {
                    message.GameMode = 1;
                    message.IsPvP = BattleWithTrophies || IsRanked;
                    message.Players = m_players;
                    message.OwnPlayer = player;

                    message.Result = 2;
                    message.TokensReward = 0;
                    message.TrophiesReward = 0;
                    message.WinstreakTrophies = 0;
                    message.TokensDoublersReward = 0;
                    message.TokensRemainingReward = player.Home?.TokenDoublers ?? 0;
                    message.StarToken = false;

                    try
                    {
                        if (m_winnerTeam == -1)
                        {
                            message.Result = 2;
                            Random random = new Random();
                            int baseTokens = random.Next(20, 151);
                            message.TokensReward = baseTokens;
                            
                            if (BattleWithTrophies)
                            {
                                int doublersAvailable = player.Home?.TokenDoublers ?? 0;
                                int doublersUsed = Math.Min(doublersAvailable, baseTokens);
                                int finalTokens = baseTokens + doublersUsed;
                                message.TokensDoublersReward = doublersUsed;
                                message.TokensRemainingReward = doublersAvailable - doublersUsed;
                                if (player.Home != null) player.Home.TokenDoublers -= doublersUsed;

                                int vipBonus = 0;
                                if (isVictory || isDraw)
                                {
                                    ApplyVipBonus(player.Avatar, ref vipBonus);
                                }
                                
                                message.TrophiesReward = vipBonus;
                                if (vipBonus > 0 && hero != null)
                                {
                                    hero.AddTrophies(vipBonus);
                                    if (player.Home != null) 
                                        player.Home.TrophiesReward = LogicMath.Max(player.Home.TrophiesReward + vipBonus, 0);
                                }
                                
                                player.Avatar.AddTokens(finalTokens);
                                
                                if (player.IsBot() == 0 && hero != null)
                                {
                                    Random masteryRandom = new Random();
                                    int masteryReward = masteryRandom.Next(100, 201);
                                    hero.MasteryPoints += masteryReward;
                                    message.MasteryPoints = hero.MasteryPoints;
                                    message.MasteryGained = masteryReward;
                                    Console.WriteLine($"[Mastery] Игрок {player.PlayerIndex} получил {masteryReward} мастерства за ничью. Всего: {hero.MasteryPoints}");
                                }
                                
                                if (player.Home != null) 
                                {
                                    player.Home.TokenReward += finalTokens;
                                    player.Home.BattleLogs.Add(BuildBattleLog(), 1, vipBonus, GetTicksGone() / 20, !BattleWithTrophies, m_locationId, 2);
                                }
                            }
                        }
                        else
                        {
                            int brawlerTrophies = hero?.Trophies ?? 0;
                            int winTrophies = 0;
                            int loseTrophies = 0;
                            
                            // ТВОЯ ТАБЛИЦА КУБКОВ (+8 система)
                            if (brawlerTrophies < 0) {
                                winTrophies = 8;
                                loseTrophies = 0;
                            }
                            else if (brawlerTrophies <= 49) { 
                                winTrophies = 8;
                                loseTrophies = 0;
                            }
                            else if (brawlerTrophies <= 99) { 
                                winTrophies = 8;
                                loseTrophies = -1;
                            }
                            else if (brawlerTrophies <= 199) { 
                                winTrophies = 8;
                                loseTrophies = -2;
                            }
                            else if (brawlerTrophies <= 299) { 
                                winTrophies = 8;
                                loseTrophies = -3;
                            }
                            else if (brawlerTrophies <= 399) { 
                                winTrophies = 8;
                                loseTrophies = -4;
                            }
                            else if (brawlerTrophies <= 499) { 
                                winTrophies = 8;
                                loseTrophies = -5;
                            }
                            else if (brawlerTrophies <= 599) { 
                                winTrophies = 8;
                                loseTrophies = -6;
                            }
                            else if (brawlerTrophies <= 699) { 
                                winTrophies = 8;
                                loseTrophies = -7;
                            }
                            else if (brawlerTrophies <= 799) { 
                                winTrophies = 8;
                                loseTrophies = -8;
                            }
                            else if (brawlerTrophies <= 899) { 
                                winTrophies = 7;
                                loseTrophies = -9;
                            }
                            else if (brawlerTrophies <= 999) { 
                                winTrophies = 6;
                                loseTrophies = -10;
                            }
                            else if (brawlerTrophies <= 1099) { 
                                winTrophies = 5;
                                loseTrophies = -11;
                            }
                            else if (brawlerTrophies <= 1199) { 
                                winTrophies = 4;
                                loseTrophies = -12;
                            }
                            else if (brawlerTrophies >= 1200) { 
                                winTrophies = 3;
                                loseTrophies = -12;
                            }
                            
                            if (isVictory)
                            {
                                message.Result = 0;
                                Random random = new Random();
                                int baseTokens = random.Next(20, 151);
                                message.TokensReward = baseTokens;
                                
                                if (BattleWithTrophies)
                                {
                                    int doublersAvailable = player.Home?.TokenDoublers ?? 0;
                                    int doublersUsed = Math.Min(doublersAvailable, baseTokens);
                                    int finalTokens = baseTokens + doublersUsed;
                                    message.TokensDoublersReward = doublersUsed;
                                    message.TokensRemainingReward = doublersAvailable - doublersUsed;
                                    if (player.Home != null) player.Home.TokenDoublers -= doublersUsed;

                                    int trophiesReward = winTrophies;
                                    message.WinstreakTrophies = 0;

                                    if (winForStreak && player.Avatar != null && player.Avatar.WinStreak >= 2)
                                    {
                                        int bonusTrophies = Math.Min(player.Avatar.WinStreak, 5);
                                        trophiesReward += bonusTrophies;
                                        message.WinstreakTrophies = bonusTrophies;
                                    }
                                    
                                    // ТВОЙ VIP БОНУС (+3)
                                    if (player.Avatar.IsVIP && player.Avatar.VIPExpire > DateTime.UtcNow)
                                    {
                                        ApplyVipBonus(player.Avatar, ref trophiesReward);
                                    }

                                    message.TrophiesReward = trophiesReward;
                                    if (hero != null) hero.AddTrophies(trophiesReward);
                                    player.Avatar.AddTokens(finalTokens);
                                    player.Avatar.TrioWins++;
                                    
                                    if (player.IsBot() == 0 && hero != null)
                                    {
                                        Random masteryRandom = new Random();
                                        int masteryReward = masteryRandom.Next(150, 301);
                                        hero.MasteryPoints += masteryReward;
                                        message.MasteryPoints = hero.MasteryPoints;
                                        message.MasteryGained = masteryReward;
                                        Console.WriteLine($"[Mastery] Игрок {player.PlayerIndex} получил {masteryReward} мастерства за победу. Всего: {hero.MasteryPoints}");
                                    }
                                    
                                    if (player.Home != null)
                                    {
                                        player.Home.TokenReward += finalTokens;
                                        player.Home.TrophiesReward = LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                                        player.Home.BattleLogs.Add(BuildBattleLog(), 1, trophiesReward, GetTicksGone() / 20, !BattleWithTrophies, m_locationId, 0);
                                    }
                                }
                            }
                            else
                            {
                                message.Result = 1;
                                Random random = new Random();
                                int baseTokens = random.Next(20, 151);
                                message.TokensReward = baseTokens;
                                
                                if (BattleWithTrophies)
                                {
                                    int doublersAvailable = player.Home?.TokenDoublers ?? 0;
                                    int doublersUsed = Math.Min(doublersAvailable, baseTokens);
                                    int finalTokens = baseTokens + doublersUsed;
                                    message.TokensDoublersReward = doublersUsed;
                                    message.TokensRemainingReward = doublersAvailable - doublersUsed;
                                    if (player.Home != null) player.Home.TokenDoublers -= doublersUsed;

                                    int trophiesReward = loseTrophies;

                                    if (hero != null && hero.Trophies < -trophiesReward)
                                    {
                                        trophiesReward = -hero.Trophies;
                                    }
                                    message.TrophiesReward = trophiesReward;
                                    if (hero != null) hero.AddTrophies(trophiesReward);
                                    player.Avatar.AddTokens(finalTokens);
                                    
                                    if (player.IsBot() == 0 && hero != null)
                                    {
                                        Random masteryRandom = new Random();
                                        int masteryReward = masteryRandom.Next(50, 151);
                                        hero.MasteryPoints += masteryReward;
                                        message.MasteryPoints = hero.MasteryPoints;
                                        message.MasteryGained = masteryReward;
                                        Console.WriteLine($"[Mastery] Игрок {player.PlayerIndex} получил {masteryReward} мастерства за поражение. Всего: {hero.MasteryPoints}");
                                    }
                                    
                                    if (player.Home != null)
                                    {
                                        player.Home.TokenReward += finalTokens;
                                        player.Home.TrophiesReward = LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                                        player.Home.BattleLogs.Add(BuildBattleLog(), 1, trophiesReward, GetTicksGone() / 20, !BattleWithTrophies, m_locationId, 1);
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (m_gameModeVariation == 3 && player.KillList != null && player.KillList.Count > 0)
                        {
                            try
                            {
                                player.Avatar?.AddStarTokens(player.KillList.Count);
                                if (player.Home != null) player.Home.StarTokenReward += player.KillList.Count;
                            }
                            catch (Exception) { }
                        }

                        if (hero != null && hero.Trophies > hero.HighestTrophies)
                        {
                            hero.HighestTrophies = hero.Trophies;
                        }
                        if (player.Avatar != null && player.Avatar.Trophies > player.Avatar.HighestTrophies)
                        {
                            player.Avatar.HighestTrophies = player.Avatar.Trophies;
                        }
                    }
                }
                else
                {
                    message.GameMode = 2;
                    message.IsPvP = BattleWithTrophies;
                    message.Result = player.BattleRoyaleRank;
                    message.Players = new List<BattlePlayer> { player };
                    message.OwnPlayer = player;

                    message.TokensReward = 0;
                    message.TrophiesReward = 0;
                    message.WinstreakTrophies = 0;
                    message.TokensDoublersReward = 0;
                    message.TokensRemainingReward = player.Home?.TokenDoublers ?? 0;
                    message.StarToken = false;

                    if (BattleWithTrophies)
                    {
                        Random random = new Random();
                        int baseTokens = random.Next(20, 151);
                        int doublersAvailable = player.Home?.TokenDoublers ?? 0;
                        int doublersUsed = Math.Min(doublersAvailable, baseTokens);
                        int finalTokens = baseTokens + doublersUsed;
                        message.TokensReward = baseTokens;
                        message.TokensDoublersReward = doublersUsed;
                        message.TokensRemainingReward = doublersAvailable - doublersUsed;
                        if (player.Home != null) player.Home.TokenDoublers -= doublersUsed;

                        message.WinstreakTrophies = 0;

                        // ТВОЯ СИСТЕМА SHOWDOWN
                        int trophiesReward = (rank <= 5) ? 8 : -8;

                        int winstreakBonusTrophies = 0;
                        if (rank <= 4 && winForStreak && player.Avatar != null && player.Avatar.WinStreak >= 2)
                        {
                            winstreakBonusTrophies = Math.Min(player.Avatar.WinStreak, 5);
                            trophiesReward += winstreakBonusTrophies;
                            message.WinstreakTrophies = winstreakBonusTrophies;
                        }

                        // ТВОЙ VIP БОНУС
                        if (player.Avatar.IsVIP && player.Avatar.VIPExpire > DateTime.UtcNow)
                        {
                            ApplyVipBonus(player.Avatar, ref trophiesReward);
                        }

                        if (hero != null && hero.Trophies < -trophiesReward)
                        {
                            trophiesReward = -hero.Trophies;
                        }

                        message.TrophiesReward = trophiesReward;
                        player.Avatar.AddTokens(finalTokens);
                        
                        if (player.IsBot() == 0 && hero != null)
                        {
                            Random masteryRandom = new Random();
                            int masteryReward;
                            if (rank <= 5)
                            {
                                masteryReward = masteryRandom.Next(150, 301);
                            }
                            else
                            {
                                masteryReward = masteryRandom.Next(50, 151);
                            }
                            hero.MasteryPoints += masteryReward;
                            message.MasteryPoints = hero.MasteryPoints;
                            message.MasteryGained = masteryReward;
                            Console.WriteLine($"[Mastery] Игрок {player.PlayerIndex} получил {masteryReward} мастерства за Showdown (ранг {rank}). Всего: {hero.MasteryPoints}");
                        }
                        
                        if (player.Home != null)
                        {
                            player.Home.TokenReward += finalTokens;
                            player.Home.BattleLogs.Add(BuildBattleLog(), 1, trophiesReward, GetTicksGone() / 20, !BattleWithTrophies, m_locationId, rank);
                        }

                        if (rank <= 4)
                        {
                            player.Avatar.AddStarTokens(1);
                            message.StarToken = true;
                            if (player.Home != null) player.Home.StarTokenReward += 1;
                        }

                        if (hero != null) hero.AddTrophies(message.TrophiesReward);
                        player.Avatar.SoloWins++;
                    }
                    else
                    {
                        int baseTokens = 40 / rank;
                        message.TokensReward = baseTokens;
                        
                        if (player.IsBot() == 0 && hero != null)
                        {
                            Random masteryRandom = new Random();
                            int masteryReward = masteryRandom.Next(50, 151);
                            hero.MasteryPoints += masteryReward;
                            message.MasteryPoints = hero.MasteryPoints;
                            message.MasteryGained = masteryReward;
                            Console.WriteLine($"[Mastery] Игрок {player.PlayerIndex} получил {masteryReward} мастерства за Showdown без трофеев (ранг {rank}). Всего: {hero.MasteryPoints}");
                        }
                    }

                    try
                    {
                        if (hero != null && hero.Trophies > hero.HighestTrophies)
                        {
                            hero.HighestTrophies = hero.Trophies;
                        }
                        if (player.Avatar != null && player.Avatar.Trophies > player.Avatar.HighestTrophies)
                        {
                            player.Avatar.HighestTrophies = player.Avatar.Trophies;
                        }
                    }
                    catch { }
                }

                message.Winstreak = (BattleWithTrophies && player.IsBot() == 0 && player.Avatar != null) ? player.Avatar.WinStreak : 0;
                if (!BattleWithTrophies && !IsRanked)
                {
                    message.BattleWithoutTrophies = true;
                }
                if (IsRanked)
                {
                    message.BattleWithoutTrophies = false;
                }

                try
                {
                    if (player?.Home != null && player.Home.CoinsEventEnabled)
                    {
                        int coins = GetRandomInt(20, 81);
                        if (coins > 0 && player.Avatar != null)
                        {
                            player.Avatar.AddGold(coins);
                            player.Home.CoinsReward += coins;
                            message.CoinsGained = coins;
                        }
                    }
                }
                catch { }

                if (IsRanked)
                {
                    RankedMatch r = rankedMatch;
                    if (r == null)
                    {
                        message.RankedMatch = true;
                        message.Solo = true;
                        message.MatchType = 0;
                        message.ELO = player.Home?.RankedSoloProgress ?? 100;
                        message.NewELO = message.ELO;
                        message.Add = 0;
                        message.Rank = (message.ELO / 1000) + 1;
                        message.UpRank = message.Rank;
                        message.TrueBlueTeamWins = 0;
                        message.TrueRedTeamWins = 0;
                        message.Round = 0;
                    }
                    else
                    {
                        int oldProgress = player.Home != null ? player.Home.RankedSoloProgress : 100;
                        if (oldProgress <= 0) oldProgress = 100;
                        int delta = 0;
                        Random rnd = new Random();

                        bool matchOver = r.MatchOver;
                        bool finalVictory = rankedWinnerTeam == player.TeamIndex;
                        bool finalDraw = rankedWinnerTeam == -1;

                        if (matchOver)
                        {
                            if (finalDraw)
                            {
                                delta = 0;
                            }
                            else if (finalVictory)
                            {
                                if (r.s_thestealdev)
                                {
                                    delta = rnd.Next(100, 220);
                                    if (player.Avatar != null && player.Avatar.IsVIP && player.Avatar.VIPExpire > DateTime.UtcNow)
                                        delta += rnd.Next(40, 80);
                                }
                                else
                                {
                                    delta = rnd.Next(150, 220);
                                    if (player.Avatar != null && player.Avatar.IsVIP && player.Avatar.VIPExpire > DateTime.UtcNow)
                                        delta += rnd.Next(40, 80);
                                }
                            }
                            else
                            {
                                if (r.s_thestealdev)
                                {
                                    delta = -rnd.Next(25, 50);
                                    if (player.Avatar != null && player.Avatar.IsVIP && player.Avatar.VIPExpire > DateTime.UtcNow)
                                        delta += rnd.Next(20, 50);
                                }
                                else
                                {
                                    delta = -rnd.Next(35, 55);
                                    if (player.Avatar != null && player.Avatar.IsVIP && player.Avatar.VIPExpire > DateTime.UtcNow)
                                        delta += rnd.Next(20, 50);
                                }
                            }
                        }

                        int newProgress = matchOver ? LogicMath.Max(0, oldProgress + delta) : oldProgress;
                        int oldRank = (oldProgress / 1000) + 1;
                        int newRank = (newProgress / 1000) + 1;

                        if (matchOver && player.Home != null)
                        {
                            player.Home.RankedSoloProgress = newProgress;
                            player.Home.RankedTrioProgress = newProgress;
                            player.Home.RankedSoloRank = newRank;
                            player.Home.RankedTrioRank = newRank;
                            if (player.Home.RankedSoloMaxProgress < newProgress) player.Home.RankedSoloMaxProgress = newProgress;
                            if (player.Home.RankedTrioMaxProgress < newProgress) player.Home.RankedTrioMaxProgress = newProgress;
                            if (player.Home.RankedSoloMaxRank < newRank) player.Home.RankedSoloMaxRank = newRank;
                            if (player.Home.RankedTrioMaxRank < newRank) player.Home.RankedTrioMaxRank = newRank;
                        }
                        if (matchOver && player.Avatar != null)
                        {
                            player.Avatar.RankedRank = newProgress;
                            if (player.IsBot() == 0) LogicServerListener.Instance?.SaveAccount(player.AccountId);
                        }

                        message.RankedMatch = true;
                        message.Solo = r.s_thestealdev;
                        message.MatchType = message.Solo ? 0 : 1;
                        message.ELO = oldProgress;
                        message.NewELO = newProgress;
                        message.Add = delta;
                        message.Rank = oldRank;
                        message.UpRank = newRank;
                        message.TrueBlueTeamWins = r.TTW_thestealdev;
                        message.TrueRedTeamWins = r.TRW_thestealdev;
                        message.Round = r.MatchOver ? 0 : r.TG_TheSteallllDev;
                    }
                }

                if (player?.Avatar != null && player.GameListener != null)
                {
                    try
                    {
                        player.GameListener.SendTCPMessage(message);
                    }
                    catch (Exception) { }
                }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine($"[SendBattleEndToPlayers] Player {player?.PlayerIndex} error: {ex.Message}");
#endif
                }
            }

            if (shouldStartNextRankedRound && rankedMatch != null)
            {
                const int nextRoundDelayMs = 10_000;
                Task.Run(() =>
                {
                    try
                    {
                        System.Threading.Thread.Sleep(nextRoundDelayMs);

                        if (rankedMatch.MatchOver) return;
                        rankedMatch.RestartRound();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Ranked] Failed to restart next round: {ex.Message}");
                    }
                });
            }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine("[SendBattleEndToPlayers] Error: " + ex.Message + " " + ex.StackTrace);
#endif
            }
        }

        public void FixStuckBall() { }
        public void RecreateBall() { }
        public void ForceBallReset() { }
        public void SyncBallState() { }
        public void SetBallOwner(Character newOwner) { }

        public void SendBattleEndToPlayer(BattlePlayer player)
        {
            if (player == null || player.GameListener == null || player.Avatar == null) return;
            try
            {
                player.Avatar.BattleId = -1;
                BattleEndMessage message = new BattleEndMessage();
                message.GameMode = (m_gameModeVariation == 6) ? 2 : 1;
                message.IsPvP = BattleWithTrophies || IsRanked;
                message.BattleWithoutTrophies = IsRanked ? false : !BattleWithTrophies;
                message.Players = m_players != null ? new List<BattlePlayer>(m_players) : new List<BattlePlayer> { player };
                message.OwnPlayer = player;
                message.Result = (m_gameModeVariation == 6) ? (player.BattleRoyaleRank > 0 ? player.BattleRoyaleRank : m_playersAlive) : 2;
                message.TokensReward = 0;
                message.TrophiesReward = 0;
                message.WinstreakTrophies = 0;
                message.TokensDoublersReward = 0;
                message.TokensRemainingReward = player.Home?.TokenDoublers ?? 0;
                message.StarToken = false;
                message.ProgressiveQuests = new List<IndusBrawl.Laser.Logic.Home.Quest.Quest>();
                try { message.StarPlayerAccountId = GetPlayers().OrderByDescending(p => p != null ? p.Kills * 300 + p.Damage / 100 + p.Heals / 100 : 0).FirstOrDefault()?.AccountId ?? 0; } catch { }
                message.MasteryPoints = player.Avatar.GetHero(player.CharacterId)?.MasteryPoints ?? 0;
                message.MasteryGained = 0;

                if (IsRanked)
                {
                    var rankedMatch = RankedId > 0 && LogicServerListener.Instance != null ? LogicServerListener.Instance.GetRankedMatch(RankedId) : null;
                    int currentProgress = player.Home?.RankedSoloProgress ?? 100;
                    if (currentProgress <= 0) currentProgress = 100;
                    message.RankedMatch = true;
                    message.Solo = rankedMatch?.s_thestealdev ?? true;
                    message.MatchType = message.Solo ? 0 : 1;
                    message.ELO = currentProgress;
                    message.NewELO = currentProgress;
                    message.Add = 0;
                    message.Rank = (currentProgress / 1000) + 1;
                    message.UpRank = message.Rank;
                    message.TrueBlueTeamWins = rankedMatch?.TTW_thestealdev ?? 0;
                    message.TrueRedTeamWins = rankedMatch?.TRW_thestealdev ?? 0;
                    message.Round = rankedMatch?.TG_TheSteallllDev ?? 0;
                    if (m_winnerTeam == -1) message.Result = 2; else message.Result = (m_winnerTeam == player.TeamIndex) ? 0 : 1;
                }

                player.GameListener.SendTCPMessage(message);
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[BattleMode] SendBattleEndToPlayer error: {ex?.Message}");
#endif
            }
        }

        public int GetTeamScore(int team)
        {
            if (m_gameModeVariation == GAME_MODE_VARIATION_BRAWL_BALL)
            {
                if (team == 0) return _brawlBallTeam0Goals;
                if (team == 1) return _brawlBallTeam1Goals;
                return 0;
            }

            int score = 0;
            foreach (BattlePlayer player in m_players)
            {
                if (player.TeamIndex == team) score += player.GetScore();
            }
            return score;
        }

        private int CalculateIsRoundOver()
        {
            var team0 = GetAliveCharactersByTeam(0);
            var team1 = GetAliveCharactersByTeam(1);
            int c0 = team0?.Count ?? 0;
            int c1 = team1?.Count ?? 0;
            if (c0 <= 0 && c1 > 0) return 1;
            if (c1 <= 0 && c0 > 0) return 0;
            if (RoundTicks > 4095) return 2;
            return -1;
        }

private bool CalculateIsGameOver()
{
    if (m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT || m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT_2)
    {
        m_team0AliveCount = 0;
        m_team1AliveCount = 0;
        if (m_gameObjectManager == null) return false;
        var characters = m_gameObjectManager.GetCharacters();
        if (characters == null) return false;
        foreach (Character character in characters)
        {
            if (character.GetPlayer() != null && character.IsAlive())
            {
                if (character.GetPlayer().TeamIndex == 0)
                    m_team0AliveCount++;
                else if (character.GetPlayer().TeamIndex == 1)
                    m_team1AliveCount++;
            }
        }
        
        int roundResult = CalculateIsRoundOver();
        if (roundResult != -1 && !nokbool)
        {
            nokbool = true;
            KnockoutTicks = 3 * 20;
            if (RoundWins != null && RoundCount >= 0 && RoundCount < RoundWins.Length)
                RoundWins[RoundCount] = roundResult;
            RoundCount++;
            if ((roundResult == 0 || roundResult == 1) && TScore != null && TScore.Count > roundResult)
                TScore[roundResult]++;
            if (m_knockoutRoundWins != null && m_knockoutRoundWins.Length >= 2 && TScore != null && TScore.Count >= 2)
            {
                m_knockoutRoundWins[0] = TScore[0];
                m_knockoutRoundWins[1] = TScore[1];
            }
            m_knockoutRound = RoundCount + 1;
            
            if (TScore != null && TScore.Count >= 2)
                Console.WriteLine($"[Knockout] Раунд {RoundCount}: Team {roundResult} победила! Счет: {TScore[0]}-{TScore[1]}");
        }
        
        if (TScore != null && TScore.Count >= 2 && TScore[0] >= 2)
        {
            m_winnerTeam = 0;
            Console.WriteLine($"[Knockout] Team 0 победила в матче! Счет: {TScore[0]}-{TScore[1]}");
            return true;
        }
        else if (TScore != null && TScore.Count >= 2 && TScore[1] >= 2)
        {
            m_winnerTeam = 1;
            Console.WriteLine($"[Knockout] Team 1 победила в матче! Счет: {TScore[0]}-{TScore[1]}");
            return true;
        }
        
        return false;
    }

    if (m_gameModeVariation == GAME_MODE_VARIATION_HOT_ZONE)
    {
        if (Zone1Score >= 100)
        {
            m_winnerTeam = 0;
            Console.WriteLine($"[HotZone] Team 0 победила! Счет: {Zone1Score}-{Zone2Score}");
            return true;
        }
        else if (Zone2Score >= 100)
        {
            m_winnerTeam = 1;
            Console.WriteLine($"[HotZone] Team 1 победила! Счет: {Zone1Score}-{Zone2Score}");
            return true;
        }
    }

    if (m_gameModeVariation == GAME_MODE_VARIATION_BRAWL_BALL)
    {
        if (GetTeamScore(0) >= 2)
        {
            m_winnerTeam = 0;
            Console.WriteLine($"[BrawlBall] Team 0 wins the match with {GetTeamScore(0)} goals!");
            return true;
        }
        else if (GetTeamScore(1) >= 2)
        {
            m_winnerTeam = 1;
            Console.WriteLine($"[BrawlBall] Team 1 wins the match with {GetTeamScore(1)} goals!");
            return true;
        }
        
        if (GetTicksGone() >= 3200 - 1)
        {
            if (GetTeamScore(0) == GetTeamScore(1))
            {
                m_winnerTeam = -1;
                Console.WriteLine($"[BrawlBall] Draw at the end of regular time!");
                return true;
            }
            else
            {
                m_winnerTeam = GetTeamScore(0) > GetTeamScore(1) ? 0 : 1;
                Console.WriteLine($"[BrawlBall] Team {m_winnerTeam} wins on time! Score: {GetTeamScore(0)}-{GetTeamScore(1)}");
                return true;
            }
        }
        
        return false;
    }

    if (m_gameModeVariation == GAME_MODE_VARIATION_GEM_GRAB)
    {
        if (GetTeamScore(0) > GetTeamScore(1) && GetTeamScore(0) >= 10)
        {
            if (m_gemGrabCountdown == 0)
            {
                m_gemGrabCountdown = GetTicksGone() + 20 * 17;
            }
            else if (GetTicksGone() > m_gemGrabCountdown)
            {
                m_winnerTeam = 0;
                return true;
            }
        }
        else if (GetTeamScore(0) < GetTeamScore(1) && GetTeamScore(1) >= 10)
        {
            if (m_gemGrabCountdown == 0)
            {
                m_gemGrabCountdown = GetTicksGone() + 20 * 17;
            }
            else if (GetTicksGone() > m_gemGrabCountdown)
            {
                m_winnerTeam = 1;
                return true;
            }
        }
        else
        {
            m_gemGrabCountdown = 0;
        }
    }

    else if (m_gameModeVariation == GAME_MODE_VARIATION_VOLLEYBRAWL)
    {
        if (GetTeamScore(0) >= 10)
        {
            m_winnerTeam = 0;
            return true;
        }
        if (GetTeamScore(1) >= 10)
        {
            m_winnerTeam = 1;
            return true;
        }
    }

    else if (m_gameModeVariation == GAME_MODE_VARIATION_SHOWDOWN)
    {
        if (m_playersAlive <= 1)
        {
            return true;
        }
    }

    else if (GameModeUtil.HasTwoBases(m_gameModeVariation))
    {
        bool Base1Alive = false;
        bool Base2Alive = false;
        foreach (Character character in m_gameObjectManager.GetCharacters())
        {
            if (character?.CharacterData == null) continue;
            if (character.CharacterData.IsBase())
            {
                if (character.GetIndex() / 16 == 0) Base1Alive = true;
                else Base2Alive = true;
            }
        }
        if (!Base1Alive || !Base2Alive)
        {
            m_winnerTeam = Base1Alive ? 0 : 1;
            if (!Base1Alive && !Base2Alive) m_winnerTeam = -1;
            return true;
        }
    }

    else if (m_gameModeVariation == GAME_MODE_VARIATION_LAST_STAND)
    {
        int Base1Alive = 0;
        int Base2Alive = 0;
        foreach (Character character in m_gameObjectManager.GetCharacters())
        {
            if (character?.CharacterData == null) continue;
            if (!character.CharacterData.IsHero()) continue;
            if (!character.IsAlive()) continue;
            if (character.GetPlayer() != null && character.GetPlayer().PlayerIndex == GetTeam1King()) Base1Alive = character.GetHitpointPercentage();
            if (character.GetPlayer() != null && character.GetPlayer().PlayerIndex == GetTeam2King()) Base2Alive = character.GetHitpointPercentage();
        }
        if (Base1Alive <= 0 || Base2Alive <= 0)
        {
            if (Base1Alive <= 0)
            {
                m_winnerTeam = 1;
            }
            else if (Base2Alive <= 0)
            {
                m_winnerTeam = 0;
            }
            return true;
        }
    }

    else if (m_gameModeVariation == GAME_MODE_VARIATION_SIEGE)
    {
        if (GetTeamScore(0) >= 8)
        {
            m_winnerTeam = 0;
            return true;
        }
        else if (GetTeamScore(1) >= 8)
        {
            m_winnerTeam = 1;
            return true;
        }
    }

    else if (m_gameModeVariation == GAME_MODE_VARIATION_PAYLOAD)
    {
        if (GetTeamScore(0) > GetTeamScore(1) && GetTeamScore(0) >= 20)
        {
            if (m_gemGrabCountdown == 0)
            {
                m_gemGrabCountdown = GetTicksGone() + 20 * 17;
            }
            else if (GetTicksGone() > m_gemGrabCountdown)
            {
                m_winnerTeam = 0;
                return true;
            }
        }
        else if (GetTeamScore(0) < GetTeamScore(1) && GetTeamScore(1) >= 20)
        {
            if (m_gemGrabCountdown == 0)
            {
                m_gemGrabCountdown = GetTicksGone() + 20 * 17;
            }
            else if (GetTicksGone() > m_gemGrabCountdown)
            {
                m_winnerTeam = 1;
                return true;
            }
        }
        else
        {
            m_gemGrabCountdown = 0;
        }
    }

    else if (m_gameModeVariation == GAME_MODE_VARIATION_BOUNTY)
    {
        int team0Score = GetTeamScore(0);
        int team1Score = GetTeamScore(1);

        if (team0Score >= 20)
        {
            m_winnerTeam = 0;
            return true;
        }
        if (team1Score >= 20)
        {
            m_winnerTeam = 1;
            return true;
        }
    }

    if (GetTicksGone() >= GameModeUtil.GetBattleTicks(m_gameModeVariation) - 1)
    {
        if (GameModeUtil.HasTwoBases(m_gameModeVariation))
        {
            int Base1Alive = 0;
            int Base2Alive = 0;
            foreach (Character character in m_gameObjectManager.GetCharacters())
            {
                if (character?.CharacterData == null) continue;
                if (character.CharacterData.IsBase())
                {
                    if (character.GetIndex() / 16 == 0) Base1Alive = character.GetHitpointPercentage();
                    else Base2Alive = character.GetHitpointPercentage();
                }
            }
            if (Base1Alive > Base2Alive) m_winnerTeam = 0;
            else if (Base1Alive < Base2Alive) m_winnerTeam = 1;
            else m_winnerTeam = -1;
        }
        else if (m_gameModeVariation == GAME_MODE_VARIATION_VOLLEYBRAWL)
        {
            int team0Score = GetTeamScore(0);
            int team1Score = GetTeamScore(1);
            if (team0Score > team1Score)
                m_winnerTeam = 0;
            else if (team1Score > team0Score)
                m_winnerTeam = 1;
            else
                m_winnerTeam = -1;
        }
        else if (m_gameModeVariation == GAME_MODE_VARIATION_LAST_STAND)
        {
            int Base1Alive = 0;
            int Base2Alive = 0;
            foreach (Character character in m_gameObjectManager.GetCharacters())
            {
                if (character?.CharacterData == null) continue;
                if (!character.CharacterData.IsHero()) continue;
                if (!character.IsAlive()) continue;
                if (character.GetPlayer() != null && character.GetPlayer().PlayerIndex == GetTeam1King()) Base1Alive = character.GetHitpointPercentage();
                if (character.GetPlayer() != null && character.GetPlayer().PlayerIndex == GetTeam2King()) Base2Alive = character.GetHitpointPercentage();
            }
            if (Base1Alive > Base2Alive) m_winnerTeam = 0;
            else if (Base1Alive < Base2Alive) m_winnerTeam = 1;
            else m_winnerTeam = -1;
        }
        else if (m_gameModeVariation == GAME_MODE_VARIATION_SIEGE)
        {
            if (GetTeamScore(0) > GetTeamScore(1)) m_winnerTeam = 0;
            else if (GetTeamScore(0) < GetTeamScore(1)) m_winnerTeam = 1;
            else m_winnerTeam = -1;
        }
        else if (m_gameModeVariation == GAME_MODE_VARIATION_BOUNTY)
        {
            int team0Score = GetTeamScore(0);
            int team1Score = GetTeamScore(1);

            if (team0Score > team1Score)
                m_winnerTeam = 0;
            else if (team1Score > team0Score)
                m_winnerTeam = 1;
            else
                m_winnerTeam = -1;
        }
        else if (m_gameModeVariation == GAME_MODE_VARIATION_HOT_ZONE)
        {
            int team0TotalProgress = Zone1Score * 100;
            int team1TotalProgress = Zone2Score * 100;
            
            foreach (KingOfHillState zone in _koHStates)
            {
                if (zone != null)
                {
                    team0TotalProgress += zone.ZonePercentageTeam0;
                    team1TotalProgress += zone.ZonePercentageTeam1;
                }
            }
            
            if (team0TotalProgress > team1TotalProgress)
                m_winnerTeam = 0;
            else if (team1TotalProgress > team0TotalProgress)
                m_winnerTeam = 1;
            else
                m_winnerTeam = -1;
                
            Console.WriteLine($"[HotZone] Time's up! Team0: {team0TotalProgress}%, Team1: {team1TotalProgress}%, Winner: Team {m_winnerTeam}");
        }
        
        if (m_gameModeVariation != GAME_MODE_VARIATION_KNOCKOUT && 
            m_gameModeVariation != GAME_MODE_VARIATION_KNOCKOUT_2 &&
            m_gameModeVariation != GAME_MODE_VARIATION_BRAWL_BALL)
            return true;
    }
    
    return false;
}
public List<Character> GetAliveCharactersByTeam(int teamIndex)
{
    if (m_gameObjectManager == null) return new List<Character>();
    List<Character> teamCharacters = new List<Character>();

    foreach (Character character in m_gameObjectManager.GetCharacters())
    {
        BattlePlayer player = character.GetPlayer();
        if (player != null && player.TeamIndex == teamIndex && player.IsAlive)
        {
            teamCharacters.Add(character);
        }
    }

    return teamCharacters;
}
        private void Tick()
        {
            if (IsRoundActionLocked())
            {
                FreezeAllHeroesDuringRoundPause();
            }

            m_gameObjectManager.Tick();
            m_tileMap.Tick(m_gameObjectManager);
            TickSpawnEventStuffDelayed();
            TickSpawnHeroes();
            TickPetrols();
            
            if (m_gameModeVariation == 27) TickInvasion();
            if (m_gameModeVariation == GAME_MODE_VARIATION_HOT_ZONE) TickKingOfHill();
            if (m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT || m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT_2) TickKnockoutSmoke();
            
            UpdatePlayerStatus();
            
            if (TmpBrawlballTick > 1) TmpBrawlballTick--;
            if (TmpBrawlballTick == 1) ResetCarryable();
            if(KnockoutTicks > 0) KnockoutTicks--;
            if (KnockoutTicks == 1) ResetRoundLaserBall(false);
            RoundTicks++;
            
            EnsureBrawlBallNotStuck();
            
            if (m_gameModeVariation == GAME_MODE_VARIATION_BRAWL_BALL && GoalTicks > 0) GoalTicks--;
            if (GoalTicks <= 1 && ToGoalTeam != -1)
            {
                try { ResetRoundLaserBall(true); } catch (Exception ex) { Console.WriteLine($"[BrawlBall] ResetRoundLaserBall error: {ex?.Message}"); }
                ToGoalTeam = -1;
            }
        }

        private void TickPetrols()
        {
            if (m_gameObjectManager?.Petrols == null) return;
            List<Petrol> ToRemove = new List<Petrol>();
            try
            {
                foreach (Petrol v5 in m_gameObjectManager.Petrols.ToArray())
                {
                    try
                    {
                        if (v5 == null) continue;
                        if (v5.Tick(m_gameObjectManager))
                        {
                            ToRemove.Add(v5);
                            foreach (Petrol v9 in m_gameObjectManager.Petrols)
                            {
                                try
                                {
                                    if (v9 != null && v9.TeamIndex == v5.TeamIndex && LogicMath.Abs(v9.X - v5.X) <= 1 && LogicMath.Abs(v9.Y - v5.Y) <= 1)
                                        v9.Ignite(m_gameObjectManager, v5.Damage);
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                    catch (Exception) { }
                }
                foreach (Petrol v1 in ToRemove)
                {
                    try { m_gameObjectManager.Petrols.Remove(v1); } catch (Exception) { }
                }
            }
            catch (Exception) { }
        }
        
        private int lastSpawnTick = -1;
        
        private void TickInvasion()
        {
            int _firstSpawnTick = 150;
            int _otherSpawnTick = 235;

            int ticksGone = GetTicksGone();
            if (m_tileMap?.InvasionBotSpawn == null || m_tileMap.InvasionBotSpawn.Count == 0) return;

            if (ticksGone == _firstSpawnTick ||
               (ticksGone > _firstSpawnTick && (ticksGone - lastSpawnTick) >= _otherSpawnTick))
            {
                try
                {
                    var areaData = DataTables.Get(DataType.AreaEffect)?.GetData<AreaEffectData>("InvasionSpawnIncoming");
                    if (areaData != null && m_tileMap.InvasionBotSpawn.Count > 0)
                    {
                        AreaEffect area = new AreaEffect(areaData);
                        int spawnIdx = GetRandomInt(m_tileMap.InvasionBotSpawn.Count);
                        area.SetPosition(m_tileMap.InvasionBotSpawn[spawnIdx].X + 150, m_tileMap.InvasionBotSpawn[spawnIdx].Y + 150, 0);
                        area.SetIndex(-16);
                        area.m_ticksElapsed = GetTicksGone();
                        m_gameObjectManager.AddGameObject(area);
                        area.Trigger();
                        lastSpawnTick = ticksGone;
                    }
                }
                catch (Exception) { }
            }

            if (ticksGone == 1500)
            {
                try
                {
                    var areaData = DataTables.Get(DataType.AreaEffect)?.GetData<AreaEffectData>("InvasionSpawnIncoming");
                    if (areaData != null && m_tileMap.InvasionBotSpawn.Count > 0)
                    {
                        AreaEffect area = new AreaEffect(areaData);
                        int spawnIdx = GetRandomInt(m_tileMap.InvasionBotSpawn.Count);
                        area.SetPosition(m_tileMap.InvasionBotSpawn[spawnIdx].X + 150, m_tileMap.InvasionBotSpawn[spawnIdx].Y + 150, 0);
                        area.SetIndex(-16);
                        area.m_ticksElapsed = GetTicksGone();
                        m_gameObjectManager.AddGameObject(area);
                        area.Trigger();
                        lastSpawnTick = ticksGone;
                    }
                }
                catch (Exception) { }
            }
        }
        
        private void SendRoundEndToPlayers()
        {
            Console.WriteLine($"[Ranked] Sending round end to players");
            
            foreach (BattlePlayer player in m_players)
            {
                if (player.GameListener != null)
                {
                    try
                    {
                        BattleEndMessage roundEndMessage = new BattleEndMessage();
                        
                        roundEndMessage.RankedMatch = IsRanked;
                        roundEndMessage.TrueBlueTeamWins = GetTeamScore(0);
                        roundEndMessage.TrueRedTeamWins = GetTeamScore(1);
                        
                        player.GameListener.SendTCPMessage(roundEndMessage);
                        Console.WriteLine($"[Ranked] Round end sent to player {player.PlayerIndex}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Ranked] Error sending round end to player {player.PlayerIndex}: {ex.Message}");
                    }
                }
            }
        }
        
        public void UpdatePlayerStatus()
        {
            if (m_players == null || m_gameObjectManager == null) return;
            foreach (BattlePlayer player in m_players)
            {
                try
                {
                    if (player == null) continue;
                    if (m_gameObjectManager.GetGameObjectByID(player.OwnObjectId) == null && player.IsAlive &&
                        m_gameObjectManager.AddObjects.ToList().Find(obj => obj != null && obj.GetGlobalID() == player.OwnObjectId) == null)
                    {
                        player.IsAlive = false;
                        player.DeathTick = GetTicksGone();
                    }
                }
                catch (Exception) { }

                try
                {
                    Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                    player.UpdateOverCharge();
                    if (character != null)
                    {
                        Accessory v9 = player.Accessory;
                        if (v9 != null) v9.UpdateAccessory(character);
                    }
                }
                catch (Exception) { }
            }
        }

        private void ApplyVipBonus(ClientAvatar avatar, ref int trophiesReward)
        {
            // VIP: x2 кубки за бой (только когда кубки начисляются, потери не удваиваются)
            if (avatar.HasVIP() && trophiesReward > 0)
            {
                trophiesReward *= 2;
            }
        }
        
        private void SendVisionUpdateToPlayers()
{
    try
    {
        bool disableSpectate = m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT || m_gameModeVariation == GAME_MODE_VARIATION_KNOCKOUT_2;
        var disconnected = new List<BattlePlayer>();
        int currentTick = GetTicksGone();

        BattlePlayer[] playersSnapshot = null;
        try { playersSnapshot = m_players?.ToArray(); } catch (Exception) { }
        if (playersSnapshot == null || playersSnapshot.Length == 0) return;

        Parallel.ForEach(playersSnapshot, player =>
        {
            if (player == null) return;
            try
            {
                if (player.GameListener == null) return;
                BitStream visionBitStream = new BitStream(64);
                try
                {
                    m_gameObjectManager.Encode(visionBitStream, m_tileMap, player.OwnObjectId, player.PlayerIndex, player.TeamIndex);
                    
                    // ========== HOT ZONE ФИКС ==========
                    // Принудительно добавляем зону в битовый поток для Hot Zone
                    if (m_gameModeVariation == GAME_MODE_VARIATION_HOT_ZONE)
                    {
                        foreach (AreaEffect area in m_gameObjectManager.GetAreaEffects())
                        {
                            if (area != null && area.AreaEffectData != null && 
                                (area.AreaEffectData.Name == "KingOfHillArea" || area.AreaEffectData.Name == "ZoneArea"))
                            {
                                // Кодируем зону для всех игроков
                                // isOwnObject = false (это не объект игрока)
                                // teamIndex = player.TeamIndex (передаем команду игрока)
                                // playerIndex = player.PlayerIndex (передаем индекс игрока)
                                area.Encode(visionBitStream, false, player.TeamIndex, player.PlayerIndex);
                                Console.WriteLine($"[HotZone] AreaEffect {area.AreaEffectData.Name} закодирован для игрока {player.PlayerIndex}");
                                break;
                            }
                        }
                    }
                    // ========== КОНЕЦ HOT ZONE ФИКС ==========
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SendVisionUpdate] Encode error: {ex.Message}");
                    lock (disconnected) { disconnected.Add(player); }
                    return;
                }
                
                VisionUpdateMessage visionUpdate = new VisionUpdateMessage();
                visionUpdate.Tick = GetTicksGone();
                visionUpdate.HandledInputs = player.LastHandledInput;
                visionUpdate.Viewers = disableSpectate ? 0 : (m_spectators?.Count ?? 0);
                visionUpdate.VisionBitStream = visionBitStream;
                try
                {
                    player.GameListener.SendMessage(visionUpdate);
                }
                catch (Exception)
                {
                    lock (disconnected) { disconnected.Add(player); }
                }
            }
            catch (Exception) { }
        });

        if (disconnected.Count > 0 && !IsRanked)
        {
            foreach (var player in disconnected.Distinct().ToList())
            {
                try { RemovePlayer(player); } catch (Exception) { }
            }
        }

        if (!disableSpectate && m_spectators != null && m_spectators.Count > 0)
        {
            if (currentTick - _lastSpectatorUpdateTick >= SPECTATOR_UPDATE_INTERVAL_TICKS)
            {
                _lastSpectatorUpdateTick = currentTick;

                BitStream spectateStream = new BitStream(64);
                try 
                { 
                    m_gameObjectManager.Encode(spectateStream, m_tileMap, 0, -1, -1);
                    
                    // Для зрителей тоже добавляем зону
                    if (m_gameModeVariation == GAME_MODE_VARIATION_HOT_ZONE)
                    {
                        foreach (AreaEffect area in m_gameObjectManager.GetAreaEffects())
                        {
                            if (area != null && area.AreaEffectData != null && 
                                (area.AreaEffectData.Name == "KingOfHillArea" || area.AreaEffectData.Name == "ZoneArea"))
                            {
                                // Для зрителей: isOwnObject = false, teamIndex = -1 (все видят), playerIndex = -1
                                area.Encode(spectateStream, false, -1, -1);
                                break;
                            }
                        }
                    }
                } 
                catch (Exception ex) 
                { 
                    Console.WriteLine($"[SendVisionUpdate] Spectator encode error: {ex.Message}");
                    return; 
                }

                Task.Run(() =>
                {
                    try
                    {
                        foreach (LogicGameListener gameListener in (m_spectators?.Values?.ToArray() ?? Array.Empty<LogicGameListener>()))
                        {
                            try
                            {
                                if (gameListener == null) continue;
                                VisionUpdateMessage visionUpdate = new VisionUpdateMessage();
                                visionUpdate.Tick = GetTicksGone();
                                visionUpdate.HandledInputs = gameListener.HandledInputs;
                                visionUpdate.Viewers = m_spectators.Count;
                                visionUpdate.VisionBitStream = spectateStream;
                                gameListener.SendMessage(visionUpdate);
                            }
                            catch (Exception) { }
                        }
                    }
                    catch (Exception) { }
                });
            }
        }
    }
    catch (Exception) { }
}
        public int GetKnockoutRound() => m_knockoutRound;

        public int GetKnockoutRoundWins(int teamIndex)
        {
            if (teamIndex >= 0 && teamIndex < 2 && m_knockoutRoundWins != null)
                return m_knockoutRoundWins[teamIndex];
            return 0;
        }
        
        public int GetTeam0AliveCount() => m_team0AliveCount;
        public int GetTeam1AliveCount() => m_team1AliveCount;

        public BattlePlayer[] GetPlayers() => m_players?.ToArray() ?? Array.Empty<BattlePlayer>();

        public int GetRandomInt(int min, int max) => m_random.Rand(max - min) + min;
        public int GetRandomInt(int max) => m_random.Rand(max);
        public int GetTicksGone() => m_time.GetTick();
        public int GetGameModeVariation() => m_gameModeVariation;
        public int GetPlayersCountWithGameModeVariation() => m_playersCountWithGameModeVariation;
        public int GetRandomSeed() => m_randomSeed;

        public LocationData Location
        {
            get
            {
                return DataTables.Get(DataType.Location).GetDataByGlobalId<LocationData>(m_locationId);
            }
        }

        public long Id { get; set; }

        public BattlePlayer GetPlayerByAccountId(long accountId)
        {
            if (m_playersByAccountId != null && m_playersByAccountId.TryGetValue(accountId, out var player))
                return player;
            
            player = m_players?.FirstOrDefault(x => x != null && x.AccountId == accountId);
            
            if (player != null && m_playersByAccountId != null)
            {
                m_playersByAccountId[accountId] = player;
            }
            
            return player;
        }

        private void TickKnockoutSmoke()
        {
            if (nokbool) return;
            const int smokeStartTicks = 20 * 15;
            if (RoundTicks < smokeStartTicks) return;

            if (!_knockoutSmokeSpawnedThisRound)
            {
                Console.WriteLine($"[Knockout] Создаем дым с уроном! Раунд: {m_knockoutRound}, RoundTick: {RoundTicks}");
                _knockoutSmokeSpawnedThisRound = true;
            }
        }
    }
}