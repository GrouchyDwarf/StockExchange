using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ExchangeSharp;

namespace StockExchange.Information
{
    public class MarketSymbols
    {
        private readonly string _stockExchangeName;

        public MarketSymbols(string stockExchangeName)
        {
            _stockExchangeName = stockExchangeName;
        }

        public async Task<List<string>> GetMarketSymbols()
        {
            IEnumerable<string> marketSymbols = await ExchangeAPI.GetExchangeAPI(_stockExchangeName).GetMarketSymbolsAsync();
            var marketSymbolsList = new List<string>();
            foreach(var marketSymbol in marketSymbols)
            {
                marketSymbolsList.Add(marketSymbol);
            }
            return marketSymbolsList;
        }
    }
}
