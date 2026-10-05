namespace IndusBrawl.Laser.Logic.Home.Gatcha
{
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Titan.DataStream;

    public class GatchaDrop
    {
        public int Count;
        public int DataGlobalId;
        public int SkinGlobalId;
        public int Type;

        public GatchaDrop(int type)
        {
            Type = type;
        }

        public void DoDrop(HomeMode homeMode)
        {
            ClientAvatar avatar = homeMode.Avatar;

            switch (Type)
            {
                case 1: 
                    CharacterData characterData = DataTables.Get(16).GetDataByGlobalId<CharacterData>(DataGlobalId);
                    if (characterData == null) return;

                    CardData cardData = DataTables.Get(23).GetData<CardData>(characterData.Name + "_unlock");
                    if (cardData == null) return;

                    avatar.UnlockHero(characterData.GetGlobalId());
                    break;
                case 2:
                    homeMode.Home.TokenDoublers += Count;
                    break;
                case 6:
                    Hero hero = avatar.GetHero(DataGlobalId);
                    if (hero == null) return;

                    hero.PowerPoints += Count;
                    break;
                case 7:
                    avatar.AddGold(Count);
                    break;
                case 8:
                    avatar.AddDiamonds(Count);
                    break;
                case 9:
                    homeMode.Home.UnlockedSkins.Add(SkinGlobalId);
                    break;
                case 11:
                    if (DataGlobalId > 28000000 && DataGlobalId < 29000000) { homeMode.Home.UnlockedThumbnails.Add(DataGlobalId); }
                    if (DataGlobalId > 52000000 && DataGlobalId < 53000000) { homeMode.Home.UnlockedEmotes.Add(DataGlobalId); }
                    if (DataGlobalId > 76000000 && DataGlobalId < 77000000) { homeMode.Home.UnlockedTituls.Add(DataGlobalId); }
                    break;
                case 22:
                    if (homeMode.Home.UnlockStarRoad == null || homeMode.Home.UnlockStarRoad.Length == 0)
                    {
                        avatar.FamePoints += Count;
                    }
                    else
                    {
                        avatar.AddRareTokens(Count);
                    }
                    break;
                case 24:
                    avatar.AddPowerPoints(Count);
                    
                    break;
                case 25:
                    avatar.AddBlings(Count);
                    break;
                case 10:
                    homeMode.Home.UnlockedEmotes.Add(DataGlobalId);
                    break;
            }
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Count);
            ByteStreamHelper.WriteDataReference(stream, DataGlobalId);
            stream.WriteVInt(Type);

            ByteStreamHelper.WriteDataReference(stream, SkinGlobalId);
            ByteStreamHelper.WriteDataReference(stream, DataGlobalId);
            ByteStreamHelper.WriteDataReference(stream, 0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
        }
    }
}
