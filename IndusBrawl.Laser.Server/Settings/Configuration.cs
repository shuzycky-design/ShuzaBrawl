namespace IndusBrawl.Laser.Server.Settings
{
    using Newtonsoft.Json;

    public class Configuration
    {
        public static Configuration Instance;

        [JsonProperty("udp_host")] public readonly string UdpHost;
        [JsonProperty("udp_port")] public readonly int UdpPort;

        [JsonProperty("database_username")] public readonly string DatabaseUsername;
        [JsonProperty("database_password")] public readonly string DatabasePassword;
        [JsonProperty("database_name")] public readonly string DatabaseName;

        [JsonProperty("update_sha")] public readonly string UpdateSha;
        [JsonProperty("ContentUrl")] public readonly string ContentUrl;
        [JsonProperty("Fingerprint")] public readonly string Fingerprint;

        [JsonProperty("MessageLogger")] public readonly bool MsgLogger;

        // Необязательные порты (0 = значение по умолчанию)
        [JsonProperty("tcp_port")] public readonly int TcpPort;
        [JsonProperty("web_port")] public readonly int WebPort;

        // Админ-панель (слушает только 127.0.0.1, наружу - через nginx)
        [JsonProperty("admin_port")] public readonly int AdminPort;
        [JsonProperty("admin_login")] public readonly string AdminLogin;
        [JsonProperty("admin_password")] public readonly string AdminPassword;

        // Telegram-бот: токен от @BotFather и Telegram ID администраторов бота
        [JsonProperty("telegram_bot_token")] public readonly string TelegramBotToken;
        [JsonProperty("telegram_admin_ids")] public readonly long[] TelegramAdminIds;
        public static Configuration LoadFromFile(string filename)
        {
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(filename));
        }
    }
}
