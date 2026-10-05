using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using IndusBrawl.Laser.Server.Telegram;
using IndusBrawl.Laser.Server.Database.Models;
using IndusBrawl.Laser.Server.Database;
using IndusBrawl.Laser.Logic.Message.Account;
using IndusBrawl.Laser.Server.Networking.Session;
using IndusBrawl.Laser.Server.Networking;
using IndusBrawl.Laser.Logic.Home.Items;
using IndusBrawl.Laser.Logic.Message.Account.Auth;
using IndusBrawl.Laser.Logic.Util;
using IndusBrawl.Laser.Server.Utils;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using IndusBrawl.Laser.Logic.Data;
using IndusBrawl.Laser.Logic.Avatar;
using Newtonsoft.Json;
using System.Text;

namespace IndusBrawl.Laser.Server.Bot
{
    public class TelegramBot
    {
        private TelegramClient _client;
        private string _botToken = IndusBrawl.Laser.Server.Settings.Configuration.Instance.TelegramBotToken;

        private TelegramLinkManager _linkManager;
        private VipMarket _vipMarket;

        private static readonly ConcurrentDictionary<long, LinkState> _linkStates = new ConcurrentDictionary<long, LinkState>();
        private static readonly ConcurrentDictionary<long, bool> _notificationSettings = new ConcurrentDictionary<long, bool>();
        private static readonly ConcurrentDictionary<long, DateTime> _lastSpinTimes = new ConcurrentDictionary<long, DateTime>();
        private static readonly ConcurrentDictionary<long, int> _dailyStreaks = new ConcurrentDictionary<long, int>();
        private static readonly ConcurrentDictionary<long, bool> _activeSpinRequests = new ConcurrentDictionary<long, bool>();
        private static readonly ConcurrentDictionary<long, UserMenuState> _menuStates = new ConcurrentDictionary<long, UserMenuState>();

        public class UserMenuState
        {
            public string CurrentMenu { get; set; } = "main";
            public Dictionary<string, object> Data { get; set; } = new();
            public int CurrentPage { get; set; } = 0;
            public string SelectedTag { get; set; }
            public DateTime LastActivity { get; set; } = DateTime.UtcNow;
            public int LastMessageId { get; set; } = 0;
        }

        // Классы для Тайного Санты
        public class SantaParticipant
        {
            public long TelegramUserId { get; set; }
            public string AccountTag { get; set; }
            public long AccountId { get; set; }
            public DateTime JoinedAt { get; set; }
            public bool IsActive { get; set; }
            public int TotalGiftsSent { get; set; }
            public int TotalGiftsReceived { get; set; }
            public int TotalAmountSent { get; set; }
            public DateTime LastGiftSent { get; set; }
            public string Wish { get; set; }
        }

        public class SantaGift
        {
            public string Id { get; set; }
            public long FromUserId { get; set; }
            public long ToUserId { get; set; }
            public string FromTag { get; set; }
            public string ToTag { get; set; }
            public int Amount { get; set; }
            public string GiftType { get; set; }
            public DateTime SentAt { get; set; }
            public bool IsDelivered { get; set; }
            public string Message { get; set; }
        }

        public class SantaData
        {
            public Dictionary<long, SantaParticipant> Participants { get; set; } = new();
            public List<SantaGift> Gifts { get; set; } = new();
            public DateTime EventStart { get; set; }
            public DateTime EventEnd { get; set; }
        }

        private static readonly ConcurrentDictionary<long, SantaParticipant> _santaParticipants = new();
        private static readonly ConcurrentDictionary<string, SantaGift> _santaGifts = new();
        private static readonly ConcurrentDictionary<long, List<DateTime>> _dailyGiftLog = new();
        private static SantaData _santaData = new();
        private static readonly string SANTA_DATA_PATH = "./SantaData.json";
        
        private static readonly ConcurrentDictionary<long, int> _dailyGemsTransferred = new();
        private static readonly ConcurrentDictionary<long, DateTime> _lastTransferDate = new();
        private static readonly ConcurrentDictionary<long, DailyLimitsRecord> _dailyLimits = new();
        private const int DAILY_GEMS_LIMIT = 4000;

        public class DailyLimitsRecord
        {
            public long TelegramUserId { get; set; }
            public int GemsTransferred { get; set; }
            public DateTime LastTransferDate { get; set; }
        }

        private readonly HashSet<long> _adminIds = new HashSet<long>(IndusBrawl.Laser.Server.Settings.Configuration.Instance.TelegramAdminIds ?? new long[0]);

        private const int MAX_GIFTS_PER_DAY = 10;
        private const int MAX_GEMS_PER_GIFT = 1000;
        private const int MAX_COINS_PER_GIFT = 1000000;
        private const int MAX_POWERPOINTS_PER_GIFT = 5000;

        public class LinkState
        {
            public string TargetAccountId { get; set; }
            public string ConfirmationCode { get; set; }
            public DateTime Expiry { get; set; }
            public long TelegramUserId { get; set; }
            public int AttemptsCount { get; set; }
        }

        public class DailyReward
        {
            public int Gems { get; set; }
            public int Coins { get; set; }
            public int PowerPoints { get; set; }
            public int BrawlerId { get; set; }
            public bool HasBrawlerChance { get; set; }
            public int StreakBonus { get; set; }
            public int BrawlerPowerPoints { get; set; }
        }

        public class ClaimedOfferRecord
        {
            public long TelegramUserId { get; set; }
            public string AccountTag { get; set; }
            public string OfferTitle { get; set; }
            public int OfferCost { get; set; }
            public int ItemCount { get; set; }
            public ShopItem ItemType { get; set; }
            public DateTime ClaimedAt { get; set; }
            public string OfferHash { get; set; }
        }
        
        public class GemTransferRecord
        {
            public string Id { get; set; }
            public long TelegramUserId { get; set; }
            public string SenderTag { get; set; }
            public string ReceiverTag { get; set; }
            public int Amount { get; set; }
            public int Commission { get; set; }
            public int TotalDeducted { get; set; }
            public int DailyUsedBefore { get; set; }
            public int DailyRemainingBefore { get; set; }
            public DateTime TransferTime { get; set; }
        }

        public class SkinClaimRecord
        {
            public long TelegramUserId { get; set; }
            public string AccountTag { get; set; }
            public int SkinId { get; set; }
            public string OfferTitle { get; set; }
            public DateTime ClaimTime { get; set; }
            public bool IsProcessed { get; set; }
        }

        public TelegramBot()
        {
            _linkManager = new TelegramLinkManager();
            _client = new TelegramClient(_botToken);
            _vipMarket = new VipMarket(_client, _linkManager, _adminIds);
            InitializeSantaEvent();
            LoadDailyLimits();
            _ = Task.Run(() => StartDailyLimitResetTask());
            _ = Task.Run(() => StartDailyLimitsAutoSave());
            _ = Task.Run(() => CleanupOldSessions());
        }

        public async Task StartAsync()
        {
            Console.WriteLine("✅ Telegram Bot started with token: " + _botToken.Substring(0, 10) + "...");
            await StartPolling();
        }

        private async Task CleanupOldSessions()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(30));
                    var oldSessions = _menuStates.Where(kvp => 
                        (DateTime.UtcNow - kvp.Value.LastActivity).TotalMinutes > 30).Select(kvp => kvp.Key).ToList();
                    
                    foreach (var userId in oldSessions)
                    {
                        _menuStates.TryRemove(userId, out _);
                    }
                    
