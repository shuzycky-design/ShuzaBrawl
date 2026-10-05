namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Battle.Objects;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;

    public class LogicSelectFavouriteBrawlerCommand : Command
    {
        public int CharacterId;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            CharacterId=ByteStreamHelper.ReadDataReference(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            if (homeMode.Avatar.HasHero(CharacterId))
            {
                homeMode.Home.FavouriteCharacter = CharacterId;
                return 0;
            }


            return -1;
        }

        public override int GetCommandType()
        {
            return 570;
        }
    }
}
