using System;
using System.Collections.Generic;
using System.Text;

namespace StockExchange.Messages
{
    public class Trades : MainMessage
    {
        public override string Message { get; } = "Trade";
    }
}
