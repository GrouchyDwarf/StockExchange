using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using ExchangeSharp;

namespace StockExchange.TelegramBot
{
    public class Data
    {
        public string MarketSymbol { get; set; }
        public string Trade { get; set; }
        public Stack<MarketCandle> Candles { get; set; }
        public string Ticker { get; set; }
        public Telegram.Bot.Types.Message Message { get; set; }
        //for candles
        public Timer Timer { get; set; }
        public bool TimeToUpdate { get; set; }
        public bool IfTimerStarted { get; set; }

        public int CandlesCounter { get; set; }

        public Data(double periodUpdate)
        {
            TimeToUpdate = true;
            Timer = new Timer(periodUpdate);
            Timer.Elapsed += (source, elapsedEventArgs) => TimeToUpdate = true;
            IfTimerStarted = false;
            CandlesCounter = 0;
        }

        /*private void Func(object source, ElapsedEventArgs elapsedEventArgs)
        {
            TimeToUpdate = true;
        }*/
    }
}
