using System;
using System.Collections.Generic;
using System.Text;

namespace StockExchange.Messages
{
    class Next : MainMessage
    {
        public override string Message { get; } = Char.ConvertFromUtf32(9193) + "Следующая";
    }
}
