// Web/AdminServer.cs
// Админ-панель ShuzaBrawl. Слушает только 127.0.0.1, наружу публикуется через nginx (HTTPS).
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IndusBrawl.Laser.Logic.Avatar;
using IndusBrawl.Laser.Logic.Data;
using IndusBrawl.Laser.Logic.Data.Helper;
using IndusBrawl.Laser.Logic.Home;
using IndusBrawl.Laser.Logic.Home.Items;
using IndusBrawl.Laser.Logic.Message.Account.Auth;
using IndusBrawl.Laser.Logic.Message.Home;
using IndusBrawl.Laser.Logic.Command.Home;
using IndusBrawl.Laser.Logic.Util;
using IndusBrawl.Laser.Server.Database;
using IndusBrawl.Laser.Server.Database.Models;
using IndusBrawl.Laser.Server.Handler;
using IndusBrawl.Laser.Server.Networking.Session;
using IndusBrawl.Laser.Server.Settings;

namespace IndusBrawl.Laser.Server.Web
{
    public class AdminServer
    {
        private const string COOKIE = "sbadm";
        private const int MAX_LOGIN_FAILS = 5;
        private static readonly TimeSpan SESSION_TTL = TimeSpan.FromHours(12);
        private static readonly TimeSpan LOCK_TIME = TimeSpan.FromMinutes(10);
        private static readonly Regex TagRegex = new Regex("^#[0289PYLQGRJCUV]{1,14}$", RegexOptions.Compiled);
        private static readonly Regex OfferIdRegex = new Regex("^[a-z0-9_]{3,64}$", RegexOptions.Compiled);

        // Типы товаров, которые магазин умеет выдавать при покупке
        private static readonly Dictionary<int, string> OfferItems = new Dictionary<int, string>
        {
            { (int)ShopItem.Gems, "Гемы" },
            { (int)ShopItem.Coin, "Монеты" },
            { (int)ShopItem.PowerPoint, "Очки силы" },
            { (int)ShopItem.Bling, "Блинги" },
            { (int)ShopItem.GuaranteedHero, "Боец" },
            { (int)ShopItem.Skin, "Скин" },
            { (int)ShopItem.Emote, "Пин" },
            { (int)ShopItem.PlayerThumbnail, "Аватарка" },
        };

        private readonly ConcurrentDictionary<string, DateTime> _sessions = new ConcurrentDictionary<string, DateTime>();
        private readonly ConcurrentDictionary<string, (int fails, DateTime until)> _loginFails = new ConcurrentDictionary<string, (int, DateTime)>();
        private readonly DateTime _startedAt = DateTime.UtcNow;
        private IHost _host;

        private class ApiError : Exception
        {
            public int Status;
            public ApiError(int status, string message) : base(message) { Status = status; }
        }

        public void Start()
        {
            Configuration config = Configuration.Instance;
            if (string.IsNullOrEmpty(config.AdminLogin) || string.IsNullOrEmpty(config.AdminPassword))
            {
                Console.WriteLine("[ADMIN] admin_login/admin_password не заданы в config.json - админ-панель выключена");
                return;
            }

            int port = config.AdminPort > 0 ? config.AdminPort : 8086;
            string www = Path.Combine(AppContext.BaseDirectory, "admin_www");

            _host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls($"http://127.0.0.1:{port}");
                    webBuilder.Configure(app =>
                    {
                        app.Use(async (context, next) =>
                        {
                            context.Response.Headers["X-Frame-Options"] = "DENY";
                            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                            context.Response.Headers["Referrer-Policy"] = "no-referrer";
                            context.Response.Headers["Cache-Control"] = "no-store";
                            await next();
                        });

                        app.Map("/api", api => api.Run(HandleApi));

                        if (Directory.Exists(www))
                        {
                            var files = new PhysicalFileProvider(www);
                            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
                            app.UseStaticFiles(new StaticFileOptions { FileProvider = files });
                        }
                    });
                })
                .Build();

