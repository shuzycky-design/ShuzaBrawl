namespace IndusBrawl.Laser.Logic.Home.Items
{
    using System.IO;
    using System.Text;
    using Newtonsoft.Json.Converters;
    using IndusBrawl.Laser.Logic.Battle.Structures;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;

    public class EventData
    {
        public int Slot;
        public int LocationId;
        public DateTime EndTime;
        public LocationData Location => DataTables.Get(DataType.Location).GetDataByGlobalId<LocationData>(LocationId);
        public BattlePlayerMap BattlePlayerMap;
        public HashSet<int> modifi = new();

        public void Encode(ByteStream encoder)
        {
            encoder.WriteVInt(-1);
            encoder.WriteVInt(Slot);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);//v53
            encoder.WriteVInt((int)(EndTime - DateTime.Now).TotalSeconds);
            encoder.WriteVInt(0);
            if (Slot == 12 || Slot == 13 || Slot == 18 || Slot == 14 || Slot == 15 || Slot == 34 || Slot == 35) ByteStreamHelper.WriteDataReference(encoder, null);
            //else if(LocationId==15000121)ByteStreamHelper.WriteDataReference(encoder, 15000122);
            else ByteStreamHelper.WriteDataReference(encoder, Location);



            encoder.WriteVInt(27); // GameModeVaridation
            encoder.WriteVInt(2);

            encoder.WriteString(null); // 0xacecac
            encoder.WriteVInt(0); // 0xacecc0
            encoder.WriteVInt(0); // 0xacecd4
            if ( Slot == 20 ) // max wins for championship
            {
                encoder.WriteVInt(5);
            }
            else
            {
                encoder.WriteVInt(0);
            }

            encoder.WriteVInt(0); // modifier

            if ( Slot == 20 ) // wins for championship
            {
                encoder.WriteVInt(2);
            }
            else
            {
                encoder.WriteVInt(0);
            }
            encoder.WriteVInt(0); // 0xacee6c
            //encoder.WriteBoolean(false);
            //encoder.WriteVInt(0); // 0xacee6c

            ByteStreamHelper.WriteBattlePlayerMap(encoder, BattlePlayerMap);

            encoder.WriteVInt(0);
            encoder.WriteBoolean(Slot == 14 || Slot == 15); // ranked
            if(Slot == 14)
            {
                encoder.WriteVInt(GeneralStaticLogic.RankedSeason);
                encoder.WriteString(GeneralStaticLogic.RankedSeasonTID);
                encoder.WriteVInt(0);
                encoder.WriteVInt(0);

                encoder.WriteByte((byte)GeneralStaticLogic.QuestForRanked.Count);
                foreach(GeneralStaticLogic.LogicRewardConfig cfg in GeneralStaticLogic.QuestForRanked)
                {
                    encoder.WriteBoolean(true); // LogicRewardConfig
                    encoder.WriteVInt((byte)cfg.QuestType); // Quest Type
                    encoder.WriteVInt((byte)cfg.NeededRank); // Rank
                    encoder.WriteBoolean(true); // LogicRewardConfig
                    cfg.GemOffer.Encode(encoder);
                }

                encoder.WriteVInt(1); // Quests Count
                encoder.WriteVInt(-1);
                encoder.WriteVInt(-1);

                encoder.WriteVInt(19);// Road Count
                for(int j = 1; j < 20; j++)
                {
                    encoder.WriteVInt(j);
                    encoder.WriteVInt(500);
                }
            }

            if(Slot == 15)
            {
                encoder.WriteVInt(GeneralStaticLogic.RankedSeason);
                encoder.WriteString(GeneralStaticLogic.RankedSeasonTID);
                encoder.WriteVInt(0);
                encoder.WriteVInt(0);

                encoder.WriteByte((byte)GeneralStaticLogic.QuestForRanked.Count);
                foreach (GeneralStaticLogic.LogicRewardConfig cfg in GeneralStaticLogic.QuestForRanked)
                {
                    encoder.WriteBoolean(true); // LogicRewardConfig
                    encoder.WriteVInt((byte)cfg.QuestType); // Quest Type
                    encoder.WriteVInt((byte)cfg.NeededRank); // Rank
                    encoder.WriteBoolean(true); // LogicRewardConfig
                    cfg.GemOffer.Encode(encoder);
                }

                encoder.WriteVInt(0); // Quests Count

                encoder.WriteVInt(19);// Road Count
                for (int j = 1; j < 20; j++)
                {
                    encoder.WriteVInt(j);
                    encoder.WriteVInt(500);
                }
            }
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
            encoder.WriteBoolean(false);
            encoder.WriteBoolean(false);
            encoder.WriteBoolean(false);
            //encoder.WriteBoolean(false);
            encoder.WriteVInt(-1);
            encoder.WriteBoolean(false);
            encoder.WriteBoolean(false);
            encoder.WriteVInt(-1);

            encoder.WriteVInt(0);//v51
            encoder.WriteVInt(0);//v51
            encoder.WriteVInt(0);//v51

            encoder.WriteBoolean(false);//v53
        }
    }
}
