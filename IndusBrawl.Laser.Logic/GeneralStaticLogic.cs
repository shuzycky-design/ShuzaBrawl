using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IndusBrawl.Laser.Logic.Battle.Structures;
using IndusBrawl.Laser.Logic.Data;
using IndusBrawl.Laser.Logic.Home.Items;
using IndusBrawl.Laser.Titan.DataStream;

namespace IndusBrawl.Laser.Logic
{
    public static class GeneralStaticLogic
    {
        public class LogicRewardConfig
        {
            public int QuestType;
            public int NeededRank;
            public GemOffer GemOffer;
            public LogicRewardConfig(int a1, int a2, GemOffer a3)
            {
                QuestType = a1; NeededRank = a2; GemOffer = a3;
            }
        }
        
        public static int RankedSeason = 0;
        public static string RankedSeasonTID = "TID_BRAWL_PASS_SEASON_4";
        public static List<LogicRewardConfig> QuestForRanked = new List<LogicRewardConfig>
        {
            {new(4, 30, new(25,1, 0,63-3)) },
            {new(2, 7, new(25,1, 0,64-3)) },
            {new(1, 60, new(26,1,0,277-3)) },
        };

       
        public static List<int> EventListForSecret = new()
        {
            697-3,
            673-3,
            667-3,
            660-3,
            27-3,
            8-3,
            5-3
        };
        public static List<int> EventListForChaos = new()
        {
            17-3,
            18-3,
            19-3,
            16-3,
            46-3,
            48-3
        }; 
        public static List<int> BrawlerListForChaos = new()
        {
            0,
            5-3,
            27-3,
            37-3,
            46-3,
            42-3
        };
        public static List<string> BlockedEventsForRanked = new()
        {
            "Knockout",
            "KingOfHill"
        };
    }
}
