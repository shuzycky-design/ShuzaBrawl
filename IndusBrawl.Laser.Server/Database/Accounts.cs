namespace IndusBrawl.Laser.Server.Database
{
    using MySql.Data.MySqlClient;
    using Newtonsoft.Json;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Server.Database.Cache;
    using IndusBrawl.Laser.Server.Database.Models;
    using IndusBrawl.Laser.Server.Settings;
    using IndusBrawl.Laser.Server.Utils;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Home;

    public static class Accounts
    {
        private static long AvatarIdCounter;
        private static string ConnectionString;
        public static string GetConnectionString() => ConnectionString;

        private static List<Account> CachedGlobalRanking = new List<Account>();
        private static Dictionary<int, List<Account>> CachedBrawlerRankings = new Dictionary<int, List<Account>>();
        private static DateTime LastGlobalUpdate = DateTime.MinValue;
        private static DateTime LastBrawlerUpdate = DateTime.MinValue;
        private const int CacheTimeoutSeconds = 15;
        private const int RANKING_LIMIT = 200;

        public static void Init(string user, string password)
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
            builder.Server = "127.0.0.1";
            builder.UserID = user;
            builder.Password = password;
            builder.SslMode = MySqlSslMode.Disabled;
            builder.Database = Configuration.Instance.DatabaseName;
            builder.CharacterSet = "utf8mb4";
            builder.AllowPublicKeyRetrieval = true;

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore
            };

            ConnectionString = builder.ToString();

            AccountCache.Init();

            try
            {
                AvatarIdCounter = GetMaxAvatarId();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL] Ошибка инициализации AvatarIdCounter: {ex.Message}");
            }
        }

        public static long GetMaxAvatarId()
        {
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                using var cmd = new MySqlCommand("SELECT COALESCE(MAX(Id), 0) FROM accounts", connection);
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt64(result) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Ошибка получения максимального ID: {ex.Message}");
                return 0;
            }
        }

        public static Account Create()
        {
            try
            {
                Account account = new Account();
                account.AccountId = ++AvatarIdCounter;
                account.PassToken = Helpers.RandomString(40);

                account.Avatar = new ClientAvatar();
                account.Avatar.AccountId = account.AccountId;
                account.Avatar.PassToken = account.PassToken;

                account.Home = new ClientHome();
                account.Home.HomeId = account.AccountId;

                // Используем правильный конструктор Hero
                Hero hero = new Hero(16000000); // Предполагаем что конструктор принимает ID героя
                account.Avatar.Heroes = new List<Hero> { hero };

                account.Avatar.Gold = 100;
                // Убираем присвоение Trophies и HighTrophies если они read-only

                // Инициализация списков
                foreach (var h in account.Avatar.Heroes)
                {
                    if (h.UnlockedGearInstanceIds == null)
                        h.UnlockedGearInstanceIds = new List<int>();
                }

                account.Home.UnlockedSkins = new List<int>();
                account.Home.UnlockedEmotes = new List<int>();
                account.Home.UnlockedThumbnails = new List<int>();
                account.Home.UnlockedTituls = new List<int>();
                account.Avatar.SPGS = new List<int>();
                account.Avatar.SelectedSPGS = new List<int>();

                account.Home.RankedSoloRank = 0;
                account.Home.RankedSoloProgress = 0;
                account.Home.RankedTrioRank = 0;
                account.Home.RankedTrioProgress = 0;

                string json = JsonConvert.SerializeObject(account);

                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                using var cmd = new MySqlCommand("INSERT INTO accounts (`Id`, `Trophies`, `Data`) VALUES (@id, @trophies, @data)", connection);
                cmd.Parameters.AddWithValue("@id", account.AccountId);
                cmd.Parameters.AddWithValue("@trophies", 0); // Начальное значение трофеев
                cmd.Parameters.AddWithValue("@data", json);
                cmd.ExecuteNonQuery();

                AccountCache.Cache(account);

                return account;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Ошибка создания аккаунта: {ex.Message}");
                return null;
            }
        }

        public static void Save(Account account)
        {
            if (account == null) return;

            try
            {
                InitializeAllLists(account);

                string json = JsonConvert.SerializeObject(account);

                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                using var cmd = new MySqlCommand("UPDATE accounts SET `Trophies`=@trophies, `Data`=@data WHERE Id = @id", connection);
                cmd.Parameters.AddWithValue("@id", account.AccountId);
                cmd.Parameters.AddWithValue("@trophies", account.Avatar?.Trophies ?? 0);
                cmd.Parameters.AddWithValue("@data", json);
                cmd.ExecuteNonQuery();

                AccountCache.Cache(account);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Ошибка сохранения аккаунта: {ex.Message}");
            }
        }

        public static Account Load(long id)
        {
            if (AccountCache.IsAccountCached(id))
            {
                var cachedAccount = AccountCache.GetAccount(id);
                if (cachedAccount != null)
                {
                    InitializeAllLists(cachedAccount);
                    return cachedAccount;
                }
            }

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                using var cmd = new MySqlCommand("SELECT `Data` FROM accounts WHERE Id = @id", connection);
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string jsonData = reader["Data"].ToString();
                    
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Ignore,
                        Error = (sender, args) => 
                        {
                            Console.WriteLine($"[Load] Deserialization error for account {id}: {args.ErrorContext.Error.Message}");
                            args.ErrorContext.Handled = true;
                        }
                    };

                    Account account = JsonConvert.DeserializeObject<Account>(jsonData, settings);

                    if (account == null) 
                    {
                        Console.WriteLine($"[Load] Failed to deserialize account {id}, creating new one");
                        return Create();
                    }

                    account.AccountId = id;
                    InitializeAllLists(account);
                    AccountCache.Cache(account);
                    return account;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL] Ошибка загрузки аккаунта {id}: {ex.Message}");
            }

            return Create();
        }

        public static List<Account> GetSoloRankingList()
{
    var list = new List<Account>();
    try
    {
        using var connection = new MySqlConnection(ConnectionString);
        connection.Open();
        
        // ✅ ИЗМЕНЕНО: Сортируем по Rank, а не по Progress
        // и берем только тех, у кого Rank >= 1
        using var command = new MySqlCommand(
            "SELECT Id, Data FROM accounts WHERE JSON_EXTRACT(Data, '$.Home.RankedSoloRank') >= 1 ORDER BY CAST(JSON_EXTRACT(Data, '$.Home.RankedSoloRank') AS UNSIGNED) DESC, CAST(JSON_EXTRACT(Data, '$.Home.RankedSoloProgress') AS UNSIGNED) DESC LIMIT 200",
            connection);
            
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            try
            {
                long accountId = reader.GetInt64("Id");
                string jsonData = reader.GetString("Data");

                var account = JsonConvert.DeserializeObject<Account>(jsonData);
                if (account == null) continue;

                account.AccountId = accountId;

                if (account.Home == null)
                    account.Home = new ClientHome();

                if (account.Avatar == null)
                    account.Avatar = new ClientAvatar();

                // ✅ Гарантируем, что Rank не меньше 1
                if (account.Home.RankedSoloRank < 1 && account.Home.RankedSoloProgress > 0)
                {
                    account.Home.RankedSoloRank = CalculateRankFromProgress(account.Home.RankedSoloProgress);
                }
                
                // ✅ Если Rank все еще меньше 1, ставим 1
                if (account.Home.RankedSoloRank < 1)
                {
                    account.Home.RankedSoloRank = 1;
                }

                InitializeAllLists(account);
                list.Add(account);
            }
            catch (Exception ex)
            {
                Logger.Error($"Solo LB Error: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        Logger.Error($"Solo Ranking DB Error: {ex.Message}");
    }

    // Сортировка по Rank (на всякий случай)
    return list.OrderByDescending(x => x.Home?.RankedSoloRank ?? 0)
               .ThenByDescending(x => x.Home?.RankedSoloProgress ?? 0)
               .ToList();
}

public static List<Account> GetTrioRankingList()
{
    var list = new List<Account>();
    try
    {
        using var connection = new MySqlConnection(ConnectionString);
        connection.Open();

        using var command = new MySqlCommand(
            "SELECT Id, Data FROM accounts WHERE JSON_EXTRACT(Data, '$.Home.RankedTrioRank') >= 1 ORDER BY CAST(JSON_EXTRACT(Data, '$.Home.RankedTrioRank') AS UNSIGNED) DESC, CAST(JSON_EXTRACT(Data, '$.Home.RankedTrioProgress') AS UNSIGNED) DESC LIMIT 200",
            connection);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            try
            {
                long accountId = reader.GetInt64("Id");
                string jsonData = reader.GetString("Data");

                var account = JsonConvert.DeserializeObject<Account>(jsonData);
                if (account == null) continue;

                account.AccountId = accountId;

                if (account.Home == null)
                    account.Home = new ClientHome();

                if (account.Avatar == null)
                    account.Avatar = new ClientAvatar();

                if (account.Home.RankedTrioRank < 1 && account.Home.RankedTrioProgress > 0)
                {
                    account.Home.RankedTrioRank = CalculateRankFromProgress(account.Home.RankedTrioProgress);
                }
                
                if (account.Home.RankedTrioRank < 1)
                {
                    account.Home.RankedTrioRank = 1;
                }

                InitializeAllLists(account);
                list.Add(account);
            }
            catch (Exception ex)
            {
                Logger.Error($"Trio LB Error: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        Logger.Error($"Trio Ranking DB Error: {ex.Message}");
    }

    return list.OrderByDescending(x => x.Home?.RankedTrioRank ?? 0)
               .ThenByDescending(x => x.Home?.RankedTrioProgress ?? 0)
               .ToList();
}

private static int CalculateRankFromProgress(int progress)
{
    // Минимальный ранг = 1 (не 0!)
    if (progress < 50) return 1;   // Бронза 1
    if (progress < 100) return 2;  // Бронза 2
    if (progress < 200) return 3;  // Бронза 3
    if (progress < 350) return 4;  // Серебро 1
    if (progress < 500) return 5;  // Серебро 2
    if (progress < 700) return 6;  // Серебро 3
    if (progress < 900) return 7;  // Золото 1
    if (progress < 1200) return 8; // Золото 2
    if (progress < 1500) return 9; // Золото 3
    return 10; // Алмаз+
}

        private static void InitializeAllLists(Account account)
        {
            if (account == null) return;

            try
            {
                if (account.Home == null)
                    account.Home = new ClientHome();

                if (account.Avatar == null)
                {
                    account.Avatar = new ClientAvatar();
                    account.Avatar.AccountId = account.AccountId;
                }

                if (account.Avatar.Heroes == null)
                {
                    account.Avatar.Heroes = new List<Hero>();
                }

                foreach (var hero in account.Avatar.Heroes)
                {
                    if (hero == null) continue;

                    if (hero.UnlockedGearInstanceIds == null)
                        hero.UnlockedGearInstanceIds = new List<int>();

                    if (hero.SelectedGearId1 == 0)
                        hero.SelectedGearId1 = -1;

                    if (hero.SelectedGearId2 == 0)
                        hero.SelectedGearId2 = -1;
                }

                if (account.Avatar.SPGS == null)
                    account.Avatar.SPGS = new List<int>();

                if (account.Avatar.SelectedSPGS == null)
                    account.Avatar.SelectedSPGS = new List<int>();

                if (account.Home.UnlockedSkins == null)
                    account.Home.UnlockedSkins = new List<int>();

                if (account.Home.UnlockedEmotes == null)
                    account.Home.UnlockedEmotes = new List<int>();

                if (account.Home.UnlockedThumbnails == null)
                    account.Home.UnlockedThumbnails = new List<int>();

                if (account.Home.UnlockedTituls == null)
                    account.Home.UnlockedTituls = new List<int>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InitializeAllLists Error: {ex.Message}");
            }
        }

        public static List<Account> GetRankingList()
        {
            var now = DateTime.UtcNow;
            if ((now - LastGlobalUpdate).TotalSeconds > CacheTimeoutSeconds)
            {
                RefreshGlobalRankingCache();
                LastGlobalUpdate = now;
            }
            return CachedGlobalRanking ?? new List<Account>();
        }

        public static List<Account> GetBrawlerRankingList(int heroId)
        {
            var now = DateTime.UtcNow;
            if (!CachedBrawlerRankings.ContainsKey(heroId) || (now - LastBrawlerUpdate).TotalSeconds > CacheTimeoutSeconds)
            {
                RefreshBrawlerRankingCache(heroId);
                LastBrawlerUpdate = now;
            }

            if (CachedBrawlerRankings.TryGetValue(heroId, out var ranking))
                return ranking ?? new List<Account>();

            return new List<Account>();
        }

        private static void RefreshGlobalRankingCache()
        {
            var list = new List<Account>();
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                using var cmd = new MySqlCommand(
                    "SELECT `Id`, `Data` FROM accounts WHERE `Trophies` > 0 ORDER BY `Trophies` DESC LIMIT @limit",
                    connection);
                cmd.Parameters.AddWithValue("@limit", RANKING_LIMIT);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    try
                    {
                        long accountId = reader.GetInt64("Id");
                        string jsonData = reader["Data"].ToString();
                        
                        var settings = new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DefaultValueHandling = DefaultValueHandling.Ignore,
                            Error = (sender, args) => args.ErrorContext.Handled = true
                        };

                        var account = JsonConvert.DeserializeObject<Account>(jsonData, settings);

                        if (account != null)
                        {
                            account.AccountId = accountId;
                            InitializeAllLists(account);
                            list.Add(account);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RANKING] Ошибка десериализации аккаунта: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RANKING] Ошибка получения глобального списка: {ex.Message}");
            }

            CachedGlobalRanking = list;
        }

        private static void RefreshBrawlerRankingCache(int heroId)
        {
            var list = new List<Account>();
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();

                using var cmd = new MySqlCommand(
                    "SELECT `Id`, `Data` FROM accounts WHERE `Trophies` > 0",
                    connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    try
                    {
                        long accountId = reader.GetInt64("Id");
                        string jsonData = reader["Data"].ToString();
                        
                        var settings = new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DefaultValueHandling = DefaultValueHandling.Ignore,
                            Error = (sender, args) => args.ErrorContext.Handled = true
                        };

                        var account = JsonConvert.DeserializeObject<Account>(jsonData, settings);

                        if (account != null)
                        {
                            account.AccountId = accountId;
                            InitializeAllLists(account);

                            // Используем GetHero метод если он есть
                            var hero = account.Avatar?.GetHero(heroId);
                            if (hero != null && hero.Trophies > 0)
                            {
                                list.Add(account);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RANKING] Ошибка десериализации аккаунта в бравлер-рейтинге: {ex.Message}");
                    }
                }

                list.Sort((a, b) =>
                {
                    var aHero = a?.Avatar?.GetHero(heroId);
                    var bHero = b?.Avatar?.GetHero(heroId);

                    int aTrophies = aHero?.Trophies ?? 0;
                    int bTrophies = bHero?.Trophies ?? 0;

                    return bTrophies.CompareTo(aTrophies);
                });

                if (list.Count > RANKING_LIMIT)
                {
                    list = list.Take(RANKING_LIMIT).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RANKING] Ошибка получения бравлер-рейтинга: {ex.Message}");
            }

            CachedBrawlerRankings[heroId] = list;
        }

        public static List<long> GetAllAccountIdsFromDatabase()
        {
            var accountIds = new List<long>();
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                using var cmd = new MySqlCommand("SELECT Id FROM accounts", connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    long accountId = reader.GetInt64("Id");
                    accountIds.Add(accountId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Accounts] Ошибка при получении всех AccountId: {ex.Message}");
            }
            return accountIds;
        }
    }
}