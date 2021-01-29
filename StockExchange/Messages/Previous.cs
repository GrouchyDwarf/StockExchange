using System;
using System.Collections.Generic;
using System.Text;

namespace StockExchange.Messages
{
    class Previous:MainMessage
    {
        public override string Message { get; } = Emoji.PreviousArrow + "Предыдущая";
    }
}
