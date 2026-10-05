namespace IndusBrawl.Laser.Logic.Home
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using IndusBrawl.Laser.Titan.DataStream;
    using Newtonsoft.Json;

    /// <summary>
    /// Испытательный режим формата стардропов. Действует только на аккаунты из списка Tags,
    /// настройки читаются из starrdrop_lab.json на лету (без перезапуска сервера).
    /// Токены: "v5" - VInt, "b1" - байт, "i0" - Int, "B1" - Boolean, "r80:3" - ссылка на данные.
    /// Вместо числа можно писать R (редкость дропа 0..4) или R1 (редкость + 1).
    /// </summary>
    public class StarrDropLabConfig
    {
        public List<long> AccountIds { get; set; } = new List<long>();
        public List<string> DeliveryTrailer { get; set; }    // блок после списка наград в команде выдачи
        public bool SkipPassBool { get; set; }                // не писать лишний Boolean перед блоком пути славы/пропуска
        public int OpenCount { get; set; } = -1;              // значение "количество" в обновлении наград при открытии (-1 = как раньше)
        public int ClaimCount { get; set; } = -1;             // то же при получении с пути славы/пропуска
        public bool TwoPhase { get; set; }                    // правильная схема: сначала "ожидающий стардроп", награда - по запросу клиента
        public bool AllAccounts { get; set; }                 // включить для всех игроков, а не только для AccountIds

        // Подбор деталей анимации (меняются на лету). Значения контейнера: -1 = фактическая редкость дропа,
        // -2 = пустая ссылка, 0..4 = конкретная редкость (0 - редкий ... 4 - легендарный)
        public int PendingContainer { get; set; } = -1;       // контейнер в списке ожидающих (команда 228)
        public int PendingC { get; set; } = 0;                // число в третьем списке ожидающих (-1 = редкость)
        public int DeliveryContainer { get; set; } = -2;      // ссылка на контейнер в команде выдачи (поле 0x58)
        public int FlagsByte { get; set; } = 2;               // два Boolean перед ссылкой (поля 0x50 и 0x51)
        public int F60 { get; set; } = -1;
        public int F64 { get; set; } = -1;

        // Стардроп с пути славы/пропуска: сразу отметить ячейку полученной пустой выдачей,
        // а сам дроп открыть с анимацией, когда игрок вернётся в лобби
        // Стардроп с пути славы/пропуска с анимацией: экран пропуска снимает ожидание только видимой выдачей,
        // поэтому ячейка сразу даёт небольшой бонус монетами (обычное окно награды), а сам дроп ставится в очередь
        // и открывается с тапами, как только клиент сможет открыть окно стардропа. 0 = выключено (награда сразу, без анимации)
        public int TileBonusCoins { get; set; }
        public bool TileAck { get; set; }
        public int TileAckPresentation { get; set; } = 2;   // пустая выдача с обычным окном (0) роняет клиент

        public static void WriteContainer(IndusBrawl.Laser.Titan.DataStream.ByteStream stream, int value, int rarity)
        {
            if (value == -2) stream.WriteDataReference(0, 0);
            else stream.WriteDataReference(80, value == -1 ? rarity : value);
        }
    }

    public static class StarrDropLab
    {
        private const string PATH = "starrdrop_lab.json";
        private static readonly object _lock = new object();
        private static StarrDropLabConfig _config;
        private static DateTime _loadedAt;

        private static StarrDropLabConfig Config
        {
            get
            {
                lock (_lock)
                {
                    try
                    {
                        if (!File.Exists(PATH)) return null;
                        DateTime modified = File.GetLastWriteTimeUtc(PATH);
                        if (_config == null || modified != _loadedAt)
                        {
                            _config = JsonConvert.DeserializeObject<StarrDropLabConfig>(File.ReadAllText(PATH));
                            _loadedAt = modified;
                            Console.WriteLine("[StarrDropLab] Настройки загружены");
                        }
                        return _config;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StarrDropLab] Ошибка чтения {PATH}: {ex.Message}");
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Настройки для аккаунта или null, если он не в испытательном списке
        /// </summary>
        public static StarrDropLabConfig For(HomeMode homeMode)
        {
            StarrDropLabConfig config = Config;
            if (config == null || homeMode?.Avatar == null) return null;
            if (config.AllAccounts) return config;
            return config.AccountIds != null && config.AccountIds.Contains(homeMode.Avatar.AccountId) ? config : null;
        }

        private static int Value(string text, int rarity)
        {
            if (text == "R") return rarity;
            if (text == "R1") return rarity + 1;
            return int.Parse(text);
        }

        public static void Write(ByteStream stream, List<string> tokens, int rarity)
        {
            foreach (string token in tokens)
            {
                string body = token.Substring(1);
                switch (token[0])
                {
                    case 'v': stream.WriteVInt(Value(body, rarity)); break;
                    case 'b': stream.WriteByte((byte)Value(body, rarity)); break;
                    case 'i': stream.WriteInt(Value(body, rarity)); break;
                    case 'B': stream.WriteBoolean(Value(body, rarity) != 0); break;
                    case 'r':
                        string[] parts = body.Split(':');
                        stream.WriteDataReference(Value(parts[0], rarity), Value(parts[1], rarity));
                        break;
                    default: throw new FormatException("Неизвестный токен: " + token);
                }
            }
        }
    }
}
