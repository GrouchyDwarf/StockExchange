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
using StockExchange.Information;

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
            foreach (var buttons in actualReplyMarkup)
            {
                foreach (var button in buttons)
                {
                    if (button.Text == new Start().Message)
                    {
                        isActual = true;
                    }
                }
            }
            IfActual(isActual);
        }
        [Fact]
        public async Task EditOversizeMessageAsync_AllButtonsAndPageNumber_DesiredPageWithButtons()
        {
            //firstPage
            var buttons = new List<string> { "first", "second", "third" };
            var expectedButtons = new List<string>() { "first", new Next().Message };
            var limit = 1;
            var pageNumber = 0;
            var user = new StockExchange.TelegramBot.User(_chatId);
            var message = new Start();

            await Check(buttons, expectedButtons, pageNumber, limit, user, message);

            //secondPage
            expectedButtons = new List<string>() { "second", new Next().Message, new Previous().Message };
            pageNumber = 1;

            await Check(buttons, expectedButtons, pageNumber, limit, user, message);

            //lastPage
            expectedButtons = new List<string>() { "third", new Previous().Message};
            pageNumber = 2;

            await Check(buttons, expectedButtons, pageNumber, limit, user, message);

            async Task Check(List<string> buttons, List<string> expectedButtons, int pageNumber, int limit, StockExchange.TelegramBot.User user, MainMessage message)
            {
                var bot = new TelegramBotClientMock();
                await Sender.EditOversizeMessageAsync(buttons, limit, pageNumber, user, message, 1111, bot);
                List<Message> actualMessages = bot.BotMessages;
                var actualButtons = ConvertMessagesToButtons(actualMessages);
                IfActual(CompareLists(expectedButtons, actualButtons));
            }
        }
        [Fact]
        public async Task EditMessageAsync_Message_EditedMessage()
        {
            //Main menu only with choose stock exchange
            var user = new StockExchange.TelegramBot.User(_chatId);
            MainMessage message = new Start();
            var pageNumber = 0;
            var expectedButtons = new List<string>() { new ChooseStockExchange().Message };
            await Check(expectedButtons, user, message, pageNumber);

            //Choose stock exchange
            message = new ChooseStockExchange();
            expectedButtons = await message.OnSend();
            await Check(expectedButtons, user, message, pageNumber);

            //Full main menu
            user.StockExchange = new StockExchanges().StockExchangesList[0];
            message = new Back() { ExchangeAPI = user.StockExchange };
            expectedButtons = await new Start().OnSend();
            await Check(expectedButtons, user, message, pageNumber);

            //Choose data type
            message = new ChooseDataType();
            expectedButtons = await message.OnSend();
            await Check(expectedButtons, user, message, pageNumber);

            //ChooseMarketSymbol
            expectedButtons = await new ChooseMarketSymbol() { ExchangeAPI = user.StockExchange }.OnSend();
            expectedButtons.RemoveRange(Sender.Limit, expectedButtons.Count - Sender.Limit);
            expectedButtons.Add(new Next().Message);
            message = new ChooseMarketSymbol() { ExchangeAPI = user.StockExchange};
            await Check(expectedButtons, user, message, 0);

            async Task Check(List<string> expectedButtons, StockExchange.TelegramBot.User user, MainMessage message, int pageNumber)
            {
                var bot = new TelegramBotClientMock();
                await Sender.EditMessageAsync(user, message, pageNumber, 1111, bot);
                List<Message> actualMessages = bot.BotMessages;
                var actualButtons = ConvertMessagesToButtons(actualMessages);
                IfActual(CompareLists(expectedButtons, actualButtons));
            }
        }

        private void IfActual(bool isActual)
        {
            if (isActual)
            {
                Assert.True(true);
            }
            else
            {
                Assert.True(false);
            }
        }

        private bool CompareLists(List<string> firstList, List<string> secondList)
        {
            if(firstList.Count == secondList.Count)
            {
                foreach(string element in firstList)
                {
                    if (!secondList.Contains(element))
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        private List<string> ConvertMessagesToButtons(List<Message> messages)
        {
            var buttons = new List<string>();
            foreach (var message in messages)
            {
                if (message.ReplyMarkup != null)
                {
                    foreach (var inlineButtons in message.ReplyMarkup.InlineKeyboard)
                    {
                        foreach (var inlineButton in inlineButtons)
                        {
                            buttons.Add(inlineButton.Text);
                        }
                    }
                }
            }
            return buttons;
        }
    }
}
