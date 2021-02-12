using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace StockExchange.TelegramHelpers
{
    public static class ButtonConverter
    {
        public static List<List<InlineKeyboardButton>> ButtonNamesToInlineButtons(List<string> buttonNames)
        {
            if(buttonNames == null)
            {
                throw new ArgumentNullException();
            }
            var buttons = new List<List<InlineKeyboardButton>>();
            foreach (var buttonName in buttonNames)
            {
                buttons.Add(new List<InlineKeyboardButton> { new InlineKeyboardButton() { Text = buttonName, CallbackData = buttonName } });
            }
            return buttons;
        }
    }
}
