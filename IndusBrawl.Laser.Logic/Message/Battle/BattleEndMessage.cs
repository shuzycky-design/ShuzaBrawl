// IndusBrawl.Laser.Logic.Message.Battle.BattleEndMessage.cs
namespace IndusBrawl.Laser.Logic.Message.Battle
{
    using System;
    using System.Collections.Generic;
    using IndusBrawl.Laser.Logic.Battle.Structures;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home.Quest;
    using IndusBrawl.Laser.Logic.Home.Structures;

    public class BattleEndMessage : GameMessage
    {
        public BattleEndMessage() : base()
        {
            ProgressiveQuests = new List<Quest>();
            Players = new List<BattlePlayer>();
            
            // Инициализация ranked полей
            RankedMatch = false;
            Rank = 0;
            UpRank = 0;
            ELO = 0;
            NewELO = 0;
            Add = 0;
            TrueBlueTeamWins = 0;
            TrueRedTeamWins = 0;
            Round = 0;
            
            // Инициализация полей силовой лиги
            IsPowerLeagueMatch = false;
            PowerLeagueWins = 0;
            PowerLeagueWinsRequired = 3;
            PowerLeagueRank = 0;
            PowerLeaguePointsGained = 0;
            PowerLeagueTier = 0;
        }

        // Основные поля
        public int Result;
        public int TokensReward;
        public int TokensDoublersReward;
        public int TokensRemainingReward;
        public int TrophiesReward;
        public List<BattlePlayer> Players { get; set; }
        public List<Quest> ProgressiveQuests;
        public BattlePlayer OwnPlayer;
        public bool StarToken;
        public int GameMode;
        public bool BattleWithoutTrophies;
        public bool IsPvP;
        public int Winstreak;
        public int WinstreakTrophies;
        public int MasteryGained;
        public int MasteryPoints;
        public int CoinsGained;
        public long StarPlayerAccountId;

        // Ranked поля
        public bool RankedMatch;
        public int MatchType;
        public int Rank;
        public int UpRank;
        public int ELO;
        public int NewELO;
        public int TrueBlueTeamWins;
        public int TrueRedTeamWins;
        public int Round;
        public bool Solo;
        public int Add;

        // ПОЛЯ СИЛОВОЙ ЛИГИ (Power League) - МОЩНЫЕ И ПРОКАЧАННЫЕ
        public bool IsPowerLeagueMatch;
        public int PowerLeagueWins;
        public int PowerLeagueWinsRequired;
        public int PowerLeagueRank;
        public int PowerLeaguePointsGained;
        public int PowerLeagueTier;

        public override void Encode()
        {
            // Создаем безопасный снапшот игроков
            List<BattlePlayer> playersSnapshot = Players != null 
                ? new List<BattlePlayer>(Players) 
                : new List<BattlePlayer>();

            Stream.WriteLong(OwnPlayer?.AccountId ?? 0);
            Stream.WriteLong(OwnPlayer?.AccountId ?? 0);

            Stream.WriteVInt(GameMode);
            Stream.WriteVInt(Result);
            Stream.WriteVInt(TokensReward);
            Stream.WriteVInt(TrophiesReward);
            
            // СИЛОВАЯ ЛИГА: структура с 5 полями (если активна)
            if (IsPowerLeagueMatch)
            {
                Stream.WriteVInt(5); // Количество полей
                Stream.WriteVInt(PowerLeagueWins);           // Текущие победы
                Stream.WriteVInt(PowerLeagueWinsRequired);   // Нужно побед
                Stream.WriteVInt(PowerLeagueRank);           // Текущий ранг
                Stream.WriteVInt(PowerLeaguePointsGained);   // Получено очков
                Stream.WriteVInt(PowerLeagueTier);           // Дивизион
            }
            else
            {
                Stream.WriteVInt(0); // Обычный режим
            }
            
            Stream.WriteVInt(TokensDoublersReward);
            Stream.WriteVInt(0); // Double Token Event
            Stream.WriteVInt(TokensRemainingReward);
            Stream.WriteVInt(0); // Длительность боя
            Stream.WriteVInt(0); // Epic Win Points
            Stream.WriteVInt(0); // Championship Level

            Stream.WriteBoolean(false); // gen offer
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteBoolean(false); // 164
            Stream.WriteBoolean(false); // 165

            Stream.WriteVInt(CoinsGained); // Монеты (теперь работают!)
            Stream.WriteVInt(0); // chromalavina
            Stream.WriteVInt(0); // underdog
            Stream.WriteVInt(WinstreakTrophies);
            Stream.WriteVInt(Winstreak);
            Stream.WriteVInt(0); // trophies +
            Stream.WriteVInt(0); // trophies +
            Stream.WriteVInt(0); // v53

            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false); // no experience
            Stream.WriteBoolean(false); // no tokens left
            Stream.WriteBoolean(BattleWithoutTrophies);
            Stream.WriteBoolean(IsPvP);
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(IsPowerLeagueMatch); // Флаг силовой лиги!
            Stream.WriteBoolean(false);

