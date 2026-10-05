namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Items;

    public class LogicClearShopTickersCommand : Command
    {
        public override int Execute(HomeMode homeMode)
        {
            OfferBundle[] bundles = homeMode.Home.OfferBundles.ToArray();
            foreach (OfferBundle bundle in bundles)
            {
                bundle.State = 2;
            }

            return 0;
        }

        public override int GetCommandType()
        {
            return 515;
        }
    }
}
