using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using StockExchange.Messages;
using StockExchange.TelegramHelpers;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace StockExchange.TelegramBot
{
    public static class Sender
    {
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
    }
}
