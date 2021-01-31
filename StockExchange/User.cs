using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using ExchangeSharp;

namespace StockExchange
{
    class User
    {
        public ExchangeAPI StockExchange { get; set; }
        public List<Data> Data { get; set; }
        public List<string> DataTypes { get; set; }
        public bool IsFirstMessage { get; set; } = true;
        public long ChatId { get; private set; }
        public User(long chatId)
        {
            ChatId = chatId;
            Data = new List<Data>();
            DataTypes = new List<string>();
        }
    }
}
