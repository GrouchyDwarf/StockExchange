using System;
using System.Collections.Generic;
using System.Text;

namespace StockExchange.Messages
{
    class Trades : MainMessage
    {
        public override string Message { get; } = "Trade";
    }
}
