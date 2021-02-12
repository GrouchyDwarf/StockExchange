using System;
using System.Collections.Generic;
using System.Text;
using StockExchange.Information;
using StockExchange.Helpers;
using Xunit;

namespace UnitTests
{
    public class MarkTest
    {
        [Fact]
        public void MarkStrings_StringsAndSelectedStrings_MarkedStrings()
        {
            var allStrings = new List<string> { "first", "second", "third" };
            var selectedStrings = new List<string> { "second" };
            string mark = Emoji.CheckMark;
            var expceted = new List<string> { "first", $"second{mark}", "third" };

            List<string> actual = Mark.MarkStrings(allStrings, selectedStrings, mark);

            Assert.Equal(expceted, actual);
        }

        [Fact]
        public void MarkString_StringWithoutSelectedStrings_StringsWithoutMark()
        {
            var allStrings = new List<string> { "first", "second", "third" };
            var selectedStrings = new List<string>();
            string mark = Emoji.CheckMark;
            var expceted = new List<string> { "first", $"second", "third" };

            List<string> actual = Mark.MarkStrings(allStrings, selectedStrings, mark);

            Assert.Equal(expceted, actual);
        }
        
        [Fact]
        public void MarkString_NullParams_ArgumentNullException()
        {
            List<string> allStrings = null;
            var selectedStrings = new List<string> { "second" };
            string mark = Emoji.CheckMark;
            Type expected = typeof(ArgumentNullException);
            Type actual = typeof(Exception);

            try
            {
                Mark.MarkStrings(allStrings, selectedStrings, mark);
            }
            catch(Exception ex)
            {
                actual = ex.GetType();
            }

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DeleteMark_StringWithMark_stringWithoutMark()
        {
            string mark = Emoji.CheckMark;
            var record = $"record{mark}";
            var expected = "record";

            string actual = Mark.DeleteMark(record, mark);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void DeleteMark_NullParams_ArgumentNullException()
        {
            string mark = Emoji.CheckMark;
            string record = null;
            Type expected = typeof(ArgumentNullException);
            Type actual = typeof(Exception);

            try
            {
                Mark.DeleteMark(record, mark);
            }
            catch(Exception ex)
            {
                actual = ex.GetType();
            }

            Assert.Equal(expected, actual);
        }
    }
}
