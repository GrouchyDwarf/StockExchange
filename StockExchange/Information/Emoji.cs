using System;
using System.Collections.Generic;
using System.Text;

namespace StockExchange.Information
{
    class Emoji
    {
        public static string CheckMark { get; } = Char.ConvertFromUtf32(9989);
        public static string PreviousArrow { get; } = Char.ConvertFromUtf32(9194);
        public static string NextArrow { get; } = Char.ConvertFromUtf32(9193);
    }
}