                    if (oldSessions.Any())
                        Console.WriteLine($"[Cleanup] Removed {oldSessions.Count} old sessions");
                }
                catch { }
            }
        }

        private UserMenuState GetOrCreateMenuState(long userId)
        {
            return _menuStates.GetOrAdd(userId, new UserMenuState());
        }

        private void InitializeSantaEvent()
        {
            _santaData.EventStart = new DateTime(DateTime.UtcNow.Year, 12, 1);
            _santaData.EventEnd = new DateTime(DateTime.UtcNow.Year, 12, 31, 23, 59, 59);
            
            LoadSantaData();
            
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(300000);
                    SaveSantaData();
                }
            });
        }

        private void LoadSantaData()
        {
            try
            {
                if (File.Exists(SANTA_DATA_PATH))
                {
                    var json = File.ReadAllText(SANTA_DATA_PATH);
                    _santaData = Newtonsoft.Json.JsonConvert.DeserializeObject<SantaData>(json) ?? new SantaData();
                    
                    foreach (var participant in _santaData.Participants)
                    {
                        _santaParticipants[participant.Key] = participant.Value;
                    }
                    
                    foreach (var gift in _santaData.Gifts)
                    {
                        _santaGifts[gift.Id] = gift;
                    }
                    
                    Console.WriteLine($"[Santa] Загружено {_santaParticipants.Count} участников и {_santaGifts.Count} подарков");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Santa] Ошибка загрузки данных: {ex.Message}");
                _santaData = new SantaData();
            }
        }

        private void SaveSantaData()
        {
            try
            {
                _santaData.Participants = _santaParticipants.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                _santaData.Gifts = _santaGifts.Values.ToList();
                
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(_santaData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(SANTA_DATA_PATH, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Santa] Ошибка сохранения данных: {ex.Message}");
            }
        }

        private bool IsAdmin(long userId) => _adminIds.Contains(userId);

        private bool IsSantaEventActive()
        {
            var now = DateTime.UtcNow;
            return now >= _santaData.EventStart && now <= _santaData.EventEnd;
        }

        private string GetTimeRemaining()
        {
            if (!IsSantaEventActive())
            {
                if (DateTime.UtcNow > _santaData.EventEnd)
                    return "🎅 *Тайный Санта завершился!*";
                else
                    return $"🎅 *Тайный Санта начнётся {_santaData.EventStart:dd.MM.yyyy}*";
            }
            
            var timeLeft = _santaData.EventEnd - DateTime.UtcNow;
            return $"⏰ *До конца:* {timeLeft.Days}д {timeLeft.Hours}ч {timeLeft.Minutes}м";
        }

        private void SaveDailyLimitsSimple()
        {
            try
            {
                string filePath = "./daily_limits_simple.txt";
                var lines = new List<string>();
                
                foreach (var kvp in _dailyLimits)
                {
                    lines.Add($"{kvp.Key}|{kvp.Value.GemsTransferred}|{kvp.Value.LastTransferDate:yyyy-MM-dd}");
                }
                
                File.WriteAllLines(filePath, lines);
                Console.WriteLine($"[TransferGems] Лимиты сохранены в простой файл: {lines.Count} записей");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TransferGems] Ошибка простого сохранения: {ex.Message}");
            }
        }

        private void LoadDailyLimitsSimple()
        {
            try
            {
                string filePath = "./daily_limits_simple.txt";
                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath);
                    Console.WriteLine($"[TransferGems] Загружено {lines.Length} строк из простого файла");
                    
                    foreach (var line in lines)
                    {
                        var parts = line.Split('|');
                        if (parts.Length == 3 && long.TryParse(parts[0], out long userId) && 
                            int.TryParse(parts[1], out int gems) && 
                            DateTime.TryParse(parts[2], out DateTime date))
                        {
                            _dailyLimits[userId] = new DailyLimitsRecord
                            {
                                TelegramUserId = userId,
                                GemsTransferred = gems,
                                LastTransferDate = date
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TransferGems] Ошибка простой загрузки: {ex.Message}");
            }
        }

        private List<ClaimedOfferRecord> LoadClaimedOffers()
        {
            try
            {
                string filePath = "./claimed_offers.json";
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<List<ClaimedOfferRecord>>(json) ?? new List<ClaimedOfferRecord>();
                }
                return new List<ClaimedOfferRecord>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClaimedOffers] Ошибка загрузки: {ex.Message}");
                return new List<ClaimedOfferRecord>();
            }
        }

        private void SaveClaimedOffers(List<ClaimedOfferRecord> claimedOffers)
        {
            try
            {
                string filePath = "./claimed_offers.json";
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(claimedOffers, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClaimedOffers] Ошибка сохранения: {ex.Message}");
            }
        }

        private string GetShopItemName(ShopItem itemType, int skinDataId, int itemDataId)
        {
            switch (itemType)
            {
                case ShopItem.Gems:
                    return "Гемы";
                case ShopItem.Coin:
                    return "Монеты";
                case ShopItem.Skin:
                    return skinDataId > 0 ? GetSkinNameById(skinDataId) : "Скин";
                case ShopItem.PowerPoint:
                    return "Очки силы";
                case ShopItem.GuaranteedHero:
                    return itemDataId > 0 ? GetBrawlerNameById(itemDataId) : "Бравлер";
                case ShopItem.Titul:
                    return "Титул";
                case ShopItem.RarityStarrDrop:
                    return "Звёздная капля";
                case ShopItem.Bling:
                    return "Блинг";
                default:
                    return "Предложение";
            }
        }

        private string GetBrawlerNameById(int brawlerId)
        {
            try
            {
                int adjustedId = brawlerId;
                if (adjustedId < 16000000 && adjustedId > 0)
                {
                    adjustedId += 16000000;
                }
                
                var characterData = DataTables.Get(16)?.GetDataWithId<CharacterData>(adjustedId);
                if (characterData != null)
                {
                    var type = characterData.GetType();
                    
                    var nameProp = type.GetProperty("Name");
                    if (nameProp != null)
                    {
                        string name = (string)nameProp.GetValue(characterData);
                        if (!string.IsNullOrEmpty(name))
                            return name;
                    }
                    
                    var tidNameProp = type.GetProperty("TID_Name");
                    if (tidNameProp != null)
                    {
                        string tidName = (string)tidNameProp.GetValue(characterData);
                        if (!string.IsNullOrEmpty(tidName))
                            return tidName;
                    }
                    
                    var getNameMethod = type.GetMethod("GetName");
                    if (getNameMethod != null)
                    {
                        string name = (string)getNameMethod.Invoke(characterData, null);
                        if (!string.IsNullOrEmpty(name))
                            return name;
                    }
                }
                
                return adjustedId switch
                {
                    16000000 => "Шелли",
                    16000001 => "Нита",
                    16000002 => "Кольт",
                    16000003 => "Булл",
                    16000004 => "Джесси",
                    16000005 => "Брок",
                    16000006 => "Динамик",
                    16000007 => "Бо",
                    16000008 => "Тик",
                    16000009 => "Эль Примо",
                    16000010 => "Барли",
                    16000011 => "Поко",
                    16000012 => "Мортис",
                    16000013 => "Тара",
                    16000014 => "Джин",
                    16000015 => "Кроу",
                    16000016 => "Леон",
                    16000017 => "Сэнди",
                    16000018 => "Спайк",
                    16000019 => "Биби",
                    _ => $"Бравлер #{adjustedId}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Ошибка получения имени бравлера {brawlerId}: {ex.Message}");
                return $"Бравлер #{brawlerId}";
            }
        }

        private string GetSkinNameById(int skinId)
        {
            try
            {
                return $"Скин #{skinId}";
            }
            catch
            {
                return $"Скин #{skinId}";
            }
        }

        private string GenerateOfferHash(string accountTag, string offerTitle, int offerCost, int itemCount)
        {
            string hashString = $"{accountTag}|{offerTitle}|{offerCost}|{itemCount}";
            return Math.Abs(hashString.GetHashCode()).ToString();
        }

        private bool IsOfferAlreadyClaimed(long userId, string accountTag, string offerTitle, int offerCost, int itemCount)
        {
            var claimedOffers = LoadClaimedOffers();
            string offerHash = GenerateOfferHash(accountTag, offerTitle, offerCost, itemCount);
            
            return claimedOffers.Any(o => 
                o.TelegramUserId == userId && 
                o.AccountTag == accountTag && 
                o.OfferHash == offerHash);
        }

        private void MarkOfferAsClaimed(long userId, string accountTag, OfferBundle offer)
        {
            var claimedOffers = LoadClaimedOffers();
            
            var mainItem = offer.Items?.FirstOrDefault();
            if (mainItem == null) return;
            
            string offerHash = GenerateOfferHash(accountTag, offer.Title, offer.Cost, mainItem.Count);
            
            claimedOffers.Add(new ClaimedOfferRecord
            {
                TelegramUserId = userId,
                AccountTag = accountTag,
                OfferTitle = offer.Title,
                OfferCost = offer.Cost,
                ItemCount = mainItem.Count,
                ItemType = mainItem.Type,
                ClaimedAt = DateTime.UtcNow,
                OfferHash = offerHash
            });
            
            SaveClaimedOffers(claimedOffers);
            Console.WriteLine($"[ClaimedOffers] Предложение '{offer.Title}' помечено как забранное для {accountTag}");
        }

        private int GetGemsTransferredToday(long userId)
        {
            if (_dailyLimits.TryGetValue(userId, out var record))
            {
                if (record.LastTransferDate.Date != DateTime.UtcNow.Date)
                {
                    record.GemsTransferred = 0;
                    record.LastTransferDate = DateTime.UtcNow;
                    SaveDailyLimits();
                    return 0;
                }
                
                return record.GemsTransferred;
            }
            
            _dailyLimits[userId] = new DailyLimitsRecord
            {
                TelegramUserId = userId,
                GemsTransferred = 0,
                LastTransferDate = DateTime.UtcNow
            };
            SaveDailyLimits();
            
            return 0;
        }

        private void UpdateDailyGemsLimit(long userId, int amount)
        {
            if (!_dailyLimits.TryGetValue(userId, out var record))
            {
                record = new DailyLimitsRecord
                {
                    TelegramUserId = userId,
                    GemsTransferred = 0,
                    LastTransferDate = DateTime.UtcNow
                };
            }
            
            if (record.LastTransferDate.Date != DateTime.UtcNow.Date)
            {
                record.GemsTransferred = 0;
                record.LastTransferDate = DateTime.UtcNow;
            }
            
            record.GemsTransferred += amount;
            record.LastTransferDate = DateTime.UtcNow;
            
            _dailyLimits[userId] = record;
            SaveDailyLimits();
            
            Console.WriteLine($"[TransferGems] Пользователь {userId} использовал {record.GemsTransferred}/{DAILY_GEMS_LIMIT} гемов сегодня");
        }

        private void ResetDailyLimits()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var usersToReset = _dailyLimits
                    .Where(kvp => kvp.Value.LastTransferDate.Date < today)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var userId in usersToReset)
                {
                    if (_dailyLimits.TryGetValue(userId, out var record))
                    {
                        record.GemsTransferred = 0;
                        record.LastTransferDate = today;
                        Console.WriteLine($"[TransferGems] Сброшен лимит для {userId}");
                    }
                }
                
                if (usersToReset.Count > 0)
                {
                    SaveDailyLimits();
                    Console.WriteLine($"[TransferGems] Сброшены лимиты для {usersToReset.Count} пользователей");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TransferGems] Ошибка сброса лимитов: {ex.Message}");
            }
        }

        private async Task LogTransferToFile(long userId, string senderTag, string receiverTag, int amount, int commission)
        {
            try
            {
                string filePath = "./gems_transfers.json";
                List<GemTransferRecord> transfers = new List<GemTransferRecord>();
                
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    transfers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GemTransferRecord>>(json) 
                        ?? new List<GemTransferRecord>();
                }
                
                int gemsUsedBeforeTransfer = 0;
                if (_dailyLimits.TryGetValue(userId, out var record))
                {
                    gemsUsedBeforeTransfer = record.GemsTransferred;
                }
                
                int gemsUsedBefore = Math.Max(0, gemsUsedBeforeTransfer - amount);
                
                transfers.Add(new GemTransferRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    TelegramUserId = userId,
                    SenderTag = senderTag,
                    ReceiverTag = receiverTag,
                    Amount = amount,
                    Commission = commission,
                    TotalDeducted = amount + commission,
                    DailyUsedBefore = gemsUsedBefore,
                    DailyRemainingBefore = DAILY_GEMS_LIMIT - gemsUsedBefore,
                    TransferTime = DateTime.UtcNow
                });
                
                if (transfers.Count > 1000)
                {
                    transfers = transfers.Skip(transfers.Count - 1000).ToList();
                }
                
                string newJson = Newtonsoft.Json.JsonConvert.SerializeObject(transfers, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, newJson);
                
                Console.WriteLine($"[TransferGems] Запись сохранена: {senderTag} → {receiverTag} ({amount} гемов), " +
                                 $"использовано до перевода: {gemsUsedBefore}/{DAILY_GEMS_LIMIT}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TransferGems] Ошибка логирования: {ex.Message}");
            }
        }

        private const string LIMITS_FILE_PATH = "./daily_limits.json";

        private void LoadDailyLimits()
        {
            try
            {
                string filePath = LIMITS_FILE_PATH;
                Console.WriteLine($"[TransferGems] Загрузка лимитов из: {filePath}");
                
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    Console.WriteLine($"[TransferGems] JSON размер: {json.Length} байт");
                    
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine($"[TransferGems] Файл пустой, инициализируем пустым списком");
                        json = "[]";
                        File.WriteAllText(filePath, json);
                    }
                    
                    var records = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DailyLimitsRecord>>(json) 
                        ?? new List<DailyLimitsRecord>();
                    
                    Console.WriteLine($"[TransferGems] Загружено {records.Count} записей");
                    
                    _dailyLimits.Clear();
                    
                    foreach (var record in records)
                    {
                        _dailyLimits[record.TelegramUserId] = record;
                        Console.WriteLine($"[TransferGems] Загружен лимит для {record.TelegramUserId}: {record.GemsTransferred} гемов, дата: {record.LastTransferDate:dd.MM.yyyy}");
                    }
                    
                    if (records.Count == 0)
                    {
                        Console.WriteLine($"[TransferGems] Нет записей в файле, создаем новую структуру");
                    }
                }
                else
                {
                    Console.WriteLine($"[TransferGems] Файл {filePath} не найден, создаем новый");
                    File.WriteAllText(filePath, "[]");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TransferGems] КРИТИЧЕСКАЯ ошибка загрузки лимитов: {ex.Message}");
                Console.WriteLine($"[TransferGems] StackTrace: {ex.StackTrace}");
                
                try
                {
                    File.WriteAllText(LIMITS_FILE_PATH, "[]");
                    Console.WriteLine($"[TransferGems] Создан новый файл лимитов");
                }
                catch { }
            }
        }

        private void SaveDailyLimits()
        {
            try
            {
                var records = _dailyLimits.Values.ToList();
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(records, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(LIMITS_FILE_PATH, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TransferGems] Ошибка сохранения лимитов: {ex.Message}");
            }
        }

        private async Task StartDailyLimitsAutoSave()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    SaveDailyLimits();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TransferGems] Ошибка автосохранения лимитов: {ex.Message}");
                }
            }
        }

        private async Task StartDailyLimitResetTask()
        {
            while (true)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var nextReset = now.Date.AddDays(1);
                    
                    var timeUntilReset = nextReset - now;
                    
                    await Task.Delay(timeUntilReset);
                    
                    ResetDailyLimits();
                    
                    Console.WriteLine($"[TransferGems] Дневные лимиты сброшены в {DateTime.UtcNow:HH:mm:ss}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TransferGems] Ошибка сброса лимитов: {ex.Message}");
                    await Task.Delay(TimeSpan.FromHours(1));
                }
            }
        }

        private async Task StartPolling()
        {
            int offset = 0;

            while (true)
            {
                try
                {
                    var updates = await _client.GetUpdatesAsync(
                        offset: offset,
                        limit: 10,
                        timeout: 30
                    );

                    foreach (var update in updates)
                    {
                        offset = update.update_id + 1;
                        _ = Task.Run(() => HandleUpdateAsync(update));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TelegramBot] Ошибка опроса: {ex.Message}");
                    await Task.Delay(5000);
                }

                await Task.Delay(1000);
            }
        }

        private async Task HandleUpdateAsync(Update update)
        {
            try
            {
                if (update.callback_query != null)
                {
                    await HandleCallbackQueryAsync(update.callback_query);
                    return;
                }

                if (update.pre_checkout_query != null)
                {
                    await _vipMarket.HandlePreCheckoutAsync(update.pre_checkout_query);
                    return;
                }

                if (update.message == null) return;

                if (update.message.successful_payment != null)
                {
                    await _vipMarket.HandleSuccessfulPaymentAsync(update.message);
                    return;
                }

                if (string.IsNullOrEmpty(update.message.text)) return;

                var message = update.message;
                var chatId = message.chat.id;
                var userId = message.from.id;
                var text = message.text.Trim();

                if (message.chat.type != "private") return;

                var state = GetOrCreateMenuState(userId);
                state.LastActivity = DateTime.UtcNow;
                state.LastMessageId = message.message_id;

                if (_linkStates.TryGetValue(userId, out var linkState))
                {
                    if (DateTime.UtcNow > linkState.Expiry)
                    {
                        _linkStates.TryRemove(userId, out _);
                        await _client.SendMessageAsync(chatId, "ℹ️ Время на ввод кода истекло. Начните заново.", TelegramClient.ParseMode.MarkdownV2);
                        return;
                    }

                    if (linkState.AttemptsCount >= 3)
                    {
                        _linkStates.TryRemove(userId, out _);
                        await _client.SendMessageAsync(chatId, "❌ Слишком много неверных попыток. Начните заново.", TelegramClient.ParseMode.MarkdownV2);
                        return;
                    }

                    if (text == linkState.ConfirmationCode)
                    {
                        await HandleConfirmationCode(chatId, linkState.TargetAccountId, userId);
                        _linkStates.TryRemove(userId, out _);
                        return;
                    }
                    else
                    {
                        linkState.AttemptsCount++;
                        await _client.SendMessageAsync(chatId, $"❌ Неверный код. Попыток осталось: {3 - linkState.AttemptsCount}", TelegramClient.ParseMode.MarkdownV2);
                        return;
                    }
                }

                if (text.StartsWith("/"))
                {
                    await HandleCommandAsync(chatId, text, userId);
                }
                else if (IsAccountId(text))
                {
                    await HandleAccountId(chatId, userId, text);
                }
                else
                {
                    await ShowMainMenu(chatId, userId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Ошибка в HandleUpdateAsync: {ex.Message}");
                try
                {
                    await _client.SendMessageAsync(update.message?.chat.id ?? 0, "Произошла ошибка. Попробуйте позже.", TelegramClient.ParseMode.MarkdownV2);
                }
                catch { }
            }
        }

        private async Task HandleAccountId(long chatId, long userId, string accountIdText)
        {
            string tag = accountIdText.ToUpper();

            if (_linkManager.IsTelegramLinked(userId))
            {
                await _client.SendMessageAsync(chatId, $"❌ Ваш Telegram уже привязан к другому игровому аккаунту.", TelegramClient.ParseMode.MarkdownV2);
                return;
            }

            if (_linkManager.IsAccountLinked(tag))
            {
                await _client.SendMessageAsync(chatId, $"❌ Аккаунт `{tag}` уже привязан к другому Telegram.", TelegramClient.ParseMode.MarkdownV2);
                return;
            }

            long targetAccountId;
            try
            {
                targetAccountId = TagToId(tag);
            }
            catch (ArgumentException ex)
            {
                await _client.SendMessageAsync(chatId, $"❌ Неверный формат тега аккаунта: {ex.Message}", TelegramClient.ParseMode.MarkdownV2);
                return;
            }

            Account account = Accounts.Load(targetAccountId);
            if (account == null)
            {
                await _client.SendMessageAsync(chatId, $"❌ Аккаунт с тегом `{tag}` не найден.", TelegramClient.ParseMode.MarkdownV2);
                return;
            }

            string code = GenerateConfirmationCode();
            var expiry = DateTime.UtcNow.AddMinutes(5);

            _linkStates[userId] = new LinkState
            {
                TargetAccountId = tag,
                ConfirmationCode = code,
                Expiry = expiry,
                TelegramUserId = userId,
                AttemptsCount = 0
            };

            bool sent = await SendConfirmationNotificationToGame(targetAccountId, code);
            if (!sent)
            {
                await _client.SendMessageAsync(chatId, "❌ Не удалось отправить уведомление в игру. Попробуйте позже.", TelegramClient.ParseMode.MarkdownV2);
                return;
            }

            await _client.SendMessageAsync(chatId, $"✅ Отлично! Я отправил код подтверждения на аккаунт `{tag}`.\n\n" +
                                                   $"*Введите код сюда, чтобы завершить привязку.*", TelegramClient.ParseMode.MarkdownV2);
        }

        private async Task HandleConfirmationCode(long chatId, string targetAccountId, long telegramUserId)
        {
            bool success = _linkManager.LinkAccount(telegramUserId, targetAccountId);
            if (success)
            {
                long targetAccountIdLong = TagToId(targetAccountId);
                bool skinSent = await SendSkinRewardNotificationToGame(targetAccountIdLong, 59);

                await _client.SendMessageAsync(chatId, $"✅ Успешно! Ваш Telegram привязан к аккаунту `{targetAccountId}`.\n\n" +
                                                       $"Теперь вы можете восстановить доступ через /recover." +
                                                       (skinSent ? $"\n🎁 Вам также выдан скин *Волшебник барли* за успешную привязку!" : ""), TelegramClient.ParseMode.MarkdownV2);
            }
            else
            {
                await _client.SendMessageAsync(chatId, "❌ Не удалось завершить привязку. Попробуйте позже.", TelegramClient.ParseMode.MarkdownV2);
            }
        }

        

private async Task SendMessage(long chatId, long userId, string text, string parseModeStr = null, object replyMarkup = null)
{
    try
    {
        var state = GetOrCreateMenuState(userId);
        
        // Конвертируем строку в enum
        TelegramClient.ParseMode parseMode;
        if (parseModeStr == "MarkdownV2")
            parseMode = TelegramClient.ParseMode.MarkdownV2;
        else
            parseMode = TelegramClient.ParseMode.None;
        
        string keyboardJson = replyMarkup != null ? System.Text.Json.JsonSerializer.Serialize(replyMarkup) : null;
        
        // ИСПРАВЛЕНИЕ: убрали присвоение переменной
        await _client.SendMessageWithKeyboardAsync(chatId, text, keyboardJson, parseMode);
        
        // Если вам нужно сохранять ID сообщения, но метод не возвращает его,
        // придется либо изменить метод SendMessageWithKeyboardAsync, чтобы он возвращал Message,
        // либо использовать другой подход
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TelegramBot] Ошибка в SendMessage: {ex.Message}");
        
        // Пробуем отправить простое сообщение без клавиатуры
        try
        {
            TelegramClient.ParseMode parseMode;
            if (parseModeStr == "MarkdownV2")
                parseMode = TelegramClient.ParseMode.MarkdownV2;
            else
                parseMode = TelegramClient.ParseMode.None;
                
            await _client.SendMessageAsync(chatId, text, parseMode);
        }
        catch (Exception innerEx)
        {
            Console.WriteLine($"[TelegramBot] Критическая ошибка отправки: {innerEx.Message}");
        }
    }
}

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
        {
            var userId = callbackQuery.from.id;
            var chatId = callbackQuery.message.chat.id;
            var messageId = callbackQuery.message.message_id;
            var callbackData = callbackQuery.data;

            try
            {
                await _client.AnswerCallbackQueryAsync(callbackQuery.id);
                
                var state = GetOrCreateMenuState(userId);
                state.LastActivity = DateTime.UtcNow;
                state.LastMessageId = messageId;

                Console.WriteLine($"[TelegramBot] Callback: {callbackData} от {userId} в {DateTime.UtcNow:HH:mm:ss}");

                // Main menu navigation
                if (callbackData == "menu_main")
                {
                    await ShowMainMenu(chatId, userId);
                }
                else if (callbackData == "menu_accounts")
                {
                    await ShowAccountsMenu(chatId, userId);
                }
                else if (callbackData == "menu_vip")
                {
                    await _vipMarket.ShowMarketAsync(chatId, userId);
                }
                else if (callbackData.StartsWith("vip_buy_"))
                {
                    await _vipMarket.SendInvoiceAsync(chatId, userId, callbackData.Replace("vip_buy_", ""));
                }
                else if (callbackData.StartsWith("unlink_confirm_"))
                {
                    string tag = callbackData.Replace("unlink_confirm_", "");
                    await ShowUnlinkConfirmation(chatId, userId, tag);
                }
                else if (callbackData.StartsWith("unlink_do_"))
                {
                    string tag = callbackData.Replace("unlink_do_", "");
                    await ProcessUnlink(chatId, userId, tag);
                }
                else if (callbackData == "menu_shop")
                {
                    await HandleShopCommand(chatId, userId);
                }
                else if (callbackData == "menu_transfers")
                {
                    await ShowTransferMenu(chatId, userId);
                }
                else if (callbackData == "menu_spin")
                {
                    await HandleDailySpinCommand(chatId, userId);
                }
                else if (callbackData == "menu_santa")
                {
                    await ShowSantaMainMenu(chatId, userId);
                }
                else if (callbackData == "menu_profile")
                {
                    await HandleAccountInfoCommand(chatId, userId, null);
                }
                else if (callbackData == "menu_help")
                {
                    await ShowHelpMenu(chatId, userId);
                }
                else if (callbackData == "menu_support")
                {
                    await ShowSupportMenu(chatId, userId);
                }
                else if (callbackData == "support_faq")
                {
                    await ShowFAQ(chatId, userId);
                }
                
                // Account management
                else if (callbackData == "account_link")
                {
                    await SendMessage(chatId, userId, 
                        "🔗 *Привязка аккаунта*\n\nОтправьте свой игровой ТЭГ (например: `#2PP`)", 
                        "MarkdownV2");
                }
                else if (callbackData == "account_list")
                {
                    await ShowAccountsMenu(chatId, userId);
                }
                else if (callbackData.StartsWith("account_view_"))
                {
                    string tag = callbackData.Replace("account_view_", "");
                    await HandleAccountInfoCommand(chatId, userId, tag);
                }
                else if (callbackData.StartsWith("account_quick_"))
                {
                    string tag = callbackData.Replace("account_quick_", "");
                    await HandleQuickLoginForTag(chatId, userId, tag);
                }
                else if (callbackData.StartsWith("account_reset_"))
                {
                    string tag = callbackData.Replace("account_reset_", "");
                    await HandleResetTokenForTag(chatId, userId, tag);
                }
                else if (callbackData.StartsWith("account_unlink_"))
                {
                    string tag = callbackData.Replace("account_unlink_", "");
                    await HandleUnlinkForTag(chatId, userId, tag);
                }
                
                // Shop
                else if (callbackData == "shop_back_to_list")
                {
                    await HandleShopCommand(chatId, userId);
                }
                else if (callbackData == "shop_refresh_list")
                {
                    await HandleShopCommand(chatId, userId);
                }
                else if (callbackData == "shop_debug")
                {
                    await HandleShopDebugCommand(chatId, userId);
                }
                else if (callbackData == "my_claims_callback")
                {
                    await HandleClaimedOffersCommand(chatId, userId);
                }
                else if (callbackData.StartsWith("shop_view_"))
                {
                    string indexStr = callbackData.Replace("shop_view_", "");
                    if (int.TryParse(indexStr, out int offerIndex))
                    {
                        await HandleShopViewCallback(chatId, userId, offerIndex);
                    }
                }
                else if (callbackData.StartsWith("shop_claim_"))
                {
                    string indexStr = callbackData.Replace("shop_claim_", "");
                    if (int.TryParse(indexStr, out int offerIndex))
                    {
                        await ProcessFreeOfferClaim(chatId, userId, offerIndex);
                    }
                }
                else if (callbackData == "shop_fix999_callback")
                {
                    await HandleQuickShopFixCommand(chatId, userId);
                }
                
                // Spin wheel
                else if (callbackData.StartsWith("spin_wheel_"))
                {
                    if (_activeSpinRequests.TryGetValue(userId, out bool isProcessing) && isProcessing)
                    {
                        await SendMessage(chatId, userId, 
                            "⏳ Ваш запрос уже обрабатывается! Пожалуйста, подождите...",
                            "MarkdownV2");
                        return;
                    }

                    if (!_linkManager.TryGetAccountId(userId, out string accountTag))
                    {
                        await SendMessage(chatId, userId, 
                            "❌ Сначала привяжите аккаунт, чтобы использовать колесо фортуны!",
                            "MarkdownV2");
                        return;
                    }

                    if (_lastSpinTimes.TryGetValue(userId, out DateTime lastSpin))
                    {
                        if (lastSpin.Date == DateTime.UtcNow.Date)
                        {
                            var nextSpinTime = lastSpin.AddDays(1).Date;
                            var timeUntilNextSpin = nextSpinTime - DateTime.UtcNow;
                            
                            if ((DateTime.UtcNow - lastSpin).TotalSeconds < 10)
                            {
                                await SendMessage(chatId, userId,
                                    "⏳ Вы уже крутили колесо! Пожалуйста, подождите немного...",
                                    "MarkdownV2");
                                return;
                            }
                            
                            await SendMessage(chatId, userId,
                                "⏳ Вы уже крутили колесо сегодня!\n\n" +
                                $"🔥 Ваша серия: {_dailyStreaks.GetValueOrDefault(userId, 0)} дней\n" +
                                $"⏰ Следующий спин будет доступен завтра в 00:00 UTC",
                                "MarkdownV2");
                            return;
                        }
                    }

                    try
                    {
                        _activeSpinRequests[userId] = true;
                        await ProcessWheelSpin(chatId, userId);
                    }
                    finally
                    {
                        _activeSpinRequests.TryRemove(userId, out _);
                    }
                }
                
                // Transfers
                else if (callbackData == "transfer_new")
                {
                    await HandleTransferGemsCommand(chatId, userId, "");
                }
                else if (callbackData == "transfer_history_callback")
                {
                    await HandleTransferHistoryCommand(chatId, userId);
                }
                else if (callbackData == "show_limits")
                {
                    await ShowTransferLimits(chatId, userId);
                }
                else if (callbackData == "check_balance")
                {
                    await HandleAccountInfoCommand(chatId, userId, null);
                }
                else if (callbackData == "my_accounts_list")
                {
                    await HandleMyAccountsCommand(chatId, userId);
                }
                else if (callbackData == "transfer_again")
                {
                    await HandleTransferGemsCommand(chatId, userId, "");
                }
                
                // Santa
                else if (callbackData == "santa_join")
                {
                    await HandleSantaJoinCommand(chatId, userId);
                }
                else if (callbackData == "santa_list")
                {
                    await HandleSantaListCommand(chatId, userId);
                }
                else if (callbackData == "santa_mygifts")
                {
                    await HandleSantaMyGiftsCommand(chatId, userId);
                }
                else if (callbackData == "santa_top")
                {
                    await HandleSantaTopCommand(chatId, userId);
                }
                else if (callbackData == "santa_wish")
                {
                    await SendMessage(chatId, userId,
                        "✨ *ЖЕЛАНИЕ*\n\n" +
                        "Напишите ваше пожелание:\n" +
                        "/wish Текст желания",
                        "MarkdownV2");
                }
                else if (callbackData == "santa_balance")
                {
                    await HandleSantaBalanceCommand(chatId, userId);
                }
                else if (callbackData == "santa_info")
                {
                    await HandleSantaInfoCommand(chatId, userId);
                }
                else if (callbackData == "santa_back")
                {
                    await ShowSantaMainMenu(chatId, userId);
                }
                
                // Other
                else if (callbackData == "online_check")
                {
                    await HandleOnlineCommand(chatId, userId);
                }
                else
                {
                    Console.WriteLine($"[TelegramBot] Неизвестный callback: {callbackData}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Ошибка в HandleCallbackQueryAsync: {ex.Message}");
                Console.WriteLine($"[TelegramBot] StackTrace: {ex.StackTrace}");
                
                _activeSpinRequests.TryRemove(userId, out _);
                
                try
                {
                    await SendMessage(chatId, userId,
                        "❌ Произошла ошибка при обработке запроса. Попробуйте позже.",
                        "MarkdownV2");
                }
                catch { }
            }
        }

        private async Task ShowUnlinkConfirmation(long chatId, long userId, string tag)
{
    var keyboard = new
    {
        inline_keyboard = new object[]
        {
            new object[]
            {
                new { text = "✅ Да, отвязать", callback_data = $"unlink_do_{tag}" },
                new { text = "❌ Нет", callback_data = "menu_accounts" }
            }
        }
    };

    await SendMessage(chatId, userId,
        $"⚠️ *Вы уверены, что хотите отвязать аккаунт* `{tag}`?\n\n" +
        "После отвязки вы не сможете управлять этим аккаунтом через бота.\n\n",
      "MarkdownV2", keyboard);
}

        private async Task ProcessUnlink(long chatId, long userId, string tag)
{
    try
    {
        bool isOwned = _linkManager.GetAllLinkedAccounts(userId).Any(t => 
            t.Equals(tag, StringComparison.OrdinalIgnoreCase));
        
        if (!isOwned)
        {
            await SendMessage(chatId, userId, 
                $"❌ Аккаунт `{tag}` не найден или уже отвязан.", 
                "MarkdownV2");
            return;
        }

        if (_linkManager.UnlinkAccount(userId, tag))
        {
            // Сбрасываем токен в игре
            try
            {
                long accountId = TagToId(tag);
                var account = Accounts.Load(accountId);
                if (account != null)
                {
                    string newToken = GeneratePassToken();
                    account.PassToken = newToken;
                    if (account.Avatar != null)
                        account.Avatar.PassToken = newToken;
                    Accounts.Save(account);

                    if (Sessions.IsSessionActive(accountId))
                    {
                        var session = Sessions.GetSession(accountId);
                        session?.GameListener?.SendTCPMessage(new AuthenticationFailedMessage()
                        {
                            ErrorCode = 8,
                            Message = "Аккаунт был отвязан от Telegram. Требуется повторная привязка."
                        });
                    }
                    
                    string newLoadCommand = $"/load {accountId} {newToken}";

                    var keyboard = new
                    {
                        inline_keyboard = new object[]
                        {
                            new object[] { new { text = "📋 Мои аккаунты", callback_data = "menu_accounts" } },
                            new object[] { new { text = "← Главное меню", callback_data = "menu_main" } }
                        }
                    };

                    await SendMessage(chatId, userId,
                        $"✅ *Аккаунт `{tag}` успешно отвязан!*\n\n" +
                        $"🔐 *Новая команда для входа:*\n" +
                        $"`{newLoadCommand}`\n\n" +
                        $"❗ Сохраните эту команду в безопасном месте!",
                        "MarkdownV2", keyboard);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Unlink] Ошибка при сбросе токена: {ex.Message}");
                await SendMessage(chatId, userId,
                    $"✅ *Аккаунт `{tag}` отвязан, но возникла ошибка при сбросе токена.*\n\n" +
                    "Обратитесь к администратору @ShuzaBrawl",
                    "MarkdownV2");
            }
        }
        else
        {
            await SendMessage(chatId, userId,
                "❌ *Ошибка при отвязке аккаунта.*\n\nПопробуйте позже.",
                "MarkdownV2");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Unlink Error] {ex.Message}");
        await SendMessage(chatId, userId,
            "❌ *Произошла ошибка*\n\nПожалуйста, сообщите администратору.",
            "MarkdownV2");
    }
}

        private async Task ShowSupportMenu(long chatId, long userId)
        {
            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[] { new { text = "👤 Администратор", url = "https://t.me/ShuzaBrawl" } },
                    new object[] { new { text = "📢 Канал", url = "https://t.me/shuzabrawl" } },
                    new object[] { new { text = "🌐 Сайт", url = "https://shuzabrawl.mooo.com/" } },
                    new object[] { new { text = "❓ Частые вопросы", callback_data = "support_faq" } },
                    new object[] { new { text = "← Назад", callback_data = "menu_main" } }
                }
            };

            string message = 
@"📞 *ПОДДЕРЖКА И СВЯЗЬ*
▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬

👑 *По вопросам писать:*
• @ShuzaBrawl

🌐 *Наш сайт:*
• https://shuzabrawl.mooo.com/

📢 *Telegram канал:*
• @shuzabrawl

🎥 *Tutorial:*
• https://youtube.com/shorts/Pj9TEaGSF4o

💬 *Если у вас есть вопросы, предложения или проблемы:*
• Напишите администратору в личные сообщения
• Подпишитесь на канал для новостей
• Посетите наш сайт

⏰ *Время ответа:* обычно в течение нескольких часов";

            await SendMessage(chatId, userId, message, "MarkdownV2", keyboard);
        }

        private async Task ShowFAQ(long chatId, long userId)
        {
            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[] { new { text = "📞 Связаться", url = "https://t.me/ShuzaBrawl" } },
                    new object[] { new { text = "← Назад", callback_data = "menu_support" } }
                }
            };

            string message = 
@"❓ *ЧАСТО ЗАДАВАЕМЫЕ ВОПРОСЫ*
▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬

