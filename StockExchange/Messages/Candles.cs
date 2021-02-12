using System;
using System.Collections.Generic;
using System.Text;

namespace StockExchange.Messages
{
    public class Candles : MainMessage
    {
        public override string Message { get; } = "Candle";
    }
}
