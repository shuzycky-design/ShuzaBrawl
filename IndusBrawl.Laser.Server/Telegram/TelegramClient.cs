// Telegram/TelegramClient.cs
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json;

namespace IndusBrawl.Laser.Server.Telegram
{
    public class TelegramClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _botToken;
        private readonly string _baseUrl;

        public TelegramClient(string botToken)
        {
            _botToken = botToken;
            _baseUrl = $"https://api.telegram.org/bot{_botToken}/";
            _httpClient = new HttpClient();
        }

        // Перечисление для режима парсинга
        public enum ParseMode
        {
            None,
            MarkdownV2,
            Html
        }

        // Отправка текстового сообщения с поддержкой MarkdownV2
        public async Task SendMessageAsync(long chatId, string text, ParseMode parseMode = ParseMode.None)
        {
            // Экранируем текст, если используется MarkdownV2
            if (parseMode == ParseMode.MarkdownV2)
            {
                text = EscapeMarkdownV2(text);
            }

            var request = new
            {
                chat_id = chatId,
                text = text,
                parse_mode = parseMode switch
                {
                    ParseMode.MarkdownV2 => "MarkdownV2",
                    ParseMode.Html => "HTML",
                    _ => (string)null
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}sendMessage", content);
            response.EnsureSuccessStatusCode();
        }

        // Отправка сообщения с клавиатурой (кнопками)
        public async Task SendMessageWithKeyboardAsync(long chatId, string text, string keyboardJson, ParseMode parseMode = ParseMode.None)
        {
            // Экранируем текст, если используется MarkdownV2
            if (parseMode == ParseMode.MarkdownV2)
            {
                text = EscapeMarkdownV2(text);
            }

            object request;
            
            if (!string.IsNullOrEmpty(keyboardJson))
            {
                var replyMarkup = JsonDocument.Parse(keyboardJson).RootElement;
                request = new
                {
                    chat_id = chatId,
                    text = text,
                    parse_mode = parseMode switch
                    {
                        ParseMode.MarkdownV2 => "MarkdownV2",
                        ParseMode.Html => "HTML",
                        _ => (string)null
                    },
                    reply_markup = replyMarkup
                };
            }
            else
            {
                request = new
                {
                    chat_id = chatId,
                    text = text,
                    parse_mode = parseMode switch
                    {
                        ParseMode.MarkdownV2 => "MarkdownV2",
                        ParseMode.Html => "HTML",
                        _ => (string)null
                    }
                };
            }

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}sendMessage", content);
            response.EnsureSuccessStatusCode();
        }

        // Редактирование разметки сообщения (удаление/изменение кнопок)
        public async Task EditMessageReplyMarkupAsync(long chatId, int messageId, string keyboardJson)
        {
            object request;
            
            if (!string.IsNullOrEmpty(keyboardJson))
            {
                var replyMarkup = JsonDocument.Parse(keyboardJson).RootElement;
                request = new
                {
                    chat_id = chatId,
                    message_id = messageId,
                    reply_markup = replyMarkup
                };
            }
            else
            {
                request = new
                {
                    chat_id = chatId,
                    message_id = messageId
                };
            }

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}editMessageReplyMarkup", content);
            response.EnsureSuccessStatusCode();
        }

        // Получение обновлений (polling)
        public async Task<Update[]> GetUpdatesAsync(int offset = 0, int limit = 100, int timeout = 0)
        {
            var url = $"{_baseUrl}getUpdates?offset={offset}&limit={limit}&timeout={timeout}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("result");
            return JsonSerializer.Deserialize<Update[]>(data.GetRawText()) ?? new Update[0];
        }

        // Произвольный вызов метода Bot API. Возвращает (успех, тело ответа)
        public async Task<(bool ok, string body)> CallAsync(string method, object request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}{method}", content);
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body);
        }

        // Ответ на callback-запрос
        public async Task AnswerCallbackQueryAsync(string callbackQueryId, string text = null)
        {
            var request = new
            {
                callback_query_id = callbackQueryId,
                text = text
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}answerCallbackQuery", content);
            response.EnsureSuccessStatusCode();
        }

        // Метод для экранирования текста под MarkdownV2
        private string EscapeMarkdownV2(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            return text
                .Replace(@"\", @"\\")
                .Replace("_", @"\_")
                .Replace("[", @"\[")
                .Replace("]", @"\]")
                .Replace("(", @"\(")
                .Replace(")", @"\)")
                .Replace("~", @"\~")
                .Replace(">", @"\>")
                .Replace("#", @"\#")
                .Replace("+", @"\+")
                .Replace("-", @"\-")
                .Replace("=", @"\=")
                .Replace("|", @"\|")
                .Replace("{", @"\{")
                .Replace("}", @"\}")
                .Replace(".", @"\.")
                .Replace("!", @"\!");
        }
    }

    // Простая модель для Update
    public class Update
    {
        public int update_id { get; set; }
        public Message message { get; set; }
        public CallbackQuery callback_query { get; set; }
        public PreCheckoutQuery pre_checkout_query { get; set; }
    }

    public class PreCheckoutQuery
    {
        public string id { get; set; }
        public User from { get; set; }
        public string currency { get; set; }
        public int total_amount { get; set; }
        public string invoice_payload { get; set; }
    }

    public class SuccessfulPayment
    {
        public string currency { get; set; }
        public int total_amount { get; set; }
        public string invoice_payload { get; set; }
        public string telegram_payment_charge_id { get; set; }
    }

    public class Message
    {
        public int message_id { get; set; }
        public User from { get; set; }
        public Chat chat { get; set; }
        public string text { get; set; }
        public string type { get; set; }
        public SuccessfulPayment successful_payment { get; set; }
    }

    public class User
    {
        public long id { get; set; }
        public string first_name { get; set; }
        public string username { get; set; }
    }

    public class Chat
    {
        public long id { get; set; }
        public string type { get; set; }
    }

    public class CallbackQuery
    {
        public string id { get; set; }
        public User from { get; set; }
        public Message message { get; set; }
        public string data { get; set; }
    }
}