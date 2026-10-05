namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Titan.Debug;
    using IndusBrawl.Laser.Titan.Math;

    public class LogicPurchaseBrawlPassProgressCommand : Command
    {
        public int Unknown { get; set; }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            Unknown = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return PurchaseBrawlPassProgressReceived(homeMode) ? 0 : -1;
        }

        private bool PurchaseBrawlPassProgressReceived(HomeMode homeMode)
        {
            if (homeMode.Avatar.UseDiamonds(10))
            {
                for (int x = 966; x < 1036 + 1; x++)
                {
                    MilestoneData milestoneData = DataTables.Get(DataType.Milestone).GetDataByGlobalId<MilestoneData>(GlobalId.CreateGlobalId((int)DataType.Milestone, x));
                    if (milestoneData.ProgressStart <= homeMode.Home.BrawlPassTokens && (milestoneData.ProgressStart + milestoneData.Progress) > homeMode.Home.BrawlPassTokens)
                    {
                        homeMode.Home.BrawlPassTokens = milestoneData.ProgressStart + milestoneData.Progress;
                        return true;
                    }
                }
            }
            return false;
        }

        public override int GetCommandType()
        {
            return 536;
        }
    }
}