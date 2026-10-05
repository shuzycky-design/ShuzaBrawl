namespace IndusBrawl.Laser.Logic.Util
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Случайные ники для ботов, похожие на ники живых игроков
    /// </summary>
    public static class BotNames
    {
        private static readonly Random _random = new Random();
        private static readonly object _lock = new object();
        private static readonly Queue<string> _recent = new Queue<string>();
        private static readonly HashSet<string> _recentSet = new HashSet<string>();

        private static readonly string[] Words =
        {
            "qold", "sqwaq", "zerk", "frost", "nexo", "vexx", "kaizo", "lunar", "ghost", "toxic", "raven", "pixel", "shadow", "blaze",
            "storm", "viper", "onyx", "drift", "karma", "venom", "astro", "neon", "rogue", "flare", "cobra", "ember", "glitch", "hazard",
            "kiro", "miko", "sora", "yuki", "taro", "riku", "nori", "haru", "zen", "jinx", "lynx", "fenix", "krix", "dexo", "tayo",
            "mishka", "kotik", "pchel", "volk", "lis", "bober", "ezhik", "tigr", "zayac", "medved", "sokol", "krot", "pirog", "arbuz",
            "sanya", "dimon", "vlad", "artem", "kirill", "max", "egor", "nikita", "danya", "timur", "roma", "leha", "misha", "ilya",
            "pro", "king", "boss", "god", "top", "ace", "noob", "main", "star", "brawl", "leon", "crow", "spike", "colt", "mortis",
        };

        private static readonly string[] Cyrillic =
        {
            "Саня", "Димон", "Влад", "Артём", "Кирилл", "Макс", "Егор", "Никита", "Даня", "Тимур", "Рома", "Лёха", "Миша", "Илья",
            "Котик", "Волк", "Лис", "Бобёр", "Ёжик", "Тигр", "Пельмень", "Арбуз", "Чебурек", "Батон", "Кефир", "Борщ", "Сырок",
            "Мастер", "Тащер", "Нагибатор", "Профи", "Король", "Босс", "Легенда", "Тень", "Гром", "Шторм", "Призрак", "Ворон",
        };

        private static readonly string[] Consonants = { "q", "w", "r", "t", "p", "s", "d", "f", "g", "h", "k", "l", "z", "x", "v", "b", "n", "m", "sh", "kr", "st", "tr", "zx", "qw" };
        private static readonly string[] Vowels = { "a", "e", "i", "o", "u", "y", "aa", "oo", "ei" };

        private static string Pick(string[] items) => items[_random.Next(items.Length)];

        private static string Capitalize(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

        // Произносимый набор букв: sqwaq, zexo, kraan
        private static string Gibberish()
        {
            int syllables = _random.Next(2, 4);
            string result = "";
            for (int i = 0; i < syllables; i++) result += Pick(Consonants) + Pick(Vowels);
            if (_random.Next(3) == 0) result += Pick(Consonants);
            return result;
        }

        private static string Number()
        {
            switch (_random.Next(6))
            {
                case 0: return _random.Next(1, 10).ToString();
                case 1: return _random.Next(10, 100).ToString();
                case 2: return _random.Next(2007, 2016).ToString();       // год рождения
                case 3: return Pick(new[] { "228", "777", "666", "1337", "007", "999", "123", "52", "67" });
                case 4: return _random.Next(100, 1000).ToString();
                default: return "0" + _random.Next(1, 10);
            }
        }

        private static string Build()
        {
            string word = _random.Next(4) == 0 ? Gibberish() : Pick(Words);
            switch (_random.Next(14))
            {
                case 0: return word;
                case 1: return Capitalize(word);
                case 2: return word + Number();
                case 3: return Capitalize(word) + Number();
                case 4: return word + "_" + Number();
                case 5: return word.Substring(0, 1) + "-" + word;                    // q-qold
                case 6: return Pick(Words) + "_" + Pick(Words);
                case 7: return Capitalize(Pick(Words)) + Capitalize(Pick(Words));
                case 8: return "xX" + Capitalize(word) + "Xx";
                case 9: return word.ToUpperInvariant();
                case 10: return Pick(Cyrillic);
                case 11: return Pick(Cyrillic) + Number();
                case 12: return Pick(Cyrillic) + "_" + Number();
                default: return Gibberish();
            }
        }

        /// <summary>
        /// Случайный ник (до 15 символов), не повторяющий недавно выданные
        /// </summary>
        public static string Next()
        {
            lock (_lock)
            {
                for (int attempt = 0; attempt < 50; attempt++)
                {
                    string name = Build();
                    if (name.Length < 3 || name.Length > 15 || _recentSet.Contains(name)) continue;

                    _recent.Enqueue(name);
                    _recentSet.Add(name);
                    if (_recent.Count > 200) _recentSet.Remove(_recent.Dequeue());
                    return name;
                }
                return "player" + _random.Next(100, 10000);
            }
        }

        public static int NextInt(int min, int maxExclusive)
        {
            lock (_lock) return _random.Next(min, maxExclusive);
        }
    }
}
