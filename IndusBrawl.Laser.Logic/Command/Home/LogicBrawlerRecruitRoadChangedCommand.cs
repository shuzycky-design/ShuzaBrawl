namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Titan.DataStream;

    public class LogicBrawlerRecruitRoadChangedCommand : Command
    {
        public HomeMode homeMode;
        public bool UnlockingBrawlers;
        public int NextBrawler;
        public int creditsCost; 
        public int gemsCost;

        public override void Encode(ByteStream stream)
        {
            // Проверка на null
            if (stream == null)
            {
                return;
            }

            stream.WriteVInt(0); // DayArrayRange
            stream.WriteVInt(0); // Timer
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0); // Road (if Poco Road - 1, if Brock Road - 2);
            stream.WriteVInt(1);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);

            // Проверка на null для homeMode и homeMode.Home
            if (homeMode == null || homeMode.Home == null)
            {
                // Если данные отсутствуют, пишем пустые значения и выходим
                stream.WriteBoolean(false);
                stream.WriteVInt(0);
                stream.WriteVInt(0);
                stream.WriteVInt(0);
                base.Encode(stream);
                return;
            }

            List<int> unlockStarRoadList = homeMode.Home.UnlockStarRoad?.ToList() ?? new List<int>();
            for (int BrawlerIndex = unlockStarRoadList.Count - 1; BrawlerIndex >= 0; BrawlerIndex--)
            {
                // Проверка на null для homeMode.Avatar
                if (homeMode.Avatar != null && homeMode.Avatar.HasHero(unlockStarRoadList[BrawlerIndex]))
                {
                    unlockStarRoadList.RemoveAt(BrawlerIndex);
                }
            }
            homeMode.Home.UnlockStarRoad = unlockStarRoadList.ToArray();

            stream.WriteBoolean(homeMode.Home.UnlockStarRoad.Length != 0);
            if (homeMode.Home.UnlockStarRoad.Length != 0)
            {
                // Обработка первого бойца
                {
                    int brawlerId = homeMode.Home.UnlockStarRoad[0] - 16000000;
                    stream.WriteDataReference(16, brawlerId);
                    
                    CharacterData characterData = DataTables.Get(DataType.Character)?.GetData<CharacterData>(brawlerId);
                    if (characterData != null)
                    {
                        CardData cardData = DataTables.Get(DataType.Card)?.GetData<CardData>(characterData.Name + "_unlock");
                        if (cardData != null && cardData.Rarity != null)
                        {
                            if (cardData.Rarity == "rare") { creditsCost = 160; gemsCost = 29; }
                            else if (cardData.Rarity == "super_rare") { creditsCost = 430; gemsCost = 79; }
                            else if (cardData.Rarity == "epic") { creditsCost = 925; gemsCost = 169; }
                            else if (cardData.Rarity == "mega_epic") { creditsCost = 1900; gemsCost = 349; }
                            else if (cardData.Rarity == "legendary") { creditsCost = 3800; gemsCost = 699; }
                            else { creditsCost = 0; gemsCost = 0; }
                        }
                        else
                        {
                            creditsCost = 0;
                            gemsCost = 0;
                        }
                    }
                    else
                    {
                        creditsCost = 0;
                        gemsCost = 0;
                    }
                    
                    stream.WriteVInt(creditsCost);
                    stream.WriteVInt(gemsCost);
                    stream.WriteVInt(-1);
                    
                    // Проверка на null для homeMode.Avatar
                    int rareTokens = (homeMode.Avatar != null) ? homeMode.Avatar.RareTokens : 0;
                    stream.WriteVInt(rareTokens); // Collected
                    stream.WriteVInt(0); // Index
                    stream.WriteVInt(0);
                }
                
                stream.WriteVInt(homeMode.Home.UnlockStarRoad.Length - 1);
                for (int BrawlerIndex = 1; BrawlerIndex < homeMode.Home.UnlockStarRoad.Length; BrawlerIndex++)
                {
                    int brawlerId = homeMode.Home.UnlockStarRoad[BrawlerIndex] - 16000000;
                    stream.WriteDataReference(16, brawlerId);
                    
                    CharacterData characterData = DataTables.Get(DataType.Character)?.GetData<CharacterData>(brawlerId);
                    if (characterData != null)
                    {
                        CardData cardData = DataTables.Get(DataType.Card)?.GetData<CardData>(characterData.Name + "_unlock");
                        if (cardData != null && cardData.Rarity != null)
                        {
                            if (cardData.Rarity == "rare") { creditsCost = 160; gemsCost = 29; }
                            else if (cardData.Rarity == "super_rare") { creditsCost = 430; gemsCost = 79; }
                            else if (cardData.Rarity == "epic") { creditsCost = 925; gemsCost = 169; }
                            else if (cardData.Rarity == "mega_epic") { creditsCost = 1900; gemsCost = 349; }
                            else if (cardData.Rarity == "legendary") { creditsCost = 3800; gemsCost = 699; }
                            else { creditsCost = 0; gemsCost = 0; }
                        }
                        else
                        {
                            creditsCost = 0;
                            gemsCost = 0;
                        }
                    }
                    else
                    {
                        creditsCost = 0;
                        gemsCost = 0;
                    }
                    
                    stream.WriteVInt(creditsCost);
                    stream.WriteVInt(gemsCost);
                    stream.WriteVInt(-1);
                    stream.WriteVInt(0); // Collected
                    stream.WriteVInt(BrawlerIndex); // Index
                    stream.WriteVInt(0);
                }

                stream.WriteVInt(0);
                stream.WriteVInt(0);
            }
            else 
            { 
                stream.WriteVInt(0); 
                stream.WriteVInt(0); 
                stream.WriteVInt(0); 
            }
            
            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 227;
        }
    }
}