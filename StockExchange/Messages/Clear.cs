using System;
using System.Collections.Generic;
using System.Text;

namespace StockExchange.Messages
{
    class Clear: MainMessage
    {
        public string Message { get; } = "Очистить";
    }
}
