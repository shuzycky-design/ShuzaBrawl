// Telegram/VipMarket.cs
// Встроенный VIP-маркет: покупка VIP за Telegram Stars (валюта XTR).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IndusBrawl.Laser.Logic.Message.Account.Auth;
using IndusBrawl.Laser.Logic.Util;
using IndusBrawl.Laser.Server.Database;
using IndusBrawl.Laser.Server.Database.Models;
using IndusBrawl.Laser.Server.Networking.Session;
using IndusBrawl.Laser.Server.Telegram;
using Newtonsoft.Json;

namespace IndusBrawl.Laser.Server.Bot
{
    public class VipMarket
    {
        public class Plan
        {
            public string Key;
            public string Title;
            public int Days;
            public int Stars;
        }

        public class PaymentRecord
        {
            public string ChargeId { get; set; }
            public long TelegramUserId { get; set; }
            public string AccountTag { get; set; }
            public int Days { get; set; }
            public int Stars { get; set; }
            public DateTime PaidAt { get; set; }
            public DateTime VipExpire { get; set; }
            public bool Refunded { get; set; }
        }

        // Тарифы VIP
        public static readonly Plan[] Plans =
        {
            new Plan { Key = "1m", Title = "1 месяц",   Days = 30,  Stars = 100 },
            new Plan { Key = "3m", Title = "3 месяца",  Days = 90,  Stars = 250 },
            new Plan { Key = "6m", Title = "6 месяцев", Days = 180, Stars = 400 },
        };

        private const string PAYMENTS_PATH = "vip_payments.json";
        private const string SUPPORT_CONTACT = "@ShuzaBrawl";

        private readonly TelegramClient _client;
        private readonly TelegramLinkManager _linkManager;
        private readonly HashSet<long> _adminIds;
        private readonly object _lock = new object();
        private List<PaymentRecord> _payments = new List<PaymentRecord>();

        public VipMarket(TelegramClient client, TelegramLinkManager linkManager, HashSet<long> adminIds)
        {
            _client = client;
            _linkManager = linkManager;
            _adminIds = adminIds;
            LoadPayments();
        }

        // ---------- Меню ----------

        public async Task ShowMarketAsync(long chatId, long userId)
        {
            if (!_linkManager.TryGetAccountId(userId, out string tag))
            {
                await SendAsync(chatId,
                    "⭐ <b>VIP-маркет ShuzaBrawl</b>\n\n" +
                    "Сначала привяжите игровой аккаунт: отправьте боту свой ТЭГ (например: #2PP).",
                    new { inline_keyboard = new object[] { new object[] { new { text = "← Назад", callback_data = "menu_main" } } } });
                return;
            }

            string status = "не активен";
            Account account = LoadAccount(tag);
            if (account?.Avatar != null && account.Avatar.HasVIP())
            {
                status = $"активен до {account.Avatar.VIPExpire:dd.MM.yyyy}";
            }

            string text =
                "⭐ <b>VIP-маркет ShuzaBrawl</b>\n" +
                "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬\n\n" +
                "<b>Что даёт VIP:</b>\n" +
                "🏆 x2 кубки за бои\n" +
                $"💎 {IndusBrawl.Laser.Logic.Avatar.ClientAvatar.VipWeeklyGems} гемов каждую неделю\n\n" +
                $"👤 Аккаунт: <code>{Escape(tag)}</code>\n" +
                $"👑 VIP: {status}\n\n" +
                "Оплата звёздами Telegram. Если VIP уже активен, срок продлевается.\n\n" +
                "<b>Выберите срок:</b>";

            var rows = Plans
                .Select(p => new object[] { new { text = $"{p.Title} — {p.Stars} ⭐", callback_data = "vip_buy_" + p.Key } })
                .ToList();
            rows.Add(new object[] { new { text = "← Назад", callback_data = "menu_main" } });

            await SendAsync(chatId, text, new { inline_keyboard = rows.ToArray() });
        }

