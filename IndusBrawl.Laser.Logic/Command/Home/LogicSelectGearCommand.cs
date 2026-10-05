// LogicSelectGearCommand.cs
using System.Collections.Generic;
using IndusBrawl.Laser.Logic.Data;
using IndusBrawl.Laser.Logic.Home;
using IndusBrawl.Laser.Logic.Home.Structures;
using IndusBrawl.Laser.Titan.DataStream;

namespace IndusBrawl.Laser.Logic.Command.Home
{
    public class LogicSelectGearCommand : Command
    {
        private int characterId;
        private int gearInstanceId; // InstanceId Gear'а (например, 1)
        private int gearSlot;       // 0 = слот 1, 1 = слот 2

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            stream.ReadVInt(); // 16
            characterId = stream.ReadVInt(); // ID героя (например, 0)

            stream.ReadVInt(); // 62
            gearInstanceId = stream.ReadVInt(); // InstanceId Gear'а

            gearSlot = stream.ReadVInt(); // Слот: 0 или 1

            // stream.ReadVInt(); // Опционально: можно пропустить ещё один VInt
        }

        public override int Execute(HomeMode homeMode)
        {
            // Создаём GlobalId героя
            int globalHeroId = GlobalId.CreateGlobalId(16, characterId);

            // Проверяем, есть ли такой герой
            if (!homeMode.Avatar.HasHero(globalHeroId))
            {
                return -1;
            }

            Hero hero = homeMode.Avatar.GetHero(globalHeroId);

            // 🔐 Проверка: игрок действительно разблокировал этот Gear?
            if (!hero.UnlockedGearInstanceIds.Contains(gearInstanceId))
            {
                // Gear не разблокирован — нельзя экипировать
                return -1;
            }

            // Теперь можно экипировать
            if (gearSlot == 0)
            {
                hero.SelectedGearId1 = gearInstanceId;
            }
            else if (gearSlot == 1)
            {
                hero.SelectedGearId2 = gearInstanceId;
            }
            else
            {
                // Неизвестный слот
                return -1;
            }

            // Опционально: уведомить клиент об изменении
            homeMode.CharacterChanged?.Invoke(0);

            return 0; // Успешно
        }

        public override int GetCommandType()
        {
            return 543; // Тип команды — выбор Gear
        }
    }
}