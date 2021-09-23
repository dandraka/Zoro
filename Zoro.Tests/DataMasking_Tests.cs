using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Zoro.Processor;

namespace Zoro.Tests
{
    /// <summary>
    /// Tests for the <c>DataMasking</c> class.
    /// </summary>
    public class DataMasking_Tests
    {
        private Utility utility = new Utility();

        public DataMasking_Tests()
        {
            utility.PrepareTestInstanceDir();
        }

        [Fact]
        public void T01_Mask_Test()
        {
            var config = MaskConfig.ReadConfig(utility.TestInstanceConfigfile);
            Console.WriteLine($"Config: InputFile = {config.InputFile}");
            Console.WriteLine($"Config: OutputFile = {config.OutputFile}");
            var masker = new DataMasking(config);
            masker.Mask();

            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.True(File.Exists(config.OutputFile));
            Assert.Equal(5, contents.Count);
        }
    }
}