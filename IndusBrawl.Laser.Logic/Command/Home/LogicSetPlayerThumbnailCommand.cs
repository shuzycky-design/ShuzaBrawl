namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;
    using System.Collections.Generic;
    using IndusBrawl.Laser.Logic.Message.Account.Auth;
    using IndusBrawl.Laser.Logic.Util;

    public class LogicSetPlayerThumbnailCommand : Command
    {
        public int ThumbnailInstanceId;

        // Таблица для иконок — содержит ТОЛЬКО Free, Trophies, HeroOwnership
        // Все остальные иконки (не указанные здесь) требуют владения по умолчанию
        private static readonly Dictionary<int, IconType> IconRules = new Dictionary<int, IconType>
        {
            { 0, new IconType { RequirementType = Requirement.Free, TrophyRequirement = 0 } },
            { 1, new IconType { RequirementType = Requirement.Free, TrophyRequirement = 0 } },
            { 2, new IconType { RequirementType = Requirement.Free, TrophyRequirement = 0 } },
            { 3, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000000, TrophyRequirement = 0 } },
            { 4, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000001, TrophyRequirement = 0 } },
            { 5, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000003, TrophyRequirement = 0 } },
            { 6, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000007, TrophyRequirement = 0 } },
            { 7, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000008, TrophyRequirement = 0 } },
            { 8, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000009, TrophyRequirement = 0 } },
            { 9, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000010, TrophyRequirement = 0 } },
            { 10, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000002, TrophyRequirement = 0 } },
            { 11, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000004, TrophyRequirement = 0 } },
            { 12, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000006, TrophyRequirement = 0 } },
            { 13, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000013, TrophyRequirement = 0 } },
            { 14, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000011, TrophyRequirement = 0 } },
            { 15, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000014, TrophyRequirement = 0 } },
            { 16, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000005, TrophyRequirement = 0 } },
            { 17, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000012, TrophyRequirement = 0 } },
            { 18, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000015, TrophyRequirement = 0 } },
            { 19, new IconType { RequirementType = Requirement.Free, TrophyRequirement = 0 } },
            { 20, new IconType { RequirementType = Requirement.Free, TrophyRequirement = 0 } },
            { 21, new IconType { RequirementType = Requirement.Free, TrophyRequirement = 0 } },
            { 22, new IconType { RequirementType = Requirement.Free, TrophyRequirement = 0 } },
            { 23, new IconType { RequirementType = Requirement.Free, TrophyRequirement = 0 } },
            { 24, new IconType { RequirementType = Requirement.Trophies, TrophyRequirement = 500 } },
            { 25, new IconType { RequirementType = Requirement.Trophies, TrophyRequirement = 1000 } },
            { 26, new IconType { RequirementType = Requirement.Trophies, TrophyRequirement = 2000 } },
            { 27, new IconType { RequirementType = Requirement.Trophies, TrophyRequirement = 3000 } },
            { 28, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000016, TrophyRequirement = 0 } },
            { 29, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000017, TrophyRequirement = 0 } },
            { 30, new IconType { RequirementType = Requirement.Trophies, TrophyRequirement = 5000 } },
            { 31, new IconType { RequirementType = Requirement.Trophies, TrophyRequirement = 7000 } },
            { 32, new IconType { RequirementType = Requirement.Trophies, TrophyRequirement = 10000 } },
            { 33, new IconType { RequirementType = Requirement.Trophies, TrophyRequirement = 13000 } },
            { 34, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000018, TrophyRequirement = 0 } },
            { 35, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000019, TrophyRequirement = 0 } },
            { 36, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000020, TrophyRequirement = 0 } },
            { 37, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000023, TrophyRequirement = 0 } },
            { 38, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000021, TrophyRequirement = 0 } },
            { 39, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000025, TrophyRequirement = 0 } },
            { 40, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000024, TrophyRequirement = 0 } },
            { 41, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000026, TrophyRequirement = 0 } },
            { 42, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000022, TrophyRequirement = 0 } },
            { 43, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000027, TrophyRequirement = 0 } },
            { 44, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000028, TrophyRequirement = 0 } },
            { 45, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000030, TrophyRequirement = 0 } },
            { 46, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000029, TrophyRequirement = 0 } },
            { 47, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000032, TrophyRequirement = 0 } },
            { 48, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000031, TrophyRequirement = 0 } },
            { 49, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000034, TrophyRequirement = 0 } },
            { 50, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000037, TrophyRequirement = 0 } },
            { 51, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000035, TrophyRequirement = 0 } },
            { 52, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000036, TrophyRequirement = 0 } },
            { 55, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000040, TrophyRequirement = 0 } },
            { 57, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000042, TrophyRequirement = 0 } },
            { 58, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000043, TrophyRequirement = 0 } },
            { 62, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000045, TrophyRequirement = 0 } },
            { 66, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000047, TrophyRequirement = 0 } },
            { 68, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000050, TrophyRequirement = 0 } },
            { 74, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000052, TrophyRequirement = 0 } },
            { 80, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000048, TrophyRequirement = 0 } },
            { 86, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000058, TrophyRequirement = 0 } },
            { 150, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000061, TrophyRequirement = 0 } },
            { 183, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000064, TrophyRequirement = 0 } },
            { 185, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000063, TrophyRequirement = 0 } },
            { 246, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000067, TrophyRequirement = 0 } },
            { 266, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000069, TrophyRequirement = 0 } },
            { 289, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000071, TrophyRequirement = 0 } },
            { 320, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000073, TrophyRequirement = 0 } },
            { 53, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000038, TrophyRequirement = 0 } },
            { 54, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000039, TrophyRequirement = 0 } },
            { 56, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000041, TrophyRequirement = 0 } },
            { 59, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000044, TrophyRequirement = 0 } },
            { 65, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000046, TrophyRequirement = 0 } },
            { 67, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000049, TrophyRequirement = 0 } },
            { 71, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000051, TrophyRequirement = 0 } },
            { 75, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000053, TrophyRequirement = 0 } },
            { 81, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000054, TrophyRequirement = 0 } },
            { 82, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000056, TrophyRequirement = 0 } },
            { 85, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000057, TrophyRequirement = 0 } },
            { 105, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000059, TrophyRequirement = 0 } },
            { 149, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000060, TrophyRequirement = 0 } },
            { 160, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000062, TrophyRequirement = 0 } },
            { 184, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000065, TrophyRequirement = 0 } },
            { 245, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000066, TrophyRequirement = 0 } },
            { 265, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000068, TrophyRequirement = 0 } },
            { 288, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000070, TrophyRequirement = 0 } },
            { 319, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000072, TrophyRequirement = 0 } },
            { 333, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000074, TrophyRequirement = 0 } },
            { 381, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000075, TrophyRequirement = 0 } },
            { 382, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000076, TrophyRequirement = 0 } },
            { 387, new IconType { RequirementType = Requirement.HeroOwnership, HeroGlobalId = 16000077, TrophyRequirement = 0 } },
            // Все остальные (не указанные) теперь требуют Ownership по умолчанию
        };

        public enum Requirement
        {
            Free,
            Trophies,
            HeroOwnership,
            Ownership // Для иконок: владение иконкой обязательно
        }

        public class IconType
        {
            public Requirement RequirementType { get; set; }
            public int TrophyRequirement { get; set; } = 0;
            public int HeroGlobalId { get; set; } = 0;
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt(); // Пропускаем неизвестный VInt
            ThumbnailInstanceId = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            // 1. Проверка диапазона
            if (ThumbnailInstanceId < 0)
                return 1;

            int maxThumbnails = DataTables.Get(DataType.PlayerThumbnail).Count;
            if (ThumbnailInstanceId >= maxThumbnails)
                return 2;

            // 2. Генерируем глобальный ID иконки (28000000 + ID)
            int globalThumbnailId = GlobalId.CreateGlobalId(28, ThumbnailInstanceId);

            // 3. Используем ThumbnailInstanceId как baseId
            int baseIconId = ThumbnailInstanceId;

            // 4. Получаем правило. Если нет — требуем Ownership по умолчанию
            if (!IconRules.TryGetValue(baseIconId, out var iconType))
            {
                Debugger.Print($"LogicSetPlayerThumbnailCommand.Execute - No rule for icon ID {baseIconId}, requiring Ownership.");
                iconType = new IconType { RequirementType = Requirement.Ownership };
            }

            // 5. Проверяем требования
            switch (iconType.RequirementType)
            {
                case Requirement.Free:
                    break;

                case Requirement.Trophies:
    if (homeMode.Avatar.HighestTrophies < iconType.TrophyRequirement)
    {
        Debugger.Error($"LogicSetPlayerThumbnailCommand.Execute - Need {iconType.TrophyRequirement} trophies, have {homeMode.Avatar.HighestTrophies}");
        // 🚫 БАН ОТКЛЮЧЁН - просто разрешаем
        Console.WriteLine($"[LogicSetPlayerThumbnailCommand] Trophy requirement ignored for icon {baseIconId}");
        // BanUtil.BanPlayer(homeMode, BanUtil.ViolationType.ModifiedClient); ← УДАЛИТЬ
        // return -1; ← УДАЛИТЬ
    }
    break;
    
    
    
    case Requirement.HeroOwnership:
    if (!homeMode.Avatar.HasHero(iconType.HeroGlobalId))
    {
        var heroData = DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(iconType.HeroGlobalId);
        string heroName = heroData?.GetName() ?? "Unknown Hero";
        Debugger.Error($"LogicSetPlayerThumbnailCommand.Execute - Need hero '{heroName}' (ID: {iconType.HeroGlobalId}) for icon {baseIconId}");
        // 🚫 БАН ОТКЛЮЧЁН - просто разрешаем
        Console.WriteLine($"[LogicSetPlayerThumbnailCommand] Hero ownership requirement ignored for icon {baseIconId}");
        // BanUtil.BanPlayer(homeMode, BanUtil.ViolationType.ModifiedClient); ← УДАЛИТЬ
        // return -1; ← УДАЛИТЬ
    }
    break;
    
    
    
    
                case Requirement.Ownership:
    if (!homeMode.Home.UnlockedThumbnails.Contains(globalThumbnailId))
    {
        Debugger.Error($"LogicSetPlayerThumbnailCommand.Execute - Player does not own icon ID {globalThumbnailId}");
        // 🚫 БАН ОТКЛЮЧЁН - просто разрешаем использовать любую иконку
        Console.WriteLine($"[LogicSetPlayerThumbnailCommand] Auto-allowing icon {baseIconId} (Global: {globalThumbnailId})");
        // BanUtil.BanPlayer(homeMode, BanUtil.ViolationType.ModifiedClient); ← УДАЛИТЬ
        // return -1; ← УДАЛИТЬ
    }
    break;

                default:
                    Debugger.Error($"LogicSetPlayerThumbnailCommand.Execute - Invalid requirement type for icon {baseIconId}");
                    return -1;
            }

            // 6. Применяем иконку
            homeMode.Home.ThumbnailId = globalThumbnailId;

            // 7. Обновляем отображение у друзей
            if (homeMode.Avatar.Friends.Count > 0)
            {
                foreach (var friend in homeMode.Avatar.Friends)
                {
                    var friendInfo = friend.Avatar.Friends.Find(x => x.AccountId == homeMode.Avatar.AccountId);
                    if (friendInfo != null)
                    {
                        friendInfo.DisplayData = new IndusBrawl.Laser.Logic.Avatar.Structures.PlayerDisplayData(
                            homeMode.Home.ThumbnailId,
                            homeMode.Home.NameColorId,
                            homeMode.Avatar.Name
                        );
                    }
                }
            }

            return 0;
        }

        public override int GetCommandType()
        {
            return 505;
        }
    }
}