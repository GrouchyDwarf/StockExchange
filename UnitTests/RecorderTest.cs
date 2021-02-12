using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using StockExchange.Helpers;

namespace UnitTests
{
    public class RecorderTest
    {
        [Fact]
        public void CheckIfRecorded_RecordsAndNewRecord_False()
        {
            var records = new List<string>() { "first", "second", "third" };
            var newRecord = "fourth";
            var expected = false;

            bool actual = Recorder.CheckIfRecorded(records, newRecord);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CheckIfRecorder_RecordsAndOldRecord_True()
        {
            var records = new List<string>() { "first", "second", "third" };
            var oldRecord = "second";
            var expected = true;

            bool actual = Recorder.CheckIfRecorded(records, oldRecord);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CheckIfRecorded_NullParams_ArgumentNullException()
        {
            List<string> records = null;
            var newRecord = "fifth";
            Type expected = typeof(ArgumentNullException);
            Type actual = typeof(Exception);

            try
            {
                Recorder.CheckIfRecorded(records, newRecord);
            }
            catch(Exception ex)
            {
                actual = ex.GetType();
            }

            Assert.Equal(expected, actual);
        }
    }
}
