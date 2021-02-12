using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StockExchange.Messages
{
    public class ChooseDataType : MainMessage
    {
        public override string Message { get; } = "Выбрать тип вебсокета";

        public override async Task<List<string>> OnSend()
        {
            return await Task.FromResult(new List<string>()
            {
                new Trades().Message,
                new Tickers().Message,
                new Candles().Message,
                new Back().Message
            });
        }
    }
}