            Stream.WriteVInt(-1); // ChallengeType
            Stream.WriteBoolean(false);

            // Запись игроков
            Stream.WriteVInt(playersSnapshot.Count);
            foreach (BattlePlayer player in playersSnapshot)
            {
                if (player == null) continue;

                bool isOwnPlayer = (OwnPlayer != null) && (player.AccountId == OwnPlayer.AccountId);
                bool isEnemy = (OwnPlayer != null) && (player.TeamIndex != OwnPlayer.TeamIndex);

                Stream.WriteBoolean(isOwnPlayer);
                Stream.WriteBoolean(isEnemy);
                Stream.WriteBoolean(player.AccountId == StarPlayerAccountId);
                Stream.WriteVInt(1);
                ByteStreamHelper.WriteDataReference(Stream, player.CharacterId);
                
                Stream.WriteVInt(1);
                ByteStreamHelper.WriteDataReference(Stream, player.SkinId);
                
                Stream.WriteVInt(1);
                Stream.WriteVInt(player.Trophies);
                
                Stream.WriteVInt(1);
                Stream.WriteVInt(player.HeroPowerLevel);
                
                Stream.WriteVInt(1);
                Stream.WriteVInt(0);

                Stream.WriteVInt(0);
                Stream.WriteVInt(0);
                
                // Боты в обычном виде идут без идентификатора игрока, и клиент не даёт ставить им "лайк".
                // Если включён файл-переключатель bots_as_players, боты описываются как обычные игроки.
                if (Stream.WriteBoolean(player.IsBot() == 0 || System.IO.File.Exists("bots_as_players")))
                {
                    Stream.WriteLong(player.AccountId);
                }

                // DisplayData с защитой
                try
                {
                    if (player.DisplayData != null)
                        player.DisplayData.Encode(Stream);
                    else
                        Stream.WriteVInt(0);
                }
                catch
                {
                    Stream.WriteVInt(0);
                }
                
                Stream.WriteBoolean(false);

                // Мастерство
                Hero hero = null;
                if (player != null && player.IsBot() < 1 && player.Avatar != null)
                {
                    hero = player.Avatar.GetHero(player.CharacterId);
                }

                if (hero != null)
                {
                    Stream.WriteVInt(1);
                    Stream.WriteVInt(MasteryPoints);
                    Stream.WriteVInt(1);
                    Stream.WriteVInt(MasteryGained);
                }
                else
                {
                    Stream.WriteVInt(0);
                    Stream.WriteVInt(0);
                }

                Stream.WriteInt16((short)(player.Kills));
                Stream.WriteInt16((short)(player.Deaths));
                Stream.WriteInt(player.Damage);
                Stream.WriteInt(player.Heals);
                
                // Титул (только для своего игрока)
                int titleId = 0;
                if (player == OwnPlayer && player.Home?.DefaultBattleCard != null)
                {
                    titleId = player.Home.DefaultBattleCard.Title;
                }
                ByteStreamHelper.WriteDataReference(Stream, titleId);
            }

