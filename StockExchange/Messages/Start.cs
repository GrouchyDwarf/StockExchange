using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StockExchange.Messages
{
    public class Start : MainMessage
    {
        public override string Message { get; } = "Новый вебсокет";

        public override async Task<List<string>> OnSend()
        {
            return await Task.FromResult(new List<string>() 
            {
                new ChooseStockExchange().Message,
                new ChooseMarketSymbol().Message,
                new ChooseDataType().Message
            });
        }
    }
}
