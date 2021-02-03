using System;
using System.Collections.Generic;
using System.Text;
using ExchangeSharp;

namespace StockExchange
{
    class Data
    {
        public string MarketSymbol { get; set; }
        public string Trade { get; set; }
        //public string Candle { get; set; }

        public MarketCandle Candle { get; set; }

        public string Ticker { get; set; }

        public Telegram.Bot.Types.Message Message { get; set; }
    }
}
