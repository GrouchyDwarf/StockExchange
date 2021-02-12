using System;
using System.Collections.Generic;
using System.Text;

namespace StockExchange.Messages
{
    public class Tickers:MainMessage
    {
        public override string Message { get; } = "Tickers";
    }
}
