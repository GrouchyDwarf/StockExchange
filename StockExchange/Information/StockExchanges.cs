using System;
using System.Collections.Generic;
using System.Text;
using ExchangeSharp;

namespace StockExchange
{
    class StockExchanges
    {
        private readonly List<ExchangeAPI> _stockExchanges;

        public List<ExchangeAPI> StockExchangesList { get => _stockExchanges; }

        public StockExchanges()
        {
            _stockExchanges = new List<ExchangeAPI>();
            _stockExchanges.Add(new ExchangeBinanceAPI());
            _stockExchanges.Add(new ExchangeBittrexAPI());
            _stockExchanges.Add(new ExchangeCoinbaseAPI());
        }
    }
}
