using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StockExchange.Helpers;
using StockExchange.Information;
using StockExchange.Messages;
using StockExchange.TelegramHelpers;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace StockExchange.TelegramBot
{
    public static class Sender
    {
        public static int Limit { get; } = 95;
        public static async Task<int> SendChangingMessageAsync(long chatId, ITelegramBotClient bot)
        {
            if(bot == null)
            {
                throw new ArgumentNullException();
            }
            var message = await bot.SendTextMessageAsync(chatId, "Главная");
            return message.MessageId;
        }

        public static async Task<int> SendFirstMessageAsync(User user, long chatId, ITelegramBotClient bot)
        {
            var replyKeyboard = new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton>{new KeyboardButton(new Start().Message)}
                }
            };
            await bot.SendTextMessageAsync(chatId, "Главная", replyMarkup: replyKeyboard);
            user.IsFirstMessage = false;
            return await Sender.SendChangingMessageAsync(chatId, bot);
        }

        //several pages
        public static async Task EditOversizeMessageAsync(List<string> buttons, int limit, int pageNumber, User user, MainMessage message, int messageId, ITelegramBotClient bot)
        {
            int modulo = buttons.Count % limit;
            var partButtons = new List<string>();
            decimal totalNumberPages = Math.Ceiling((decimal)buttons.Count / limit) - 1;
            if (pageNumber != totalNumberPages)
            {
                for (int i = pageNumber * limit; i < (pageNumber + 1) * limit; ++i)
                {
                    partButtons.Add(buttons[i]);
                }
            }
            else
            {
                if(modulo == 0)
                {
                    ++modulo;
                }
                for (int i = pageNumber * limit; i < pageNumber * limit + modulo; ++i)
                {
                    partButtons.Add(buttons[i]);
                }
            }
            if (pageNumber < totalNumberPages)
            {
                partButtons.Add(new Next().Message);
            }
            if (pageNumber > 0)
            {
                partButtons.Add(new Previous().Message);
            }
            var inlineKeyboard = new InlineKeyboardMarkup(ButtonConverter.ButtonNamesToInlineButtons(partButtons));
            await bot.EditMessageTextAsync(user.ChatId, messageId, message.Message);
            await bot.EditMessageReplyMarkupAsync(user.ChatId, messageId, replyMarkup: inlineKeyboard);
        }

        public static async Task EditMessageAsync(User user, MainMessage message, int pageNumber, int messageId, ITelegramBotClient bot)
        {
            InlineKeyboardMarkup inlineKeyboard = null;
            var isOversizeMessage = false;
            if (user.StockExchange == null && (message.Message == new Start().Message || message.Message == new Back().Message))
            {
                inlineKeyboard = new InlineKeyboardMarkup(ButtonConverter.ButtonNamesToInlineButtons(new List<string>() { new ChooseStockExchange().Message }));
            }
            else
            {
                List<string> buttons = await message.OnSend();
                if (user.Data.Count > 0 && message.GetType() == new ChooseMarketSymbol().GetType())
                {
                    var globalSelectedSymbols = new List<string>();
                    foreach (var selectedMarketSymbol in user.Data.Select(d => d.MarketSymbol).ToList())
                    {
                        globalSelectedSymbols.Add(await user.StockExchange.ExchangeMarketSymbolToGlobalMarketSymbolAsync(selectedMarketSymbol));
                    }
                    buttons = Mark.MarkStrings(buttons, globalSelectedSymbols, Emoji.CheckMark);
                }
                else if (user.DataTypes.Count > 0 && message.GetType() == new ChooseDataType().GetType())
                {
                    buttons = Mark.MarkStrings(buttons, user.DataTypes, Emoji.CheckMark);
                }
                else if (user.StockExchange != null && message.GetType() == new ChooseStockExchange().GetType())
                {
                    buttons = Mark.MarkStrings(buttons, new List<string>() { user.StockExchange.Name }, Emoji.CheckMark);
                }
                if (buttons.Count > Limit)
                {

                    await Sender.EditOversizeMessageAsync(buttons, Limit, pageNumber, user, message, messageId, bot);
                    isOversizeMessage = true;
                }
                else
                {
                    inlineKeyboard = new InlineKeyboardMarkup(ButtonConverter.ButtonNamesToInlineButtons(buttons));
                }
            }
            if (!isOversizeMessage)
            {
                await bot.EditMessageTextAsync(user.ChatId, messageId, message.Message);
                await bot.EditMessageReplyMarkupAsync(user.ChatId, messageId, inlineKeyboard);
            }
        }
    }
}
