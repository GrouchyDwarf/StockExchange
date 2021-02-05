using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StockExchange
{
    public interface IInteractive
    {
        Task OutputAsync(string message);
    }
}
