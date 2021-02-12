using System;
using System.Collections.Generic;
using System.Text;

namespace StockExchange.Helpers
{
    public static class Recorder
    {
        public static bool CheckIfRecorded(List<string> records, string newRecord)
        {
            if(records == null || newRecord == null)
            {
                throw new ArgumentNullException();
            }
            var isAlreadyRecorded = false;
            foreach (var record in records)
            {
                if (record == newRecord)
                {
                    isAlreadyRecorded = true;
                    break;
                }
            }
            return isAlreadyRecorded;
        }
    }
}
