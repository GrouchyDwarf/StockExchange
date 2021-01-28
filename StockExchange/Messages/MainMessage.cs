using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ExchangeSharp;
using Telegram.Bot.Types.ReplyMarkups;

namespace StockExchange
{
    abstract class MainMessage
    {
        public virtual string Message { get; }

        public ExchangeAPI ExchangeAPI { get; set; }
        /*public virtual List<string> OnSend()
        {
            return new List<string>();
        }*/
        public async virtual Task<List<string>> OnSend()
        {
            return await Task.FromResult(new List<string>());
        }
    }
}
