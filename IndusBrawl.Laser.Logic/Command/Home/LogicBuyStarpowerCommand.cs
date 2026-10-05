namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Titan.DataStream;

    public class LogicBuyStarpowerCommand : Command
    {
        private int ID;
        private int CharacterId1;
        private const double GEMS_PER_COIN = 0.1;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            ID = stream.ReadVInt(); 
            stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            bool Check(int count)
            {
                if (count > homeMode.Avatar.Gold)
                {
                    homeMode.Avatar.Gold = 0;
                    int deficit = count - homeMode.Avatar.Gold;
                    if (!homeMode.Avatar.UseDiamonds((int)Math.Ceiling(deficit * GEMS_PER_COIN)))
                    {
                        return false;
                    }
                    return true;
                }
                homeMode.Avatar.Gold -= count;
                return true;
            }
            CardData spg = DataTables.Get(23).GetData<CardData>(ID);
            if (spg == null || spg.Name == null || spg.DirectPurchasePrice == 0) return 0;
            Console.WriteLine(spg.Name);
            homeMode.Avatar.SPGS.Add(spg.GetGlobalId());
            
            Check(spg.DirectPurchasePrice);
            CharacterData boetc = DataTables.Get(16).GetData<CharacterData>(spg.Name.IndexOf('_') >= 0 ? spg.Name.Substring(0, spg.Name.IndexOf('_')) : spg.Name);
            return 0;
        }

        public override int GetCommandType()
        {
            return 557;
        }
    }
}
