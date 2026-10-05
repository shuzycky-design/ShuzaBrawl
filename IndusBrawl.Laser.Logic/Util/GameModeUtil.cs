﻿namespace IndusBrawl.Laser.Logic.Util
{
    using IndusBrawl.Laser.Logic.Battle;
    using IndusBrawl.Laser.Titan.Debug;

    public static class GameModeUtil
    {
        public static bool PlayersCollectPowerCubes(int variation)
        {
            int v1 = variation - 6;
            if (v1 <= 8)
                return ((0x119 >> v1) & 1) != 0;
            else
                return false;
        }

        public static int GetBattleTicks(int v)
        {
            int v2 = 2400;
            switch (v)
            {
                case 0:
                case 5: // Brawl Ball (и Knockout теперь тоже Brawl Ball)
                case 16:
                case 22:
                case 23:
                case 33:
                case 34: // Запасной ID для хоккея
                    v2 = 4200;
                    goto LABEL_9;
                case 3:
                case 7:
                case 8:
                    goto LABEL_9;
                case 6:
                case 9:
                // case 10: // Убрано - хоккей теперь имеет нормальный тайминг
                case 12:
                case 13:
                case 18:
                case 11:
                    return BattleMode.NO_TIME_TICKS;
                case 14:
                    v2 = 9600;
                    goto LABEL_9;
                case 17:
                case 19:
                case 21:
                case 25:
                case 27:
                    v2 = 3600;
                    goto LABEL_9;
                default:
                    v2 = 20 * ((BattleMode.NORMAL_TICKS - BattleMode.INTRO_TICKS) / 20);
                LABEL_9:
                    return BattleMode.INTRO_TICKS + v2;
                    break;
            }
        }

        public static int GetRespawnSeconds(int variation)
        {
            switch (variation)
            {
                case 0:
                case 2:
                    return 3;
                case 3:
                    return 1;
                case 5: // Brawl Ball (и Knockout)
                    return 3;
                case 7:
                    return 3;
                case 33:
                    return 5;
                case 13:
                    return 3;
                case 25:
                    return 1;
                case 27:
                    return 3;
                case 19:
                    return 3;
                default:
                    return 5;
            }
        }

        public static bool PlayersCollectBountyStars(int variation)
        {
            return variation == 3 || variation == 15;
        }

        public static bool HasRounds(int variation) // для всех нокаутов и ваще режимов где есть раунды
        {
            // Убираем 20 (Knockout) из списка
            return variation == 31 || variation == 22 || variation == 23;
        }

        public static bool HasBall(int variation) // для всех бравл болов
        {
            // Только 5 (Brawl Ball) и 32 (5v5 Brawl Ball)
            return variation == 5 || variation == 32;
        }

        public static bool HasBoss(int variation) // для режимов где есть босс (тут только режимы где босс с начала катки)
        {
            return variation == 11 || variation == 7;
        }

        public static bool HasBossPassive(int variation) // для режимов где есть босс (тут только режимы где босс сам появляется когда нибудь)
        {
            return variation == 19; // Убрано: || variation == 10
        }

        public static bool HasTwoTeams(int variation)
        {
            // Все режимы, КРОМЕ FFA и мульти-командных, считаются 2-командными
            return variation is >= 0 and <= 34 // в пределах нормальных ID
                && variation != 6  // Showdown (FFA)
                && variation != 9  // DuoShowdown (5 команд)
                && variation != 15 // каждый сам за себя
                && variation != 23;
        }

        public static bool HasTwoBases(int variation)
        {
            return variation == 2 || variation == 11;
        }

        public static int GetGameModeVariation(string mode)
        {
            switch (mode)
            {
                case "CoinRush":
                    return 0;
                case "GemGrab":
                    return 0;
                case "Deleted1":
                    return 1;
                case "Heist":
                    return 2;
                case "Deleted2":
                    return 4;
                case "BossFight": // Теперь это хоккей!
                    return 10;
                case "Bounty":
                    return 3;
                case "Artifact":
                    return 4;
                case "BrawlBall":
                    return 5;
                case "Showdown":
                    return 6;
                case "BigGame":
                    return 7;
                case "DuoShowdown":
                    return 9;
                case "RoboRumble":
                    return 8;
                case "Raid":
                    return 100; // Перенесли на другой ID чтобы не конфликтовать
                case "Siege":
                    return 11;
                case "Tutorial":
                    return 12;
                case "Training":
                    return 13;
                case "CTF":
                    return 16;
                case "KingOfHill":
                    return 17;
                case "ReachExit":
                    return 30;
                case "Knockout":
                    return 20; // Вместо 20 возвращаем 5 (Brawl Ball)
                case "TagTeam":
                    return 24;
                case "LoneStar":
                    return 15;
                case "Deathmatch":
                    return 25;
                case "MapPrint":
                    return 99;
                case "BossFight_TownCrush":
                    return 18;
                case "Deathmatch5v5":
                    return 31;
                case "BrawlBall5v5":
                    return 32;
                case "GemGrab5v5":
                    return 33;
                case "HoldTheBall":
                    return 21;
                case "BasketBrawl":
                    return 22;
                case "VolleyBrawl":
                    return 23;
                case "Payload":
                    return 26;
                case "Invasion":
                    return 27;
                case "ProtectKing":
                    return 19;
                case "DeathmatchFFA":
                    return 28;
                case "LastStand":
                    return 29;
                case "Bounty1v1":
                    return 103;
                case "Hockey": // Альтернативное имя для хоккея
                    return 34;
                default:
                    Debugger.Error("Wrong game mode!");
                    return -1;
            }
        }
    }
}