        public async Task ShowPaySupportAsync(long chatId)
        {
            await SendAsync(chatId,
                "💬 <b>Поддержка по оплате</b>\n\n" +
                $"По вопросам оплаты и возврата звёзд напишите {SUPPORT_CONTACT}.\n" +
                "Укажите свой игровой ТЭГ и дату покупки.", null);
        }

        // ---------- Оплата ----------

        public async Task SendInvoiceAsync(long chatId, long userId, string planKey)
        {
            Plan plan = Plans.FirstOrDefault(p => p.Key == planKey);
            if (plan == null) return;

            if (!_linkManager.TryGetAccountId(userId, out string tag))
            {
                await ShowMarketAsync(chatId, userId);
                return;
            }

            var (ok, body) = await _client.CallAsync("sendInvoice", new
            {
                chat_id = chatId,
                title = $"VIP ShuzaBrawl — {plan.Title}",
                description = $"x2 кубки и {IndusBrawl.Laser.Logic.Avatar.ClientAvatar.VipWeeklyGems} гемов еженедельно на {plan.Days} дней. Аккаунт {tag}.",
                payload = BuildPayload(plan, tag),
                provider_token = "",
                currency = "XTR",
                prices = new object[] { new { label = $"VIP {plan.Title}", amount = plan.Stars } }
            });

            if (!ok)
            {
                Console.WriteLine($"[VipMarket] sendInvoice error: {body}");
                await SendAsync(chatId, "❌ Не удалось создать счёт. Попробуйте позже.", null);
            }
        }

        public async Task HandlePreCheckoutAsync(PreCheckoutQuery query)
        {
            string error = null;

            if (!TryParsePayload(query.invoice_payload, out Plan plan, out string tag))
                error = "Счёт устарел. Откройте VIP-маркет заново.";
            else if (query.currency != "XTR" || query.total_amount != plan.Stars)
                error = "Цена изменилась. Откройте VIP-маркет заново.";
            else if (LoadAccount(tag) == null)
                error = "Игровой аккаунт не найден.";

            var (ok, body) = await _client.CallAsync("answerPreCheckoutQuery", error == null
                ? new { pre_checkout_query_id = query.id, ok = true, error_message = (string)null }
                : new { pre_checkout_query_id = query.id, ok = false, error_message = error });

            if (!ok) Console.WriteLine($"[VipMarket] answerPreCheckoutQuery error: {body}");
        }

        public async Task HandleSuccessfulPaymentAsync(IndusBrawl.Laser.Server.Telegram.Message message)
        {
            var payment = message.successful_payment;
            long chatId = message.chat.id;
            long userId = message.from?.id ?? chatId;

            lock (_lock)
            {
                // Защита от повторной обработки одного платежа
                if (_payments.Any(p => p.ChargeId == payment.telegram_payment_charge_id)) return;
            }

            Account account = null;
            Plan plan = null;
            string tag = null;
            if (TryParsePayload(payment.invoice_payload, out plan, out tag))
            {
                account = LoadAccount(tag);
            }

            if (account?.Avatar == null)
            {
                // VIP выдать некуда - возвращаем звёзды
                Console.WriteLine($"[VipMarket] Платёж {payment.telegram_payment_charge_id}: аккаунт не найден, возврат");
                var (refunded, body) = await _client.CallAsync("refundStarPayment", new
                {
                    user_id = userId,
                    telegram_payment_charge_id = payment.telegram_payment_charge_id
                });
                if (!refunded) Console.WriteLine($"[VipMarket] refundStarPayment error: {body}");

                await SendAsync(chatId, refunded
                    ? "❌ Игровой аккаунт не найден, VIP не выдан. Звёзды возвращены."
                    : $"❌ Игровой аккаунт не найден, VIP не выдан. Напишите {SUPPORT_CONTACT} для возврата звёзд.", null);
                return;
            }

            account.Avatar.ActivateVip(plan.Days);
            int gems = account.Avatar.CollectVipWeeklyGems();
            Accounts.Save(account);

            var record = new PaymentRecord
            {
                ChargeId = payment.telegram_payment_charge_id,
                TelegramUserId = userId,
                AccountTag = tag,
                Days = plan.Days,
                Stars = payment.total_amount,
                PaidAt = DateTime.UtcNow,
                VipExpire = account.Avatar.VIPExpire
            };
            lock (_lock)
            {
                _payments.Add(record);
                SavePayments();
            }

            Console.WriteLine($"[VipMarket] VIP {plan.Days} дн. выдан {tag} (tg {userId}) за {payment.total_amount} XTR, до {account.Avatar.VIPExpire:dd.MM.yyyy}");

            // Онлайн-игрока переподключаем, чтобы изменения применились
            long accountId = account.AccountId;
            if (Sessions.IsSessionActive(accountId))
            {
                var session = Sessions.GetSession(accountId);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = $"VIP активирован до {account.Avatar.VIPExpire:dd.MM.yyyy}!\nx2 кубки и {IndusBrawl.Laser.Logic.Avatar.ClientAvatar.VipWeeklyGems} гемов каждую неделю."
                });
                Sessions.Remove(accountId);
            }

