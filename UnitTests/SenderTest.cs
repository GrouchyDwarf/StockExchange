using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Telegram.Bot;
using StockExchange.TelegramBot;
using System.Threading.Tasks;
using System.Linq;
using UnitTests.SenderMocks;
using Telegram.Bot.Types;
using StockExchange.Messages;

namespace UnitTests
{
    public class SenderTest
    {
        private long _chatId = 764606140;
        [Fact]
        public async Task SendChangingMessageAsync_ChatId_MessageId()
        {
            var bot = new TelegramBotClientMock();
            int actual = await Sender.SendChangingMessageAsync(_chatId, bot);
            Message expectedMessage = await new TelegramBotClientMock().SendTextMessageAsync(_chatId, "aaa");
            int expectedId = expectedMessage.MessageId;
            Assert.Equal(expectedId, actual);
        }
        [Fact]
        public async Task SendFirstMessageAsync_NewUserAndChatId_IsFirstMessage()
        {
            var bot = new TelegramBotClientMock();
            var user = new StockExchange.TelegramBot.User(_chatId) { IsFirstMessage = true };
            bool expectedIsFirstMessage = !user.IsFirstMessage;

            int actual = await Sender.SendFirstMessageAsync(user, _chatId, bot);
            bool actualIsFirstMessage = user.IsFirstMessage;

            Assert.Equal(expectedIsFirstMessage, actualIsFirstMessage);
        }
        [Fact]
        public async Task SendFirstMessageAsync_UserAndChatId_ReplyKeyboard()
        {
            var bot = new TelegramBotClientMock();
            var user = new StockExchange.TelegramBot.User(_chatId);
            await Sender.SendFirstMessageAsync(user, _chatId, bot);
            var actualReplyMarkup = bot.ReplyKeyboardMarkup.Keyboard;
            var isActual = false;
            foreach(var buttons in actualReplyMarkup)
            {
                foreach(var button in buttons)
                {
                    if(button.Text == new Start().Message)
                    {
                        isActual = true;
                    }
                }
            }
            if (isActual)
            {
                Assert.True(true);
            }
            else
            {
                Assert.True(false);
            }
        }
        [Fact]
        public async Task EditOversizeMessageAsync_AllButtonsAndPageNumber_DesiredPageWithButtons()
        {
            var buttons = new List<string> { "first", "second", "third" };
            var limit = 2;
            var pageNumber = 1;
            //исправить ошибку с кендлами
        }
    }
}
