using System;
using System.Collections.Generic;
using Xunit;
using Dandraka.Zoro.Processor;
using Dandraka.Zoro.Tests;

namespace Dandraka.Zoro.Tests
{
    /// <summary>
    /// Tests for the <c>DataMasking</c> class where both data source and data destination are database.
    /// </summary>
    public class Db2Db_Tests : IDisposable
    {

        private Utility utility = new Utility();

        public Db2Db_Tests()
        {
            utility.PrepareLocalDb();
        }

        public void Dispose()
        {
            this.utility.Dispose();
        }

        [Fact]
        public void T01_TestLocalDb()
        {

        }
    }
}