            await SendAsync(chatId,
                "✅ <b>VIP активирован!</b>\n\n" +
                $"👤 Аккаунт: <code>{Escape(tag)}</code>\n" +
                $"📅 Действует до: {account.Avatar.VIPExpire:dd.MM.yyyy}\n" +
                "🏆 x2 кубки за бои\n" +
                $"💎 {IndusBrawl.Laser.Logic.Avatar.ClientAvatar.VipWeeklyGems} гемов каждую неделю" +
                (gems > 0 ? $"\n\n💎 Начислено сейчас: +{gems} гемов" : "") +
                "\n\nСпасибо за поддержку ShuzaBrawl!",
                new { inline_keyboard = new object[] { new object[] { new { text = "← В меню", callback_data = "menu_main" } } } });

            foreach (long adminId in _adminIds)
            {
                if (adminId == userId) continue;
                await SendAsync(adminId, $"⭐ Покупка VIP: {Escape(tag)}, {plan.Title}, {payment.total_amount} ⭐ (tg {userId})", null);
            }
        }

        // ---------- Вспомогательное ----------

        private static string BuildPayload(Plan plan, string tag) => $"vip|{plan.Key}|{tag}";

        private static bool TryParsePayload(string payload, out Plan plan, out string tag)
        {
            plan = null;
            tag = null;
            if (string.IsNullOrEmpty(payload)) return false;

            string[] parts = payload.Split('|');
            if (parts.Length != 3 || parts[0] != "vip") return false;

            string key = parts[1];
            plan = Plans.FirstOrDefault(p => p.Key == key);
            tag = parts[2];
            return plan != null && !string.IsNullOrEmpty(tag);
        }

        private static Account LoadAccount(string tag)
        {
            try
            {
                return Accounts.Load(LogicLongCodeGenerator.ToId(tag));
            }
            catch
            {
                return null;
            }
        }

        private async Task SendAsync(long chatId, string html, object keyboard)
        {
            try
            {
                var (ok, body) = await _client.CallAsync("sendMessage", keyboard != null
                    ? new { chat_id = chatId, text = html, parse_mode = "HTML", reply_markup = keyboard }
                    : (object)new { chat_id = chatId, text = html, parse_mode = "HTML" });
                if (!ok) Console.WriteLine($"[VipMarket] sendMessage error: {body}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VipMarket] sendMessage exception: {ex.Message}");
            }
        }

        private static string Escape(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private void LoadPayments()
        {
            try
            {
                if (File.Exists(PAYMENTS_PATH))
                {
                    _payments = JsonConvert.DeserializeObject<List<PaymentRecord>>(File.ReadAllText(PAYMENTS_PATH)) ?? new List<PaymentRecord>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VipMarket] Ошибка загрузки {PAYMENTS_PATH}: {ex.Message}");
            }
        }

        private void SavePayments()
        {
            try
            {
                File.WriteAllText(PAYMENTS_PATH, JsonConvert.SerializeObject(_payments, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VipMarket] Ошибка сохранения {PAYMENTS_PATH}: {ex.Message}");
            }
        }
    }
}
