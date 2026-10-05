// GearEncoder.cs
using System.Collections.Generic;
using IndusBrawl.Laser.Titan.DataStream;
using IndusBrawl.Laser.Logic.Avatar;
using IndusBrawl.Laser.Logic.Home.Structures;

namespace IndusBrawl.Laser.Logic.Home.Items
{
    public static class GearEncoder
    {
        /// <summary>
        /// Отправляет Gear-данные для всех героев.
        /// Использует UnlockedGearInstanceIds для определения разблокированных Gear'ов.
        /// Экипировка берётся из SelectedGearId1/2.
        /// Использует 62000000 + InstanceId для совместимости.
        /// </summary>
        public static void Encode(ByteStream stream, List<Hero> heroes)
        {
            stream.WriteVInt(heroes.Count);

            foreach (Hero hero in heroes)
            {
                // === 1. ID героя ===
                stream.WriteDataReference(hero.CharacterId);

                // === 2. Отправляем список разблокированных Gear'ов ===
                stream.WriteVInt(hero.UnlockedGearInstanceIds.Count);

                foreach (int gearId in hero.UnlockedGearInstanceIds)
                {
                    // Используем 62000000 + ID (твой рабочий формат)
                    stream.WriteDataReference(62000000 + gearId);
                }

                // === 3. Количество слотов (всегда 2) ===
                stream.WriteVInt(2);

                // === 4. Экипировка в слот 1 ===
                WriteEquippedGear(stream, hero.SelectedGearId1, hero.UnlockedGearInstanceIds);

                // === 5. Экипировка в слот 2 ===
                WriteEquippedGear(stream, hero.SelectedGearId2, hero.UnlockedGearInstanceIds);
            }
        }

        private static void WriteEquippedGear(ByteStream stream, int gearInstanceId, List<int> unlockedIds)
        {
            if (gearInstanceId <= -1 || !unlockedIds.Contains(gearInstanceId))
            {
                stream.WriteDataReference(0);
                return;
            }

            stream.WriteDataReference(62000000 + gearInstanceId);
        }
    }
}