using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StockExchange.Messages
{
    class Start : MainMessage
    {
        public override string Message { get; } = "Начать";

        public override async Task<List<string>> OnSend()
        {
            return await Task.FromResult(new List<string>() 
            {
                "Выбрать биржу",
                "Выбрать символ",
                "Выбрать тип вебсокета"
            });
        }
    }
}
