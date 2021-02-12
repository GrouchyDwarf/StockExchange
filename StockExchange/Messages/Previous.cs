using System;
using System.Collections.Generic;
using System.Text;
using StockExchange.Information;

namespace StockExchange.Messages
{
    public class Previous:MainMessage
    {
        public override string Message { get; } = Emoji.PreviousArrow + "Предыдущая";
    }
}
