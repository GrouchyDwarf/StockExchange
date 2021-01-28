using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StockExchange
{
    interface IInteractive
    {
        Task OutputAsync(string message);
    }
}
