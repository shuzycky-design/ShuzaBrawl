namespace IndusBrawl.Laser.Logic.Home.Quest
{
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Logic.Helper;

    public class Quest
    {
        public int MissionType { get; set; }
        public int CurrentGoal { get; set; }
        public int QuestGoal { get; set; }
        public int GameModeVariation { get; set; }
        public bool QuestSeen { get; set; }
        public int CharacterId { get; set; }
        public int Reward { get; set; }
        public int Progress { get; set; }

        public Quest Clone()
        {
            return new Quest()
            {
                MissionType = MissionType,
                CurrentGoal = CurrentGoal,
                QuestGoal = QuestGoal,
                GameModeVariation = GameModeVariation,
                QuestSeen = QuestSeen,
                CharacterId = CharacterId,
                Reward = Reward,
                Progress = Progress,
            };
        }

        public void Encode(ChecksumEncoder encoder)
        {
            encoder.WriteVInt(111);
            encoder.WriteVInt(23); // Brawl Pass seasn
            encoder.WriteVInt(MissionType); // Mission Type
            encoder.WriteVInt(CurrentGoal); // Achieved Goal
            encoder.WriteVInt(QuestGoal); // Quest Goal
            encoder.WriteVInt(-1);
            encoder.WriteVInt(1);
            encoder.WriteVInt(GameModeVariation); //Game Mode
            encoder.WriteVInt(0); // Refresh
            encoder.WriteVInt(-1); //EventSlot idk what this
            encoder.WriteVInt(1); //Reward Type
            encoder.WriteVInt(2);
            encoder.WriteVInt(Reward); // Reward count
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
            encoder.WriteVInt(1);
            encoder.WriteVInt(1);
            encoder.WriteVInt(1);
            encoder.WriteVInt(3);
            encoder.WriteVInt(0);
            encoder.WriteVInt(2);
            encoder.WriteVInt(1);
            encoder.WriteVInt(0); // current lvl
            encoder.WriteVInt(0); // max lvl
            encoder.WriteVInt(10000); // Timer
            encoder.WriteVInt(0); // Max LVL
            encoder.WriteVInt(100); //Timer
            encoder.WriteVInt(0);//Brawl Pass Need(1- Need);
            encoder.WriteVInt(0);
        }
    }
}
