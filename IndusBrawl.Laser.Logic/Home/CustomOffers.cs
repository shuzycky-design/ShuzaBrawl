namespace IndusBrawl.Laser.Logic.Home
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// Акция магазина, созданная через админ-панель
    /// </summary>
    /// <summary>
    /// Один товар внутри акции (набор может содержать несколько)
    /// </summary>
    public class CustomOfferItem
    {
        public int Item { get; set; }           // ShopItem
        public int Count { get; set; }
        public int BrawlerId { get; set; }      // для бойца
        public int ExtraId { get; set; }        // скин / пин / аватарка
    }

    public class CustomOffer
    {
        public string Id { get; set; }          // уникальный ID (он же ключ "уже куплено")
        public string Title { get; set; }
        public int Item { get; set; }           // ShopItem
        public int Count { get; set; }
        public int BrawlerId { get; set; }      // для бойца
        public int ExtraId { get; set; }        // скин / пин / аватарка / титул
        public int Cost { get; set; }
        public int OldCost { get; set; }
        public int Currency { get; set; }       // 0 - гемы, 1 - монеты, 6 - блинги
        public string Background { get; set; }
        public DateTime Start { get; set; }     // время сервера
        public DateTime End { get; set; }
        public bool Enabled { get; set; } = true;
        public List<CustomOfferItem> Items { get; set; }   // состав набора

        /// <summary>
        /// Состав акции: список Items, а для старых записей - одиночные поля
        /// </summary>
        public List<CustomOfferItem> GetItems()
        {
            if (Items != null && Items.Count > 0) return Items;
            return new List<CustomOfferItem>
            {
                new CustomOfferItem { Item = Item, Count = Count, BrawlerId = BrawlerId, ExtraId = ExtraId }
            };
        }
    }

    public static class CustomOffers
    {
        private const string PATH = "shop_offers.json";
        private static readonly object _lock = new object();
        private static List<CustomOffer> _offers = new List<CustomOffer>();

        public static void Load()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(PATH))
                    {
                        _offers = JsonConvert.DeserializeObject<List<CustomOffer>>(File.ReadAllText(PATH)) ?? new List<CustomOffer>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CustomOffers] Ошибка загрузки {PATH}: {ex.Message}");
                    _offers = new List<CustomOffer>();
                }
            }
        }

        private static void Save()
        {
            // Явные настройки: глобальные выбрасывают нули и false (тогда выключенная акция включилась бы после перезапуска)
            File.WriteAllText(PATH, JsonConvert.SerializeObject(_offers, Formatting.Indented, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Include
            }));
        }

        public static List<CustomOffer> GetAll()
        {
            lock (_lock) return _offers.ToList();
        }

        /// <summary>
        /// Акции, которые сейчас должны быть видны в магазине
        /// </summary>
        public static List<CustomOffer> GetActive()
        {
            DateTime now = DateTime.Now;
            lock (_lock) return _offers.Where(o => o.Enabled && o.Start <= now && o.End > now).ToList();
        }

        public static bool Add(CustomOffer offer)
        {
            lock (_lock)
            {
                if (_offers.Any(o => o.Id == offer.Id)) return false;
                _offers.Add(offer);
                Save();
                return true;
            }
        }

        public static bool Remove(string id)
        {
            lock (_lock)
            {
                int removed = _offers.RemoveAll(o => o.Id == id);
                if (removed > 0) Save();
                return removed > 0;
            }
        }

        public static bool SetEnabled(string id, bool enabled)
        {
            lock (_lock)
            {
                CustomOffer offer = _offers.FirstOrDefault(o => o.Id == id);
                if (offer == null) return false;
                offer.Enabled = enabled;
                Save();
                return true;
            }
        }
    }
}
