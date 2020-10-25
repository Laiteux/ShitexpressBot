using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using ShitexpressBot.Models;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using File = System.IO.File;

namespace ShitexpressBot
{
    public class Program
    {
        public static Settings Settings { get; private set; }
        public static readonly HttpClient HttpClient = new HttpClient();

        private static ITelegramBotClient _bot;
        private static readonly Dictionary<Message, Order> _orders = new Dictionary<Message, Order>();

        public static void Main()
        {
            Settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path.Combine("Files", "Settings.json")));

            _bot = new TelegramBotClient(Settings.Bot.Token);

            _bot.OnMessage += OnMessageAsync;
            _bot.OnCallbackQuery += OnCallbackQueryAsync;
            _bot.OnInlineQuery += OnInlineQueryAsync;

            _bot.StartReceiving();

            Thread.Sleep(-1);
        }

        private static async void OnMessageAsync(object sender, MessageEventArgs e)
        {
            try
            {
                var message = e.Message;

                if (message.ReplyToMessage != null)
                {
                    var order = _orders.SingleOrDefault(o => o.Value.Replies.ContainsKey(message.ReplyToMessage.MessageId)).Value;

                    if (order != null)
                    {
                        await order.HandleReplyAsync(message.ReplyToMessage.MessageId, message.From.Id, message.Text);
                    }
                }
                else if (message.Text != null)
                {
                    if (message.Text == "/start")
                    {
                        if (message.Chat.Type != ChatType.Private)
                        {
                            return;
                        }

                        await _bot.SendTextMessageAsync(message.Chat, File.ReadAllText(Path.Combine("Files", "Start.txt")), ParseMode.Markdown);
                    }
                    else if (message.Text == "/order" || message.Text == $"/order@{Settings.Bot.Username}")
                    {
                        var orderMessage = await _bot.SendTextMessageAsync(message.Chat, "💩");

                        var order = new Order(_bot, orderMessage, message.From.Id);

                        await order.UpdateMessageAsync();

                        _orders.Add(orderMessage, order);
                    }
                }
            }
            catch { }
        }

        private static async void OnCallbackQueryAsync(object sender, CallbackQueryEventArgs e)
        {
            try
            {
                var callbackQuery = e.CallbackQuery;

                var order = _orders.SingleOrDefault(o => o.Key.MessageId == callbackQuery.Message.MessageId).Value;

                if (order != null)
                {
                    await order.HandleCallbackAsync(callbackQuery);
                }
            }
            catch { }
        }

        private static async void OnInlineQueryAsync(object sender, InlineQueryEventArgs e)
        {
            try
            {
                var inlineQuery = e.InlineQuery;

                if (string.IsNullOrEmpty(inlineQuery.Query))
                {
                    return;
                }

                using var responseMessage = await HttpClient.PostAsync("https://www.shitexpress.com/status.php", new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "id", inlineQuery.Query }
                }));

                var contentString = await responseMessage.Content.ReadAsStringAsync();

                var text = new StringBuilder();

                if (contentString == string.Empty)
                {
                    text.AppendLine("Order not found - wrong ID.")
                        .AppendLine("Try again, please.");
                }
                else
                {
                    string[] split = contentString.Split('|');

                    string status = int.Parse(split[1]) switch
                    {
                        0 => "Order received. Your package will be processed in 48 hours.",
                        1 => "Processing the order. Your package will be shipped in 48 hours.",
                        2 => "Your package has been shipped."
                    };

                    text.AppendLine("*Status:* " + status)
                        .AppendLine("*Last update:* " + split[2]);
                }

                await _bot.AnswerInlineQueryAsync(inlineQuery.Id, new[]
                {
                    new InlineQueryResultArticle(inlineQuery.Query, inlineQuery.Query, new InputTextMessageContent(text.ToString())
                    {
                        ParseMode = ParseMode.Markdown
                    }),
                });
            }
            catch { }
        }
    }
}
