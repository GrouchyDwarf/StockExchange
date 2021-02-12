using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit;
using StockExchange.TelegramHelpers;

namespace UnitTests
{
    public class ButtonConverterTest
    {
        [Fact]
        public void ButtonNamesToInlineButtons_ButtonNames_InlineButtons()
        {
            var buttonName = new List<string> { "first", "second", "third"};
            var expected = new List<List<InlineKeyboardButton>> {
                new List<InlineKeyboardButton>{ new InlineKeyboardButton { Text = buttonName[0], CallbackData = buttonName[0] } },
                new List<InlineKeyboardButton>{ new InlineKeyboardButton { Text = buttonName[1], CallbackData = buttonName[1] } },
                new List<InlineKeyboardButton>{ new InlineKeyboardButton { Text = buttonName[2], CallbackData = buttonName[2] } }
            };

            List<List<InlineKeyboardButton>> actual = ButtonConverter.ButtonNamesToInlineButtons(buttonName);

            if (expected.Count != actual.Count)
            {
                Assert.True(false);
            }
            for (var i = 0; i < expected.Count; ++i)
            {
                for (var j = 0; j < expected[i].Count; ++j)
                {
                    if (expected[i][j].Text != actual[i][j].Text || expected[i][j].CallbackData != actual[i][j].CallbackData)
                    {
                        Assert.True(false);
                    }
                }
            }
            Assert.True(true);
        }
    }
}
