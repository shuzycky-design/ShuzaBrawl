using IndusBrawl.Laser.Logic.Data;
using IndusBrawl.Laser.Logic.Data.Helper;
using IndusBrawl.Laser.Logic.Util;
using IndusBrawl.Laser.Titan.Debug;
using IndusBrawl.Laser.Logic.Battle.Objects;
using IndusBrawl.Laser.Titan.DataStream;
using IndusBrawl.Laser.Titan.Math;

namespace IndusBrawl.Laser.Logic.Battle.Structures
{
    public class Gear
    {
        public GearData GearData { get; private set; }
        public bool IsActive { get; set; }
        public int ShieldAmount { get; set; }

        /// <summary>
        /// Основной конструктор. Принимает GearData напрямую.
        /// </summary>
        public Gear(GearData gearData)
        {
            if (gearData == null)
            {
                Debugger.Error("GearData is null in Gear constructor!");
                return;
            }

            GearData = gearData;
            IsActive = false;

            // Инициализация ShieldAmount для щитов
            if (GearData.LogicType == 4) // Предположим, 4 = Shield
            {
                ShieldAmount = 600;
            }
        }

        /// <summary>
        /// Удобный конструктор от имени Gear.
        /// Автоматически ищет GearData по имени в таблицах.
        /// </summary>
        public Gear(string gearName) : this(GetGearDataByName(gearName))
        {
            if (GearData == null)
               Debugger.Error($"Failed to create Gear: '{gearName}' not found in Gear table!");
        }

        private static GearData GetGearDataByName(string name)
        {
             return DataTables.GetGearByName(name);
        }

        /// <summary>
        /// Статический метод для получения GearData по имени.
        /// Инкапсулирует доступ к DataTables.
        /// </summary>

        // --- Остальная логика Gear (Tick, Encode и т.д.) ---
        public void Tick(Character character)
        {
            IsActive = false;

            switch (GearData.LogicType)
            {
                case 0: // Увеличение скорости в кустах
                    if (character.IsInBush())
                    {
                        character.GiveSpeedFasterBuff(character.CharacterData.Speed / 5, 2, false);
                        IsActive = true;
                    }
                    break;

                case 2: // Увеличение урона при низком здоровье
                    if (character.GetHitpointPercentage() < 50)
                    {
                        character.GiveDamageBuff(GearData.ModifierValue, 2);
                        IsActive = true;
                    }
                    break;

                case 4: // Накопление щита
                    if (character.GetHitpointPercentage() == 100)
                    {
                        int ticksSinceFull = character.TicksGone - character.m_lastSelfHealTick;
                        if (ticksSinceFull > 0 && ticksSinceFull % 10 == 0)
                        {
                            ShieldAmount = LogicMath.Min(GearData.ModifierValue, ShieldAmount + GearData.ModifierValue / 20);
                        }
                    }
                    break;
            }
        }

        public void OnBoost()
        {
            IsActive = true;
        }

        public void Encode(BitStream stream)
        {
            stream.WriteBoolean(IsActive);
            if (GearData.LogicType == 4)
            {
                stream.WritePositiveIntMax1023(ShieldAmount);
            }
        }
    }
}