namespace IndusBrawl.Laser.Logic.Message.Home
{
    public class SeasonRewardsMessage : GameMessage
    {
        public int Type;
        public SeasonRewardsMessage()
        {
            ;
        }
        public override void Encode()
        {
            int[] StarPointsTrophiesStart = { 550, 600, 650, 700, 750, 800, 850, 900, 950, 1000, 1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400 };
            int[] StarPointsTrophiesEnd = { 599, 649, 699, 749, 799, 849, 899, 949, 999, 1049, 1099, 1149, 1199, 1249, 1299, 1349, 1399, -1 };
            int[] StarPointsSeasonRewardAmount = { 70, 120, 160, 200, 220, 240, 260, 280, 300, 320, 340, 360, 380, 400, 420, 440, 460, 480 };
            int[] StarPointsTrophiesInReset = { 525, 550, 600, 650, 700, 725, 750, 775, 800, 825, 850, 875, 900, 925, 950, 975, 1000, 1025 };
            Console.WriteLine(Type);
            Stream.WriteVInt(Type);

            Stream.WriteVInt(3);

            Stream.WriteVInt(1);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(1);
            Stream.WriteVInt(1);
            Stream.WriteVInt(1);
            Stream.WriteDataReference(16, 0);
            Stream.WriteVInt(1);
            Stream.WriteVInt(0);

            Stream.WriteVInt(2);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(1);
            Stream.WriteVInt(1);
            Stream.WriteVInt(1);
            Stream.WriteDataReference(16, 0);
            Stream.WriteVInt(1);
            Stream.WriteVInt(0);

            Stream.WriteVInt(3);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(1);
            Stream.WriteVInt(1);
            Stream.WriteVInt(1);
            Stream.WriteDataReference(16, 0);
            Stream.WriteVInt(1);
            Stream.WriteVInt(0);




        }

        public override int GetMessageType()
        {
            return 24123;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