            Stream.WriteVInt(0); // xp
            Stream.WriteVInt(0); // dataref

            // Статистика игрока
            Stream.WriteVInt(2);
            {
                Stream.WriteVInt(1);
                Stream.WriteVInt(OwnPlayer?.Trophies ?? 0);
                Stream.WriteVInt(OwnPlayer?.HighestTrophies ?? 0);
                Stream.WriteVInt(5);
                Stream.WriteVInt(100);
                Stream.WriteVInt(100);
            }

            // Thumbnail
            int thumbnailId = 0;
            var thumbnail = OwnPlayer?.Home?.Thumbnail;
            if (thumbnail != null)
            {
                thumbnailId = thumbnail.GetGlobalId();
            }
            ByteStreamHelper.WriteDataReference(Stream, thumbnailId);

            // Play Again
            if (Stream.WriteBoolean(false))
            {
                Stream.WriteInt(3);
                Stream.WriteVInt(0);
                Stream.WriteVInt(0);
                Stream.WriteInt(0);
                Stream.WriteInt(0);
            }

            // Квесты
            if (Stream.WriteBoolean(false))
            {
                Stream.WriteVInt(-1);
                Stream.WriteVInt(0);
                Stream.WriteVInt(0);
                Stream.WriteVInt(0);
            }

            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);

            // RANKED МОД (УСИЛЕННАЯ ВЕРСИЯ)
            if (Stream.WriteBoolean(RankedMatch))
            {
                // Расчет прогресса для плавной анимации
                int oldInRank = Rank > 0 ? Math.Clamp(ELO - (Rank - 1) * 1000, 0, 999) : (ELO % 1000);
                int newInRank = UpRank > 0 ? Math.Clamp(NewELO - (UpRank - 1) * 1000, 0, 999) : (NewELO % 1000);
                
                // При повышении ранга ставим 999 для полной анимации
                if (UpRank > Rank)
                    newInRank = 999;
                
                Console.WriteLine($"[BattleEndMessage] Ranked: old={ELO} (inRank={oldInRank}), new={NewELO} (inRank={newInRank}), rankUp={UpRank > Rank}, Add={Add}");

                Stream.WriteVInt(6);
                Stream.WriteVInt(Solo ? 0 : 1);      // match type
                Stream.WriteVInt(newInRank);          // цель (куда едет полоска)
                Stream.WriteVInt(Rank);                // старый ранг
                Stream.WriteVInt(oldInRank);          // старт (откуда)
                Stream.WriteVInt(UpRank);              // новый ранг
                Stream.WriteVInt(Add);                 // очки +/-
                Stream.WriteVInt(TrueBlueTeamWins);    // победы синих
                Stream.WriteVInt(TrueRedTeamWins);     // победы красных
                Stream.WriteVInt(Round);                // текущий раунд
                Stream.WriteVInt(10);                   // время
                
                // Дополнительные поля для полной совместимости
                Stream.WriteVInt(0); // quest
                Stream.WriteVInt(0); // unknown
                Stream.WriteVInt(0); // unknown
                Stream.WriteVInt(0); // unknown
                Stream.WriteVInt(0); // unknown
                Stream.WriteVInt(0); // unknown
                Stream.WriteVInt(0); // unknown
                Stream.WriteVInt(4); // round needed
                Stream.WriteVInt(0); // unknown
            }

            Stream.WriteVInt(-1);
            Stream.WriteBoolean(false); // chronosTextEntry
            Stream.WriteVInt(0);

            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);
        }

        public override int GetMessageType()
        {
            return 23456;
        }

        public override int GetServiceNodeType()
        {
            return 27;
        }
    }
}