using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using StockExchange.TelegramHelpers;
using Telegram.Bot.Types;
using StockExchange.Messages;

namespace UnitTests
{
    public class GetterTest
    {
        [Fact]
        public void GetChatId_UpdateMessage_ChatId()
        {
            long expected = 1111;
            var update = new Update()
            {
                Message = new Message()
                {
                    Chat = new Chat()
                    {
                        Id = expected
                    }
                }
            };

            long actual = Getter.GetChatId(update);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetChatId_UpdateCallbackQuery_ChatId()
        {
            int expected = 1111;
            var update = new Update()
            {
                CallbackQuery = new CallbackQuery()
                {
                    From = new User()
                    {
                        Id = expected
                    }
                }
            };

            long actual = Getter.GetChatId(update);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetUser_OldChatId_User()
        {
            long oldChatId = 1111;
            var expected = new StockExchange.TelegramBot.User(oldChatId);
            var secondUser = new StockExchange.TelegramBot.User(12345);
            var users = new List<StockExchange.TelegramBot.User>() { expected, secondUser };

            StockExchange.TelegramBot.User actual = Getter.GetUser(oldChatId, users);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetUser_NewChatId_User()
        {
            long newChatId = 1111;
            var expected = new StockExchange.TelegramBot.User(newChatId);
            var users = new List<StockExchange.TelegramBot.User>();

            StockExchange.TelegramBot.User actual = Getter.GetUser(newChatId, users);

            if (expected.ChatId == actual.ChatId)
            {
                Assert.True(true);
            }
            else
            {
                Assert.True(false);
            }
        }

        [Fact]
        public void GetMessage_MessageText_Message()
        {
            var text = new Back().Message;
            var expected = new Back();
            var messages = new List<MainMessage>() { new Back(), new Start(), new Candles() };

            MainMessage actual = Getter.GetMessage(text, out _, messages);

            if (expected.Message == actual.Message) 
            { 
                Assert.True(true); 
            }
            else 
            { 
                Assert.True(false); 
            }
        }

        [Fact]
        public void GetMessage_NotMessageText_MessageDoesNotExist()
        {
            var expected = false;
            var text = "text";
            var messages = new List<MainMessage>();

            Getter.GetMessage(text, out bool actual, messages);

            Assert.Equal(expected, actual);
        }
    }
}
