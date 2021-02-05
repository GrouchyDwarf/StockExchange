using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StockExchange
{
    public class MessageBoxInteractive : IInteractive
    {
        public Task OutputAsync(string message)
        {
            MessageBox.Show(message);
            return Task.CompletedTask;
        }
    }
}
