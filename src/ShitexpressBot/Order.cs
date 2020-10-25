using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ShitexpressBot
{
    public class Order
    {
        private readonly ITelegramBotClient _bot;
        private readonly Message _orderMessage;
        private readonly int _userId;

        public Order(ITelegramBotClient bot, Message orderMessage, int userId)
        {
            _bot = bot;
            _orderMessage = orderMessage;
            _userId = userId;
        }

        public Dictionary<int, string> Replies { get; } = new Dictionary<int, string>();

        public KeyValuePair<string, string> Animal { get; set; } = new KeyValuePair<string, string>("🐴", "horse");
        public KeyValuePair<string, string> Sticker { get; set; } = new KeyValuePair<string, string>("None", "1");
        public string RecipientFullName { get; set; }
        public string StreetAddress { get; set; }
        public string TownCity { get; set; }
        public string Postcode { get; set; }
        public string StateCounty { get; set; }
        public KeyValuePair<string, string> Country { get; set; }
        public string Message { get; set; }
        public string Email { get; set; }

        private static readonly InlineKeyboardMarkup _menuReplyMarkup = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Edit animal", "edit_animal"),
                InlineKeyboardButton.WithCallbackData("Edit sticker", "edit_sticker")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Edit recipient's full name", "edit_recipient_full_name"),
                InlineKeyboardButton.WithCallbackData("Edit street address", "edit_street_address")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Edit town / city", "edit_town_city"),
                InlineKeyboardButton.WithCallbackData("Edit postcode", "edit_postcode")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Edit state / county", "edit_state_county"),
                InlineKeyboardButton.WithCallbackData("Edit country", "edit_country")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Edit message for the recipient", "edit_message"),
                InlineKeyboardButton.WithCallbackData("Edit your email address", "edit_email")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🚫 Cancel order", "cancel_order"),
                InlineKeyboardButton.WithCallbackData("✅ Place order", "place_order")
            }
        });

        public async Task UpdateMessageAsync(InlineKeyboardMarkup replyMarkup = null)
        {
            var text = new StringBuilder()
                .AppendLine("Let's send a smelly surprise!")
                .AppendLine()
                .AppendLine("*Animal* » " + Animal.Key)
                .AppendLine("*Sticker* » " + Sticker.Key)
                .AppendLine()
                .AppendLine("*Recipient's full name* » " + RecipientFullName)
                .AppendLine("*Street address* » " + StreetAddress)
                .AppendLine("*Town / City* » " + TownCity)
                .AppendLine("*Postcode* » " + Postcode)
                .AppendLine("*State / County (optional)* » " + StateCounty)
                .AppendLine("*Country* » " + Country.Key)
                .AppendLine("*Message for the recipient (optional)* » " + Message)
                .AppendLine()
                .AppendLine("*Your email address (optional - notifications only)* » " + Email);

            await _bot.EditMessageTextAsync(_orderMessage.Chat, _orderMessage.MessageId, text.ToString(), ParseMode.Markdown, replyMarkup: replyMarkup ?? _menuReplyMarkup);
        }

        public async Task HandleCallbackAsync(CallbackQuery callbackQuery)
        {
            if (callbackQuery.From.Id != _userId)
            {
                return;
            }

            if (callbackQuery.Data.StartsWith("set_animal"))
            {
                await SetAnimalAsync(callbackQuery.Data.Split('_', 3).Last());
            }
            else if (callbackQuery.Data.StartsWith("set_sticker"))
            {
                await SetStickerAsync(callbackQuery.Data.Split('_', 3).Last());
            }
            else
            {
                Func<string, Task> callbackFunction = callbackQuery.Data switch
                {
                    "edit_animal" => EditAnimalAsync,
                    "edit_sticker" => EditStickerAsync,
                    "cancel_order" => CancelOrderAsync,
                    "place_order" => PlaceOrderAsync,
                    _ => null
                };

                if (callbackFunction != null)
                {
                    await callbackFunction.Invoke(callbackQuery.Id);
                }
                else
                {
                    await EditPropertyAsync(callbackQuery.Id, callbackQuery.Data.Split('_', 2).Last());
                }
            }
        }

        public async Task HandleReplyAsync(int messageId, int userId, string text)
        {
            if (userId != _userId)
            {
                return;
            }

            string property = Replies[messageId];

            if (property == "Country")
            {
                text = text.ToUpper();

                if (!Program.Settings.Countries.TryGetValue(text, out string country))
                {
                    await _bot.SendTextMessageAsync(_orderMessage.Chat, "Country code not found.");

                    return;
                }

                Country = new KeyValuePair<string, string>(country, text);
            }
            else if (property == "Message")
            {
                if (text.Length > 300)
                {
                    await _bot.SendTextMessageAsync(_orderMessage.Chat, "Message for the recipient cannot exceed 300 characters.");

                    return;
                }

                Message = text;
            }
            else if (property == "Email")
            {
                if (!new EmailAddressAttribute().IsValid(text))
                {
                    await _bot.SendTextMessageAsync(_orderMessage.Chat, "Invalid email address.");

                    return;
                }

                Email = text;
            }
            else
            {
                GetType().GetProperty(property).SetValue(this, text);
            }

            await UpdateMessageAsync();
        }

        private async Task EditAnimalAsync(string callbackQueryId)
        {
            var animals = Program.Settings.Animals.Select(a => InlineKeyboardButton.WithCallbackData(a.Key, $"set_animal_{a.Value}"));

            await UpdateMessageAsync(new InlineKeyboardMarkup(animals));
        }

        private async Task SetAnimalAsync(string animal)
        {
            Animal = Program.Settings.Animals.Single(a => a.Value == animal);

            await UpdateMessageAsync();
        }

        private async Task EditStickerAsync(string callbackQueryId)
        {
            var stickers = Program.Settings.Stickers.Select(s => InlineKeyboardButton.WithCallbackData(s.Key, $"set_sticker_{s.Value}"));

            await UpdateMessageAsync(new InlineKeyboardMarkup(stickers));
        }

        private async Task SetStickerAsync(string sticker)
        {
            Sticker = Program.Settings.Stickers.Single(s => s.Value == sticker);

            await UpdateMessageAsync();
        }

        private async Task EditPropertyAsync(string callbackQueryId, string property)
        {
            var (propertyName, text) = property switch
            {
                "recipient_full_name" => new KeyValuePair<string, string>("RecipientFullName", "Reply with recipient's full name."),
                "street_address" => new KeyValuePair<string, string>("StreetAddress", "Reply with street address."),
                "town_city" => new KeyValuePair<string, string>("TownCity", "Reply with town / city."),
                "postcode" => new KeyValuePair<string, string>("Postcode", "Reply with postcode."),
                "state_county" => new KeyValuePair<string, string>("StateCounty", "Reply with state / county."),
                "country" => new KeyValuePair<string, string>("Country", "Reply with two-letter (ISO 3166-1) country code.\nExample for France: FR"),
                "message" => new KeyValuePair<string, string>("Message", "Reply with message for the recipient."),
                "email" => new KeyValuePair<string, string>("Email", "Reply with your email address.")
            };

            var message = await _bot.SendTextMessageAsync(_orderMessage.Chat, $"[ ](tg://user?id={_userId})" + text, ParseMode.Markdown, replyMarkup: new ForceReplyMarkup()
            {
                Selective = true
            });

            Replies.Add(message.MessageId, propertyName);

            await _bot.AnswerCallbackQueryAsync(callbackQueryId);
        }

        private async Task CancelOrderAsync(string callbackQueryId)
        {
            await _bot.DeleteMessageAsync(_orderMessage.Chat, _orderMessage.MessageId);
        }

        private async Task PlaceOrderAsync(string callbackQueryId)
        {
            if (string.IsNullOrEmpty(RecipientFullName) || string.IsNullOrEmpty(StreetAddress) || string.IsNullOrEmpty(TownCity) || string.IsNullOrEmpty(Postcode) || string.IsNullOrEmpty(Country.Value))
            {
                await _bot.AnswerCallbackQueryAsync(callbackQueryId, "Please fill all the required fields.");

                return;
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://www.shitexpress.com/btc/create.php")
            {
                // We sadly can't use FormUrlEncodedContent because spaces are escaped as '+': https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/FormUrlEncodedContent.cs#L52-L53
                Content = new StringContent(
                    $"btc={Uri.EscapeDataString("1")}" +
                    $"&name={Uri.EscapeDataString(RecipientFullName)}" +
                    $"&street={Uri.EscapeDataString(StreetAddress)}" +
                    $"&city={Uri.EscapeDataString(TownCity)}" +
                    $"&zip={Uri.EscapeDataString(Postcode)}" +
                    $"&state={Uri.EscapeDataString(StateCounty ?? string.Empty)}" +
                    $"&country={Uri.EscapeDataString(Country.Value)}" +
                    $"&notes={Uri.EscapeDataString(Message ?? string.Empty)}" +
                    $"&animal={Uri.EscapeDataString(Animal.Value)}" +
                    $"&packaging={Uri.EscapeDataString(Sticker.Value)}" +
                    $"&email={Uri.EscapeDataString(Email ?? string.Empty)}" +
                    $"&referer={Uri.EscapeDataString("https://github.com/Laiteux/ShitexpressBot")}" +
                    $"&ref={Uri.EscapeDataString("https://github.com/Laiteux/ShitexpressBot")}",
                    Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            using var responseMessage = await Program.HttpClient.SendAsync(requestMessage);

            var contentString = await responseMessage.Content.ReadAsStringAsync();

            string[] split = contentString.Split('|');

            var text = new StringBuilder()
                .AppendLine("*Order ID:* " + split[0])
                .AppendLine("*Address:* " + split[2])
                .AppendLine("*BTC:* " + split[3])
                .AppendLine()
                .AppendLine("Please pay the amount to the provided blockchain address.");

            await _bot.SendTextMessageAsync(_orderMessage.Chat, text.ToString(), ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Check order status", split[0])));
        }
    }
}
