namespace IndusBrawl.Laser.Server.Logic.Game
{
    using IndusBrawl.Laser.Logic.Club;
    using IndusBrawl.Laser.Server.Database.Models;

    public static class Leaderboards
    {
        private static List<Account> Accounts;
        private static List<Alliance> Alliances;
        private static Thread Thread;
                private static List<Account> RankedSoloAccounts;
        private static List<Account> RankedTeamAccounts;

        public static void Init()
        {
            Accounts = new List<Account>();
            Alliances = new List<Alliance>();
            RankedSoloAccounts = new List<Account>();
            RankedTeamAccounts = new List<Account>();
            Thread = new Thread(Update);
            Thread.Start();
        }

        public static Account[] GetAvatarRankingList()
        {
            return Accounts.ToArray();
        }
 public static Account[] GetRankedSoloAccountsList()
        {
            return RankedSoloAccounts.ToArray();
        }
        public static Account[] GetRankedTeamAccountsList()
        {
            return RankedTeamAccounts.ToArray();
        }
        public static Account[] GetBrawlerRankingList(int HeroDataId)
        {
            return Database.Accounts.GetBrawlerRankingList(HeroDataId).ToArray();
        }

        public static Alliance[] GetAllianceRankingList()
        {
            return Alliances.ToArray();
        }

        private static void Update()
        {
            while (true)
            {
                Accounts = Database.Accounts.GetRankingList();
                Alliances = Database.Alliances.GetRankingList();
                                    RankedSoloAccounts = Database.Accounts.GetSoloRankingList();
                    RankedTeamAccounts = Database.Accounts.GetSoloRankingList();
                Thread.Sleep(20 * 1000);
            }
        }
    }
}
