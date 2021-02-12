using System;
using System.Collections.Generic;
using System.Text;
using ExchangeSharp;

namespace StockExchange
{
    public class Data
    {
        public string MarketSymbol { get; set; }
        public string Trade { get; set; }
        public Stack<MarketCandle> Candles { get; set; }

        public string Ticker { get; set; }

        public Telegram.Bot.Types.Message Message { get; set; }
    }
}
