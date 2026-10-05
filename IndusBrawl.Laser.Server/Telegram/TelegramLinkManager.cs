using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace IndusBrawl.Laser.Server.Bot
{
    public class TelegramLinkManager
    {
        public class LinkData
        {
            [JsonProperty]
            public ConcurrentDictionary<long, string> TelegramToAccountId { get; set; } = new ConcurrentDictionary<long, string>();

            [JsonProperty]
            public ConcurrentDictionary<string, long> AccountIdToTelegram { get; set; } = new ConcurrentDictionary<string, long>();
        }

        private readonly string _filePath;
        private LinkData _data;

        public TelegramLinkManager(string filePath = "./TelegramLinks.json")
        {
            _filePath = filePath;
            Load();
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    _data = JsonConvert.DeserializeObject<LinkData>(json) ?? new LinkData();
                }
                else
                {
                    _data = new LinkData();
                    Save();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramLinkManager] Ошибка при загрузке: {ex.Message}");
                _data = new LinkData();
            }
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
                string json = JsonConvert.SerializeObject(_data, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramLinkManager] Ошибка при сохранении: {ex.Message}");
            }
        }

        public bool LinkAccount(long telegramUserId, string accountId)
        {
            if (_data.TelegramToAccountId.ContainsKey(telegramUserId) || 
                _data.AccountIdToTelegram.ContainsKey(accountId))
            {
                return false;
            }

            _data.TelegramToAccountId[telegramUserId] = accountId;
            _data.AccountIdToTelegram[accountId] = telegramUserId;
            Save();
            return true;
        }

        public bool UnlinkAccount(long telegramUserId, string accountId)
        {
            if (_data.TelegramToAccountId.TryRemove(telegramUserId, out _) &&
                _data.AccountIdToTelegram.TryRemove(accountId, out _))
            {
                Save();
                return true;
            }
            return false;
        }

        public bool TryGetAccountId(long telegramUserId, out string accountId)
        {
            return _data.TelegramToAccountId.TryGetValue(telegramUserId, out accountId);
        }

        public bool TryGetTelegramUserId(string accountId, out long telegramUserId)
        {
            return _data.AccountIdToTelegram.TryGetValue(accountId, out telegramUserId);
        }

        public bool IsTelegramLinked(long telegramUserId)
        {
            return _data.TelegramToAccountId.ContainsKey(telegramUserId);
        }

        public bool IsAccountLinked(string accountId)
        {
            return _data.AccountIdToTelegram.ContainsKey(accountId);
        }

        public List<string> GetAllLinkedAccounts(long telegramUserId)
        {
            return _data.TelegramToAccountId
                .Where(x => x.Key == telegramUserId)
                .Select(x => x.Value)
                .ToList();
        }

        public void ForceReload()
        {
            try
            {
                Console.WriteLine($"[TelegramLinkManager] Принудительная перезагрузка данных...");
                Load();
                Console.WriteLine($"[TelegramLinkManager] Данные перезагружены");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramLinkManager] Ошибка при перезагрузке: {ex.Message}");
            }
        }

        public int GetLinkedAccountsCount(long telegramUserId)
        {
            return _data.TelegramToAccountId.Count(x => x.Key == telegramUserId);
        }

        public bool IsAccountOwnedByUser(long telegramUserId, string accountId)
        {
            // Проверяем все аккаунты пользователя
            var userAccounts = GetAllLinkedAccounts(telegramUserId);
            return userAccounts.Contains(accountId);
        }

        public void UnlinkAllAccounts(long telegramUserId)
        {
            var accounts = GetAllLinkedAccounts(telegramUserId);
            foreach (var account in accounts)
            {
                _data.AccountIdToTelegram.TryRemove(account, out _);
            }
            _data.TelegramToAccountId.TryRemove(telegramUserId, out _);
            Save();
        }

        // НОВЫЙ МЕТОД для получения Telegram ID по тегу аккаунта
        public long? GetTelegramIdByAccountTag(string accountTag)
        {
            try
            {
                // Прямой поиск в словаре
                if (_data.AccountIdToTelegram.TryGetValue(accountTag, out long telegramId))
                {
                    return telegramId;
                }
                
                Console.WriteLine($"[TelegramLinkManager] Не найден Telegram ID для тега: {accountTag}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramLinkManager] Ошибка получения Telegram ID: {ex.Message}");
                return null;
            }
        }

        // НОВЫЙ МЕТОД для получения всех аккаунтов пользователя (альтернативная версия)
        public Dictionary<string, long> GetAllUserAccountsWithIds(long telegramUserId)
        {
            var result = new Dictionary<string, long>();
            
            foreach (var kvp in _data.AccountIdToTelegram)
            {
                if (kvp.Value == telegramUserId)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            
            return result;
        }

        // НОВЫЙ МЕТОД для быстрой проверки владения несколькими аккаунтами
        public bool AreAccountsOwnedBySameUser(string accountTag1, string accountTag2)
        {
            if (_data.AccountIdToTelegram.TryGetValue(accountTag1, out long telegramId1) &&
                _data.AccountIdToTelegram.TryGetValue(accountTag2, out long telegramId2))
            {
                return telegramId1 == telegramId2;
            }
            return false;
        }

        // НОВЫЙ МЕТОД для получения статистики
        public void PrintStatistics()
        {
            try
            {
                int totalLinks = _data.AccountIdToTelegram.Count;
                int uniqueUsers = _data.TelegramToAccountId.Keys.Distinct().Count();
                
                Console.WriteLine($"[TelegramLinkManager] Статистика:");
                Console.WriteLine($"  Всего привязок: {totalLinks}");
                Console.WriteLine($"  Уникальных пользователей: {uniqueUsers}");
                Console.WriteLine($"  Среднее аккаунтов на пользователя: {(totalLinks > 0 ? (double)totalLinks / uniqueUsers : 0):F2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramLinkManager] Ошибка статистики: {ex.Message}");
            }
        }

        // НОВЫЙ МЕТОД для резервного копирования
        public void CreateBackup()
        {
            try
            {
                string backupPath = $"{_filePath}.backup.{DateTime.UtcNow:yyyyMMddHHmmss}";
                string json = JsonConvert.SerializeObject(_data, Formatting.Indented);
                File.WriteAllText(backupPath, json);
                Console.WriteLine($"[TelegramLinkManager] Резервная копия создана: {backupPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramLinkManager] Ошибка создания резервной копии: {ex.Message}");
            }
        }

        // НОВЫЙ МЕТОД для очистки устаревших записей (опционально)
        public void CleanupOrphanedEntries()
        {
            try
            {
                int removed = 0;
                var orphanedAccounts = new List<string>();
                
                // Находим аккаунты без Telegram
                foreach (var account in _data.AccountIdToTelegram.Keys.ToList())
                {
                    if (!_data.AccountIdToTelegram.TryGetValue(account, out long telegramId) ||
                        !_data.TelegramToAccountId.ContainsKey(telegramId))
                    {
                        orphanedAccounts.Add(account);
                    }
                }
                
                // Удаляем их
                foreach (var account in orphanedAccounts)
                {
                    if (_data.AccountIdToTelegram.TryRemove(account, out _))
                    {
                        removed++;
                    }
                }
                
                if (removed > 0)
                {
                    Save();
                    Console.WriteLine($"[TelegramLinkManager] Очищено {removed} устаревших записей");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramLinkManager] Ошибка очистки: {ex.Message}");
            }
        }
    }
}