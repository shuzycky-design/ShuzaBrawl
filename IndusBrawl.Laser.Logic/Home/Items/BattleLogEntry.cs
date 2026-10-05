using IndusBrawl.Laser.Logic.Home;
using System;
using System.Collections.Generic;
using System.Linq;
using IndusBrawl.Laser.Titan.DataStream;
using Newtonsoft.Json;
using IndusBrawl.Laser.Logic.Data;

namespace BattleLogEntry
{
    public enum BattleType
    {
        Normal = 1,
        Crash = 2,
        Survived = 3,
        BigGame = 4,
        Duels = 7
    }

    public enum BattleResult
    {
        Defeat = 0,
        Victory = 1,
        Draw = 2
    }

    public class PlayerBattleInfo
    {
        public int Index { get; set; }
        public long AccountId { get; set; }
        public string Name { get; set; }
        public int BrawlerId { get; set; }
        public int BrawlerTrophies { get; set; }
        public int BrawlerLevel { get; set; }
        public bool IsStarPlayer { get; set; }
        public int Team { get; set; }
        public int PlayerExperience { get; set; }

        public int ThumbnailId { get; set; } = 28000000;
        public int NameColorId { get; set; } = 43000000;

        // Формат сверен с клиентом v53 (участник записи истории боёв)
        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Index);
            stream.WriteLong(AccountId);
            stream.WriteVInt(Team);
            stream.WriteBoolean(IsStarPlayer);

            stream.WriteVInt(1); // количество бойцов игрока в этом бою
            stream.WriteDataReference(BrawlerId);
            stream.WriteVInt(BrawlerTrophies);
            stream.WriteVInt(BrawlerTrophies);
            stream.WriteVInt(Math.Clamp(BrawlerLevel - 1, 0, 10)); // уровень силы у клиента считается с нуля

            stream.WriteVInt(0);

            // PlayerDisplayData
            stream.WriteString(Name ?? "Player");
            stream.WriteVInt(PlayerExperience);
            stream.WriteVInt(ThumbnailId);
            stream.WriteVInt(NameColorId);
            stream.WriteVInt(-1);

