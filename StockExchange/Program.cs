using System;
using System.Threading.Tasks;

namespace StockExchange
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var key = "1435718439:AAE6JYxkaKzMetzsseQ6yKf9esMG8H8-czk";
            var messageBoxInteractive = new ConsoleInteractive();
            var bot = new TelegramBot(messageBoxInteractive, key);
            await bot.Run();
        }
    }
}
