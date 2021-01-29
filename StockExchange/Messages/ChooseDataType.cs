using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StockExchange.Messages
{
    class ChooseDataType : MainMessage
    {
        public override string Message { get; } = "Выбрать тип вебсокета";

        public override async Task<List<string>> OnSend()
        {
            return await Task.FromResult(new List<string>()
            {
                "Trades",
                "Tickers",
                "Candles",
                "Назад"
            });
        }
    }
}
