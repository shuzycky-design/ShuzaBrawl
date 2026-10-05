namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;

    public class LogicSelectStarPowerCommand : Command
    {
        public int CardInstanceId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            CardInstanceId=stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            //Debugger.Print("bruh");
            //CardData spg = DataTables.Get(23).GetData<CardData>(CardInstanceId);

            //Debugger.Print(spg.Target.ToString());
            //if (spg == null) return 0;
            //Hero hero = homeMode.Avatar.GetHeroForCard(spg);
            //if (hero == null) return 0;
            //if (spg.MetaType==4) hero.SelectedStarPowerId=spg.GetInstanceId();
            //else hero.SelectedGadgetId=spg.GetInstanceId();
            //Debugger.Print(hero.ToString());
            //homeMode.CharacterChanged.Invoke(0);

            CharacterData hero;
            CardData card = DataTables.Get(DataType.Card).GetDataByGlobalId<CardData>(GlobalId.CreateGlobalId(29, CardInstanceId));
            if (card == null) return -1;

            string m = card.Name.Replace("_2", "");
            m = m.Replace("_3", "");
            CardData card1 = DataTables.Get(DataType.Card).GetData<CardData>(m);
            CardData card2 = DataTables.Get(DataType.Card).GetData<CardData>(m + "_2");
            CardData card3 = DataTables.Get(DataType.Card).GetData<CardData>(m + "_3");
            hero = DataTables.Get(DataType.Character).GetData<CharacterData>(card.Name.Split("_")[0]);
            Hero h = homeMode.Avatar.GetHero(hero.GetGlobalId());

            homeMode.Avatar.SelectedSPGS.Remove(card1.GetGlobalId());
            if(card2 != null)homeMode.Avatar.SelectedSPGS.Remove(card2.GetGlobalId());
            if (card3 != null)
            {
                homeMode.Avatar.SelectedSPGS.Remove(card3.GetGlobalId());
            }

            Hero playerHero = homeMode.Avatar.GetHeroForCard(card);
            if (card.MetaType == 4) playerHero.SelectedStarPowerId = card.GetInstanceId();
            else playerHero.SelectedGadgetId = card.GetInstanceId();
            Console.WriteLine(playerHero.SelectedStarPowerId);
            homeMode.Avatar.SelectedSPGS.Add(card.GetGlobalId());
            homeMode.CharacterChanged.Invoke(0);
            return 0;
        }

        public override int GetCommandType()
        {
            return 529;
        }
    }
}
