using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ExchangeSharp;
using StockExchange.Information;

namespace StockExchange.Messages
{
    class ChooseMarketSymbol: MainMessage
    {
        public override string Message { get; } = "Выбрать символ";

        private MarketSymbols _marketSymbols;

        /*public ChooseMarketSymbol(ExchangeAPI exchangeAPI)
        {
            _exchangeAPI = exchangeAPI;
            _marketSymbols = new MarketSymbols(_exchangeAPI.Name);
        }*/

        public async override Task<List<string>> OnSend()
        {
            if(ExchangeAPI == null)
            {
                return await Task.FromResult(new List<string>() { "Не выбрана биржа" });
            }
            _marketSymbols = new MarketSymbols(ExchangeAPI.Name);
            var globalSymbols = new List<string>();
            foreach(var marketSymbol in await _marketSymbols.GetMarketSymbols())
            {
                globalSymbols.Add(await ExchangeAPI.ExchangeMarketSymbolToGlobalMarketSymbolAsync(marketSymbol));
            }
            globalSymbols.Add(new Back().Message);
            return await Task.FromResult(globalSymbols);
        }
    }
}