            stream.WriteBoolean(false);
        }

        public static PlayerBattleInfo CreateBot(int index, string name = "Bot")
        {
            return new PlayerBattleInfo
            {
                Index = index,
                AccountId = 0,
                Name = name,
                BrawlerId = Constants.BRAWLER_BASE_ID,
                BrawlerTrophies = 500,
                BrawlerLevel = 1,
                IsStarPlayer = false,
                Team = 0,
                PlayerExperience = 1000
            };
        }
    }

    public static class Constants
    {
        public const int MAX_PLAYERS_PER_BATTLE = 10;
        public const int MAX_BATTLE_LOGS_DISPLAY = 25;
        public const int BRAWLER_BASE_ID = 16000000;
        public const int DEFAULT_BATTLE_TIME = 140;
        public const int DEFAULT_PLAYER_EXPERIENCE = 1000;
    }

    public class BattleLogStructure
    {
        public DateTime BattleLogCreateTime { get; set; }
        public BattleType BattleType { get; set; }
        public int TrophiesChange { get; set; }
        public int BattleDuration { get; set; }
        public bool IsFriendlyMatch { get; set; }
        public int MapId { get; set; }
        public BattleResult Result { get; set; }
        public int Index { get; set; }
        public List<PlayerBattleInfo> Players { get; set; }

        public BattleLogStructure()
        {
            Players = new List<PlayerBattleInfo>();
            BattleLogCreateTime = DateTime.UtcNow;
        }

        public void AddPlayer(PlayerBattleInfo player)
        {
            if (player == null) return;
            if (Players.Count < Constants.MAX_PLAYERS_PER_BATTLE)
            {
                Players.Add(player);
            }
        }

        // Формат сверен с клиентом v53 (запись истории боёв). Раньше после результата шло лишнее поле,
        // а число участников стояло не на своём месте (клиент читал -1) - из-за этого история была пустой.
        public void Encode(ByteStream stream)
        {
            if (stream == null) return;

            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt((int)(DateTime.UtcNow - BattleLogCreateTime).TotalSeconds);
            stream.WriteVInt((int)BattleType);
            stream.WriteVInt(TrophiesChange);
            stream.WriteVInt(BattleDuration);
            stream.WriteBoolean(IsFriendlyMatch);
            stream.WriteDataReference(MapId);
            stream.WriteVInt((int)Result);
            stream.WriteVInt(0);

            stream.WriteLong(0L);
            stream.WriteLong(0L);

            stream.WriteVInt(Players.Count);
            stream.WriteBoolean(Players.Count > 0);

            stream.WriteVInt(0); // список чисел (пустой)

            stream.WriteVInt(Players.Count);
            foreach (var player in Players)
            {
                player.Encode(stream);
            }

            stream.WriteVInt(0);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);

            stream.WriteBoolean(false); // необязательный блок 1
            stream.WriteBoolean(false); // необязательный блок 2

            stream.WriteVInt(0);
        }
    }

    public interface IBattleLogFactory
    {
        BattleLogStructure CreateNormalBattle(int mapId, BattleResult result, int trophiesChange);
        BattleLogStructure CreateDuelsBattle(int mapId, BattleResult result, int trophiesChange);
        BattleLogStructure CreateBigGameBattle(int mapId, BattleResult result);
        BattleLogStructure CreateWithPlayers(int mapId, BattleResult result, List<PlayerBattleInfo> players);
    }

    public class BattleLogFactory : IBattleLogFactory
    {
        public BattleLogStructure CreateNormalBattle(int mapId, BattleResult result, int trophiesChange)
        {
            var battle = new BattleLogStructure
            {
                BattleType = BattleType.Normal,
                MapId = mapId,
                Result = result,
                TrophiesChange = trophiesChange,
                BattleDuration = Constants.DEFAULT_BATTLE_TIME,
                IsFriendlyMatch = false
            };

            AddDefaultPlayers(battle);
            return battle;
        }

        public BattleLogStructure CreateDuelsBattle(int mapId, BattleResult result, int trophiesChange)
        {
            var battle = new BattleLogStructure
            {
                BattleType = BattleType.Duels,
                MapId = mapId,
                Result = result,
                TrophiesChange = trophiesChange,
                BattleDuration = 90,
                IsFriendlyMatch = false
            };

            // Duels typically have fewer players
            for (int i = 0; i < 4; i++)
            {
                battle.AddPlayer(PlayerBattleInfo.CreateBot(i + 1, $"DuelsBot_{i + 1}"));
            }

            return battle;
        }

        public BattleLogStructure CreateBigGameBattle(int mapId, BattleResult result)
        {
            var battle = new BattleLogStructure
            {
                BattleType = BattleType.BigGame,
                MapId = mapId,
                Result = result,
                TrophiesChange = 0, // Big Game typically doesn't affect trophies
                BattleDuration = 180,
                IsFriendlyMatch = false
            };

            AddDefaultPlayers(battle);
            return battle;
        }

        public BattleLogStructure CreateWithPlayers(int mapId, BattleResult result, List<PlayerBattleInfo> players)
        {
            var battle = new BattleLogStructure
            {
                BattleType = BattleType.Normal,
                MapId = mapId,
                Result = result,
                TrophiesChange = CalculateTrophiesChange(result),
                BattleDuration = Constants.DEFAULT_BATTLE_TIME,
                IsFriendlyMatch = false
            };

            if (players != null)
            {
                foreach (var player in players.Take(Constants.MAX_PLAYERS_PER_BATTLE))
                {
                    battle.AddPlayer(player);
                }
            }

            return battle;
        }

        private void AddDefaultPlayers(BattleLogStructure battle)
        {
            if (battle == null) return;

            for (int i = 0; i < Constants.MAX_PLAYERS_PER_BATTLE; i++)
            {
                battle.AddPlayer(PlayerBattleInfo.CreateBot(i + 1));
            }
        }

        private int CalculateTrophiesChange(BattleResult result)
        {
            return result switch
            {
                BattleResult.Victory => 8,
                BattleResult.Defeat => -6,
                BattleResult.Draw => 0,
                _ => 0
            };
        }
    }

    public class BattleLog
    {
        [JsonProperty("battle_logs")]
        public List<BattleLogStructure> BattleLogs { get; set; }

        private readonly IBattleLogFactory _factory;

        public BattleLog(IBattleLogFactory factory = null)
        {
            _factory = factory ?? new BattleLogFactory();
            BattleLogs = new List<BattleLogStructure>();
        }

        // Старый метод для обратной совместимости - ИСПРАВЛЕН
        public void Add(BattleLogStructure log, int battleLogType, int trophiesResult, int battleTime, bool isFriendly, int mapId, int result)
        {
            if (log == null) 
            {
                // Если log null, создаем новый
                log = new BattleLogStructure();
            }

            log.Index = GetNextIndex();
            log.BattleLogCreateTime = DateTime.UtcNow;
            log.BattleType = (BattleType)battleLogType;
            log.TrophiesChange = trophiesResult;
            log.BattleDuration = battleTime;
            log.IsFriendlyMatch = isFriendly;
            log.MapId = mapId;
            log.Result = (BattleResult)result;
            
            BattleLogs.Insert(0, log);
            
            // Maintain maximum logs
            if (BattleLogs.Count > Constants.MAX_BATTLE_LOGS_DISPLAY * 2)
            {
                BattleLogs = BattleLogs.Take(Constants.MAX_BATTLE_LOGS_DISPLAY * 2).ToList();
            }
        }

        public void AddBattleLog(BattleLogStructure battleLog)
        {
            if (battleLog == null) return;

            battleLog.Index = GetNextIndex();
            BattleLogs.Insert(0, battleLog); // Add to beginning for chronological order

            // Maintain maximum logs
            if (BattleLogs.Count > Constants.MAX_BATTLE_LOGS_DISPLAY * 2)
            {
                BattleLogs = BattleLogs.Take(Constants.MAX_BATTLE_LOGS_DISPLAY * 2).ToList();
            }
        }

        public BattleLogStructure AddNormalBattle(int mapId, BattleResult result, int trophiesChange = 0)
        {
            var battle = _factory.CreateNormalBattle(mapId, result, trophiesChange);
            AddBattleLog(battle);
            return battle;
        }

        public BattleLogStructure AddDuelsBattle(int mapId, BattleResult result, int trophiesChange = 0)
        {
            var battle = _factory.CreateDuelsBattle(mapId, result, trophiesChange);
            AddBattleLog(battle);
            return battle;
        }

        public BattleLogStructure AddBigGameBattle(int mapId, BattleResult result)
        {
            var battle = _factory.CreateBigGameBattle(mapId, result);
            AddBattleLog(battle);
            return battle;
        }

        public void Clear()
        {
            BattleLogs.Clear();
        }

        public void RemoveOldLogs(int keepCount = Constants.MAX_BATTLE_LOGS_DISPLAY)
        {
            if (BattleLogs.Count > keepCount)
            {
                BattleLogs = BattleLogs.Take(keepCount).ToList();
            }
        }

        public int GetNextIndex()
        {
            if (BattleLogs.Count == 0) return 0;
            return BattleLogs.Max(x => x.Index) + 1;
        }

        public int GetIndex()
        {
            return BattleLogs.Count;
        }

        public void Encode(ByteStream stream)
        {
            if (stream == null) return;

            var logsToEncode = BattleLogs
                .OrderByDescending(x => x.BattleLogCreateTime)
                .Take(Constants.MAX_BATTLE_LOGS_DISPLAY)
                .ToList();

            stream.WriteVInt(logsToEncode.Count);

            foreach (var log in logsToEncode)
            {
                log.Encode(stream);
            }

            Console.WriteLine($"Encoded {logsToEncode.Count} battle logs");
        }

        // НОВЫЙ МЕТОД для простого добавления лога
        public void AddSimpleBattle(int mapId, BattleResult result, int trophiesChange = 0)
        {
            AddNormalBattle(mapId, result, trophiesChange);
        }
    }

    // Extension methods for easier usage
    public static class BattleLogExtensions
    {
        public static BattleLogStructure SetFriendlyMatch(this BattleLogStructure battle, bool isFriendly = true)
        {
            if (battle == null) return null;
            
            battle.IsFriendlyMatch = isFriendly;
            battle.TrophiesChange = 0; // Friendly matches don't affect trophies
            return battle;
        }

        public static BattleLogStructure SetBattleDuration(this BattleLogStructure battle, int duration)
        {
            if (battle == null) return null;
            
            battle.BattleDuration = Math.Max(0, duration);
            return battle;
        }

        public static BattleLogStructure SetStarPlayer(this BattleLogStructure battle, int playerIndex)
        {
            if (battle == null) return null;
            
            var player = battle.Players.FirstOrDefault(p => p.Index == playerIndex);
            if (player != null)
            {
                // Reset all players first
                foreach (var p in battle.Players)
                {
                    p.IsStarPlayer = false;
                }
                player.IsStarPlayer = true;
            }
            return battle;
        }

        // Новый метод для быстрого создания лога с игроками
        public static BattleLogStructure AddRealPlayer(this BattleLogStructure battle, long accountId, string name, int brawlerId, int brawlerTrophies, int brawlerLevel, int team)
        {
            if (battle == null) return null;

            var player = new PlayerBattleInfo
            {
                Index = battle.Players.Count + 1,
                AccountId = accountId,
                Name = name,
                BrawlerId = brawlerId,
                BrawlerTrophies = brawlerTrophies,
                BrawlerLevel = brawlerLevel,
                Team = team,
                PlayerExperience = Constants.DEFAULT_PLAYER_EXPERIENCE
            };

            battle.AddPlayer(player);
            return battle;
        }
    }
}