namespace IndusBrawl.Laser.Logic.Battle.Objects
{
    using IndusBrawl.Laser.Logic.Battle.Level;
    using IndusBrawl.Laser.Logic.Battle.Structures;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;

    public static class GameObjectFactory//+32 type 64 encode 48IsAlive
    {
        public static Character CreateGameObjectByData(CharacterData characterData)
        {
            return new Character(characterData);
        }
        public static Projectile CreateGameObjectByData(ProjectileData characterData)
        {
            return new Projectile(characterData);
        }
        public static Item CreateGameObjectByData(ItemData characterData)
        {
            return new Item(characterData);
        }
        public static AreaEffect CreateGameObjectByData(AreaEffectData characterData)
        {
            return new AreaEffect(characterData);
        }
    }
}
