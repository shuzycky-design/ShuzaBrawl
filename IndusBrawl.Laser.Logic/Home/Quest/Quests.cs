// Quests.cs
namespace IndusBrawl.Laser.Logic.Home.Quest
{
    using Newtonsoft.Json;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Titan.DataStream;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using IndusBrawl.Laser.Titan.Debug;

    public class Quests
    {
        // Разрешённые режимы: 0=gem grab, 3=bounty, 6=шд, 25=зачистка
        private static readonly int[] ALLOWED_MODES = { 0, 2, 3, 25 };

        // Цели для "урон" и "лечение"
        private static readonly int[] GOAL_TABLE = { 40000, 50000, 60000, 80000, 100000 };

        [JsonProperty("quest_list")]
        public List<Quest> QuestList;

        [JsonProperty]
        public DateTime LastQuestReset = DateTime.MinValue;

        public Quests()
        {
            QuestList = new List<Quest>();
        }

        /// <summary>
        /// Генерирует квесты: без привязки к бравлерам, с рандомной привязкой к режимам
        /// </summary>
        public void AddRandomQuests(List<Hero> unlockedHeroes, int count)
        {
            if (unlockedHeroes == null || !unlockedHeroes.Any())
                return;

            Random rand = new Random(Guid.NewGuid().GetHashCode());

            for (int i = 0; i < count; i++)
            {
                Quest quest = new Quest();
                quest.MissionType = rand.Next(1, 5); // 1-4

                // ✅ Рандом: 70% — на любой режим, 30% — на конкретный
                bool forAnyMode = rand.Next(100) < 70;
                if (forAnyMode)
                {
                    quest.GameModeVariation = -1; // Любой режим
                }
                else
                {
                    quest.GameModeVariation = ALLOWED_MODES[rand.Next(0, ALLOWED_MODES.Length)];
                }

                // ✅ Всегда: не привязан к бравлеру
                quest.CharacterId = -1;

                switch (quest.MissionType)
                {
                    case 1: // Победы
                        quest.QuestGoal = rand.Next(6, 13);
                        quest.Reward = quest.QuestGoal >= 10 ? 500 : 250;
                        break;
                    case 2: // Убийства
                        quest.QuestGoal = rand.Next(12, 31);
                        quest.Reward = quest.QuestGoal >= 20 ? 500 : 250;
                        break;
                    case 3: // Урон
                    case 4: // Лечение
                        quest.QuestGoal = GOAL_TABLE[rand.Next(0, GOAL_TABLE.Length)];
                        quest.Reward = quest.QuestGoal >= 80000 ? 500 : 250;
                        break;
                }

                QuestList.Add(quest);
                Debugger.Print($"[Quests] Добавлен квест: Type={quest.MissionType}, Mode={(quest.GameModeVariation == -1 ? "Any" : quest.GameModeVariation)}, Goal={quest.QuestGoal}");
            }
        }

        /// <summary>
        /// Обновляет прогресс: проверяет режим, игнорирует бравлера
        /// </summary>
        public List<Quest> UpdateQuestsProgress(int gameModeVariation, int characterId, int kills, int damage, int heals, ClientHome home)
        {
            List<Quest> completed = new List<Quest>();
            List<Quest> progressive = new List<Quest>();

            foreach (Quest quest in QuestList.ToArray())
            {
                // ✅ Проверка по режиму: -1 = любой режим
                bool matchesMode = (quest.GameModeVariation == gameModeVariation || quest.GameModeVariation == -1);
                if (!matchesMode) continue;

                var progress = quest.Clone();
                progress.Progress = 0;

                if (quest.MissionType == 1) // Победы
                {
                    quest.CurrentGoal += 1;
                    progress.Progress = 1;
                    if (quest.CurrentGoal >= quest.QuestGoal) completed.Add(quest);
                    progressive.Add(progress);
                }
                else if (quest.MissionType == 2) // Убийства
                {
                    quest.CurrentGoal += kills;
                    progress.Progress = kills;
                    if (quest.CurrentGoal >= quest.QuestGoal) completed.Add(quest);
                    progressive.Add(progress);
                }
                else if (quest.MissionType == 3) // Урон
                {
                    quest.CurrentGoal += damage;
                    progress.Progress = damage;
                    if (quest.CurrentGoal >= quest.QuestGoal) completed.Add(quest);
                    progressive.Add(progress);
                }
                else if (quest.MissionType == 4) // Лечение
                {
                    quest.CurrentGoal += heals;
                    progress.Progress = heals;
                    if (progress.CurrentGoal + progress.Progress > progress.QuestGoal)
                        progress.Progress = progress.QuestGoal - progress.CurrentGoal;
                    if (quest.CurrentGoal >= quest.QuestGoal) completed.Add(quest);
                    progressive.Add(progress);
                }
            }

            // Начисляем награды
            foreach (Quest quest in completed)
            {
                home.TokenReward += quest.Reward;
                home.BrawlPassTokens += quest.Reward;
                Debugger.Print($"[Quests] ✅ Квест завершён: +{quest.Reward} монет");
            }

            // Удаляем завершённые
            QuestList.RemoveAll(q => completed.Contains(q));
            return progressive;
        }

        /// <summary>
        /// Сериализация для клиента
        /// </summary>
        public void Encode(ChecksumEncoder encoder)
        {
            encoder.WriteVInt(QuestList?.Count ?? 0);
            foreach (Quest quest in QuestList ?? new List<Quest>())
            {
                quest.Encode(encoder);
            }
        }
    }
}