// LogicLevelUpCommand.cs
namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Titan.DataStream;
    using System;
    using IndusBrawl.Laser.Titan.Debug;

    public class LogicLevelUpCommand : Command
    {
        private int CharacterId;

        // Новые курсы обмена
        const int COINS_PER_GEM = 10;     // 10 монет = 1 гем
        const int POWER_PER_GEM = 5;      // 5 PP = 1 гем

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt(); // class (unused)
            CharacterId = stream.ReadVInt(); // ID бойца
            Debugger.Print($"[LevelUp] Декодирована команда. Целевой боец: {CharacterId}");
        }

        public override int Execute(HomeMode homeMode)
        {
            ClientAvatar avatar = homeMode.Avatar;
            int globalHeroId = GlobalId.CreateGlobalId(16, CharacterId);

            // === Шаг 1: Проверка на существование и владение бойцом ===
            if (!avatar.HasHero(globalHeroId))
            {
                Debugger.Print($"[LevelUp] Ошибка: Игрок не владеет бойцом с ID {CharacterId}");
                return -1;
            }

            Hero hero = avatar.GetHero(globalHeroId);
            if (hero == null)
            {
                Debugger.Print($"[LevelUp] Ошибка: Боец {CharacterId} не найден в списке.");
                return -1;
            }

            // === Шаг 2: Проверка уровня ===
            int currentLevel = hero.PowerLevel;
            Debugger.Print($"[LevelUp] Текущий уровень бойца: {currentLevel}");

            if (currentLevel >= 11)
            {
                Debugger.Print($"[LevelUp] Ошибка: Боец {hero.CharacterData.GetName()} уже на максимальном уровне (Lvl 11).");
                return -3;
            }

            int nextLevel = currentLevel + 1;
            if (nextLevel > 11)
            {
                Debugger.Print($"[LevelUp] Ошибка: Попытка прокачать на уровень {nextLevel}, который выше максимального.");
                return -1;
            }

            // === Шаг 3: Получаем стоимость прокачки из таблиц ===
            int costIndex = currentLevel - 1;
            if (costIndex < 0 || costIndex >= Hero.UpgradeCostTable.Length || costIndex >= Hero.UpgradeCost1Table.Length)
            {
                Debugger.Print($"[LevelUp] Ошибка: Индекс {costIndex} выходит за пределы таблицы.");
                return -1;
            }

            int goldCost = Hero.UpgradeCostTable[costIndex]; // Стоимость в монетах
            int ppCost = Hero.UpgradeCost1Table[costIndex];   // Стоимость в PP
            Debugger.Print($"[LevelUp] Прокачка с Lvl {currentLevel} до Lvl {nextLevel} требует: {goldCost} монет и {ppCost} PP");

            // === Шаг 4: Рассчитываем стоимость в гемах ===
            int gemsNeeded = CalculateGemCost(goldCost, ppCost, avatar.Gold, avatar.PowerPoints);
            Debugger.Print($"[LevelUp] Общая стоимость в гемах: {gemsNeeded}");

            if (avatar.Diamonds < gemsNeeded)
            {
                Debugger.Print($"[LevelUp] Ошибка: Недостаточно алмазов. Требуется {gemsNeeded}, есть {avatar.Diamonds}");
                return -2;
            }

            // === Шаг 5: Списываем ресурсы ===
            int coinsToUse = Math.Min(avatar.Gold, goldCost);
            int ppToUse = Math.Min(avatar.PowerPoints, ppCost);

            avatar.Gold -= coinsToUse;
            avatar.PowerPoints -= ppToUse;
            avatar.Diamonds -= gemsNeeded;

            // === Шаг 6: Повышаем уровень ===
            hero.PowerLevel = nextLevel;
            Debugger.Print($"[LevelUp] Успешно! {hero.CharacterData.GetName()} повышен до Lvl {hero.PowerLevel} с использованием алмазов.");
            return 0;
        }

        /// <summary>
        /// Вычисляет минимальную стоимость апгрейда в гемах.
        /// </summary>
        private int CalculateGemCost(int coinCost, int powerCost, int coinsAvailable, int powerAvailable)
        {
            int coinsNeeded = Math.Max(0, coinCost - coinsAvailable);
            int powerNeeded = Math.Max(0, powerCost - powerAvailable);

            double gemsForCoins = (double)coinsNeeded / COINS_PER_GEM;
            double gemsForPower = (double)powerNeeded / POWER_PER_GEM;

            double totalGems = gemsForCoins + gemsForPower;
            return (int)Math.Ceiling(totalGems);
        }

        public override int GetCommandType()
        {
            return 520;
        }
    }
}