❔ *Как привязать аккаунт?*
→ Отправьте свой ТЭГ (#2PP) боту

❔ *Не приходит код?*
→ Проверьте уведомления в игре
→ Попробуйте через 5 минут

❔ *Как отвязать аккаунт?*
→ Используйте /unlink #ТЭГ

❔ *Потерял доступ к аккаунту?*
→ Используйте /recover

❔ *Как перевести гемы?*
→ /transfer #ТЭГ КОЛИЧЕСТВО

❔ *Лимит переводов?*
→ 4000 гемов в день

❔ *Как крутить колесо?*
→ /spin - 1 раз в день

❔ *Что такое Тайный Санта?*
→ Сезонное событие в декабре

❔ *Бот не работает?*
→ Напишите @ShuzaBrawl";

            await SendMessage(chatId, userId, message, "MarkdownV2", keyboard);
        }

        private async Task ShowMainMenu(long chatId, long userId)
{
    bool hasAccount = _linkManager.TryGetAccountId(userId, out string accountTag);
    
    var keyboard = new
    {
        inline_keyboard = new object[]
        {
            new object[]
            {
                new { text = "🎮 Аккаунты", callback_data = "menu_accounts" },
                new { text = "⭐ VIP-маркет", callback_data = "menu_vip" }
    /////            new { text = "🛒 Магазин", callback_data = "menu_shop" }
            },
            new object[]
            {
                new { text = "💰 Переводы", callback_data = "menu_transfers" },
                new { text = "🎰 Колесо", callback_data = "menu_spin" }
            },
            new object[]
            {
          /////      new { text = "🎅 Тайный Санта", callback_data = "menu_santa" },
                new { text = "👤 Профиль", callback_data = "menu_profile" }
            },
            new object[]
            {
                new { text = "📊 Онлайн", callback_data = "online_check" },
                new { text = "📞 Поддержка", callback_data = "menu_support" }
            }
        }
    };

    string message = 
@"👑 *sh*
 *ShuzaBrawl*
▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬

" + (hasAccount ? $"✅ *Аккаунт привязан:* `{accountTag}`" : "❌ *Аккаунт не привязан*\nОтправьте свой ТЭГ (например: #2PP)") + @"

📱 *Выберите раздел:*";

    await SendMessage(chatId, userId, message, "MarkdownV2", keyboard);
}

        private async Task ShowAccountsMenu(long chatId, long userId)
{
    var linkedAccounts = _linkManager.GetAllLinkedAccounts(userId).ToList();
    bool hasAccount = _linkManager.TryGetAccountId(userId, out string _);
    
    var buttons = new List<object[]>();
    
    foreach (var acc in linkedAccounts.Take(5))
    {
        buttons.Add(new object[]
        {
            new { text = $"📌 {acc}", callback_data = $"account_view_{acc}" }
        });
    }
    
    // Показываем кнопку привязки только если нет ни одного привязанного аккаунта
    if (!hasAccount || linkedAccounts.Count == 0)
    {
        buttons.Add(new object[]
        {
            new { text = "➕ Привязать новый", callback_data = "account_link" }
        });
    }
    
    // Всегда добавляем кнопку "Назад" в главное меню
    buttons.Add(new object[]
    {
        new { text = "← Назад в главное меню", callback_data = "menu_main" }
    });

    var keyboard = new { inline_keyboard = buttons.ToArray() };
    
    string message;
    if (linkedAccounts.Any())
    {
        message = $"📋 *Ваши аккаунты:*\n\n{string.Join("\n", linkedAccounts.Select(t => $"• `{t}`"))}\n\nВсего: {linkedAccounts.Count}\n\nВыберите действие:";
    }
    else
    {
        message = "📭 *У вас нет привязанных аккаунтов*\n\nОтправьте свой ТЭГ или нажмите кнопку ниже:";
    }

    await SendMessage(chatId, userId, message, "MarkdownV2", keyboard);
}

        private async Task ShowTransferMenu(long chatId, long userId)
        {
            if (!_linkManager.TryGetAccountId(userId, out string accountTag))
            {
                await SendMessage(chatId, userId, 
                    "❌ *Сначала привяжите аккаунт!*\n\nОтправьте свой ТЭГ (например: #2PP)",
                    "MarkdownV2");
                return;
            }

            int gemsUsedToday = GetGemsTransferredToday(userId);
            int gemsRemaining = DAILY_GEMS_LIMIT - gemsUsedToday;
            
            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[]
                    {
                        new { text = "📤 Новый перевод", callback_data = "transfer_new" }
                    },
                    new object[]
                    {
                        new { text = "📜 История", callback_data = "transfer_history_callback" },
                        new { text = "📊 Лимиты", callback_data = "show_limits" }
                    },
                    new object[]
                    {
                        new { text = "← Назад", callback_data = "menu_main" }
                    }
                }
            };

            string message = 
$@"💰 *ПЕРЕВОД ГЕМОВ*
▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬

📊 *Лимиты сегодня:*
• Использовано: {gemsUsedToday}/{DAILY_GEMS_LIMIT} гемов
• Осталось: {gemsRemaining} гемов
• Сброс: 00:00 UTC

📋 *Правила:*
• Минимум: 10 гемов
• Максимум: 1000 за раз
• Комиссия: 5% (мин. 1 гем)
• Переводить можно любому игроку";

            await SendMessage(chatId, userId, message, "MarkdownV2", keyboard);
        }

        private async Task ShowSantaMainMenu(long chatId, long userId)
        {
            bool isActive = IsSantaEventActive();
            bool isParticipant = _santaParticipants.ContainsKey(userId);
            
            var buttons = new List<object[]>();
            
            if (isActive)
            {
                if (!isParticipant)
                {
                    buttons.Add(new object[] { new { text = "🎅 Участвовать", callback_data = "santa_join" } });
                }
                else
                {
                    buttons.Add(new object[]
                    {
                        new { text = "🎁 Подарить", callback_data = "transfer_new" },
                        new { text = "📋 Список", callback_data = "santa_list" }
                    });
                    buttons.Add(new object[]
                    {
                        new { text = "📦 Мои подарки", callback_data = "santa_mygifts" },
                        new { text = "🏆 Топ", callback_data = "santa_top" }
                    });
                    buttons.Add(new object[]
                    {
                        new { text = "✨ Желание", callback_data = "santa_wish" },
                        new { text = "💰 Баланс", callback_data = "santa_balance" }
                    });
                }
            }
            
            buttons.Add(new object[]
            {
                new { text = "📊 Инфо", callback_data = "santa_info" },
                new { text = "← Назад", callback_data = "menu_main" }
            });

            var keyboard = new { inline_keyboard = buttons.ToArray() };

            string message = 
$@"🎅 *ТАЙНЫЙ САНТА*
▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬

{(isActive ? "🎄 *Событие активно!*" : "⏳ *Событие временно неактивно*")}

{GetTimeRemaining()}
👥 *Участников:* {_santaParticipants.Count}";

            await SendMessage(chatId, userId, message, "MarkdownV2", keyboard);
        }

        private async Task ShowHelpMenu(long chatId, long userId)
        {
            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[]
                    {
                        new { text = "← Назад", callback_data = "menu_main" }
                    }
                }
            };

            string message = 
@"❓ *ПОМОЩЬ*
▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬

