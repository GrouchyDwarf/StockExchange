using System;
using System.Collections.Generic;
using System.Text;

namespace StockExchange.Messages
{
    class Next : MainMessage
    {
        public override string Message { get; } = "Следующая" + Emoji.NextArrow;
    }
}
