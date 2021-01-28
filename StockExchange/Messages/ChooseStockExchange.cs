using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StockExchange.Messages
{
    class ChooseStockExchange : MainMessage
    {
        public override string Message { get; } = "Выбрать биржу";

        private readonly StockExchanges _stockExchanges;

        public ChooseStockExchange()
        {
            _stockExchanges = new StockExchanges();
        }
        public async override Task<List<string>> OnSend()
        {
            var stockExchangesNames = new List<string>();
            foreach(var stockExchange in _stockExchanges.StockExchangesList)
            {
                stockExchangesNames.Add(stockExchange.Name);
            }
            stockExchangesNames.Add(new Back().Message);
            return await Task.FromResult(stockExchangesNames);
        }
    }
}
