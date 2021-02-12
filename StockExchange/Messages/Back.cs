using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StockExchange.Messages
{
    public class Back: MainMessage
    {
        public override string Message { get; } = "Назад";

        public override async Task<List<string>> OnSend()
        {
            return await new Start().OnSend();
        }
    }
}
