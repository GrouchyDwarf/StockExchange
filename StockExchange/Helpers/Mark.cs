using System;
using System.Collections.Generic;
using System.Text;

namespace StockExchange.Helpers
{
    public static class Mark
    {
        public static List<string> MarkStrings(List<string> allStrings, List<string> selectedStrings, string mark)
        {
            if(allStrings == null || selectedStrings == null || mark == null)
            {
                throw new ArgumentNullException();
            }
            foreach (var selectedString in selectedStrings)
            {
                for (var i = 0; i < allStrings.Count; ++i)
                {
                    if (selectedString == allStrings[i])
                    {
                        allStrings[i] = allStrings[i] + mark;
                    }
                }
            }
            return allStrings;
        }

        public static string DeleteMark(string record, string mark)
        {
            if(record == null || mark == null)
            {
                throw new ArgumentNullException();
            }
            if (record.Contains(mark))
            {
                return record.Remove(record.IndexOf(mark));
            }
            return record;
        }
    }
}
