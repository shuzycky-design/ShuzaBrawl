namespace IndusBrawl.Laser.Server.Database
{
    using MySql.Data.MySqlClient;
    using Newtonsoft.Json;
    using IndusBrawl.Laser.Logic.Club;
    using IndusBrawl.Laser.Server.Database.Cache;
    using IndusBrawl.Laser.Server.Settings;
    using System;
    using System.Collections.Generic;

    public static class Alliances
    {
        private static long AllianceIdCounter;
        private static string ConnectionString;

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
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            ConnectionString = builder.ToString();

            AllianceCache.Init();

            try
            {
                AllianceIdCounter = GetMaxAllianceId();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to initialize AllianceIdCounter: {ex.Message}");
            }
        }

        public static long GetMaxAllianceId()
        {
            const string query = "SELECT COALESCE(MAX(Id), 0) FROM alliances";

            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            using var command = new MySqlCommand(query, connection);
            var result = command.ExecuteScalar();

            return result != null ? Convert.ToInt64(result) : 0;
        }

        public static void Create(Alliance alliance)
        {
            if (alliance == null) return;

            try
            {
                alliance.Id = ++AllianceIdCounter;
                string json = JsonConvert.SerializeObject(alliance);

                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();

                using var command = new MySqlCommand(
                    "INSERT INTO alliances (`Id`, `Name`, `Trophies`, `Data`) VALUES (@id, @name, @trophies, @data)",
                    connection);

                command.Parameters.AddWithValue("@id", alliance.Id);
                command.Parameters.AddWithValue("@name", alliance.Name);
                command.Parameters.AddWithValue("@trophies", alliance.Trophies);
                command.Parameters.AddWithValue("@data", json);

                command.ExecuteNonQuery();

                AllianceCache.Cache(alliance);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to create alliance: {ex.Message}");
            }
        }

        public static void Save(Alliance alliance)
        {
            if (alliance == null) return;

            try
            {
                string json = JsonConvert.SerializeObject(alliance);

                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();

                using var command = new MySqlCommand(
                    "UPDATE alliances SET `Trophies` = @trophies, `Data` = @data WHERE Id = @id",
                    connection);

                command.Parameters.AddWithValue("@trophies", alliance.Trophies);
                command.Parameters.AddWithValue("@data", json);
                command.Parameters.AddWithValue("@id", alliance.Id);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to save alliance: {ex.Message}");
            }
        }

        public static Alliance Load(long id)
        {
            if (AllianceCache.IsAllianceCached(id))
            {
                return AllianceCache.GetAlliance(id);
            }

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();

                using var command = new MySqlCommand("SELECT Data FROM alliances WHERE Id = @id", connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    var json = (string)reader["Data"];
                    var alliance = JsonConvert.DeserializeObject<Alliance>(json);
                    if (alliance != null)
                    {
                        AllianceCache.Cache(alliance);
                    }
                    return alliance;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load alliance with ID {id}: {ex.Message}");
            }

            return null;
        }

        public static List<Alliance> GetRankingList()
        {
            var list = new List<Alliance>();

            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();

                using var command = new MySqlCommand(
                    "SELECT Data FROM alliances ORDER BY `Trophies` DESC LIMIT 200",
                    connection);

                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var json = (string)reader["Data"];
                    var alliance = JsonConvert.DeserializeObject<Alliance>(json);
                    if (alliance != null)
                    {
                        list.Add(alliance);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get ranking list: {ex.Message}");
            }

            return list;
        }

        public static List<Alliance> GetRandomAlliances(int maxCount)
        {
            var list = new List<Alliance>();
            long count = Math.Min(maxCount, AllianceIdCounter);

            try
            {
                var rand = new Random();
                var loadedIds = new HashSet<long>(); // избегаем дубликатов

                for (int i = 0; i < count; i++)
                {
                    long id;
                    do
                    {
                        id = rand.NextInt64(1, AllianceIdCounter + 1);
                    }
                    while (loadedIds.Contains(id));

                    var alliance = Load(id);
                    if (alliance != null)
                    {
                        list.Add(alliance);
                        loadedIds.Add(id);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get random alliances: {ex.Message}");
            }

            return list;
        }
    }
}