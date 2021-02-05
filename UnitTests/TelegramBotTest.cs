using System;
using Xunit;
using StockExchange;

namespace UnitTests
{
    public class TelegramBotTest
    {
        private readonly TelegramBot _bot;
        public TelegramBotTest()
        {
            var messageBoxInteractive = new MessageBoxInteractive();
            var key = "1435718439:AAE6JYxkaKzMetzsseQ6yKf9esMG8H8-czk";
            _bot = new TelegramBot(messageBoxInteractive, key);
        }

        [Fact]
        public void Test1()
        {
            _bot.
        }
    }
}