🎮 *Аккаунты*
• Отправьте ТЭГ (#2PP) для привязки
• /myaccounts - список аккаунтов
• /unlink #ТЭГ - отвязать

💰 *Переводы*
• /transfer #ТЭГ 50 - перевод
• Лимит 4000 гемов/день
• Комиссия 5%

🎰 *Колесо*
• /spin - крутить колесо
• 1 раз в день
• Бонус за серию

🛒 *Магазин*
• /shop - открыть магазин
• Бесплатные предложения

🎅 *Тайный Санта*
• /santa_join - участвовать
• Дарите подарки другим

👑 *Поддержка*
• @ShuzaBrawl

🎥 *Tutorial:*
https://youtube.com/shorts/Pj9TEaGSF4o";

            await SendMessage(chatId, userId, message, "MarkdownV2", keyboard);
        }

        private async Task HandleCommandAsync(long chatId, string commandText, long userId)
        {
            var parts = commandText.Split(' ', 2);
            var command = parts[0].ToLower();

            switch (command)
            {
                case "/vip":
                case "/market":
                    await _vipMarket.ShowMarketAsync(chatId, userId);
                    break;
                case "/paysupport":
                    await _vipMarket.ShowPaySupportAsync(chatId);
                    break;
                case "/start":
                    await ShowMainMenu(chatId, userId);
                    break;
                case "/recover":
                    await HandleRecoverCommand(chatId, userId);
                    break;
                case "/reset_token":
                    await HandleResetTokenCommand(chatId, userId, parts.Length > 1 ? parts[1] : null);
                    break;
                case "/myaccounts":
                    await HandleMyAccountsCommand(chatId, userId);
                    break;
                case "/unlink":
                    await HandleUnlinkCommand(chatId, userId, parts.Length > 1 ? parts[1] : null);
                    break;
                case "/support":
                case "/помощь":
                    await ShowSupportMenu(chatId, userId);
                    break;
                case "/faq":
                case "/вопросы":
                    await ShowFAQ(chatId, userId);
                    break;
                case "/account_info":
                    await HandleAccountInfoCommand(chatId, userId, parts.Length > 1 ? parts[1] : null);
                    break;
                case "/quick_login":
                    await HandleQuickLoginCommand(chatId, userId);
                    break;
                case "/notifications":
                    await HandleNotificationsCommand(chatId, userId, parts.Length > 1 ? parts[1] : null);
                    break;
                case "/unlink_all":
                    await HandleUnlinkAllCommand(chatId, userId);
                    break;
                case "/spin":
                case "/колесо":
                case "/фортуна":
                    await HandleDailySpinCommand(chatId, userId);
                    break;
                case "/transfer_gems":
                case "/передать":
                case "/transfer":
                    await HandleTransferGemsCommand(chatId, userId, parts.Length > 1 ? parts[1] : null);
                    break;
                case "/лимиты":
                case "/limits":
                case "/transfer_limits":
                    await ShowTransferLimits(chatId, userId);
                    break;
                case "/история_переводов":
                case "/transfer_history":
                    await HandleTransferHistoryCommand(chatId, userId);
                    break;
                case "/santa_join":
                    await HandleSantaJoinCommand(chatId, userId);
                    break;
                case "/santa_list":
                    await HandleSantaListCommand(chatId, userId);
                    break;
                case "/santa_gift":
                    await HandleSantaGiftCommand(chatId, userId, parts.Length > 1 ? parts[1].Split(' ') : Array.Empty<string>());
                    break;
                case "/santa_mygifts":
                    await HandleSantaMyGiftsCommand(chatId, userId);
                    break;
                case "/santa_top":
                    await HandleSantaTopCommand(chatId, userId);
                    break;
                case "/santa_info":
                    await HandleSantaInfoCommand(chatId, userId);
                    break;
                case "/santa_wish":
                    await HandleSantaWishCommand(chatId, userId, parts.Length > 1 ? parts[1] : null);
                    break;
                case "/santa_balance":
                    await HandleSantaBalanceCommand(chatId, userId);
                    break;
                case "/santa_stats":
                    await HandleSantaStatsCommand(chatId, userId);
                    break;
                case "/santa_admin":
                    await HandleSantaAdminCommand(chatId, userId, parts.Length > 1 ? parts[1] : null);
                    break;
                case "/wish":
                    if (_santaParticipants.TryGetValue(userId, out var me))
                    {
                        var wish = parts.Length > 1 ? parts[1] : null;
                        if (!string.IsNullOrEmpty(wish))
                        {
                            me.Wish = wish;
                            SaveSantaData();
                            await SendMessage(chatId, userId,
                                $"✨ *Желание сохранено:*\n\n{wish}",
                                "MarkdownV2");
                        }
                    }
                    break;
                case "/online":
                    await HandleOnlineCommand(chatId, userId);
                    break;
                case "/shop_view":
                    string argument = parts.Length > 1 ? parts[1] : null;
                    await HandleShopViewCommand(chatId, userId, argument);
                    break;
                case "/resettg782978256":
                    await HandleResetTelegramCommand(chatId, userId, parts.Length > 1 ? parts[1] : null);
                    break;
                case "/shop":
                    await HandleShopCommand(chatId, userId);
                    break;
                case "/my_claims":
                    await HandleClaimedOffersCommand(chatId, userId);
                    break;
                case "/skin_claims":
                    await HandleSkinClaimsCommand(chatId, userId);
                    break;
                case "/skin_mark":
                    await HandleSkinMarkCommand(chatId, userId, parts.Length > 1 ? parts[1] : null);
                    break;
                case "/shop_refresh":
                    await HandleShopRefreshCommand(chatId, userId);
                    break;
                case "/shop_fix9997272":
                    await HandleQuickShopFixCommand(chatId, userId);
                    break;
                case "/sync_offers":
                    await SyncOffersFromGame(chatId, userId);
                    break;
                case "/shop_debug":
                    await HandleShopDebugCommand(chatId, userId);
                    break;
                default:
                    await ShowMainMenu(chatId, userId);
                    break;
            }
        }

        private async Task HandleQuickLoginForTag(long chatId, long userId, string tag)
        {
            if (!_linkManager.IsAccountOwnedByUser(userId, tag))
            {
                await SendMessage(chatId, userId, "❌ Этот аккаунт не принадлежит вам", "MarkdownV2");
                return;
            }

            long accountId = TagToId(tag);
            Account account = Accounts.Load(accountId);
            
            if (account != null)
            {
                string loadCommand = $"/load {accountId} {account.PassToken}";
                
                await SendMessage(chatId, userId,
                    $"🚀 *Быстрый вход в аккаунт* `{tag}`\n\n" +
                    $"💬 Команда для входа:\n" +
                    $"`{loadCommand}`\n\n" +
                    $"❗ Создайте команду в игре и введите эту команду в чате.",
                    "MarkdownV2");
            }
        }

        private async Task HandleResetTokenForTag(long chatId, long userId, string tag)
        {
            if (!_linkManager.IsAccountOwnedByUser(userId, tag))
            {
                await SendMessage(chatId, userId, "❌ Этот аккаунт не принадлежит вам", "MarkdownV2");
                return;
            }

            long accountId = TagToId(tag);
            Account account = Accounts.Load(accountId);
            
            if (account == null)
            {
                await SendMessage(chatId, userId, $"❌ Аккаунт {tag} не найден", "MarkdownV2");
                return;
            }

            string newPassToken = GeneratePassToken();
            account.PassToken = newPassToken;
            SetAvatarPassToken(account, newPassToken);
            Accounts.Save(account);

            if (Sessions.IsSessionActive(accountId))
            {
                var session = Sessions.GetSession(accountId);
                if (session?.GameListener != null)
                {
                    session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                    {
                        ErrorCode = 10,
                        Message = "Токен аккаунта был сброшен. Вам нужно восстановить доступ."
                    });
                }
            }

            string newLoadCommand = $"/load {accountId} {newPassToken}";

            await SendMessage(chatId, userId, 
                $"✅ Токен аккаунта `{tag}` успешно сброшен!\n\n" +
                $"Новая команда для входа:\n" +
                $"`{newLoadCommand}`",
                "MarkdownV2");
        }

        private async Task HandleUnlinkForTag(long chatId, long userId, string tag)
        {
            if (!_linkManager.IsAccountOwnedByUser(userId, tag))
            {
                await SendMessage(chatId, userId, "❌ Этот аккаунт не принадлежит вам", "MarkdownV2");
                return;
            }

            if (_linkManager.UnlinkAccount(userId, tag))
            {
                await SendMessage(chatId, userId, 
                    $"✅ Аккаунт `{tag}` успешно отвязан от вашего Telegram.",
                    "MarkdownV2");
            }
        }

        private async Task HandleShopDebugCommand(long chatId, long userId)
        {
            if (!_linkManager.TryGetAccountId(userId, out string accountTag))
            {
                await SendMessage(chatId, userId, 
                    "❌ *Сначала привяжите аккаунт!*", 
                    "MarkdownV2");
                return;
            }

            long accountId = TagToId(accountTag);
            var account = Accounts.Load(accountId);
            
            var debugInfo = new StringBuilder();
            debugInfo.AppendLine("🔍 *ДЕБАГ МАГАЗИНА*");
            debugInfo.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            debugInfo.AppendLine("");
            
            if (account == null)
            {
                debugInfo.AppendLine("❌ Аккаунт не найден в базе данных");
            }
            else if (account.Home == null)
            {
                debugInfo.AppendLine("❌ ClientHome не инициализирован");
            }
            else if (account.Home.OfferBundles == null)
            {
                debugInfo.AppendLine("❌ OfferBundles = null (список предложений пуст)");
            }
            else
            {
                debugInfo.AppendLine($"✅ *Аккаунт:* `{accountTag}`");
                debugInfo.AppendLine($"✅ *Home:* Загружен");
                debugInfo.AppendLine($"✅ *Всего предложений:* {account.Home.OfferBundles.Count}");
                debugInfo.AppendLine("");
                
                int activeCount = 0;
                int freeCount = 0;
                
                foreach (var offer in account.Home.OfferBundles.Take(15))
                {
                    bool isPurchased = offer.Purchased;
                    bool isActive = offer.EndTime > DateTime.UtcNow;
                    bool isFree = offer.Cost == 0;
                    string status = isPurchased ? "🛒 Куплен" : (isActive ? "✅ Активен" : "⏰ Истек");
                    
                    debugInfo.AppendLine($"{status} *{offer.Title}*");
                    debugInfo.AppendLine($"   Цена: {(isFree ? "БЕСПЛАТНО" : $"{offer.Cost}")}");
                    debugInfo.AppendLine($"   Куплен: {isPurchased}");
                    debugInfo.AppendLine($"   Действует до: {offer.EndTime:dd.MM.yyyy HH:mm}");
                    debugInfo.AppendLine("");
                    
                    if (isActive && !isPurchased) activeCount++;
                    if (isFree && !isPurchased) freeCount++;
                }
                
                debugInfo.AppendLine($"📊 *Статистика:*");
                debugInfo.AppendLine($"   • Активных: {activeCount}");
                debugInfo.AppendLine($"   • Бесплатных: {freeCount}");
                debugInfo.AppendLine($"   • Всего: {account.Home.OfferBundles.Count}");
            }

            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[]
                    {
                        new { text = "🛒 В магазин", callback_data = "shop_back_to_list" },
                        new { text = "← Назад", callback_data = "menu_main" }
                    }
                }
            };

            await SendMessage(chatId, userId, debugInfo.ToString(), "MarkdownV2", keyboard);
        }

        private async Task HandleShopRefreshCommand(long chatId, long userId)
        {
            if (!_linkManager.TryGetAccountId(userId, out string accountTag))
            {
                await SendMessage(chatId, userId, 
                    "❌ *Сначала привяжите аккаунт!*\n\nОтправьте свой игровой ТЭГ (например: `#2PP`)",
                    "MarkdownV2");
                return;
            }

            long accountId = TagToId(accountTag);
            var account = Accounts.Load(accountId);
            
            if (account?.Home == null)
            {
                await SendMessage(chatId, userId, "❌ *Ошибка загрузки аккаунта!*", "MarkdownV2");
                return;
            }

            try
            {
                bool refreshSuccess = await RefreshShopOffers(account);
                
                if (refreshSuccess)
                {
                    await SendMessage(chatId, userId,
                        "🔄 *Магазин обновлён!*\n\n" +
                        "Используйте команду `/shop` для просмотра новых акций.",
                        "MarkdownV2");
                }
                else
                {
                    await SendMessage(chatId, userId,
                        "⚠️ *Магазин уже содержит актуальные акции!*\n\n" +
                        "Используйте команду `/shop` для просмотра.",
                        "MarkdownV2");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramShop] Ошибка обновления магазина: {ex.Message}");
                await SendMessage(chatId, userId,
                    "❌ *Ошибка при обновлении магазина!*\n\n" +
                    "Попробуйте позже.",
                    "MarkdownV2");
            }
        }

        private async Task<bool> RefreshShopOffers(Account account)
        {
            try
            {
                if (account.Home.OfferBundles == null)
                    account.Home.OfferBundles = new List<OfferBundle>();
                
                await Task.Run(() => Accounts.Save(account));
                
                Console.WriteLine($"[TelegramShop] Магазин обновлён для аккаунта {account.AccountId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramShop] Ошибка обновления акций: {ex.Message}");
                return false;
            }
        }

        private async Task HandleShopCommand(long chatId, long userId)
        {
            try
            {
                if (!_linkManager.TryGetAccountId(userId, out string accountTag))
                {
                    await SendMessage(chatId, userId, 
                        "❌ *Сначала привяжите аккаунт!*\n\nОтправьте свой игровой ТЭГ (например: `#2PP`)",
                        "MarkdownV2");
                    return;
                }

                long accountId = TagToId(accountTag);
                var account = Accounts.Load(accountId);
                
                if (account?.Home?.OfferBundles == null)
                {
                    await SendMessage(chatId, userId, 
                        "🛒 *МАГАЗИН ПУСТ!*\n\n",
                        "MarkdownV2");
                    return;
                }

                var allOffers = account.Home.OfferBundles;
                var claimedOffers = LoadClaimedOffers();
                
                Console.WriteLine($"[Shop] Загружено {allOffers.Count} предложений для {accountTag}");

                var activeOffers = new List<OfferBundle>();
                
                foreach (var offer in allOffers)
                {
                    if (offer.Purchased) continue;
                    if (offer.EndTime <= DateTime.UtcNow) continue;
                    
                    var mainItem = offer.Items?.FirstOrDefault();
                    if (mainItem != null)
                    {
                        string offerHash = GenerateOfferHash(accountTag, offer.Title, offer.Cost, mainItem.Count);
                        bool alreadyClaimed = claimedOffers.Any(c => 
                            c.TelegramUserId == userId && 
                            c.AccountTag == accountTag && 
                            c.OfferHash == offerHash);
                        
                        if (alreadyClaimed) continue;
                    }
                    
                    activeOffers.Add(offer);
                }

                activeOffers = activeOffers
                    .OrderByDescending(o => o.Cost == 0)
                    .ThenBy(o => o.Cost)
                    .Take(20)
                    .ToList();

                if (!activeOffers.Any())
                {
                    int userClaimsCount = claimedOffers.Count(c => 
                        c.TelegramUserId == userId && c.AccountTag == accountTag);
                    
                    var message = new StringBuilder();
                    message.AppendLine("🎉 *ВСЕ ПРЕДЛОЖЕНИЯ ЗАБРАНЫ!*");
                    message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
                    message.AppendLine("");
                    message.AppendLine($"Вы успешно забрали *{userClaimsCount}* предложений!");
                    message.AppendLine("");
                    message.AppendLine("🔄 *Что можно сделать:*");
                    message.AppendLine("• Подождать новые предложения");
                    message.AppendLine("• Использовать `/my_claims` чтобы посмотреть историю");
                    
                    var emptyKeyboard = new
                    {
                        inline_keyboard = new object[]
                        {
                            new object[] { new { text = "📋 Мои покупки", callback_data = "my_claims_callback" } },
                            new object[] { new { text = "← Назад", callback_data = "menu_main" } }
                        }
                    };
                    
                    await SendMessage(chatId, userId, message.ToString(), "MarkdownV2", emptyKeyboard);
                    return;
                }

                var messageText = new StringBuilder();
                messageText.AppendLine("🛒 *МАГАЗИН SHUZABRAWL*");
                messageText.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
                messageText.AppendLine("");
                messageText.AppendLine($"*Доступно:* {activeOffers.Count} предложений");
                
                int userClaimsCount2 = claimedOffers.Count(c => 
                    c.TelegramUserId == userId && c.AccountTag == accountTag);
                if (userClaimsCount2 > 0)
                {
                    messageText.AppendLine($"*Уже забрано:* {userClaimsCount2} предложений");
                }
                
                messageText.AppendLine("");
                messageText.AppendLine("🎁 *Выберите предложение:*");

                var keyboardButtons = new List<object[]>();
                
                for (int i = 0; i < activeOffers.Count; i++)
                {
                    var offer = activeOffers[i];
                    string buttonText;
                    
                    string shortTitle = offer.Title.Length > 20 
                        ? offer.Title.Substring(0, 17) + "..." 
                        : offer.Title;
                    
                    string itemEmoji = "🎁";
                    var mainItem = offer.Items?.FirstOrDefault();
                    if (mainItem != null)
                    {
                        itemEmoji = mainItem.Type switch
                        {
                            ShopItem.Gems => "💎",
                            ShopItem.Coin => "🪙",
                            ShopItem.Skin => "🎨",
                            ShopItem.PowerPoint => "⚡",
                            ShopItem.GuaranteedHero => "👤",
                            ShopItem.Titul => "🏷️",
                            ShopItem.RarityStarrDrop => "✨",
                            _ => "🎁"
                        };
                    }
                    
                    if (offer.Cost == 0)
                        buttonText = $"{itemEmoji} {shortTitle} (БЕСПЛАТНО)";
                    else
                        buttonText = $"{itemEmoji} {shortTitle} - {offer.Cost}";
                    
                    buttonText = buttonText.Length > 60 
                        ? buttonText.Substring(0, 57) + "..." 
                        : buttonText;
                    
                    keyboardButtons.Add(new object[]
                    {
                        new
                        {
                            text = buttonText,
                            callback_data = $"shop_view_{i}"
                        }
                    });
                }
                
                keyboardButtons.Add(new object[]
                {
                    new
                    {
                        text = "🔄 Обновить магазин",
                        callback_data = "shop_refresh_list"
                    },
                    new
                    {
                        text = "📊 Мои покупки",
                        callback_data = "my_claims_callback"
                    }
                });
                
                keyboardButtons.Add(new object[]
                {
                    new
                    {
                        text = "🔧",
                        callback_data = "shop_fix999_callback"
                    },
                    new
                    {
                        text = "📋 Статистика",
                        callback_data = "shop_debug"
                    },
                    new
                    {
                        text = "← Назад",
                        callback_data = "menu_main"
                    }
                });

                var keyboard = new
                {
                    inline_keyboard = keyboardButtons
                };
                
                await SendMessage(chatId, userId, 
                    messageText.ToString(), 
                    "MarkdownV2", keyboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Shop] Критическая ошибка: {ex.Message}");
                
                await SendMessage(chatId, userId,
                    "❌ *Произошла ошибка при загрузке магазина!*",
                    "MarkdownV2");
            }
        }

        private async Task HandleClaimedOffersCommand(long chatId, long userId)
        {
            if (!_linkManager.TryGetAccountId(userId, out string accountTag))
            {
                await SendMessage(chatId, userId, "❌ Аккаунт не привязан!", "MarkdownV2");
                return;
            }

            var claimedOffers = LoadClaimedOffers()
                .Where(c => c.TelegramUserId == userId && c.AccountTag == accountTag)
                .OrderByDescending(c => c.ClaimedAt)
                .Take(15)
                .ToList();
            
            var message = new StringBuilder();
            message.AppendLine("📋 *МОИ ЗАБРАННЫЕ ПРЕДЛОЖЕНИЯ*");
            message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            message.AppendLine("");
            
            if (claimedOffers.Any())
            {
                message.AppendLine($"🎮 *Аккаунт:* `{accountTag}`");
                message.AppendLine($"");
                
                var gems = claimedOffers.Where(c => c.ItemType == ShopItem.Gems).Sum(c => c.ItemCount);
                var coins = claimedOffers.Where(c => c.ItemType == ShopItem.Coin).Sum(c => c.ItemCount);
                var powerPoints = claimedOffers.Where(c => c.ItemType == ShopItem.PowerPoint).Sum(c => c.ItemCount);
                var skins = claimedOffers.Count(c => c.ItemType == ShopItem.Skin);
                
                message.AppendLine("💰 *Общая статистика:*");
                if (gems > 0) message.AppendLine($"💎 Гемы: {gems}");
                if (coins > 0) message.AppendLine($"🪙 Монеты: {coins}");
                if (powerPoints > 0) message.AppendLine($"⚡ Очки силы: {powerPoints}");
                if (skins > 0) message.AppendLine($"🎨 Скины: {skins}");
                message.AppendLine($"");
                
                message.AppendLine("*Последние забранные:*");
                foreach (var claim in claimedOffers.Take(5))
                {
                    string itemIcon = GetShopItemIcon(claim.ItemType);
                    message.AppendLine($"• {itemIcon} **{claim.OfferTitle}**");
                    message.AppendLine($"  🎁 {claim.ItemCount} | ⏰ {claim.ClaimedAt:dd.MM HH:mm}");
                    message.AppendLine("");
                }
                
                message.AppendLine($"📊 *Всего забрано:* {claimedOffers.Count} предложений");
                message.AppendLine("");
                message.AppendLine("🔄 *Команды:*");
                message.AppendLine("/shop - вернуться в магазин");
            }
            else
            {
                message.AppendLine("📭 *Вы ещё не забирали предложения!*");
                message.AppendLine("");
                message.AppendLine("🎁 *Начните здесь:*");
                message.AppendLine("/shop - перейти в магазин");
            }
            
            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[]
                    {
                        new { text = "← Назад в магазин", callback_data = "shop_back_to_list" }
                    }
                }
            };
            
            await SendMessage(chatId, userId, 
                message.ToString(), 
                "MarkdownV2", keyboard);
        }

        private async Task HandleQuickShopFixCommand(long chatId, long userId)
        {
            if (!_linkManager.TryGetAccountId(userId, out string accountTag))
            {
                await SendMessage(chatId, userId, 
                    "❌ *Сначала привяжите аккаунт!*\n\nОтправьте свой ТЭГ (например: #2PP)",
                    "MarkdownV2");
                return;
            }

            try
            {
                long accountId = TagToId(accountTag);
                var account = Accounts.Load(accountId);
                
                if (account == null)
                {
                    await SendMessage(chatId, userId,
                        $"❌ *Аккаунт `{accountTag}` не найден!*",
                        "MarkdownV2");
                    return;
                }

                if (account.Home == null)
                {
                    await SendMessage(chatId, userId,
                        "❌ *ClientHome не инициализирован!*\n\n" +
                        "Войдите в игру хотя бы раз.",
                        "MarkdownV2");
                    return;
                }

                if (account.Home.OfferBundles == null)
                {
                    account.Home.OfferBundles = new List<OfferBundle>();
                    Console.WriteLine($"[ShopFix] Создан новый список предложений");
                }
                else
                {
                    Console.WriteLine($"[ShopFix] Очищаем {account.Home.OfferBundles.Count} старых предложений");
                    account.Home.OfferBundles.Clear();
                }

                account.Home.OfferBundles.Add(CreateShopOffer(
                    "hockey_skin_offer",
                    "Hockey!",
                    ShopItem.Skin,
                    706,
                    1,
                    0,
                    149
                ));

                account.Home.OfferBundles.Add(CreateShopOffer(
                    "gems_149_offer", 
                    "149 гемов",
                    ShopItem.Gems,
                    0,
                    149,
                    0,
                    299
                ));

                account.Home.OfferBundles.Add(CreateShopOffer(
                    "coins_1000_offer",
                    "1000 монет", 
                    ShopItem.Coin,
                    0,
                    1000,
                    0,
                    49
                ));

                account.Home.OfferBundles.Add(CreateShopOffer(
                    "powerpoints_500_offer",
                    "500 очков силы",
                    ShopItem.PowerPoint,
                    0,
                    500,
                    0,
                    29
                ));

                account.Home.OfferBundles.Add(CreateShopOffer(
                    "title_winter_offer",
                    "Титул: Зима",
                    ShopItem.Titul,
                    100,
                    1,
                    0,
                    59
                ));

                Accounts.Save(account);
                
                Console.WriteLine($"[ShopFix] Создано {account.Home.OfferBundles.Count} предложений для {accountTag}");

                await SendMessage(chatId, userId,
                    $"✅ *МАГАЗИН СОЗДАН!*\n\n" +
                    $"🎮 *Аккаунт:* `{accountTag}`\n" +
                    $"🎁 *Предложений:* {account.Home.OfferBundles.Count}\n" +
                    $"📅 *Действуют до:* {DateTime.UtcNow.AddDays(30):dd.MM.yyyy}\n\n" +
                    $"✨ *Теперь используйте:*\n" +
                    $"`/shop` - просмотр магазина\n" +
                    $"`/shop_debug` - диагностика",
                    "MarkdownV2");

            }
            catch (Exception ex)
            {
                await SendMessage(chatId, userId,
                    $"❌ *ОШИБКА:*\n```{ex.Message}```",
                    "MarkdownV2");
            }
        }

        private OfferBundle CreateShopOffer(string id, string title, ShopItem itemType, int extraId, int count, int cost, int oldCost)
        {
            return new OfferBundle
            {
                Title = title,
                Purchased = false,
                EndTime = DateTime.UtcNow.AddDays(30),
                Cost = cost,
                OldCost = oldCost,
                Items = new List<Offer>
                {
                    new Offer
                    {
                        Type = itemType,
                        Count = count,
                        SkinDataId = (itemType == ShopItem.Skin) ? extraId : 0,
                        ItemDataId = (itemType == ShopItem.GuaranteedHero || itemType == ShopItem.Titul) ? extraId : 0
                    }
                }
            };
        }

        private async Task HandleShopViewCallback(long chatId, long userId, int offerIndex)
        {
            try
            {
                if (!_linkManager.TryGetAccountId(userId, out string accountTag))
                {
                    await SendMessage(chatId, userId, "❌ Аккаунт не привязан", "MarkdownV2");
                    return;
                }

                long accountId = TagToId(accountTag);
                var account = Accounts.Load(accountId);
                
                if (account?.Home?.OfferBundles == null)
                {
                    await SendMessage(chatId, userId, "❌ Магазин недоступен", "MarkdownV2");
                    return;
                }

                var allOffers = account.Home.OfferBundles;
                
                var activeOffers = allOffers
                    .Where(b => !b.Purchased && b.EndTime > DateTime.UtcNow)
                    .OrderByDescending(b => b.Cost == 0)
                    .ThenBy(b => b.Cost)
                    .Take(20)
                    .ToList();

                if (offerIndex < 0 || offerIndex >= activeOffers.Count)
                {
                    await SendMessage(chatId, userId,
                        $"❌ Предложение #{offerIndex + 1} не найдено!\nДоступно: {activeOffers.Count} предложений",
                        "MarkdownV2");
                    return;
                }

                var offer = activeOffers[offerIndex];
                
                var message = new StringBuilder();
                
                message.AppendLine($"🎁 *{offer.Title}*");
                message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
                message.AppendLine("");
                
                message.AppendLine("📦 *Содержимое:*");
                if (offer.Items != null && offer.Items.Count > 0)
                {
                    foreach (var item in offer.Items)
                    {
                        if (item == null) continue;
                        
                        string itemName = GetShopItemName(item.Type, item.SkinDataId, item.ItemDataId);
                        string itemIcon = GetShopItemIcon(item.Type);
                        
                        message.AppendLine($"{itemIcon} {itemName} ×{item.Count}");
                    }
                }
                else
                {
                    message.AppendLine("⚠️ Нет информации о содержимом");
                }
                
                message.AppendLine("");
                
                message.AppendLine("💰 *Стоимость:*");
                if (offer.Cost == 0)
                {
                    message.AppendLine("🎁 *БЕСПЛАТНО*");
                }
                else
                {
                    message.AppendLine($"{GetPriceInfoDetailed(offer)}");
                }
                
                message.AppendLine("");
                
                message.AppendLine("⏰ *Действует до:*");
                message.AppendLine($"{offer.EndTime:dd.MM.yyyy HH:mm}");
                
                var timeLeft = offer.EndTime - DateTime.UtcNow;
                if (timeLeft.TotalDays > 0)
                    message.AppendLine($"⏳ *Осталось: {timeLeft.Days}д {timeLeft.Hours}ч*");
                else if (timeLeft.TotalHours > 0)
                    message.AppendLine($"⏳ *Осталось: {timeLeft.Hours}ч {timeLeft.Minutes}м*");
                
                message.AppendLine("");
                
                if (offer.Purchased)
                {
                    message.AppendLine("✅ *Уже забрано*");
                }
                
                var keyboardButtons = new List<object[]>();
                
                if (!offer.Purchased && offer.Cost == 0)
                {
                    keyboardButtons.Add(new object[]
                    {
                        new
                        {
                            text = "🎁 ЗАБРАТЬ БЕСПЛАТНО",
                            callback_data = $"shop_claim_{offerIndex}"
                        }
                    });
                }
                
                keyboardButtons.Add(new object[]
                {
                    new
                    {
                        text = "← Назад",
                        callback_data = "shop_back_to_list"
                    }
                });
                
                var keyboard = new
                {
                    inline_keyboard = keyboardButtons
                };
                
                await SendMessage(chatId, userId, 
                    message.ToString(), 
                    "MarkdownV2", keyboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramShop] Ошибка в HandleShopViewCallback: {ex.Message}");
                await SendMessage(chatId, userId,
                    $"❌ Ошибка: {ex.Message}\n\nИспользуйте `/shop` для возврата",
                    "MarkdownV2");
            }
        }

        private async Task ProcessFreeOfferClaim(long chatId, long userId, int offerIndex)
        {
            if (!_linkManager.TryGetAccountId(userId, out string accountTag))
            {
                await SendMessage(chatId, userId, "❌ Ошибка: аккаунт не найден!", "MarkdownV2");
                return;
            }

            long accountId = TagToId(accountTag);
            var account = Accounts.Load(accountId);
            
            if (account?.Home?.OfferBundles == null)
            {
                await SendMessage(chatId, userId, "❌ Магазин недоступен!", "MarkdownV2");
                return;
            }

            var allOffers = account.Home.OfferBundles;
            
            var activeOffers = allOffers
                .Where(o => !o.Purchased && o.EndTime > DateTime.UtcNow)
                .OrderByDescending(o => o.Cost == 0)
                .ThenBy(o => o.Cost)
                .Take(20)
                .ToList();
            
            if (offerIndex < 0 || offerIndex >= activeOffers.Count)
            {
                await SendMessage(chatId, userId, "❌ Предложение не найдено!", "MarkdownV2");
                return;
            }

            var offer = activeOffers[offerIndex];
            var mainItem = offer.Items?.FirstOrDefault();
            
            if (mainItem == null)
            {
                await SendMessage(chatId, userId, "❌ Ошибка в предложении!", "MarkdownV2");
                return;
            }
            
            if (IsOfferAlreadyClaimed(userId, accountTag, offer.Title, offer.Cost, mainItem.Count))
            {
                await SendMessage(chatId, userId, 
                    $"❌ *Вы уже забирали это предложение!*\n\n" +
                    $"🎁 **{offer.Title}**\n" +
                    $"💰 *Стоимость:* {GetPriceInfo(offer)}\n\n" +
                    $"⚠️ Нельзя забрать одно и то же предложение дважды!",
                    "MarkdownV2");
                return;
            }
            
            if (offer.Purchased)
            {
                MarkOfferAsClaimed(userId, accountTag, offer);
                
                await SendMessage(chatId, userId, 
                    $"❌ *Это предложение уже забрано!*\n\n" +
                    $"🎁 **{offer.Title}**\n" +
                    $"💰 *Стоимость:* {GetPriceInfo(offer)}",
                    "MarkdownV2");
                return;
            }
            
            if (offer.EndTime <= DateTime.UtcNow)
            {
                await SendMessage(chatId, userId, "❌ Срок действия предложения истек!", "MarkdownV2");
                return;
            }
            
            try
            {
                var rewardsSummary = new StringBuilder();
                bool hasSkin = false;
                int skinId = 0;
                
                if (offer.Items != null)
                {
                    foreach (var item in offer.Items)
                    {
                        if (item == null) continue;
                        
                        switch (item.Type)
                        {
                            case ShopItem.Gems:
                                account.Avatar.Diamonds += item.Count;
                                rewardsSummary.AppendLine($"💎 +{item.Count} гемов");
                                break;
                                
                            case ShopItem.Coin:
                                account.Avatar.Gold += item.Count;
                                rewardsSummary.AppendLine($"🪙 +{item.Count} монет");
                                break;
                                
                            case ShopItem.PowerPoint:
                                account.Avatar.PowerPoints += item.Count;
                                rewardsSummary.AppendLine($"⚡ +{item.Count} очков силы");
                                break;
                                
                            case ShopItem.Skin:
                                hasSkin = true;
                                skinId = item.SkinDataId;
                                rewardsSummary.AppendLine($"🎨 Скин #{skinId}");
                                SaveSkinClaimRecord(userId, accountTag, skinId, offer.Title);
                                break;
                        }
                    }
                }
                
                offer.Purchased = true;
                Accounts.Save(account);
                
                MarkOfferAsClaimed(userId, accountTag, offer);
                
                var message = new StringBuilder();
                message.AppendLine($"✅ *ПРЕДЛОЖЕНИЕ ЗАБРАНО!*");
                message.AppendLine($"▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
                message.AppendLine($"");
                message.AppendLine($"🎁 **{offer.Title}**");
                message.AppendLine($"👤 Аккаунт: `{accountTag}`");
                message.AppendLine($"");
                message.AppendLine($"💰 *Полученные награды:*");
                message.AppendLine(rewardsSummary.ToString());
                message.AppendLine($"");
                message.AppendLine($"🔄 *Используйте:*");
                message.AppendLine($"`/shop` - вернуться в магазин");
                
                var keyboard = new
                {
                    inline_keyboard = new object[]
                    {
                        new object[] { new { text = "🛒 В магазин", callback_data = "shop_back_to_list" } },
                        new object[] { new { text = "📋 Мои покупки", callback_data = "my_claims_callback" } }
                    }
                };
                
                await SendMessage(chatId, userId, message.ToString(), "MarkdownV2", keyboard);
                
                if (hasSkin)
                {
                    await NotifyAdminsAboutSkinClaim(userId, accountTag, skinId, offer.Title);
                }
            }
            catch (Exception ex)
            {
                await SendMessage(chatId, userId,
                    $"❌ *Ошибка:* {ex.Message}",
                    "MarkdownV2");
            }
        }

        private void SaveSkinClaimRecord(long userId, string accountTag, int skinId, string offerTitle)
        {
            try
            {
                string filePath = "./skin_claims.json";
                List<SkinClaimRecord> claims = new List<SkinClaimRecord>();
                
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    claims = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SkinClaimRecord>>(json) ?? new List<SkinClaimRecord>();
                }
                
                claims.Add(new SkinClaimRecord
                {
                    TelegramUserId = userId,
                    AccountTag = accountTag,
                    SkinId = skinId,
                    OfferTitle = offerTitle,
                    ClaimTime = DateTime.UtcNow,
                    IsProcessed = false
                });
                
                var newJson = Newtonsoft.Json.JsonConvert.SerializeObject(claims, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, newJson);
                
                Console.WriteLine($"[Shop] Запись о скине сохранена: {accountTag} -> скин {skinId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Shop] Ошибка сохранения записи о скине: {ex.Message}");
            }
        }

        private async Task HandleSkinClaimsCommand(long chatId, long userId)
        {
            if (!IsAdmin(userId))
            {
                await SendMessage(chatId, userId, "❌ *Нет доступа!*", "MarkdownV2");
                return;
            }
            
            try
            {
                string filePath = "./skin_claims.json";
                if (!File.Exists(filePath))
                {
                    await SendMessage(chatId, userId, "📭 *Нет запросов на скины!*", "MarkdownV2");
                    return;
                }
                
                var json = File.ReadAllText(filePath);
                var claims = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SkinClaimRecord>>(json) ?? new List<SkinClaimRecord>();
                
                var pendingClaims = claims.Where(c => !c.IsProcessed).ToList();
                
                if (!pendingClaims.Any())
                {
                    await SendMessage(chatId, userId, 
                        "✅ *Все запросы обработаны!*\n\n" +
                        $"Всего записей: {claims.Count}\n" +
                        $"Обработано: {claims.Count(c => c.IsProcessed)}",
                        "MarkdownV2");
                    return;
                }
                
                var message = new StringBuilder();
                message.AppendLine("🎨 *ОЖИДАЮЩИЕ ЗАПРОСЫ СКИНОВ*");
                message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
                message.AppendLine("");
                
                for (int i = 0; i < Math.Min(pendingClaims.Count, 10); i++)
                {
                    var claim = pendingClaims[i];
                    message.AppendLine($"`#{i + 1}` *{claim.AccountTag}*");
                    message.AppendLine($"   🎨 Скин: #{claim.SkinId}");
                    message.AppendLine($"   🎁 Предложение: {claim.OfferTitle}");
                    message.AppendLine($"   ⏰ Запрошен: {claim.ClaimTime:dd.MM HH:mm}");
                    message.AppendLine($"   👤 Telegram: `{claim.TelegramUserId}`");
                    message.AppendLine("");
                }
                
                message.AppendLine($"📊 *Статистика:*");
                message.AppendLine($"• Ожидает: {pendingClaims.Count}");
                message.AppendLine($"• Всего: {claims.Count}");
                message.AppendLine($"• Обработано: {claims.Count(c => c.IsProcessed)}");
                message.AppendLine("");
                message.AppendLine("⚙️ *Команды:*");
                message.AppendLine("`/skin_mark [номер]` - отметить как выполненное");

                var keyboard = new
                {
                    inline_keyboard = new object[]
                    {
                        new object[] { new { text = "← Назад", callback_data = "menu_main" } }
                    }
                };
                
                await SendMessage(chatId, userId, message.ToString(), "MarkdownV2", keyboard);
            }
            catch (Exception ex)
            {
                await SendMessage(chatId, userId,
                    $"❌ *Ошибка:* {ex.Message}",
                    "MarkdownV2");
            }
        }

        private async Task HandleSkinMarkCommand(long chatId, long userId, string input)
        {
            if (!IsAdmin(userId))
            {
                await SendMessage(chatId, userId, "❌ *Нет доступа!*", "MarkdownV2");
                return;
            }
            
            if (string.IsNullOrEmpty(input) || !int.TryParse(input, out int claimIndex) || claimIndex < 1)
            {
                await SendMessage(chatId, userId,
                    "ℹ️ *Использование:*\n" +
                    "`/skin_mark [номер]`\n\n" +
                    "Пример: `/skin_mark 1`",
                    "MarkdownV2");
                return;
            }
            
            try
            {
                string filePath = "./skin_claims.json";
                if (!File.Exists(filePath))
                {
                    await SendMessage(chatId, userId, "❌ Файл запросов не найден!", "MarkdownV2");
                    return;
                }
                
                var json = File.ReadAllText(filePath);
                var claims = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SkinClaimRecord>>(json) ?? new List<SkinClaimRecord>();
                
                var pendingClaims = claims.Where(c => !c.IsProcessed).ToList();
                
                if (claimIndex > pendingClaims.Count)
                {
                    await SendMessage(chatId, userId,
                        $"❌ Запрос #{claimIndex} не найден!\n" +
                        $"Доступно: {pendingClaims.Count} запросов",
                        "MarkdownV2");
                    return;
                }
                
                var claim = pendingClaims[claimIndex - 1];
                claim.IsProcessed = true;
                
                var newJson = Newtonsoft.Json.JsonConvert.SerializeObject(claims, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, newJson);
                
                await SendMessage(chatId, userId,
                    $"✅ *Запрос отмечен как выполненный!*\n\n" +
                    $"👤 Аккаунт: `{claim.AccountTag}`\n" +
                    $"🎨 Скин: #{claim.SkinId}\n" +
                    $"🎁 Предложение: {claim.OfferTitle}",
                    "MarkdownV2");
                
                try
                {
                    await _client.SendMessageAsync(claim.TelegramUserId,
                        $"🎨 *ВАШ СКИН ВЫДАН!*\n\n" +
                        $"Администратор выдал вам скин:\n" +
                        $"🎨 *{claim.OfferTitle}*\n" +
                        $"👤 Аккаунт: `{claim.AccountTag}`\n\n" +
                        $"✅ Скин добавлен в вашу коллекцию!\n" +
                        $"🔄 Перезайдите в игру, чтобы увидеть изменения.",
                        TelegramClient.ParseMode.MarkdownV2);
                }
                catch { }
            }
            catch (Exception ex)
            {
                await SendMessage(chatId, userId,
                    $"❌ *Ошибка:* {ex.Message}",
                    "MarkdownV2");
            }
        }

        private async Task NotifyAdminsAboutSkinClaim(long userId, string accountTag, int skinId, string offerTitle)
        {
            try
            {
                string message = $"🎨 *ЗАПРОС СКИНА*\n\n" +
                                $"👤 Пользователь: `{userId}`\n" +
                                $"🎮 Аккаунт: `{accountTag}`\n" +
                                $"🎨 Скин: #{skinId}\n" +
                                $"🎁 Предложение: {offerTitle}\n" +
                                $"⏰ Время: {DateTime.UtcNow:dd.MM.yyyy HH:mm}";
                
                foreach (var adminId in _adminIds)
                {
                    try
                    {
                        await _client.SendMessageAsync(adminId, message, TelegramClient.ParseMode.MarkdownV2);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Shop] Ошибка уведомления администраторов: {ex.Message}");
            }
        }

        private async Task HandleShopViewCommand(long chatId, long userId, string input)
        {
            if (!_linkManager.TryGetAccountId(userId, out string accountTag))
            {
                await SendMessage(chatId, userId, "❌ *Сначала привяжите аккаунт!*", "MarkdownV2");
                return;
            }

            if (string.IsNullOrEmpty(input) || !int.TryParse(input, out int offerNumber) || offerNumber < 1)
            {
                await SendMessage(chatId, userId,
                    "📋 *Использование:*\n" +
                    "`/shop_view [номер]`\n\n" +
                    "*Пример:*\n" +
                    "`/shop_view 1` - посмотреть первое предложение",
                    "MarkdownV2");
                return;
            }

            long accountId = TagToId(accountTag);
            var account = Accounts.Load(accountId);
            
            if (account?.Home?.OfferBundles == null)
            {
                await SendMessage(chatId, userId, "❌ *Магазин недоступен!*", "MarkdownV2");
                return;
            }

            var allOffers = account.Home.OfferBundles;
            var activeOffers = allOffers
                .Where(b => !b.Purchased && b.EndTime > DateTime.UtcNow)
                .OrderByDescending(b => b.Cost == 0)
                .ThenBy(b => b.Cost)
                .Take(20)
                .ToList();

            if (offerNumber > activeOffers.Count)
            {
                await SendMessage(chatId, userId,
                    $"❌ *Предложение #{offerNumber} не найдено!*\n" +
                    $"Доступно предложений: {activeOffers.Count}",
                    "MarkdownV2");
                return;
            }

            var offer = activeOffers[offerNumber - 1];
            
            var message = new StringBuilder();
            message.AppendLine($"🎁 *{offer.Title}*");
            message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            message.AppendLine("");
            
            message.AppendLine("📦 *Содержимое:*");
            if (offer.Items != null && offer.Items.Count > 0)
            {
                foreach (var item in offer.Items)
                {
                    if (item == null) continue;
                    
                    string itemName = GetShopItemName(item.Type, item.SkinDataId, item.ItemDataId);
                    string itemIcon = GetShopItemIcon(item.Type);
                    
                    message.AppendLine($"{itemIcon} {itemName} ×{item.Count}");
                }
            }
            else
            {
                message.AppendLine("⚠️ Нет информации о содержимом");
            }
            
            message.AppendLine("");
            
            message.AppendLine("💰 *Стоимость:*");
            if (offer.Cost == 0)
            {
                message.AppendLine("🎁 *БЕСПЛАТНО*");
            }
            else
            {
                message.AppendLine($"{GetPriceInfoDetailed(offer)}");
            }
            
            message.AppendLine("");
            
            message.AppendLine("⏰ *Акция действует до:*");
            message.AppendLine($"{offer.EndTime:dd.MM.yyyy HH:mm}");
            
            var timeLeft = offer.EndTime - DateTime.UtcNow;
            if (timeLeft.TotalDays > 0)
                message.AppendLine($"⏳ *Осталось: {timeLeft.Days}д {timeLeft.Hours}ч*");
            else if (timeLeft.TotalHours > 0)
                message.AppendLine($"⏳ *Осталось: {timeLeft.Hours}ч {timeLeft.Minutes}м*");
            
            message.AppendLine("");
            
            message.AppendLine("💳 *Ваш баланс:*");
            message.AppendLine(GetBalanceInfo(account));
            
            message.AppendLine("");
            message.AppendLine("🔄 *Для возврата в магазин:*");
            message.AppendLine("`/shop`");

            await SendMessage(chatId, userId, message.ToString(), "MarkdownV2");
        }

        private string GetOfferButtonText(OfferBundle offer, Offer mainItem)
        {
            if (offer == null) return "Предложение";
            
            string itemType = "";
            
            if (mainItem != null)
            {
                itemType = mainItem.Type switch
                {
                    ShopItem.Gems => "💎",
                    ShopItem.Coin => "🪙",
                    ShopItem.Skin => "🎨",
                    ShopItem.Bling => "💠",
                    ShopItem.PowerPoint => "⚡",
                    ShopItem.GuaranteedHero => "👤",
                    ShopItem.Titul => "🏷️",
                    ShopItem.RarityStarrDrop => "✨",
                    _ => "🎁"
                };
            }
            
            string priceInfo = GetPriceInfo(offer);
            
            if (offer.Cost == 0)
                return $"{itemType} {offer.Title}";
            else
                return $"{itemType} {offer.Title} - {priceInfo}";
        }

        private string GetOfferIcon(OfferBundle offer)
        {
            if (offer.Cost == 0) return "🎁";
            
            var mainItem = offer.Items.FirstOrDefault();
            if (mainItem != null)
            {
                return mainItem.Type switch
                {
                    ShopItem.Skin => "🎨",
                    ShopItem.Gems => "💎",
                    ShopItem.GuaranteedHero => "👑",
                    ShopItem.RarityStarrDrop => "✨",
                    _ => "🛒"
                };
            }
            
            return "🛒";
        }

        private string GetBalanceInfo(Account account)
        {
            if (account?.Avatar == null)
                return "Не удалось загрузить баланс";
            
            return $"💎 {account.Avatar.Diamonds} | 🪙 {account.Avatar.Gold} | ⚡ {account.Avatar.PowerPoints}";
        }

        private string GetShopItemIcon(ShopItem itemType)
        {
            return itemType switch
            {
                ShopItem.Gems => "💎",
                ShopItem.Coin => "🪙",
                ShopItem.Skin => "🎨",
                ShopItem.Bling => "💠",
                ShopItem.PowerPoint => "⚡",
                ShopItem.GuaranteedHero => "👤",
                ShopItem.Titul => "🏷️",
                ShopItem.RarityStarrDrop => "✨",
                _ => "🎁"
            };
        }

        private string GetPriceInfo(OfferBundle offer)
        {
            if (offer.Cost == 0)
                return "БЕСПЛАТНО";
            
            return $"{offer.Cost} гемов";
        }

        private string GetPriceInfoDetailed(OfferBundle offer)
        {
            if (offer.Cost == 0)
                return "🎁 БЕСПЛАТНО";
            
            if (offer.OldCost > 0 && offer.OldCost > offer.Cost)
            {
                return $"~~{offer.OldCost}~~ → **{offer.Cost}** гемов";
            }
            
            return $"{offer.Cost} гемов";
        }

        private async Task HandleOnlineCommand(long chatId, long userId)
        {
            try
            {
                int onlineCount = Sessions.Count;
                
                string message = $"🟢 *Онлайн на сервере:* {onlineCount}";
                
                var keyboard = new
                {
                    inline_keyboard = new object[]
                    {
                        new object[] { new { text = "← Назад", callback_data = "menu_main" } }
                    }
                };
                
                await SendMessage(chatId, userId, message, "MarkdownV2", keyboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Ошибка получения онлайн: {ex.Message}");
                await SendMessage(chatId, userId, 
                    "❌ *Не удалось получить информацию об онлайн!*",
                    "MarkdownV2");
            }
        }

        private async Task ShowTransferLimits(long chatId, long userId)
        {
            int gemsUsedToday = GetGemsTransferredToday(userId);
            int gemsRemaining = DAILY_GEMS_LIMIT - gemsUsedToday;
            
            var timeUntilReset = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
            
            var message = new StringBuilder();
            message.AppendLine("📊 *ВАШИ ЛИМИТЫ ПЕРЕВОДОВ*");
            message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            message.AppendLine("");
            message.AppendLine("💰 *Гемы сегодня:*");
            message.AppendLine($"• Использовано: **{gemsUsedToday}**/{DAILY_GEMS_LIMIT} гемов");
            message.AppendLine($"• Доступно: **{gemsRemaining}** гемов");
            message.AppendLine("");
            
            int progressBars = (gemsUsedToday * 10) / DAILY_GEMS_LIMIT;
            string progressBar = new string('▓', progressBars) + new string('░', 10 - progressBars);
            message.AppendLine($"📈 Прогресс: `{progressBar}`");
            message.AppendLine("");
            
            message.AppendLine("⏰ *Сброс лимитов:*");
            message.AppendLine($"• Время сброса: **00:00 UTC**");
            message.AppendLine($"• Осталось времени: **{timeUntilReset.Hours}ч {timeUntilReset.Minutes}м**");
            message.AppendLine("");
            
            message.AppendLine("⚙️ *Основные правила:*");
            message.AppendLine($"• Максимум в день: **{DAILY_GEMS_LIMIT}** гемов");
            message.AppendLine($"• Минимум за раз: **10** гемов");
            message.AppendLine($"• Максимум за раз: **1000** гемов");
            message.AppendLine($"• Комиссия: **5%** (мин. 1 гем)");

            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[]
                    {
                        new { text = "💰 Сделать перевод", callback_data = "transfer_new" }
                    },
                    new object[]
                    {
                        new { text = "📜 История переводов", callback_data = "transfer_history_callback" },
                        new { text = "📋 Мои аккаунты", callback_data = "my_accounts_list" }
                    },
                    new object[]
                    {
                        new { text = "← Назад", callback_data = "menu_main" }
                    }
                }
            };
            
            await SendMessage(chatId, userId, 
                message.ToString(), 
                "MarkdownV2", keyboard);
        }

        private async Task HandleTransferGemsCommand(long chatId, long userId, string args)
        {
            try
            {
                if (!_linkManager.TryGetAccountId(userId, out string senderTag))
                {
                    await SendMessage(chatId, userId,
                        "❌ *У вас нет привязанного аккаунта!*\n\n" +
                        "Сначала привяжите аккаунт, отправив свой ТЭГ (например: `#2PP`)",
                        "MarkdownV2");
                    return;
                }

                if (string.IsNullOrEmpty(args))
                {
                    int gemsUsedTodayInitial = GetGemsTransferredToday(userId);
                    int gemsRemainingInitial = DAILY_GEMS_LIMIT - gemsUsedTodayInitial;
                    
                    await SendMessage(chatId, userId,
                        "💰 *ПЕРЕДАЧА ГЕМОВ ЛЮБОМУ ИГРОКУ*\n" +
                        "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬\n\n" +
                        $"📊 *Сегодня использовано:* {gemsUsedTodayInitial}/{DAILY_GEMS_LIMIT} гемов\n" +
                        $"🎯 *Осталось до лимита:* {gemsRemainingInitial} гемов\n" +
                        $"🕐 *Лимит сбросится:* в 00:00 UTC\n\n" +
                        "📋 *Формат команды:*\n" +
                        "`/transfer_gems #ТЭГ_ПОЛУЧАТЕЛЯ КОЛИЧЕСТВО`\n\n" +
                        "📌 *Примеры:*\n" +
                        "`/transfer_gems #89LJP 50` - передать 50 гемов\n" +
                        "`/transfer_gems #89LJP 1000` - передать 1000 гемов\n\n" +
                        "⚙️ *Ограничения:*\n" +
                        $"• Максимум в день: {DAILY_GEMS_LIMIT} гемов\n" +
                        "• Минимум за раз: 10 гемов\n" +
                        "• Максимум за раз: 1000 гемов\n" +
                        "• Комиссия: 5% (мин. 1 гем)",
                        "MarkdownV2");
                    return;
                }

                var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    await SendMessage(chatId, userId,
                        "❌ *Неверный формат!*\n\n" +
                        "Используйте: `/transfer_gems #ТЭГ КОЛИЧЕСТВО`\n" +
                        "Пример: `/transfer_gems #89LJP 50`",
                        "MarkdownV2");
                    return;
                }

                string receiverTag = parts[0].ToUpper();
                if (!receiverTag.StartsWith("#"))
                {
                    receiverTag = "#" + receiverTag;
                }

                if (!int.TryParse(parts[1], out int amount) || amount <= 0)
                {
                    await SendMessage(chatId, userId,
                        "❌ *Неверное количество гемов!*\n" +
                        "Укажите число от 10 до 1000",
                        "MarkdownV2");
                    return;
                }

                if (amount < 10)
                {
                    await SendMessage(chatId, userId,
                        "❌ *Минимальная сумма для передачи - 10 гемов!*",
                        "MarkdownV2");
                    return;
                }

                if (amount > 1000)
                {
                    await SendMessage(chatId, userId,
                        "❌ *Максимальная сумма за раз - 1000 гемов!*",
                        "MarkdownV2");
                    return;
                }

                int gemsUsedToday = GetGemsTransferredToday(userId);
                int gemsRemaining = DAILY_GEMS_LIMIT - gemsUsedToday;
                
                if (amount > gemsRemaining)
                {
                    await SendMessage(chatId, userId,
                        $"❌ *Превышен дневной лимит!*\n\n" +
                        $"💰 *Сегодня уже отправлено:* {gemsUsedToday}/{DAILY_GEMS_LIMIT} гемов\n" +
                        $"🎯 *Осталось до лимита:* {gemsRemaining} гемов\n" +
                        $"📤 *Вы хотите отправить:* {amount} гемов\n\n" +
                        $"ℹ️ Уменьшите сумму или дождитесь сброса лимита в 00:00 UTC",
                        "MarkdownV2");
                    return;
                }

                long receiverAccountId = TagToId(receiverTag);
                var receiverAccount = Accounts.Load(receiverAccountId);

                if (receiverAccount?.Avatar == null)
                {
                    await SendMessage(chatId, userId,
                        $"❌ *Аккаунт `{receiverTag}` не существует!*\n\n" +
                        "Проверьте правильность ТЭГа получателя.",
                        "MarkdownV2");
                    return;
                }

                if (senderTag == receiverTag)
                {
                    await SendMessage(chatId, userId,
                        "❌ *Нельзя передавать гемы самому себе!*\n" +
                        "Укажите другого игрока.",
                        "MarkdownV2");
                    return;
                }

                int commission = Math.Max(1, (int)Math.Ceiling(amount * 0.05));
                int totalToDeduct = amount + commission;

                long senderAccountId = TagToId(senderTag);
                var senderAccount = Accounts.Load(senderAccountId);
                
                if (senderAccount?.Avatar == null)
                {
                    await SendMessage(chatId, userId,
                        "❌ *Ошибка загрузки вашего аккаунта!*",
                        "MarkdownV2");
                    return;
                }

                if (senderAccount.Avatar.Diamonds < totalToDeduct)
                {
                    await SendMessage(chatId, userId,
                        $"❌ *Недостаточно гемов!*\n\n" +
                        $"💰 *Ваш баланс:* {senderAccount.Avatar.Diamonds} гемов\n" +
                        $"📤 *Требуется:* {totalToDeduct} гемов ({amount} + {commission} комиссия)\n" +
                        $"📊 *Не хватает:* {totalToDeduct - senderAccount.Avatar.Diamonds} гемов",
                        "MarkdownV2");
                    return;
                }

                senderAccount.Avatar.Diamonds -= totalToDeduct;
                receiverAccount.Avatar.Diamonds += amount;

                Accounts.Save(senderAccount);
                Accounts.Save(receiverAccount);

                UpdateDailyGemsLimit(userId, amount);
                await LogTransferToFile(userId, senderTag, receiverTag, amount, commission);

                int gemsUsedAfter = GetGemsTransferredToday(userId);
                int gemsRemainingAfter = DAILY_GEMS_LIMIT - gemsUsedAfter;
                
                var message = new StringBuilder();
                message.AppendLine("✅ *ПЕРЕВОД ВЫПОЛНЕН!*");
                message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
                message.AppendLine("");
                
                message.AppendLine("📊 *Лимиты сегодня:*");
                message.AppendLine($"• Использовано: {gemsUsedAfter}/{DAILY_GEMS_LIMIT} гемов");
                message.AppendLine($"• Осталось: {gemsRemainingAfter} гемов");
                message.AppendLine($"• Сбросится в: 00:00 UTC");
                message.AppendLine("");
                
                message.AppendLine("📤 *Отправитель:*");
                message.AppendLine($"👤 Аккаунт: `{senderTag}`");
                message.AppendLine($"👤 Имя: {senderAccount.Avatar.Name}");
                message.AppendLine($"💰 Новый баланс: {senderAccount.Avatar.Diamonds} гемов");
                message.AppendLine("");
                
                message.AppendLine("📥 *Получатель:*");
                message.AppendLine($"👤 Аккаунт: `{receiverTag}`");
                message.AppendLine($"👤 Имя: {receiverAccount.Avatar.Name}");
                message.AppendLine($"💰 Новый баланс: {receiverAccount.Avatar.Diamonds} гемов");
                message.AppendLine("");
                
                message.AppendLine("📋 *Детали перевода:*");
                message.AppendLine($"🎁 Передано: {amount} гемов");
                message.AppendLine($"💸 Комиссия: {commission} гемов (5%)");
                message.AppendLine($"📤 Списано всего: {totalToDeduct} гемов");
                
                var keyboard = new
                {
                    inline_keyboard = new object[]
                    {
                        new object[]
                        {
                            new { text = "💰 Мой баланс", callback_data = "check_balance" },
                            new { text = "📋 Мои аккаунты", callback_data = "my_accounts_list" }
                        },
                        new object[]
                        {
                            new { text = "📊 Лимиты", callback_data = "show_limits" },
                            new { text = "🔄 Ещё перевод", callback_data = "transfer_again" }
                        },
                        new object[]
                        {
                            new { text = "← Назад", callback_data = "menu_main" }
                        }
                    }
                };
                
                await SendMessage(chatId, userId, 
                    message.ToString(), 
                    "MarkdownV2", keyboard);

                var receiverTelegramId = _linkManager.GetTelegramIdByAccountTag(receiverTag);
                if (receiverTelegramId.HasValue)
                {
                    try
                    {
                        await _client.SendMessageAsync(receiverTelegramId.Value,
                            $"🎁 *ВЫ ПОЛУЧИЛИ ГЕМЫ!*\n\n" +
                            $"От: `{senderTag}` ({senderAccount.Avatar.Name})\n" +
                            $"Получено: **{amount}** гемов 💎\n" +
                            $"Ваш баланс: **{receiverAccount.Avatar.Diamonds}** гемов\n" +
                            $"Время: {DateTime.UtcNow:HH:mm:ss}",
                            TelegramClient.ParseMode.MarkdownV2);
                    }
                    catch { }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TransferGems] Ошибка: {ex.Message}");
                
                await SendMessage(chatId, userId,
                    $"❌ *Ошибка при выполнении перевода!*\n\n" +
                    $"Попробуйте позже или обратитесь к администратору.",
                    "MarkdownV2");
            }
        }

        private async Task HandleTransferHistoryCommand(long chatId, long userId)
        {
            try
            {
                string filePath = "./gems_transfers.json";
                if (!File.Exists(filePath))
                {
                    await SendMessage(chatId, userId,
                        "📭 *История переводов пуста!*\n" +
                        "Вы ещё не совершали переводов между аккаунтами.",
                        "MarkdownV2");
                    return;
                }
                
                string json = File.ReadAllText(filePath);
                var allTransfers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GemTransferRecord>>(json) 
                    ?? new List<GemTransferRecord>();
                
                var userTransfers = allTransfers
                    .Where(t => t.TelegramUserId == userId)
                    .OrderByDescending(t => t.TransferTime)
                    .Take(10)
                    .ToList();
                
                if (!userTransfers.Any())
                {
                    await SendMessage(chatId, userId,
                        "📭 *Ваша история переводов пуста!*",
                        "MarkdownV2");
                    return;
                }
                
                var message = new StringBuilder();
                message.AppendLine("📜 *ИСТОРИЯ ПЕРЕВОДОВ*");
                message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
                message.AppendLine("");
                
                int totalSent = userTransfers.Sum(t => t.Amount);
                int totalCommission = userTransfers.Sum(t => t.Commission);
                
                message.AppendLine("📊 *Статистика:*");
                message.AppendLine($"• Всего переводов: {userTransfers.Count}");
                message.AppendLine($"• Отправлено всего: {totalSent} гемов");
                message.AppendLine($"• Комиссий уплачено: {totalCommission} гемов");
                message.AppendLine($"• Списано всего: {userTransfers.Sum(t => t.TotalDeducted)} гемов");
                message.AppendLine("");
                
                message.AppendLine("🕐 *Последние переводы:*");
                foreach (var transfer in userTransfers.Take(5))
                {
                    message.AppendLine($"`{transfer.SenderTag} → {transfer.ReceiverTag}`");
                    message.AppendLine($"   💎 {transfer.Amount} гемов (комиссия: {transfer.Commission})");
                    message.AppendLine($"   ⏰ {transfer.TransferTime:dd.MM HH:mm}");
                    message.AppendLine($"   📊 Было лимита: {transfer.DailyRemainingBefore} гемов");
                    message.AppendLine("");
                }

                var keyboard = new
                {
                    inline_keyboard = new object[]
                    {
                        new object[] { new { text = "💰 Новый перевод", callback_data = "transfer_new" } },
                        new object[] { new { text = "← Назад", callback_data = "menu_main" } }
                    }
                };
                
                await SendMessage(chatId, userId, message.ToString(), "MarkdownV2", keyboard);
            }
            catch (Exception ex)
            {
                await SendMessage(chatId, userId,
                    $"❌ *Ошибка загрузки истории:* {ex.Message}",
                    "MarkdownV2");
            }
        }

        private async Task HandleDailySpinCommand(long chatId, long userId)
        {
            if (chatId != userId)
            {
                await SendMessage(chatId, userId, "❌ Эта команда доступна только в личных сообщениях.", "MarkdownV2");
                return;
            }

            if (!_linkManager.TryGetAccountId(userId, out string accountTag))
            {
                await SendMessage(chatId, userId, "❌ Сначала привяжите аккаунт, чтобы использовать колесо фортуны!", "MarkdownV2");
                return;
            }

            if (_lastSpinTimes.TryGetValue(userId, out DateTime lastSpin))
            {
                if (lastSpin.Date == DateTime.UtcNow.Date)
                {
                    var nextSpinTime = lastSpin.AddDays(1).Date;
                    var timeUntilNextSpin = nextSpinTime - DateTime.UtcNow;
                    
                    await SendMessage(chatId, userId,
                        $"⏳ *ВЫ УЖЕ КРУТИЛИ КОЛЕСО СЕГОДНЯ!*\n\n" +
                        $"🕐 *Последний спин:* {lastSpin:HH:mm:ss}\n" +
                        $"🔥 *Текущая серия:* {_dailyStreaks.GetValueOrDefault(userId, 0)} дней\n" +
                        $"⏰ *До следующего спина:* {timeUntilNextSpin.Hours}ч {timeUntilNextSpin.Minutes}м\n\n" +
                        $"🎰 *Приходите завтра за новой наградой!*",
                        "MarkdownV2");
                    return;
                }
            }

            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[]
                    {
                        new
                        {
                            text = "🎡 КРУТИТЬ КОЛЕСО!",
                            callback_data = $"spin_wheel_{userId}"
                        }
                    },
                    new object[]
                    {
                        new { text = "← Назад", callback_data = "menu_main" }
                    }
                }
            };

            int currentStreak = _dailyStreaks.GetValueOrDefault(userId, 0);
            
            var message = new StringBuilder();
            message.AppendLine("🎰 *ДНЕВНОЕ КОЛЕСО ФОРТУНЫ*");
            message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            message.AppendLine("");
            message.AppendLine("🔸 *Правила:*");
            message.AppendLine("• 1 спин в сутки (сбрасывается в 00:00 UTC)");
            message.AppendLine("• Чем дольше серия, тем лучше награды");
            message.AppendLine("");
            message.AppendLine("🎁 *Возможные награды:*");
            message.AppendLine("• 💎 Гемы (5-100)");
            message.AppendLine("• 🪙 Монеты (100-1000)");
            message.AppendLine("• ⚡ Очки силы (10-500)");
            message.AppendLine("• 👤 Новые бравлеры (шанс)");
            message.AppendLine("");
            message.AppendLine($"🔥 *Ваша серия:* **{currentStreak}** дней подряд");
            message.AppendLine($"🎁 *Бонус серии:* **+{Math.Min(currentStreak * 3, 50)}%** к награде");
            message.AppendLine("");
            message.AppendLine("🍀 *Нажмите кнопку ниже, чтобы попытать удачу!*");

            await SendMessage(chatId, userId,
                message.ToString(),
                "MarkdownV2", keyboard);
        }

        private async Task ProcessWheelSpin(long chatId, long userId)
        {
            Console.WriteLine($"[WheelSpin] Начало обработки спина для пользователя {userId} в {DateTime.UtcNow:HH:mm:ss}");
            
            try
            {
                if (!_linkManager.TryGetAccountId(userId, out string accountTag))
                {
                    await SendMessage(chatId, userId, "❌ Ошибка: аккаунт не найден!", "MarkdownV2");
                    return;
                }
                
                DateTime spinTime = DateTime.UtcNow;
                _lastSpinTimes[userId] = spinTime;
                
                Console.WriteLine($"[WheelSpin] Пользователь {userId} ({accountTag}) начал спин в {spinTime:HH:mm:ss}");
                
                int currentStreak = 1;
                if (_lastSpinTimes.TryGetValue(userId, out DateTime lastSpin))
                {
                    if (lastSpin.Date == spinTime.Date.AddDays(-1))
                    {
                        currentStreak = _dailyStreaks.GetValueOrDefault(userId, 0) + 1;
                    }
                    else if (lastSpin.Date == spinTime.Date)
                    {
                        currentStreak = _dailyStreaks.GetValueOrDefault(userId, 0);
                    }
                    else
                    {
                        currentStreak = 1;
                    }
                }
                
                _dailyStreaks[userId] = currentStreak;
                Console.WriteLine($"[WheelSpin] Серия пользователя {userId}: {currentStreak} дней");
                
                var reward = GenerateDailyReward(currentStreak);
                Console.WriteLine($"[WheelSpin] Сгенерирована награда: {reward.Gems} гемов, {reward.Coins} монет, {reward.PowerPoints} ОС");
                
                bool rewardSent = await SendRewardToGame(accountTag, reward);
                
                if (!rewardSent)
                {
                    Console.WriteLine($"[WheelSpin] ОШИБКА: Не удалось отправить награду пользователю {userId}");
                    
                    await SendMessage(chatId, userId,
                        "❌ Не удалось выдать награду. Свяжитесь с администратором @ShuzaBrawl\n\n" +
                        "⚠️ Время спина уже зафиксировано, повторная попытка невозможна!",
                        "MarkdownV2");
                    return;
                }
                
                Console.WriteLine($"[WheelSpin] Награда успешно отправлена пользователю {userId}");
                
                var resultMessage = new StringBuilder();
                resultMessage.AppendLine($"🎉 *ВЫ ВЫИГРАЛИ!*");
                resultMessage.AppendLine($"▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
                resultMessage.AppendLine($"");
                resultMessage.AppendLine($"👤 *Аккаунт:* `{accountTag}`");
                resultMessage.AppendLine($"⏰ *Время спина:* {spinTime:HH:mm:ss}");
                resultMessage.AppendLine($"");
                resultMessage.AppendLine($"🎁 *Награды:*");
                
                if (reward.Gems > 0)
                    resultMessage.AppendLine($"💎 **{reward.Gems}** гемов");
                if (reward.Coins > 0)
                    resultMessage.AppendLine($"🪙 **{reward.Coins}** монет");
                if (reward.PowerPoints > 0)
                    resultMessage.AppendLine($"⚡ **{reward.PowerPoints}** очков силы");
                
                if (reward.HasBrawlerChance && reward.BrawlerId > 0)
                {
                    string brawlerName = GetBrawlerNameById(reward.BrawlerId);
                    resultMessage.AppendLine($"👤 **Новый бравлер:** {brawlerName}!");
                    
                    if (reward.BrawlerPowerPoints > 0)
                        resultMessage.AppendLine($"⚡ **+{reward.BrawlerPowerPoints}** очков силы для бравлера");
                }
                
                resultMessage.AppendLine($"");
                resultMessage.AppendLine($"📊 *Статистика:*");
                resultMessage.AppendLine($"🔥 Серия: **{currentStreak}** дней подряд");
                resultMessage.AppendLine($"🎁 Бонус серии: **+{reward.StreakBonus}%**");
                resultMessage.AppendLine($"");
                resultMessage.AppendLine($"⏰ *Следующий спин:* завтра в 00:00 UTC");

                var keyboard = new
                {
                    inline_keyboard = new object[]
                    {
                        new object[] { new { text = "🎰 Крутить ещё", callback_data = "menu_spin" } },
                        new object[] { new { text = "← В меню", callback_data = "menu_main" } }
                    }
                };
                
                await SendMessage(chatId, userId, resultMessage.ToString(), "MarkdownV2", keyboard);
                
                Console.WriteLine($"[WheelSpin] Успешно завершен спин для пользователя {userId}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WheelSpin] КРИТИЧЕСКАЯ ОШИБКА для {userId}: {ex.Message}");
                Console.WriteLine($"[WheelSpin] StackTrace: {ex.StackTrace}");
                
                await SendMessage(chatId, userId,
                    "❌ Произошла критическая ошибка. Свяжитесь с администратором @ShuzaBrawl",
                    "MarkdownV2");
            }
        }

        private DailyReward GenerateDailyReward(int streak)
        {
            var random = new Random(Guid.NewGuid().GetHashCode());
            
            int baseGems = random.Next(5, 21);
            int baseCoins = random.Next(100, 501);
            int basePowerPoints = random.Next(10, 51);
            
            int streakBonusPercent = Math.Min(streak * 3, 50);
            int streakBonus = (streakBonusPercent * 100) / 100;
            
            int totalGems = baseGems + (baseGems * streakBonus / 100);
            int totalCoins = baseCoins + (baseCoins * streakBonus / 100);
            int totalPowerPoints = basePowerPoints + (basePowerPoints * streakBonus / 100);
            
            bool hasBrawlerChance = false;
            int brawlerId = 0;
            int brawlerPowerPoints = 0;
            
            int brawlerChance = Math.Min(5 + (streak * 1), 25);
            
            if (random.Next(0, 100) < brawlerChance)
            {
                hasBrawlerChance = true;
                
                int[] commonBrawlers = { 16000000, 16000001, 16000002 };
                int[] rareBrawlers = { 16000003, 16000004, 16000005 };
                int[] superRareBrawlers = { 16000006, 16000007, 16000008 };
                int[] epicBrawlers = { 16000009, 16000010, 16000011 };
                int[] mythicBrawlers = { 16000012, 16000013, 16000014 };
                int[] legendaryBrawlers = { 16000015, 16000016, 16000017 };
                
                int chanceTier = random.Next(0, 100);
                
                if (streak >= 30 && chanceTier < 1)
                    brawlerId = legendaryBrawlers[random.Next(legendaryBrawlers.Length)];
                else if (streak >= 21 && chanceTier < 5)
                    brawlerId = mythicBrawlers[random.Next(mythicBrawlers.Length)];
                else if (streak >= 14 && chanceTier < 15)
                    brawlerId = epicBrawlers[random.Next(epicBrawlers.Length)];
                else if (streak >= 7 && chanceTier < 30)
                    brawlerId = superRareBrawlers[random.Next(superRareBrawlers.Length)];
                else if (chanceTier < 50)
                    brawlerId = rareBrawlers[random.Next(rareBrawlers.Length)];
                else
                    brawlerId = commonBrawlers[random.Next(commonBrawlers.Length)];
                
                brawlerPowerPoints = random.Next(10, 31);
            }
            
            Console.WriteLine($"[WheelSpin] Сгенерирована награда: {totalGems}г/{totalCoins}м/{totalPowerPoints}ос, " +
                             $"серия: {streak} дней, бонус: +{streakBonusPercent}%, " +
                             $"шанс бравлера: {brawlerChance}% ({(hasBrawlerChance ? "выпал" : "не выпал")})");
            
            return new DailyReward
            {
                Gems = totalGems,
                Coins = totalCoins,
                PowerPoints = totalPowerPoints,
                BrawlerId = brawlerId,
                HasBrawlerChance = hasBrawlerChance,
                StreakBonus = streakBonusPercent,
                BrawlerPowerPoints = brawlerPowerPoints
            };
        }

        private string GetRewardDescription(DailyReward reward)
        {
            string description = "";
            
            if (reward.Gems > 0)
                description += $"💎 {reward.Gems} гемов\n";
            if (reward.Coins > 0)
                description += $"🪙 {reward.Coins} монет\n";
            if (reward.PowerPoints > 0)
                description += $"⚡ {reward.PowerPoints} очков силы\n";
            if (reward.BrawlerPowerPoints > 0)
                description += $"⚡ {reward.BrawlerPowerPoints} очков силы для бравлера\n";
            
            if (reward.HasBrawlerChance && reward.BrawlerId > 0)
            {
                string brawlerName = GetBrawlerNameById(reward.BrawlerId);
                description += $"👤 Новый бравлер: {brawlerName}!\n";
            }
            
            return description.Trim();
        }

        private async Task<bool> SendRewardToGame(string accountTag, DailyReward reward)
        {
            try
            {
                long accountId = TagToId(accountTag);
                Account account = Accounts.Load(accountId);
                
                if (account == null || account.Avatar == null)
                {
                    Console.WriteLine($"[WheelSpin] ОШИБКА: Аккаунт {accountTag} не найден или нет Avatar");
                    return false;
                }
                
                Console.WriteLine($"[WheelSpin] Загружен аккаунт {accountTag}, баланс до: " +
                                 $"{account.Avatar.Diamonds}💎/{account.Avatar.Gold}🪙/{account.Avatar.PowerPoints}⚡");
                
                account.Avatar.Diamonds += reward.Gems;
                account.Avatar.Gold += reward.Coins;
                account.Avatar.PowerPoints += reward.PowerPoints;
                
                Console.WriteLine($"[WheelSpin] Начислено: +{reward.Gems}💎, +{reward.Coins}🪙, +{reward.PowerPoints}⚡");
                
                if (reward.HasBrawlerChance && reward.BrawlerId > 0)
                {
                    int brawlerId = reward.BrawlerId;
                    if (brawlerId < 16000000 && brawlerId > 0)
                    {
                        brawlerId += 16000000;
                    }
                    
                    if (brawlerId >= 16000000 && brawlerId <= 16000077)
                    {
                        if (!account.Avatar.HasHero(brawlerId))
                        {
                            account.Avatar.UnlockHero(brawlerId);
                            Console.WriteLine($"[WheelSpin] Выдан новый бравлер ID: {brawlerId}");
                            
                            if (reward.BrawlerPowerPoints > 0)
                            {
                                var hero = account.Avatar.GetHero(brawlerId);
                                if (hero != null)
                                {
                                    hero.PowerPoints += reward.BrawlerPowerPoints;
                                    Console.WriteLine($"[WheelSpin] Добавлено {reward.BrawlerPowerPoints} ОС бравлеру {brawlerId}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[WheelSpin] Бравлер ID {brawlerId} уже есть у игрока");
                            
                            int convertedPowerPoints = reward.BrawlerPowerPoints * 2;
                            account.Avatar.PowerPoints += convertedPowerPoints;
                            Console.WriteLine($"[WheelSpin] Конвертировано в {convertedPowerPoints} ОС (бравлер уже был)");
                        }
                    }
                }
                
                Accounts.Save(account);
                
                Console.WriteLine($"[WheelSpin] Аккаунт {accountTag} сохранен, баланс после: " +
                                 $"{account.Avatar.Diamonds}💎/{account.Avatar.Gold}🪙/{account.Avatar.PowerPoints}⚡");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WheelSpin] КРИТИЧЕСКАЯ ОШИБКА при выдаче награды {accountTag}: {ex.Message}");
                return false;
            }
        }

        private async Task HandleSantaJoinCommand(long chatId, long userId)
        {
            if (!IsSantaEventActive())
            {
                await SendMessage(chatId, userId, 
                    $"❌ *Тайный Санта сейчас не активен!*\n\n{GetTimeRemaining()}",
                    "MarkdownV2");
                return;
            }

            if (!_linkManager.TryGetAccountId(userId, out string accountTag))
            {
                await SendMessage(chatId, userId, 
                    "❌ *Сначала привяжи аккаунт!*\n\nОтправь свой игровой ТЭГ (например: `#2PP`)",
                    "MarkdownV2");
                return;
            }

            if (_santaParticipants.ContainsKey(userId))
            {
                var participant = _santaParticipants[userId];
                await SendMessage(chatId, userId, 
                    $"✅ *Ты уже участвуешь в Тайном Санте!*\n\n" +
                    $"🎁 *Статистика:*\n" +
                    $"• Отправлено подарков: {participant.TotalGiftsSent}\n" +
                    $"• Получено подарков: {participant.TotalGiftsReceived}\n" +
                    $"• Всего отправлено: {participant.TotalAmountSent}",
                    "MarkdownV2");
                return;
            }

            _santaParticipants[userId] = new SantaParticipant
            {
                TelegramUserId = userId,
                AccountTag = accountTag,
                AccountId = TagToId(accountTag),
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
                TotalGiftsSent = 0,
                TotalGiftsReceived = 0,
                TotalAmountSent = 0
            };

            SaveSantaData();

            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[]
                    {
                        new { text = "📋 Список", callback_data = "santa_list" },
                        new { text = "🎁 Подарить", callback_data = "transfer_new" }
                    },
                    new object[]
                    {
                        new { text = "✨ Желание", callback_data = "santa_wish" },
                        new { text = "💰 Баланс", callback_data = "santa_balance" }
                    },
                    new object[]
                    {
                        new { text = "← Назад", callback_data = "menu_main" }
                    }
                }
            };

            await SendMessage(chatId, userId, 
                $"🎅 *ТЫ В КОМАНДЕ САНТЫ!*\n\n" +
                $"🎄 *Твой тег:* `{accountTag}`\n" +
                $"👥 *Участников всего:* {_santaParticipants.Count}\n\n" +
                $"🎁 *Как дарить подарки:*\n" +
                $"`/santa_gift [тег] [кол-во] [тип]`\n" +
                $"*Пример:* `/santa_gift #89LJP 50 gems`\n\n" +
                $"{GetTimeRemaining()}",
                "MarkdownV2", keyboard);
        }

        private async Task HandleSantaListCommand(long chatId, long userId)
        {
            if (!_santaParticipants.Any())
            {
                await SendMessage(chatId, userId, 
                    "📭 *Пока никто не присоединился!*\n\n" +
                    "Стань первым участником:\n`/santa_join`",
                    "MarkdownV2");
                return;
            }

            var activeParticipants = _santaParticipants.Values
                .Where(p => p.IsActive)
                .OrderBy(p => p.JoinedAt)
                .Take(30)
                .ToList();

            var message = new StringBuilder();
            message.AppendLine("🎄 *УЧАСТНИКИ ТАЙНОГО САНТЫ*");
            message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            message.AppendLine("");

            for (int i = 0; i < activeParticipants.Count; i++)
            {
                var p = activeParticipants[i];
                var giftInfo = $"🎁 {p.TotalGiftsSent} | 📥 {p.TotalGiftsReceived}";
                message.AppendLine($"`{i + 1}. {p.AccountTag}`");
                message.AppendLine($"   {giftInfo}");
                
                if (!string.IsNullOrEmpty(p.Wish))
                {
                    message.AppendLine($"   ✨ Желает: {p.Wish}");
                }
                message.AppendLine("");
            }

            message.AppendLine($"📊 *Всего участников:* {_santaParticipants.Count}");
            message.AppendLine($"🎅 *Активных:* {activeParticipants.Count}");

            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[] { new { text = "← Назад", callback_data = "santa_back" } }
                }
            };
            
            await SendMessage(chatId, userId, message.ToString(), "MarkdownV2", keyboard);
        }

        private async Task HandleSantaGiftCommand(long chatId, long userId, string[] args)
        {
            if (!IsSantaEventActive())
            {
                await SendMessage(chatId, userId, 
                    $"❌ *Тайный Санта завершён!*\n\n{GetTimeRemaining()}",
                    "MarkdownV2");
                return;
            }

            if (args.Length < 3)
            {
                await SendMessage(chatId, userId,
                    "❌ *Неправильный формат!*\n\n" +
                    "🎁 *Использование:*\n" +
                    "`/santa_gift [тег] [кол-во] [тип]`\n\n" +
                    "📌 *Примеры:*\n" +
                    "• `/santa_gift #89LJP 50 gems`\n" +
                    "• `/santa_gift #89LJP 1000 coins`\n" +
                    "• `/santa_gift #89LJP 500 powerpoints`\n\n" +
                    "💡 *Типы:* gems, coins, powerpoints",
                    "MarkdownV2");
                return;
            }

            string targetTag = args[0].ToUpper();
            if (!int.TryParse(args[1], out int amount) || amount <= 0)
            {
                await SendMessage(chatId, userId, "❌ *Неверное количество!*", "MarkdownV2");
                return;
            }

            string giftType = args[2].ToLower();
            if (!new[] { "gems", "coins", "powerpoints" }.Contains(giftType))
            {
                await SendMessage(chatId, userId, 
                    "❌ *Неверный тип!*\n\n" +
                    "Доступно: `gems`, `coins`, `powerpoints`",
                    "MarkdownV2");
                return;
            }

            if (!_santaParticipants.ContainsKey(userId))
            {
                await SendMessage(chatId, userId, 
                    "❌ *Сначала присоединись к Тайному Санте!*\n\n" +
                    "Используй команду: `/santa_join`",
                    "MarkdownV2");
                return;
            }

            var recipient = _santaParticipants.Values.FirstOrDefault(p => 
                p.AccountTag == targetTag && p.IsActive);
            
            if (recipient == null)
            {
                await SendMessage(chatId, userId, 
                    $"❌ *Участник с тегом `{targetTag}` не найден!*\n\n" +
                    "Посмотреть всех участников:\n`/santa_list`",
                    "MarkdownV2");
                return;
            }

            if (recipient.TelegramUserId == userId)
            {
                await SendMessage(chatId, userId, 
                    "❌ *Нельзя дарить подарки самому себе!* 🎅\n\n" +
                    "Подари подарок другому участнику!",
                    "MarkdownV2");
                return;
            }

            if (!_dailyGiftLog.ContainsKey(userId))
            {
                _dailyGiftLog[userId] = new List<DateTime>();
            }

            var todayGifts = _dailyGiftLog[userId].Count(d => d.Date == DateTime.UtcNow.Date);
            if (todayGifts >= MAX_GIFTS_PER_DAY)
            {
                await SendMessage(chatId, userId, 
                    $"❌ *Лимит подарков исчерпан!*\n\n" +
                    $"Максимально можно подарить {MAX_GIFTS_PER_DAY} подарков в день.\n" +
                    $"Завтра лимит обновится!",
                    "MarkdownV2");
                return;
            }

            int maxAmount = giftType switch
            {
                "gems" => MAX_GEMS_PER_GIFT,
                "coins" => MAX_COINS_PER_GIFT,
                "powerpoints" => MAX_POWERPOINTS_PER_GIFT,
                _ => 100
            };

            if (amount > maxAmount)
            {
                await SendMessage(chatId, userId, 
                    $"❌ *Слишком много!*\n\n" +
                    $"Максимально можно подарить {maxAmount} {GetGiftTypeName(giftType)} за раз.",
                    "MarkdownV2");
                return;
            }

            if (!_linkManager.TryGetAccountId(userId, out string senderTag))
            {
                await SendMessage(chatId, userId, "❌ *У тебя нет привязанного аккаунта!*", "MarkdownV2");
                return;
            }

            long senderAccountId = TagToId(senderTag);
            var senderAccount = Accounts.Load(senderAccountId);
            
            if (senderAccount?.Avatar == null)
            {
                await SendMessage(chatId, userId, "❌ *Ошибка загрузки твоего аккаунта!*", "MarkdownV2");
                return;
            }

            bool hasEnough = giftType switch
            {
                "gems" => senderAccount.Avatar.Diamonds >= amount,
                "coins" => senderAccount.Avatar.Gold >= amount,
                "powerpoints" => senderAccount.Avatar.PowerPoints >= amount,
                _ => false
            };

            if (!hasEnough)
            {
                await SendMessage(chatId, userId, 
                    $"❌ *Недостаточно {GetGiftTypeName(giftType)}!*\n\n" +
                    $"У тебя: *{GetResourceAmount(senderAccount, giftType)}*\n" +
                    $"Нужно: *{amount}*",
                    "MarkdownV2");
                return;
            }

            long recipientAccountId = recipient.AccountId;
            var recipientAccount = Accounts.Load(recipientAccountId);
            
            if (recipientAccount?.Avatar == null)
            {
                await SendMessage(chatId, userId, $"❌ *Ошибка загрузки аккаунта получателя!*", "MarkdownV2");
                return;
            }

            switch (giftType)
            {
                case "gems":
                    senderAccount.Avatar.Diamonds -= amount;
                    recipientAccount.Avatar.Diamonds += amount;
                    break;
                case "coins":
                    senderAccount.Avatar.Gold -= amount;
                    recipientAccount.Avatar.Gold += amount;
                    break;
                case "powerpoints":
                    senderAccount.Avatar.PowerPoints -= amount;
                    recipientAccount.Avatar.PowerPoints += amount;
                    break;
            }

            try
            {
                Accounts.Save(senderAccount);
                Accounts.Save(recipientAccount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Santa] Ошибка сохранения: {ex.Message}");
                await SendMessage(chatId, userId, 
                    "❌ *Ошибка системы!* Попробуйте позже.",
                    "MarkdownV2");
                return;
            }

            var giftId = Guid.NewGuid().ToString();
            var gift = new SantaGift
            {
                Id = giftId,
                FromUserId = userId,
                ToUserId = recipient.TelegramUserId,
                FromTag = senderTag,
                ToTag = targetTag,
                Amount = amount,
                GiftType = giftType,
                SentAt = DateTime.UtcNow,
                IsDelivered = true,
                Message = $"Подарок от {senderTag}"
            };
            
            _santaGifts[giftId] = gift;
            _dailyGiftLog[userId].Add(DateTime.UtcNow);

            if (_santaParticipants.TryGetValue(userId, out var senderParticipant))
            {
                senderParticipant.TotalGiftsSent++;
                senderParticipant.TotalAmountSent += amount;
                senderParticipant.LastGiftSent = DateTime.UtcNow;
            }

            if (_santaParticipants.TryGetValue(recipient.TelegramUserId, out var recipientParticipant))
            {
                recipientParticipant.TotalGiftsReceived++;
            }

            SaveSantaData();

            await SendMessage(chatId, userId,
                $"🎁 *ПОДАРОК ОТПРАВЛЕН!*\n\n" +
                $"🎄 *Получатель:* `{targetTag}`\n" +
                $"🎁 *Подарок:* {amount} {GetGiftTypeName(giftType)}\n" +
                $"💰 *Твой баланс:* {GetResourceAmount(senderAccount, giftType)}\n" +
                $"📊 *Подарков сегодня:* {todayGifts + 1}/{MAX_GIFTS_PER_DAY}\n\n" +
                $"🎅 *Спасибо за участие в Тайном Санте!*",
                "MarkdownV2");

            try
            {
                await _client.SendMessageAsync(recipient.TelegramUserId,
                    $"🎁 *ТЫ ПОЛУЧИЛ ПОДАРОК!*\n\n" +
                    $"🎅 *От:* `{senderTag}`\n" +
                    $"🎁 *Подарок:* {amount} {GetGiftTypeName(giftType)}\n" +
                    $"💰 *Твой баланс:* {GetResourceAmount(recipientAccount, giftType)}\n\n" +
                    $"🎄 *Счастливого Рождества!*",
                    TelegramClient.ParseMode.MarkdownV2);
            }
            catch { }
        }

        private async Task HandleSantaMyGiftsCommand(long chatId, long userId)
        {
            if (!_santaParticipants.ContainsKey(userId))
            {
                await SendMessage(chatId, userId, 
                    "❌ *Ты не участвуешь в Тайном Санте!*\n\n" +
                    "Присоединись: `/santa_join`",
                    "MarkdownV2");
                return;
            }

            var sentGifts = _santaGifts.Values
                .Where(g => g.FromUserId == userId)
                .OrderByDescending(g => g.SentAt)
                .Take(20)
                .ToList();

            var receivedGifts = _santaGifts.Values
                .Where(g => g.ToUserId == userId)
                .OrderByDescending(g => g.SentAt)
                .Take(20)
                .ToList();

            var participant = _santaParticipants[userId];

            var message = new StringBuilder();
            message.AppendLine("🎁 *МОИ ПОДАРКИ*");
            message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            message.AppendLine("");
            message.AppendLine($"📊 *Статистика:*");
            message.AppendLine($"• Отправлено: {participant.TotalGiftsSent}");
            message.AppendLine($"• Получено: {participant.TotalGiftsReceived}");
            message.AppendLine($"• Всего отправлено: {participant.TotalAmountSent}");
            message.AppendLine("");

            if (sentGifts.Any())
            {
                message.AppendLine("📤 *Отправленные:*");
                foreach (var gift in sentGifts.Take(5))
                {
                    message.AppendLine($"`→ {gift.ToTag}`: {gift.Amount} {GetGiftTypeName(gift.GiftType)}");
                    message.AppendLine($"  {gift.SentAt:dd.MM HH:mm}");
                    message.AppendLine("");
                }
            }

            if (receivedGifts.Any())
            {
                message.AppendLine("📥 *Полученные:*");
                foreach (var gift in receivedGifts.Take(5))
                {
                    message.AppendLine($"`← {gift.FromTag}`: {gift.Amount} {GetGiftTypeName(gift.GiftType)}");
                    message.AppendLine($"  {gift.SentAt:dd.MM HH:mm}");
                    message.AppendLine("");
                }
            }

            if (!sentGifts.Any() && !receivedGifts.Any())
            {
                message.AppendLine("📭 *Пока нет подарков!*\n");
                message.AppendLine("🎁 *Подари первым:*");
                message.AppendLine("`/santa_gift [тег] [кол-во] [тип]`");
            }

            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[] { new { text = "← Назад", callback_data = "santa_back" } }
                }
            };
            
            await SendMessage(chatId, userId, message.ToString(), "MarkdownV2", keyboard);
        }

        private async Task HandleSantaTopCommand(long chatId, long userId)
        {
            var topGivers = _santaParticipants.Values
                .Where(p => p.TotalGiftsSent > 0)
                .OrderByDescending(p => p.TotalAmountSent)
                .ThenByDescending(p => p.TotalGiftsSent)
                .Take(15)
                .ToList();

            var topReceivers = _santaParticipants.Values
                .Where(p => p.TotalGiftsReceived > 0)
                .OrderByDescending(p => p.TotalGiftsReceived)
                .Take(15)
                .ToList();

            var message = new StringBuilder();
            message.AppendLine("🏆 *ТОП ТАЙНОГО САНТЫ*");
            message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            message.AppendLine("");

            if (topGivers.Any())
            {
                message.AppendLine("🎅 *ТОП ДАРИТЕЛЕЙ:*");
                for (int i = 0; i < Math.Min(topGivers.Count, 10); i++)
                {
                    var giver = topGivers[i];
                    string medal = i switch
                    {
                        0 => "🥇",
                        1 => "🥈",
                        2 => "🥉",
                        _ => $"`{i + 1}.`"
                    };
                    
                    message.AppendLine($"{medal} `{giver.AccountTag}`");
                    message.AppendLine($"   Подарков: {giver.TotalGiftsSent} | Всего: {giver.TotalAmountSent}");
                    message.AppendLine("");
                }
            }

            if (topReceivers.Any())
            {
                message.AppendLine("🎁 *ТОП ПОЛУЧАТЕЛЕЙ:*");
                for (int i = 0; i < Math.Min(topReceivers.Count, 10); i++)
                {
                    var receiver = topReceivers[i];
                    message.AppendLine($"`{i + 1}. {receiver.AccountTag}`");
                    message.AppendLine($"   Получено: {receiver.TotalGiftsReceived} подарков");
                    message.AppendLine("");
                }
            }

            if (!topGivers.Any() && !topReceivers.Any())
            {
                message.AppendLine("📊 *Пока нет статистики!*\n");
                message.AppendLine("🎁 *Стань первым в топе:*");
                message.AppendLine("`/santa_gift [тег] [кол-во] [тип]`");
            }

            message.AppendLine($"📈 *Общая статистика:*");
            message.AppendLine($"• Участников: {_santaParticipants.Count}");
            message.AppendLine($"• Всего подарков: {_santaGifts.Count}");
            message.AppendLine($"• Всего отправлено: {_santaParticipants.Values.Sum(p => p.TotalAmountSent)}");
            message.AppendLine("");
            message.AppendLine($"{GetTimeRemaining()}");

            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[] { new { text = "← Назад", callback_data = "santa_back" } }
                }
            };
            
            await SendMessage(chatId, userId, message.ToString(), "MarkdownV2", keyboard);
        }

        private async Task HandleSantaInfoCommand(long chatId, long userId)
        {
            var message = new StringBuilder();
            message.AppendLine("🎅 *ТАЙНЫЙ САНТА*");
            message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            message.AppendLine("");
            message.AppendLine("🎄 *Как это работает:*");
            message.AppendLine("1. Присоединяешься: `/santa_join`");
            message.AppendLine("2. Даришь подарки другим участникам");
            message.AppendLine("3. Получаешь подарки от других");
            message.AppendLine("");
            message.AppendLine("🎁 *Команды:*");
            message.AppendLine("• `/santa_join` - Участвовать");
            message.AppendLine("• `/santa_list` - Участники");
            message.AppendLine("• `/santa_gift` - Подарить");
            message.AppendLine("• `/santa_mygifts` - Мои подарки");
            message.AppendLine("• `/santa_top` - Топ игроков");
            message.AppendLine("• `/santa_balance` - Мой баланс");
            message.AppendLine("• `/santa_wish [текст]` - Пожелание");
            message.AppendLine("");
            message.AppendLine("⚙️ *Лимиты:*");
            message.AppendLine($"• Максимально подарков в день: {MAX_GIFTS_PER_DAY}");
            message.AppendLine($"• Максимально гемов за раз: {MAX_GEMS_PER_GIFT}");
            message.AppendLine($"• Максимально монет за раз: {MAX_COINS_PER_GIFT}");
            message.AppendLine("");
            message.AppendLine($"{GetTimeRemaining()}");

            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[] { new { text = "🎅 Участвовать", callback_data = "santa_join" } },
                    new object[] { new { text = "← Назад", callback_data = "menu_main" } }
                }
            };
            
            await SendMessage(chatId, userId, message.ToString(), "MarkdownV2", keyboard);
        }

        private async Task HandleSantaWishCommand(long chatId, long userId, string wishText)
        {
            if (!_santaParticipants.ContainsKey(userId))
            {
                await SendMessage(chatId, userId, 
                    "❌ *Сначала присоединись к Тайному Санте!*\n\n" +
                    "Используй команду: `/santa_join`",
                    "MarkdownV2");
                return;
            }

            if (string.IsNullOrEmpty(wishText))
            {
                var currentWish = _santaParticipants[userId].Wish;
                if (!string.IsNullOrEmpty(currentWish))
                {
                    await SendMessage(chatId, userId,
                        $"✨ *Твоё текущее пожелание:*\n\n" +
                        $"`{currentWish}`\n\n" +
                        $"Чтобы изменить, введи:\n" +
                        "`/santa_wish [новое пожелание]`",
                        "MarkdownV2");
                }
                else
                {
                    await SendMessage(chatId, userId,
                        "✨ *У тебя нет пожелания!*\n\n" +
                        "Расскажи, что ты хочешь получить:\n" +
                        "`/santa_wish [твоё пожелание]`\n\n" +
                        "*Пример:* `/santa_wish Хочу новый скин для Шелли!`",
                        "MarkdownV2");
                }
                return;
            }

            _santaParticipants[userId].Wish = wishText;
            SaveSantaData();

            await SendMessage(chatId, userId,
                $"✨ *Пожелание сохранено!*\n\n" +
                $"`{wishText}`\n\n" +
                $"Теперь другие участники увидят твоё пожелание в списке!",
                "MarkdownV2");
        }

        private async Task HandleSantaBalanceCommand(long chatId, long userId)
        {
            if (!_linkManager.TryGetAccountId(userId, out string accountTag))
            {
                await SendMessage(chatId, userId, 
                    "❌ *У тебя нет привязанного аккаунта!*",
                    "MarkdownV2");
                return;
            }

            long accountId = TagToId(accountTag);
            var account = Accounts.Load(accountId);
            
            if (account?.Avatar == null)
            {
                await SendMessage(chatId, userId, "❌ *Ошибка загрузки аккаунта!*", "MarkdownV2");
                return;
            }

            var participant = _santaParticipants.ContainsKey(userId) ? _santaParticipants[userId] : null;

            var message = new StringBuilder();
            message.AppendLine("💰 *МОЙ БАЛАНС*");
            message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            message.AppendLine("");
            message.AppendLine($"👤 *Аккаунт:* `{accountTag}`");
            message.AppendLine("");
            message.AppendLine("💎 *Ресурсы:*");
            message.AppendLine($"• Гемы: {account.Avatar.Diamonds} 💎");
            message.AppendLine($"• Монеты: {account.Avatar.Gold} 🪙");
            message.AppendLine($"• Очки силы: {account.Avatar.PowerPoints} ⚡");
            message.AppendLine("");

            if (participant != null)
            {
                message.AppendLine("🎅 *Статистика Санты:*");
                message.AppendLine($"• Отправлено подарков: {participant.TotalGiftsSent}");
                message.AppendLine($"• Получено подарков: {participant.TotalGiftsReceived}");
                message.AppendLine($"• Всего отправлено: {participant.TotalAmountSent}");
                message.AppendLine("");
                
                var todayGifts = _dailyGiftLog.ContainsKey(userId) 
                    ? _dailyGiftLog[userId].Count(d => d.Date == DateTime.UtcNow.Date)
                    : 0;
                message.AppendLine($"*Сегодня:* {todayGifts}/{MAX_GIFTS_PER_DAY} подарков");
            }
            else
            {
                message.AppendLine("🎅 *Присоединись к Тайному Санте:*");
                message.AppendLine("`/santa_join`");
            }

            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[] { new { text = "← Назад", callback_data = "santa_back" } }
                }
            };
            
            await SendMessage(chatId, userId, message.ToString(), "MarkdownV2", keyboard);
        }

        private async Task HandleSantaStatsCommand(long chatId, long userId)
        {
            var totalGifts = _santaGifts.Count;
            var totalAmount = _santaGifts.Values.Sum(g => g.Amount);
            var totalParticipants = _santaParticipants.Count;
            var activeParticipants = _santaParticipants.Values.Count(p => p.IsActive);

            var gemsGifted = _santaGifts.Values.Where(g => g.GiftType == "gems").Sum(g => g.Amount);
            var coinsGifted = _santaGifts.Values.Where(g => g.GiftType == "coins").Sum(g => g.Amount);
            var powerpointsGifted = _santaGifts.Values.Where(g => g.GiftType == "powerpoints").Sum(g => g.Amount);

            var message = new StringBuilder();
            message.AppendLine("📊 *СТАТИСТИКА ТАЙНОГО САНТЫ*");
            message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            message.AppendLine("");
            message.AppendLine("👥 *Участники:*");
            message.AppendLine($"• Всего: {totalParticipants}");
            message.AppendLine($"• Активных: {activeParticipants}");
            message.AppendLine("");
            message.AppendLine("🎁 *Подарки:*");
            message.AppendLine($"• Всего отправлено: {totalGifts}");
            message.AppendLine($"• Общая стоимость: {totalAmount}");
            message.AppendLine("");
            message.AppendLine("💰 *Распределение:*");
            message.AppendLine($"• Гемы: {gemsGifted} 💎");
            message.AppendLine($"• Монеты: {coinsGifted} 🪙");
            message.AppendLine($"• Очки силы: {powerpointsGifted} ⚡");
            message.AppendLine("");
            
            if (totalGifts > 0)
            {
                var avgGift = totalAmount / totalGifts;
                var giftsPerPerson = totalParticipants > 0 ? (double)totalGifts / totalParticipants : 0;
                
                message.AppendLine("📈 *Средние значения:*");
                message.AppendLine($"• Средний подарок: {avgGift}");
                message.AppendLine($"• Подарков на человека: {giftsPerPerson:F1}");
                message.AppendLine("");
            }

            message.AppendLine($"{GetTimeRemaining()}");

            var keyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[] { new { text = "← Назад", callback_data = "santa_back" } }
                }
            };
            
            await SendMessage(chatId, userId, message.ToString(), "MarkdownV2", keyboard);
        }

        private async Task HandleSantaAdminCommand(long chatId, long userId, string action)
        {
            if (!IsAdmin(userId))
            {
                await SendMessage(chatId, userId, "❌ *Нет доступа!*", "MarkdownV2");
                return;
            }

            if (string.IsNullOrEmpty(action))
            {
                var stats = new StringBuilder();
                stats.AppendLine("🎅 *АДМИН ПАНЕЛЬ*");
                stats.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
                stats.AppendLine("");
                stats.AppendLine("📊 *Статистика:*");
                stats.AppendLine($"• Участников: {_santaParticipants.Count}");
                stats.AppendLine($"• Подарков: {_santaGifts.Count}");
                stats.AppendLine($"• Активных: {_santaParticipants.Values.Count(p => p.IsActive)}");
                
                await SendMessage(chatId, userId, stats.ToString(), "MarkdownV2");
                return;
            }
        }

        private string GetGiftTypeName(string giftType)
        {
            return giftType switch
            {
                "gems" => "гемов 💎",
                "coins" => "монет 🪙",
                "powerpoints" => "очков силы ⚡",
                _ => "ресурсов"
            };
        }

        private int GetResourceAmount(Account account, string giftType)
        {
            return giftType switch
            {
                "gems" => account.Avatar?.Diamonds ?? 0,
                "coins" => account.Avatar?.Gold ?? 0,
                "powerpoints" => account.Avatar?.PowerPoints ?? 0,
                _ => 0
            };
        }

        private async Task HandleRecoverCommand(long chatId, long userId)
        {
            if (chatId != userId)
            {
                await SendMessage(chatId, userId, "❌ Эта команда доступна только в личных сообщениях с ботом.", "MarkdownV2");
                return;
            }

            if (_linkManager.TryGetAccountId(userId, out string accountId))
            {
                long targetAccountIdLong = TagToId(accountId);
                Account account = Accounts.Load(targetAccountIdLong);
                if (account == null)
                {
                    await SendMessage(chatId, userId, "❌ Не удалось загрузить данные аккаунта для восстановления.", "MarkdownV2");
                    return;
                }

                bool isOnline = Sessions.IsSessionActive(targetAccountIdLong);
                string loadCommand = $"/load {targetAccountIdLong} {account.PassToken}";

                if (isOnline)
                {
                    ForceRefreshClient(targetAccountIdLong);
                    
                    await SendMessage(chatId, userId, 
                        $"🔑 *Восстановление доступа*\n\n" +
                        $"👤 *Аккаунт:* `{accountId}`\n" +
                        $"📱 *Статус:* 🟢 В игре\n\n" +
                        $"📋 *Команда для входа:*\n" +
                        $"`{loadCommand}`",
                        "MarkdownV2");
                }
                else
                {
                    await SendMessage(chatId, userId, 
                        $"🔑 *Восстановление доступа*\n\n" +
                        $"👤 *Аккаунт:* `{accountId}`\n" +
                        $"📱 *Статус:* 🔴 Не в игре\n\n" +
                        $"📋 *Команда для входа:*\n" +
                        $"`{loadCommand}`\n\n" +
                        $"📱 *Инструкция:*\n" +
                        $"1. Зайдите в игру\n" +
                        $"2. Создайте команду\n" +
                        $"3. Введите команду в чате команды\n",
                        "MarkdownV2");
                }
            }
            else
            {
                await SendMessage(chatId, userId, "❌ Ваш Telegram не привязан ни к одному аккаунту.", "MarkdownV2");
            }
        }

        private async Task HandleResetTokenCommand(long chatId, long userId, string tag)
        {
            if (chatId != userId)
            {
                await SendMessage(chatId, userId, "❌ Эта команда доступна только в личных сообщениях с ботом.", "MarkdownV2");
                return;
            }

            if (!_linkManager.TryGetAccountId(userId, out string linkedTag))
            {
                await SendMessage(chatId, userId, "❌ Ваш Telegram не привязан ни к одному аккаунту. Невозможно сбросить токен.", "MarkdownV2");
                return;
            }

            if (string.IsNullOrEmpty(tag))
            {
                tag = linkedTag;
            }
            else
            {
                if (tag.ToUpper() != linkedTag.ToUpper())
                {
                    await SendMessage(chatId, userId, $"❌ Вы можете сбросить токен только для своего аккаунта!(`{linkedTag}`).", "MarkdownV2");
                    return;
                }
            }

            long accountId;
            try
            {
                accountId = TagToId(tag);
            }
            catch (ArgumentException ex)
            {
                await SendMessage(chatId, userId, $"❌ Неверный формат тега аккаунта: {ex.Message}", "MarkdownV2");
                return;
            }

            Account account = Accounts.Load(accountId);
            if (account == null)
            {
                await SendMessage(chatId, userId, $"❌ Аккаунт с тегом `{tag}` не найден.", "MarkdownV2");
                return;
            }

            string newPassToken = GeneratePassToken();

            account.PassToken = newPassToken;
            SetAvatarPassToken(account, newPassToken);
            Accounts.Save(account);

            if (Sessions.IsSessionActive(accountId))
            {
                var session = Sessions.GetSession(accountId);
                if (session?.GameListener != null)
                {
                    session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                    {
                        ErrorCode = 10,
                        Message = "Токен аккаунта был сброшен. Вам нужно восстановить доступ."
                    });
                }
            }

            string newLoadCommand = $"/load {accountId} {newPassToken}";

            await SendMessage(chatId, userId, 
                $"✅ Токен аккаунта `{tag}` успешно сброшен!\n\n" +
                $"Ваша новая команда для входа:\n" +
                $"`{newLoadCommand}`\n" +
                $"*Используйте её для восстановления доступа.*",
                "MarkdownV2");
        }

        private void SetAvatarPassToken(Account account, string passToken)
        {
            if (account.Avatar != null)
            {
                account.Avatar.PassToken = passToken;
            }
        }

        private async Task HandleMyAccountsCommand(long chatId, long userId)
        {
            var linkedAccounts = _linkManager.GetAllLinkedAccounts(userId).ToList();
            
            if (linkedAccounts.Any())
            {
                var message = "🔗 *Ваши привязанные аккаунты:*\n\n";
                foreach (var account in linkedAccounts)
                {
                    message += $"• `{account}`\n";
                }
                message += $"\nВсего аккаунтов: {linkedAccounts.Count}";

                var keyboard = new
                {
                    inline_keyboard = new object[]
                    {
                        new object[] { new { text = "📊 Инфо", callback_data = "menu_profile" } },
                        new object[] { new { text = "← Назад", callback_data = "menu_main" } }
                    }
                };
                
                await SendMessage(chatId, userId, message, "MarkdownV2", keyboard);
            }
            else
            {
                await SendMessage(chatId, userId, "❌ У вас нет привязанных аккаунтов.", "MarkdownV2");
            }
        }

        private async Task HandleUnlinkCommand(long chatId, long userId, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                // Показываем список аккаунтов для выбора
                var accounts = _linkManager.GetAllLinkedAccounts(userId).ToList();
                
                if (!accounts.Any())
                {
                    await SendMessage(chatId, userId, 
                        "❌ У вас нет привязанных аккаунтов.", 
                        "MarkdownV2");
                    return;
                }

                var buttons = new List<object[]>();
                foreach (var acc in accounts.Take(10))
                {
                    buttons.Add(new object[]
                    {
                        new { text = $"🔗 {acc}", callback_data = $"unlink_confirm_{acc}" }
                    });
                }
                buttons.Add(new object[] { new { text = "← Назад", callback_data = "menu_main" } });

                var keyboard = new { inline_keyboard = buttons.ToArray() };

                await SendMessage(chatId, userId,
                    "🔗 *Выберите аккаунт для отвязки:*",
                    "MarkdownV2", keyboard);
                return;
            }

            tag = tag.ToUpper().TrimStart('#');
            if (!tag.StartsWith("#")) tag = "#" + tag;

            // Проверяем, принадлежит ли аккаунт пользователю
            bool isOwned = _linkManager.GetAllLinkedAccounts(userId).Any(t => 
                t.Equals(tag, StringComparison.OrdinalIgnoreCase));
            
            if (!isOwned)
            {
                await SendMessage(chatId, userId, 
                    $"❌ Аккаунт `{tag}` не привязан к вашему Telegram.", 
                    "MarkdownV2");
                return;
            }

            // Спрашиваем подтверждение
            var confirmKeyboard = new
            {
                inline_keyboard = new object[]
                {
                    new object[]
                    {
                        new { text = "✅ Да, отвязать", callback_data = $"unlink_do_{tag}" },
                        new { text = "❌ Нет", callback_data = "menu_accounts" }
                    }
                }
            };

            await SendMessage(chatId, userId,
                $"⚠️ *Вы уверены, что хотите отвязать аккаунт* `{tag}`?\n\n" +
                "После отвязки вы не сможете управлять этим аккаунтом через бота.",
                "MarkdownV2", confirmKeyboard);
        }

        private async Task HandleUnlinkAllCommand(long chatId, long userId)
        {
            var accounts = _linkManager.GetAllLinkedAccounts(userId).ToList();
            if (!accounts.Any())
            {
                await SendMessage(chatId, userId, "❌ У вас нет привязанных аккаунтов.", "MarkdownV2");
                return;
            }

            _linkManager.UnlinkAllAccounts(userId);
            await SendMessage(chatId, userId, 
                $"✅ Все аккаунты ({accounts.Count}) были отвязаны от вашего Telegram.", 
                "MarkdownV2");
        }

        private async Task HandleAccountInfoCommand(long chatId, long userId, string tag)
{
    if (string.IsNullOrEmpty(tag))
    {
        if (_linkManager.TryGetAccountId(userId, out string linkedTag))
        {
            tag = linkedTag;
        }
        else
        {
            await SendMessage(chatId, userId, 
                "❌ Укажите тег аккаунта: `/account_info #ТЭГ`\nИли привяжите аккаунт отправив свой ТЭГ", 
                "MarkdownV2");
            return;
        }
    }

    tag = tag.ToUpper();
    
    if (!_linkManager.IsAccountOwnedByUser(userId, tag))
    {
        await SendMessage(chatId, userId, 
            $"❌ Аккаунт `{tag}` не привязан к вашему Telegram.", 
            "MarkdownV2");
        return;
    }

    long accountId = TagToId(tag);
    Account account = Accounts.Load(accountId);

    if (account == null)
    {
        await SendMessage(chatId, userId, $"❌ Аккаунт `{tag}` не найден.", "MarkdownV2");
        return;
    }

    var info = GetExtendedAccountInfo(account, accountId, tag);
    
    // ИЗМЕНЕНО: Добавлена кнопка "🔗 Отвязать аккаунт"
    var keyboard = new
    {
        inline_keyboard = new object[]
        {
            new object[]
            {
                new { text = "🚀 Быстрый вход", callback_data = $"account_quick_{tag}" },
                new { text = "🔄 Сбросить токен", callback_data = $"reset_token_{tag}" }
            },
            new object[]
            {
                new { text = "🔗 Отвязать аккаунт", callback_data = $"unlink_confirm_{tag}" } // НОВАЯ КНОПКА
            },
            new object[]
            {
                new { text = "← Назад в аккаунты", callback_data = "menu_accounts" },
                new { text = "← Главное меню", callback_data = "menu_main" }
            }
        }
    };
    
    await SendMessage(chatId, userId, info, "MarkdownV2", keyboard);
}

        private string GetExtendedAccountInfo(Account account, long accountId, string tag)
        {
            var avatar = account.Avatar;
            
            bool isOnline = Sessions.IsSessionActive(accountId);
            string favoriteBrawler = GetFavoriteBrawlerName(avatar);
            
            int currentWinStreak = avatar.WinStreak;
            int maxWinStreak = avatar.MaxWinstreak;
            int totalWins = avatar.TrioWins + avatar.DuoWins + avatar.SoloWins;

            var info = $"🎮 *ЛИЧНЫЙ КАБИНЕТ*\n" +
                       $"▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬\n\n" +
                       
                       $"👤 *Основная информация*\n" +
                       $"• Тег: `{tag}`\n" +
                       $"• Никнейм: {avatar.Name}\n" +
                       $"• Трофеи: {avatar.Trophies} 🏆\n" +
                       $"• Бравлеры: {avatar.GetUnlockedHeroesCount()}\n\n" +
                       
                       $"📈 *Статистика побед*\n" +
                       $"• Всего побед: {totalWins} 🏅\n" +
                       $"• 3x3: {avatar.TrioWins}\n" +
                       $"• Парное столкновение: {avatar.DuoWins}\n" +
                       $"• Одиночное столкновение: {avatar.SoloWins}\n" +
                       $"• Текущий винстрик: *{currentWinStreak}*\n" +
                       $"• Макс. винстрик: *{maxWinStreak}*\n\n" +
                       
                       $"💰 *Ресурсы*\n" +
                       $"• Монеты: {avatar.Gold} 🪙\n" +
                       $"• Гемы: {avatar.Diamonds} 💎\n" +
                       $"• Очки силы: {avatar.PowerPoints} ⚡\n" +
                       $"• Блинги: {avatar.Blings} 💠\n\n" +
                       
                       $"👑 *Достижения*\n" +
                       $"• Любимый бравлер: {favoriteBrawler}\n" +
                       $"• Клуб: {GetClubInfo(avatar)}\n" +
                       $"• Рекорд трофеев: {avatar.HighestTrophies} 🏆\n\n" +
                       
                       $"🔒 *Безопасность*\n" +
                       $"• Привязка Telegram: ✅\n" +
                       $"• Статус: {(isOnline ? "🟢 В сети" : "🔴 Не в сети")}";

            return info;
        }

        private string GetFavoriteBrawlerName(ClientAvatar avatar)
        {
            if (avatar == null || avatar.Heroes == null || avatar.Heroes.Count == 0)
                return "Неизвестно";
            
            var favorite = avatar.Heroes.OrderByDescending(h => h.Trophies).FirstOrDefault();
            if (favorite != null)
            {
                try
                {
                    int characterIndex = favorite.CharacterId - 16000000;
                    var characterData = DataTables.Get(16)
                        .GetDataWithId<CharacterData>(characterIndex);
                    
                    if (characterData != null)
                    {
                        var type = characterData.GetType();
                        
                        var nameProp = type.GetProperty("Name");
                        if (nameProp != null)
                        {
                            string name = (string)nameProp.GetValue(characterData);
                            if (!string.IsNullOrEmpty(name))
                                return $"{name} ({favorite.Trophies}🏆)";
                        }
                        
                        var tidNameProp = type.GetProperty("TID_Name");
                        if (tidNameProp != null)
                        {
                            string tidName = (string)tidNameProp.GetValue(characterData);
                            if (!string.IsNullOrEmpty(tidName))
                                return $"{tidName} ({favorite.Trophies}🏆)";
                        }
                        
                        var getNameMethod = type.GetMethod("GetName");
                        if (getNameMethod != null)
                        {
                            string name = (string)getNameMethod.Invoke(characterData, null);
                            if (!string.IsNullOrEmpty(name))
                                return $"{name} ({favorite.Trophies}🏆)";
                        }
                    }
                    
                    return $"Бравлер #{favorite.CharacterId} ({favorite.Trophies}🏆)";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TelegramBot] Ошибка получения имени бравлера: {ex.Message}");
                    return $"Неизвестно ({favorite.Trophies}🏆)";
                }
            }
            
            return "Неизвестно";
        }

        private string GetClubInfo(ClientAvatar avatar)
        {
            if (avatar == null) return "Не в клубе";
            
            if (avatar.AllianceId > 0)
            {
                string roleName = avatar.AllianceRole switch
                {
                    AllianceRole.Leader => "Президент",
                    AllianceRole.CoLeader => "Вице-президент", 
                    AllianceRole.Elder => "Ветеран",
                    AllianceRole.Member => "Участник",
                    _ => "Участник"
                };
                
                return $"{roleName} (ID: {avatar.AllianceId})";
            }
            
            return "Не в клубе";
        }

        private async Task HandleQuickLoginCommand(long chatId, long userId)
        {
            if (_linkManager.TryGetAccountId(userId, out string accountId))
            {
                long accountIdLong = TagToId(accountId);
                Account account = Accounts.Load(accountIdLong);
                
                if (account != null)
                {
                    string loadCommand = $"/load {accountIdLong} {account.PassToken}";
                    
                    await SendMessage(chatId, userId,
                        $"🚀 *Быстрый вход в аккаунт* `{accountId}`\n\n" +
                        $"💬 Команда для входа:\n" +
                        $"`{loadCommand}`\n\n" +
                        $"❗ Создайте команду в игре и введите эту команду в чате.",
                        "MarkdownV2");
                }
            }
            else
            {
                await SendMessage(chatId, userId, 
                    "❌ У вас нет привязанных аккаунтов.", 
                    "MarkdownV2");
            }
        }

        private async Task HandleNotificationsCommand(long chatId, long userId, string action)
        {
            if (action == "on")
            {
                _notificationSettings[userId] = true;
                await SendMessage(chatId, userId,
                    "🔔 *Уведомления включены*\n\nВы будете получать уведомления о входах в ваш аккаунт.",
                    "MarkdownV2");
            }
            else if (action == "off")
            {
                _notificationSettings[userId] = false;
                await SendMessage(chatId, userId,
                    "🔕 *Уведомления выключены*",
                    "MarkdownV2");
            }
            else
            {
                bool isEnabled = _notificationSettings.GetValueOrDefault(userId, true);
                await SendMessage(chatId, userId,
                    $"ℹ️ *Управление уведомлениями:*\n\n" +
                    $"Текущий статус: {(isEnabled ? "🔔 Включены" : "🔕 Выключены")}\n\n" +
                    "/notifications on - Включить уведомления\n" +
                    "/notifications off - Выключить уведомления",
                    "MarkdownV2");
            }
        }

        private async Task HandleResetTelegramCommand(long chatId, long userId, string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                await SendMessage(chatId, userId,
                    "🔧 *Принудительная отвязка Telegram*\n\n" +
                    "Использование: `/resettg782978256 #ТЭГ`\n" +
                    "Пример: `/resettg782978256 #9UU0`\n\n" +
                    "⚠️ *Будет сделано:*\n" +
                    "1. Отвязка от Telegram\n" +
                    "2. Сброс токена безопасности\n" +
                    "3. Новая команда для входа",
                    "MarkdownV2");
                return;
            }

            string tag = input.ToUpper();
            
            if (!tag.StartsWith("#"))
            {
                await SendMessage(chatId, userId,
                    "❌ *Неверный формат тега!*\n" +
                    "Тег должен начинаться с #\n" +
                    "Пример: `#9UU0` или `#2PP`",
                    "MarkdownV2");
                return;
            }

            try
            {
                bool unlinked = false;
                string telegramId = "неизвестно";
                string filePath = "./TelegramLinks.json";
                
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
                    
                    if (data != null && 
                        data.ContainsKey("AccountIdToTelegram") && 
                        data.ContainsKey("TelegramToAccountId"))
                    {
                        if (data["AccountIdToTelegram"].TryGetValue(tag, out telegramId))
                        {
                            data["AccountIdToTelegram"].Remove(tag);
                            data["TelegramToAccountId"].Remove(telegramId);
                            
                            File.WriteAllText(filePath, 
                                Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
                            
                            unlinked = true;
                            Console.WriteLine($"[USER {userId}] отвязал {tag} от Telegram {telegramId}");
                            
                            _linkManager = new TelegramLinkManager();
                        }
                    }
                }

                if (!unlinked)
                {
                    await SendMessage(chatId, userId,
                        $"ℹ️ *Аккаунт `{tag}` не найден в привязках.*\n" +
                        $"Возможно он не был привязан.",
                        "MarkdownV2");
                    return;
                }

                long accountId = TagToId(tag);
                var account = Accounts.Load(accountId);
                
                if (account == null)
                {
                    await SendMessage(chatId, userId,
                        $"⚠️ *Аккаунт `{tag}` отвязан, но не найден в игре!*",
                        "MarkdownV2");
                    return;
                }

                string newPassToken = GeneratePassToken();
                account.PassToken = newPassToken;
                SetAvatarPassToken(account, newPassToken);
                Accounts.Save(account);

                string newLoadCommand = $"/load {accountId} {newPassToken}";

                if (Sessions.IsSessionActive(accountId))
                {
                    var session = Sessions.GetSession(accountId);
                    if (session?.GameListener != null)
                    {
                        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                        {
                            ErrorCode = 8,
                            Message = "Аккаунт был отвязан. Требуется повторная привязка."
                        });
                    }
                }

                await SendMessage(chatId, userId,
                    $"✅ *ОТВЯЗКА ВЫПОЛНЕНА!*\n\n" +
                    $"🎮 *Аккаунт:* `{tag}`\n" +
                    $"📱 *Был привязан к:* `{telegramId}`\n" +
                    $"🔄 *Токен сброшен:* ✅\n\n" +
                    $"🔐 *Новая команда для владельца:*\n" +
                    $"`{newLoadCommand}`\n\n" +
                    $"📋 *Что сообщить владельцу:*\n" +
                    "1. Используй команду выше для входа\n" +
                    "2. Привяжи новый Telegram через бота\n" +
                    "3. Сохрани команду в безопасном месте",
                    "MarkdownV2");

                string auditLog = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ПОЛЬЗОВАТЕЛЬ {userId} -> Отвязал {tag} (Telegram: {telegramId})\n";
                File.AppendAllText("./admin_audit.log", auditLog);

            }
            catch (Exception ex)
            {
                await SendMessage(chatId, userId,
                    $"❌ *Ошибка выполнения:*\n```{ex.Message}```",
                    "MarkdownV2");
                
                Console.WriteLine($"[ERROR] /resettg782978256 от {userId}: {ex.Message}");
            }
        }

        private async Task SyncOffersFromGame(long chatId, long userId)
        {
            if (!_linkManager.TryGetAccountId(userId, out string accountTag))
            {
                await SendMessage(chatId, userId, 
                    "❌ *Сначала привяжите аккаунт!*", 
                    "MarkdownV2");
                return;
            }

            long accountId = TagToId(accountTag);
            
            try
            {
                var account = Accounts.Load(accountId);
                if (account?.Home == null)
                {
                    await SendMessage(chatId, userId,
                        "❌ *Аккаунт не загружен!*\n" +
                        "Войдите в игру, чтобы создать предложения.",
                        "MarkdownV2");
                    return;
                }

                int totalOffers = account.Home.OfferBundles?.Count ?? 0;
                int activeOffers = account.Home.OfferBundles?
                    .Count(o => !o.Purchased && o.EndTime > DateTime.UtcNow) ?? 0;
                
                var message = new StringBuilder();
                message.AppendLine("🔄 *СИНХРОНИЗАЦИЯ ПРЕДЛОЖЕНИЙ*");
                message.AppendLine("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
                message.AppendLine("");
                message.AppendLine($"✅ *Синхронизировано с игрой!*");
                message.AppendLine($"");
                message.AppendLine($"📊 *Статистика:*");
                message.AppendLine($"• Всего предложений: {totalOffers}");
                message.AppendLine($"• Активных: {activeOffers}");
                message.AppendLine($"• Тег аккаунта: `{accountTag}`");
                message.AppendLine($"");
                message.AppendLine("💡 *Что дальше:*");
                message.AppendLine("1. Используйте `/shop` для просмотра");
                message.AppendLine("2. Используйте `/shop_debug` для деталей");

                await SendMessage(chatId, userId, message.ToString(), "MarkdownV2");
            }
            catch (Exception ex)
            {
                await SendMessage(chatId, userId,
                    $"❌ *Ошибка синхронизации:*\n```{ex.Message}```",
                    "MarkdownV2");
                
                Console.WriteLine($"[TelegramBot] Ошибка синхронизации: {ex.Message}");
            }
        }

        private void ForceRefreshClient(long accountId)
        {
            try
            {
                if (Sessions.IsSessionActive(accountId))
                {
                    var session = Sessions.GetSession(accountId);
                    if (session?.GameListener != null)
                    {
                        var keepAlive = new KeepAliveMessage();
                        session.GameListener.SendTCPMessage(keepAlive);
                        
                        Console.WriteLine($"[ForceRefresh] Отправлен keep-alive для {accountId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ForceRefresh] Ошибка: {ex.Message}");
            }
        }

        private async Task<bool> SendConfirmationNotificationToGame(long accountId, string code)
        {
            try
            {
                var account = Accounts.Load(accountId);
                if (account == null) return false;

                var clientHome = account.Home;
                if (clientHome == null) return false;

                var notification = new Notification
                {
                    Id = 81,
                    MessageEntry = $"Ваш код для привязки в ShuzaBrawl ID: {code}",
                    IsViewed = false
                };

                if (clientHome.NotificationFactory == null)
                {
                    clientHome.NotificationFactory = new NotificationFactory();
                }
                
                clientHome.NotificationFactory.Add(notification);

                Accounts.Save(account);

                if (Sessions.IsSessionActive(accountId))
                {
                    var session = Sessions.GetSession(accountId);
                    if (session?.GameListener != null)
                    {
                        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                        {
                            Message = $"Вам пришёл код подтверждения привязки к аккаунту Telegram!"
                        });
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Ошибка при отправке уведомления: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendSkinRewardNotificationToGame(long accountId, int skinId)
        {
            try
            {
                var account = Accounts.Load(accountId);
                if (account == null) return false;

                var clientHome = account.Home;
                if (clientHome == null) return false;

                var notification = new Notification
                {
                    Id = 94,
                    MessageEntry = "Аккаунт Telegram успешно привязан к боту @shuzabrawl_bot",
                    IsViewed = false,
                    SkinID = skinId
                };

                if (clientHome.NotificationFactory == null)
                {
                    clientHome.NotificationFactory = new NotificationFactory();
                }
                
                clientHome.NotificationFactory.Add(notification);

                Accounts.Save(account);

                if (Sessions.IsSessionActive(accountId))
                {
                    var session = Sessions.GetSession(accountId);
                    if (session?.GameListener != null)
                    {
                        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                        {
                            Message = $"Поздравляем! Вам выдан новый скин."
                        });
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Ошибка при отправке скина: {ex.Message}");
                return false;
            }
        }

        private string GenerateConfirmationCode()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        private bool IsAccountId(string text)
        {
            text = text.ToUpper().TrimStart('#');
            const string Base32 = "0289PYLQGRJCUV";
            return text.Length > 0 && text.All(c => Base32.Contains(c));
        }

        private long TagToId(string tag)
        {
            return LogicLongCodeGenerator.ToId(tag);
        }

        private string GeneratePassToken()
        {
            return Helpers.RandomString(40);
        }
    }
}