            _host.Start();
            Console.WriteLine($"[ADMIN] Админ-панель запущена на 127.0.0.1:{port}");
        }

        // ---------- Маршрутизация ----------

        private async Task HandleApi(HttpContext context)
        {
            string path = context.Request.Path.Value?.TrimEnd('/') ?? "";
            string method = context.Request.Method;

            try
            {
                object result;

                if (path == "/login" && method == "POST")
                {
                    result = await Login(context);
                }
                else
                {
                    if (!IsAuthorized(context)) throw new ApiError(401, "Требуется вход");

                    JObject body = method == "POST" ? await ReadBody(context) : null;
                    var query = context.Request.Query;

                    result = (method, path) switch
                    {
                        ("POST", "/logout") => Logout(context),
                        ("GET", "/me") => new { login = Configuration.Instance.AdminLogin },
                        ("GET", "/stats") => Stats(),
                        ("GET", "/players") => Players(query["q"].ToString()),
                        ("GET", "/player") => PlayerInfo(LoadAccount(query["tag"].ToString(), out _)),
                        ("POST", "/player/resources") => PlayerResources(body),
                        ("POST", "/player/vip") => PlayerVip(body),
                        ("POST", "/player/max") => PlayerMax(body),
                        ("POST", "/player/ban") => PlayerBan(body),
                        ("POST", "/player/rename") => PlayerRename(body),
                        ("POST", "/player/kick") => PlayerKick(body),
                        ("POST", "/player/starrdrop") => PlayerStarrDrop(body),
                        ("GET", "/offers") => Offers(),
                        ("POST", "/offers") => OfferCreate(body),
                        ("POST", "/offers/delete") => OfferDelete(body),
                        ("POST", "/offers/toggle") => OfferToggle(body),
                        ("GET", "/catalog") => Catalog(),
                        ("GET", "/mail") => MailLog(),
                        ("POST", "/mail") => MailSend(body),
                        ("GET", "/payments") => Payments(),
                        ("POST", "/maintenance") => Maintenance(body),
                        _ => throw new ApiError(404, "Не найдено")
                    };

                    if (method == "POST")
                    {
                        Console.WriteLine($"[ADMIN] {path} {body?.ToString(Formatting.None)}");
                    }
                }

                await WriteJson(context, 200, result);
            }
            catch (ApiError error)
            {
                await WriteJson(context, error.Status, new { error = error.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ADMIN] Ошибка {path}: {ex.Message}");
                await WriteJson(context, 500, new { error = "Внутренняя ошибка: " + ex.Message });
            }
        }

        private static async Task<JObject> ReadBody(HttpContext context)
        {
            // Только JSON: обычная HTML-форма с чужого сайта такой запрос отправить не может
            if (context.Request.ContentType == null || !context.Request.ContentType.StartsWith("application/json"))
                throw new ApiError(415, "Ожидается application/json");

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            string text = await reader.ReadToEndAsync();
            try
            {
                return string.IsNullOrWhiteSpace(text) ? new JObject() : JObject.Parse(text);
            }
            catch (JsonException)
            {
                throw new ApiError(400, "Некорректный JSON");
            }
        }

        // Свои настройки: глобальные (JsonConvert.DefaultSettings) выбрасывают нули и false
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Include,
            NullValueHandling = NullValueHandling.Include
        };

        private static async Task WriteJson(HttpContext context, int status, object data)
        {
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonConvert.SerializeObject(data, JsonSettings));
        }

        // ---------- Вход ----------

        private static string ClientIp(HttpContext context)
        {
            string forwarded = context.Request.Headers["X-Real-IP"].ToString();
            return string.IsNullOrEmpty(forwarded) ? context.Connection.RemoteIpAddress?.ToString() ?? "?" : forwarded;
        }

        private static bool SafeEquals(string a, string b)
        {
            byte[] x = SHA256.HashData(Encoding.UTF8.GetBytes(a ?? ""));
            byte[] y = SHA256.HashData(Encoding.UTF8.GetBytes(b ?? ""));
            return CryptographicOperations.FixedTimeEquals(x, y);
        }

        private async Task<object> Login(HttpContext context)
        {
            string ip = ClientIp(context);
            if (_loginFails.TryGetValue(ip, out var state) && state.fails >= MAX_LOGIN_FAILS && state.until > DateTime.UtcNow)
                throw new ApiError(429, "Слишком много попыток. Попробуйте позже.");

            JObject body = await ReadBody(context);
            bool ok = SafeEquals((string)body["login"], Configuration.Instance.AdminLogin)
                    & SafeEquals((string)body["password"], Configuration.Instance.AdminPassword);

            if (!ok)
            {
                _loginFails.AddOrUpdate(ip, (1, DateTime.UtcNow + LOCK_TIME),
                    (_, old) => (old.until > DateTime.UtcNow ? old.fails + 1 : 1, DateTime.UtcNow + LOCK_TIME));
                Console.WriteLine($"[ADMIN] Неудачный вход с {ip}");
                throw new ApiError(401, "Неверный логин или пароль");
            }

            _loginFails.TryRemove(ip, out _);

            string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            _sessions[token] = DateTime.UtcNow + SESSION_TTL;
            context.Response.Cookies.Append(COOKIE, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = SESSION_TTL,
                Path = "/"
            });
            Console.WriteLine($"[ADMIN] Вход с {ip}");
            return new { ok = true };
        }

        private bool IsAuthorized(HttpContext context)
        {
            if (!context.Request.Cookies.TryGetValue(COOKIE, out string token) || string.IsNullOrEmpty(token)) return false;
            if (!_sessions.TryGetValue(token, out DateTime expires)) return false;
            if (expires < DateTime.UtcNow)
            {
                _sessions.TryRemove(token, out _);
                return false;
            }
            return true;
        }

        private object Logout(HttpContext context)
        {
            if (context.Request.Cookies.TryGetValue(COOKIE, out string token)) _sessions.TryRemove(token, out _);
            context.Response.Cookies.Delete(COOKIE);
            return new { ok = true };
        }

        // ---------- Общее ----------

        private static Account LoadAccount(string tag, out long id)
        {
            tag = (tag ?? "").Trim().ToUpperInvariant();
            if (!tag.StartsWith("#")) tag = "#" + tag;
            if (!TagRegex.IsMatch(tag)) throw new ApiError(400, "Некорректный тэг");

            id = LogicLongCodeGenerator.ToId(tag);
            // Accounts.Load для несуществующего ID создаёт новый аккаунт - проверяем заранее
            if (!AccountExists(id)) throw new ApiError(404, "Аккаунт не найден");
            Account account = Accounts.Load(id);
            if (account?.Avatar == null || account.Home == null) throw new ApiError(404, "Аккаунт не найден");
            return account;
        }

        private static bool AccountExists(long id)
        {
            if (Database.Cache.AccountCache.IsAccountCached(id)) return true;
            using var connection = new MySqlConnection(Accounts.GetConnectionString());
            connection.Open();
            using var cmd = new MySqlCommand("SELECT 1 FROM accounts WHERE Id = @id", connection);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteScalar() != null;
        }

        private static string TagOf(long id) => LogicLongCodeGenerator.ToCode(id);

        // Сохраняет аккаунт и переподключает игрока, чтобы клиент увидел изменения
        private static void Commit(Account account, string message = "Your account updated!")
        {
            Accounts.Save(account);
            Kick(account.AccountId, message);
        }

        private static bool Kick(long id, string message)
        {
            if (!Sessions.IsSessionActive(id)) return false;
            var session = Sessions.GetSession(id);
            session.GameListener.SendTCPMessage(new AuthenticationFailedMessage() { Message = message });
            Sessions.Remove(id);
            return true;
        }

        private static int Int(JObject body, string name, int min, int max, int fallback = 0)
        {
            JToken token = body?[name];
            if (token == null || token.Type == JTokenType.Null || token.ToString() == "") return fallback;
            if (!int.TryParse(token.ToString(), out int value)) throw new ApiError(400, $"Поле {name}: нужно целое число");
            if (value < min || value > max) throw new ApiError(400, $"Поле {name}: допустимо от {min} до {max}");
            return value;
        }

        private static object PlayerInfo(Account account)
        {
            ClientAvatar avatar = account.Avatar;
            return new
            {
                tag = TagOf(account.AccountId),
                id = account.AccountId,
                name = avatar.Name,
                trophies = avatar.Trophies,
                highestTrophies = avatar.HighestTrophies,
                gems = avatar.Diamonds,
                gold = avatar.Gold,
                powerPoints = avatar.PowerPoints,
                blings = avatar.Blings,
                brawlers = avatar.Heroes?.Count ?? 0,
                skins = account.Home.UnlockedSkins?.Count ?? 0,
                vip = avatar.HasVIP(),
                vipExpire = avatar.HasVIP() ? avatar.VIPExpire : (DateTime?)null,
                banned = avatar.Banned,
                online = Sessions.IsSessionActive(account.AccountId),
                lastOnline = avatar.LastOnline
            };
        }

        // ---------- Сводка ----------

        private object Stats()
        {
            long accounts = 0;
            try
            {
                using var connection = new MySqlConnection(Accounts.GetConnectionString());
                connection.Open();
                using var cmd = new MySqlCommand("SELECT COUNT(*) FROM accounts", connection);
                accounts = Convert.ToInt64(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ADMIN] stats: {ex.Message}");
            }

            var payments = ReadPayments();
            return new
            {
                online = Sessions.Count,
                accounts,
                maintenance = Sessions.Maintenance,
                uptimeMinutes = (int)(DateTime.UtcNow - _startedAt).TotalMinutes,
                serverTime = DateTime.Now,
                vipPurchases = payments.Count,
                vipStars = payments.Sum(p => (int?)p["Stars"] ?? 0),
                offers = CustomOffers.GetAll().Count
            };
        }

        private object Maintenance(JObject body)
        {
            Sessions.Maintenance = (bool?)body["enabled"] ?? false;
            return new { maintenance = Sessions.Maintenance };
        }

        private static List<JObject> ReadPayments()
        {
            try
            {
                if (File.Exists("vip_payments.json"))
                    return JsonConvert.DeserializeObject<List<JObject>>(File.ReadAllText("vip_payments.json")) ?? new List<JObject>();
            }
            catch { }
            return new List<JObject>();
        }

        private object Payments()
        {
            var payments = ReadPayments();
            payments.Reverse();
            return payments.Take(200);
        }

        // ---------- Игроки ----------

        private object Players(string q)
        {
            q = (q ?? "").Trim();
            var list = new List<object>();

            if (q.StartsWith("#"))
            {
                try
                {
                    list.Add(PlayerInfo(LoadAccount(q, out _)));
                }
                catch (ApiError) { }
                return list;
            }

            using var connection = new MySqlConnection(Accounts.GetConnectionString());
            connection.Open();
            using var cmd = new MySqlCommand(
                "SELECT Id, Trophies, JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Avatar.Name')) AS Name FROM accounts " +
                (q.Length > 0 ? "WHERE JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Avatar.Name')) LIKE @q " : "") +
                "ORDER BY Trophies DESC LIMIT 100", connection);
            if (q.Length > 0)
            {
                string escaped = q.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
                cmd.Parameters.AddWithValue("@q", "%" + escaped + "%");
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                long id = reader.GetInt64(0);
                list.Add(new
                {
                    tag = TagOf(id),
                    name = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    trophies = reader.GetInt32(1),
                    online = Sessions.IsSessionActive(id)
                });
            }
            return list;
        }

        private object PlayerResources(JObject body)
        {
            Account account = LoadAccount((string)body["tag"], out _);
            ClientAvatar avatar = account.Avatar;
            bool set = (string)body["mode"] == "set";
            const int MAX = 100_000_000;

            int Apply(int current, string field)
            {
                if (body[field] == null || body[field].ToString() == "") return current;
                int value = Int(body, field, set ? 0 : -MAX, MAX);
                long result = set ? value : (long)current + value;
                return (int)Math.Clamp(result, 0, MAX);
            }

            avatar.Diamonds = Apply(avatar.Diamonds, "gems");
            avatar.Gold = Apply(avatar.Gold, "gold");
            avatar.PowerPoints = Apply(avatar.PowerPoints, "powerPoints");
            avatar.Blings = Apply(avatar.Blings, "blings");

            Commit(account);
            return PlayerInfo(account);
        }

        private object PlayerVip(JObject body)
        {
            Account account = LoadAccount((string)body["tag"], out _);
            ClientAvatar avatar = account.Avatar;
            int days = Int(body, "days", 0, 3650);

            if (days == 0)
            {
                avatar.IsVIP = false;
                avatar.VIPExpire = DateTime.MinValue;
                avatar.IsPremium = false;
                avatar.HasDoubleTrophyVIP = false;
                avatar.DoubleTrophyVIPEndTime = DateTime.MinValue;
                Commit(account);
            }
            else
            {
                avatar.ActivateVip(days);
                avatar.CollectVipWeeklyGems();
                Commit(account, $"VIP активирован до {avatar.VIPExpire:dd.MM.yyyy}!");
            }
            return PlayerInfo(account);
        }

        private object PlayerMax(JObject body)
        {
            Account account = LoadAccount((string)body["tag"], out long id);
            CmdHandler.HandleCmd("/maxaccount " + TagOf(id));
            return PlayerInfo(account);
        }

        private object PlayerBan(JObject body)
        {
            Account account = LoadAccount((string)body["tag"], out _);
            bool banned = (bool?)body["banned"] ?? false;

            account.Avatar.Banned = banned;
            if (!banned)
            {
                account.Avatar.BanID = 0;
                account.Avatar.BanEndTime = DateTime.MinValue;
            }
            Commit(account, banned ? "Аккаунт заблокирован." : "Your account updated!");
            return PlayerInfo(account);
        }

        private object PlayerRename(JObject body)
        {
            Account account = LoadAccount((string)body["tag"], out _);
            string name = ((string)body["name"] ?? "").Trim();
            if (name.Length < 2 || name.Length > 15) throw new ApiError(400, "Имя: от 2 до 15 символов");

            account.Avatar.Name = name;
            Commit(account);
            return PlayerInfo(account);
        }

        private object PlayerKick(JObject body)
        {
            LoadAccount((string)body["tag"], out long id);
            return new { kicked = Kick(id, "Вы были отключены администратором.") };
        }

        // Стардропы выдаются только игроку в сети: награда приходит ему сразу, как при покупке в магазине
        private object PlayerStarrDrop(JObject body)
        {
            Account account = LoadAccount((string)body["tag"], out long id);
            int count = Int(body, "count", 1, 10, 1);
            if (!Sessions.IsSessionActive(id)) throw new ApiError(409, "Игрок не в сети. Стардроп можно выдать только тому, кто сейчас в игре.");

            var session = Sessions.GetSession(id);
            for (int i = 0; i < count; i++) session.Home.Home.GiveStarrDrop();
            Accounts.Save(account);
            return new { given = count };
        }

        // ---------- Акции магазина ----------

        private object Offers()
        {
            DateTime now = DateTime.Now;
            return new
            {
                serverTime = now,
                items = OfferItems.Select(i => new { id = i.Key, name = i.Value }),
                offers = CustomOffers.GetAll().OrderByDescending(o => o.Start).Select(o => new
                {
                    o.Id, o.Title,
                    o.Cost, o.OldCost, o.Currency, o.Background, o.Start, o.End, o.Enabled,
                    items = o.GetItems().Select(i => new { i.Item, i.Count, i.BrawlerId, i.ExtraId }),
                    status = !o.Enabled ? "off" : o.End <= now ? "ended" : o.Start > now ? "planned" : "active"
                })
            };
        }

        private const int MAX_OFFER_ITEMS = 5;

        private object OfferCreate(JObject body)
        {
            string title = ((string)body["title"] ?? "").Trim();
            if (title.Length == 0 || title.Length > 40) throw new ApiError(400, "Название: от 1 до 40 символов");

            int currency = Int(body, "currency", 0, 6);
            if (currency != 0 && currency != 1 && currency != 6) throw new ApiError(400, "Валюта: гемы, монеты или блинги");

            if (!DateTime.TryParse((string)body["start"], out DateTime start)) start = DateTime.Now;
            if (!DateTime.TryParse((string)body["end"], out DateTime end)) throw new ApiError(400, "Укажите дату окончания");
            if (end <= start) throw new ApiError(400, "Окончание должно быть позже начала");

            var rawItems = body["items"] as JArray;
            if (rawItems == null || rawItems.Count == 0) throw new ApiError(400, "Добавьте хотя бы один товар");
            if (rawItems.Count > MAX_OFFER_ITEMS) throw new ApiError(400, $"В наборе не больше {MAX_OFFER_ITEMS} товаров");

            var items = new List<CustomOfferItem>();
            foreach (JToken raw in rawItems)
            {
                var row = raw as JObject ?? throw new ApiError(400, "Некорректный товар");
                int item = Int(row, "item", 0, 100, -1);
                if (!OfferItems.ContainsKey(item)) throw new ApiError(400, "Неизвестный тип товара");

                var entry = new CustomOfferItem { Item = item, Count = 1 };
                int target = Int(row, "target", 0, 100_000);
                switch ((ShopItem)item)
                {
                    case ShopItem.GuaranteedHero:
                        RequireData(DataType.Character, target, "Боец");
                        entry.BrawlerId = target;
                        break;
                    case ShopItem.Skin:
                        RequireData(DataType.Skin, target, "Скин");
                        entry.BrawlerId = 999;
                        entry.ExtraId = target;
                        break;
                    case ShopItem.Emote:
                        RequireData(DataType.Emote, target, "Пин");
                        entry.ExtraId = target;
                        break;
                    case ShopItem.PlayerThumbnail:
                        RequireData(DataType.PlayerThumbnail, target, "Аватарка");
                        entry.ExtraId = target;
                        break;
                    default:
                        entry.Count = Int(row, "count", 1, 10_000_000, 0);
                        if (entry.Count < 1) throw new ApiError(400, "Укажите количество");
                        break;
                }
                items.Add(entry);
            }

            var offer = new CustomOffer
            {
                Title = title,
                Items = items,
                Item = items[0].Item,
                Count = items[0].Count,
                BrawlerId = items[0].BrawlerId,
                ExtraId = items[0].ExtraId,
                Cost = Int(body, "cost", 0, 10_000_000),
                OldCost = Int(body, "oldCost", 0, 10_000_000),
                Currency = currency,
                Background = ((string)body["background"] ?? "").Trim(),
                Start = start,
                End = end,
                Enabled = true
            };
            if (offer.Background.Length == 0) offer.Background = "offer_bgr_retro2023";
            if (!Regex.IsMatch(offer.Background, "^[A-Za-z0-9_]{1,64}$")) throw new ApiError(400, "Некорректный фон");

            string id = ((string)body["id"] ?? "").Trim().ToLowerInvariant();
            if (id.Length == 0) id = "adm_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(3)).ToLowerInvariant();
            if (!OfferIdRegex.IsMatch(id)) throw new ApiError(400, "ID акции: латиница в нижнем регистре, цифры и _, от 3 до 64 символов");
            offer.Id = id;

            if (!CustomOffers.Add(offer)) throw new ApiError(409, "Акция с таким ID уже есть");
            return Offers();
        }

        private static void RequireData(DataType type, int instanceId, string what)
        {
            var datas = DataTables.Get(type).GetDatas();
            if (instanceId < 0 || instanceId >= datas.Count) throw new ApiError(400, $"{what} с номером {instanceId} не существует");
        }

        private object OfferDelete(JObject body)
        {
            if (!CustomOffers.Remove((string)body["id"])) throw new ApiError(404, "Акция не найдена");
            return Offers();
        }

        private object OfferToggle(JObject body)
        {
            if (!CustomOffers.SetEnabled((string)body["id"], (bool?)body["enabled"] ?? false)) throw new ApiError(404, "Акция не найдена");
            return Offers();
        }

        // Справочники для выпадающих списков
        private object Catalog()
        {
            var characters = DataTables.Get(DataType.Character).GetDatas();
            var confs = DataTables.Get(DataType.SkinConf).GetDatas().OfType<SkinConfData>()
                .Where(c => c.Name != null).GroupBy(c => c.Name).ToDictionary(g => g.Key, g => g.First().Character);
            var characterIds = new Dictionary<string, int>();
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i].GetName() != null) characterIds[characters[i].GetName()] = i;
            }

            var skins = new List<object>();
            var skinDatas = DataTables.Get(DataType.Skin).GetDatas();
            for (int i = 0; i < skinDatas.Count; i++)
            {
                if (!(skinDatas[i] is SkinData skin) || skin.Disabled) continue;
                int brawlerId = -1;
                if (skin.Conf != null && confs.TryGetValue(skin.Conf, out string character) && character != null)
                {
                    characterIds.TryGetValue(character, out brawlerId);
                    if (!characterIds.ContainsKey(character)) brawlerId = -1;
                }
                skins.Add(new { id = i, name = skin.Name, brawlerId });
            }

            IEnumerable<object> List(DataType type, Func<LogicData, bool> filter = null)
            {
                var datas = DataTables.Get(type).GetDatas();
                for (int i = 0; i < datas.Count; i++)
                {
                    if (filter != null && !filter(datas[i])) continue;
                    yield return new { id = i, name = datas[i].GetName() };
                }
            }

            return new
            {
                brawlers = List(DataType.Character, d => d is CharacterData c && c.IsHero() && !c.Disabled).ToList(),
                skins,
                pins = List(DataType.Emote, d => d is EmoteData e && !e.Disabled).ToList(),
                icons = List(DataType.PlayerThumbnail).ToList()
            };
        }

        // ---------- Рассылка на внутриигровую почту ----------

        private const string MAIL_LOG_PATH = "mail_log.json";
        private const int MAX_MAIL_REWARDS = 10;
        private static readonly object MailLock = new object();

        // Ресурсы для уведомления 90: 1 - монеты, 22 - очки силы, 23 - блинги
        private static readonly Dictionary<string, (string name, int resourceId)> MailResources = new Dictionary<string, (string, int)>
        {
            { "gold", ("монет", 1) },
            { "powerPoints", ("очков силы", 22) },
            { "blings", ("блингов", 23) },
        };

        private class MailReward
        {
            public string Type;
            public int Count;
            public int SkinId;
        }

        private static List<JObject> ReadMailLog()
        {
            try
            {
                if (File.Exists(MAIL_LOG_PATH))
                    return JsonConvert.DeserializeObject<List<JObject>>(File.ReadAllText(MAIL_LOG_PATH)) ?? new List<JObject>();
            }
            catch { }
            return new List<JObject>();
        }

        private object MailLog()
        {
            var log = ReadMailLog();
            log.Reverse();
            return log.Take(100);
        }

        private object MailSend(JObject body)
        {
            string text = ((string)body["text"] ?? "").Trim();
            if (text.Length > 200) throw new ApiError(400, "Текст: не больше 200 символов");

            var rewards = new List<MailReward>();
            foreach (JToken raw in body["rewards"] as JArray ?? new JArray())
            {
                var row = raw as JObject ?? throw new ApiError(400, "Некорректная награда");
                string type = (string)row["type"];
                if (type == "skin")
                {
                    int skinId = Int(row, "target", 1, 100_000);
                    RequireData(DataType.Skin, skinId, "Скин");
                    rewards.Add(new MailReward { Type = type, SkinId = skinId, Count = 1 });
                }
                else if (type == "gems" || MailResources.ContainsKey(type ?? ""))
                {
                    int count = Int(row, "count", 1, 1_000_000, 0);
                    if (count < 1) throw new ApiError(400, "Укажите количество");
                    rewards.Add(new MailReward { Type = type, Count = count });
                }
                else
                {
                    throw new ApiError(400, "Неизвестный тип награды");
                }
            }
            if (rewards.Count > MAX_MAIL_REWARDS) throw new ApiError(400, $"Не больше {MAX_MAIL_REWARDS} наград в одном письме");
            if (rewards.Count == 0 && text.Length == 0) throw new ApiError(400, "Письмо пустое: добавьте текст или награду");
            if (text.Length == 0) text = "Подарок от ShuzaBrawl!";

            string to = ((string)body["to"] ?? "").Trim();
            List<long> ids;
            string target;
            if (to == "all")
            {
                ids = Accounts.GetAllAccountIdsFromDatabase();
                target = "все игроки";
            }
            else
            {
                LoadAccount(to, out long id);
                ids = new List<long> { id };
                target = TagOf(id);
            }

            int sent = 0;
            lock (MailLock)
            {
                foreach (long id in ids)
                {
                    try
                    {
                        if (DeliverMail(id, text, rewards)) sent++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ADMIN] mail {id}: {ex.Message}");
                    }
                }

                var log = ReadMailLog();
                log.Add(JObject.FromObject(new
                {
                    SentAt = DateTime.UtcNow,
                    To = target,
                    Text = text,
                    Recipients = sent,
                    Rewards = rewards.Select(r => new { r.Type, r.Count, r.SkinId })
                }, JsonSerializer.Create(JsonSettings)));
                File.WriteAllText(MAIL_LOG_PATH, JsonConvert.SerializeObject(log, Formatting.Indented, JsonSettings));
            }

            return new { sent, total = ids.Count };
        }

        // Кладёт письмо в почту одного аккаунта. Каждая награда - отдельное письмо с кнопкой получения
        private static bool DeliverMail(long id, string text, List<MailReward> rewards)
        {
            Account account = Accounts.Load(id);
            if (account?.Home == null || account.Avatar == null) return false;

            var notifications = new List<Notification>();
            foreach (MailReward reward in rewards)
            {
                if (reward.Type == "gems")
                {
                    notifications.Add(new Notification { Id = 89, MessageEntry = text, DonationCount = reward.Count });
                }
                else if (reward.Type == "skin")
                {
                    // Скин, который уже есть, повторно не отправляем
                    if (account.Home.UnlockedSkins != null && account.Home.UnlockedSkins.Contains(29000000 + reward.SkinId)) continue;
                    notifications.Add(new Notification { Id = 94, MessageEntry = text, SkinID = reward.SkinId });
                }
                else
                {
                    var resource = MailResources[reward.Type];
                    notifications.Add(new Notification
                    {
                        Id = 90,
                        MessageEntry = $"{text}\n+{reward.Count} {resource.name}",
                        ResourceID = resource.resourceId,
                        ResourceCount = reward.Count
                    });
                }
            }

            if (rewards.Count == 0) notifications.Add(new Notification { Id = 81, MessageEntry = text });
            if (notifications.Count == 0) return false;

            if (account.Home.NotificationFactory == null) account.Home.NotificationFactory = new NotificationFactory();
            foreach (Notification notification in notifications) account.Home.NotificationFactory.Add(notification);
            Accounts.Save(account);

            // Игроку в сети письмо приходит сразу, без переподключения
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                foreach (Notification notification in notifications)
                {
                    session.GameListener.SendTCPMessage(new AvailableServerCommandMessage
                    {
                        Command = new LogicAddNotificationRealCommand { Notification = notification }
                    });
                }
            }
            return true;
        }
    }
}
