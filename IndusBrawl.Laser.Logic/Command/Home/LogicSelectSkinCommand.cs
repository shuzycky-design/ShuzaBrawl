namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Battle.Objects;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Logic.Team;
    using IndusBrawl.Laser.Titan.DataStream;
    using System.Collections.Generic;
    using IndusBrawl.Laser.Logic.Util;

    public class LogicSelectSkinCommand : Command
    {
        int skinId;

        // Список ID скинов, которые являются "бесплатными" и не требуют проверки UnlockedSkins
        private static readonly HashSet<int> FreeSkins = new HashSet<int>
        {
            0,   // Default Skin
            14,   // Ninja Skin
            1,   // Pirate Skin
            3,  // Santa Skin
            4,
            12,
            7,
            21,
            77,
            13,
            6,
            86,
            106,
            9,
            34,
            41,
            73,
            142,
            560,
            22,
            119,
            265,
            23,
            24,
            42,
            81,
            114,
            156,
            240,
            289,
            278,
            496,
            155,
            206,
            271,
            352,
            396,
            559,
            601,
            664,
            665,
            696,
            18,
            63,
            32,
            67,
            127,
            121,
            157,
            239,
            272,
            749,
            231,
            261,
            288,
            418,
            460,
            494,
            533,
            586,
            598,
            627,
            629,
            677,
            697,
            719,
            738,
            10,
            19,
            62,
            113,
            207,
            360,
            184,
            595,
            676,
            739,
        };

        // Словарь зависимостей скинов: ключ - скин, который хотим выбрать, значение - требуемый скин
        private static readonly Dictionary<int, int> SkinDependencies = new Dictionary<int, int>
        {
            { 228, 227 },  // Чтобы выбрать скин 228, нужно владеть скином 227
            { 230, 229 },  // Чтобы выбрать скин 230, нужно владеть скином 229
        };

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            int read1 = stream.ReadVInt();
            skinId = stream.ReadVInt();
            int read2 = stream.ReadVInt();

            // Отладка: вывод значений, прочитанных из потока
            Console.WriteLine($"[LogicSelectSkinCommand.Decode] ReadVInt1: {read1}, SkinId: {skinId}, ReadVInt2: {read2}");
        }

        public override int Execute(HomeMode homeMode)
        {
            Console.WriteLine($"[LogicSelectSkinCommand.Execute] Attempting to select skin with ID: {skinId}");

            // Получаем полный ID скина (в игре скины часто имеют базовый ID 29000000 + skinId)
            int fullSkinId = 29000000 + skinId;

            // Проверяем, является ли скин "бесплатным"
            bool isFreeSkin = FreeSkins.Contains(skinId);

            // Проверяем зависимости
            bool hasDependency = false;
            if (SkinDependencies.TryGetValue(skinId, out int requiredSkinId))
            {
                int fullRequiredSkinId = 29000000 + requiredSkinId;

                if (!homeMode.Home.UnlockedSkins.Contains(fullRequiredSkinId))
                {
                    Console.WriteLine($"[LogicSelectSkinCommand.Execute] FAILED: Skin {skinId} requires owning skin {requiredSkinId} ({fullRequiredSkinId})");
                    BanUtil.BanPlayer(homeMode, BanUtil.ViolationType.ModifiedClient);
                    return -1;
                }
                else
                {
                    Console.WriteLine($"[LogicSelectSkinCommand.Execute] Required skin {requiredSkinId} ({fullRequiredSkinId}) is owned. Proceeding...");
                    hasDependency = true;
                }
            }

            // Если скин не бесплатный и нет зависимости - проверяем разблокировку
if (!isFreeSkin && !hasDependency)
{
    if (!homeMode.Home.UnlockedSkins.Contains(fullSkinId))
    {
        Console.WriteLine($"[LogicSelectSkinCommand.Execute] Skin {fullSkinId} is not unlocked. Auto-allowing custom skin...");
        // 🚫 БАН ОТКЛЮЧЁН - просто разрешаем использование
        // Вместо бана - добавляем скин в разблокированные (по желанию)
        // homeMode.Home.UnlockedSkins.Add(fullSkinId);
    }
                else
                {
                    Console.WriteLine($"[LogicSelectSkinCommand.Execute] Skin {fullSkinId} is unlocked. Proceeding...");
                }
            }
            else if (isFreeSkin)
            {
                Console.WriteLine($"[LogicSelectSkinCommand.Execute] Skin {skinId} is free. Skipping unlock check.");
            }
            else if (hasDependency)
            {
                Console.WriteLine($"[LogicSelectSkinCommand.Execute] Skin {skinId} has dependency satisfied. Skipping unlock check.");
            }

            // Получаем данные скина и конфигурации
            SkinData skinData = DataTables.Get(DataType.Skin).GetData<SkinData>(skinId);
            if (skinData == null)
            {
                Console.WriteLine($"[LogicSelectSkinCommand.Execute] FAILED: SkinData not found for skinId {skinId}");
                return -1;
            }

            SkinConfData skinConfData = DataTables.Get(DataType.SkinConf).GetData<SkinConfData>(skinData.Conf);
            if (skinConfData == null)
            {
                Console.WriteLine($"[LogicSelectSkinCommand.Execute] FAILED: SkinConfData not found for Conf {skinData.Conf}");
                return -1;
            }

            int characterId = DataTables.Get(DataType.Character).GetData<CharacterData>(skinConfData.Character).GetInstanceId() + 16000000;
            Console.WriteLine($"[LogicSelectSkinCommand.Execute] Resolved characterId: {characterId} for skin.");

            Hero hero = homeMode.Avatar.GetHero(characterId);
            if (hero != null)
            {
                hero.SelectedSkinId = skinId;
                Console.WriteLine($"[LogicSelectSkinCommand.Execute] Successfully set skin {skinId} for hero with characterId {characterId}");
            }
            else
            {
                Console.WriteLine($"[LogicSelectSkinCommand.Execute] WARNING: Hero not found for characterId {characterId}");
            }

            // Уведомляем об изменении персонажа
            homeMode.CharacterChanged.Invoke(0);
            Console.WriteLine("[LogicSelectSkinCommand.Execute] CharacterChanged event invoked.");

            return 0;
        }

        public override int GetCommandType()
        {
            return 506;
        }
    }
}