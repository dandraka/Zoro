using System;
using System.IO;
using Xunit;
using Zoro.Processor;

namespace Zoro.Tests
{
    /// <summary>
    /// Summary description for DataMasking_Tests
    /// </summary>
    public class DataMasking_Tests
    {
        private const string testDir = @"C:\temp\Zorotests\";
        private const string configfile = testDir + "test1.xml";

        [Fact]
        public void T01_Mask_Test()
        {
            var config = MaskConfig.ReadConfig(configfile);
            var masker = new DataMasking(config);
            masker.Mask();

            Assert.IsTrue(File.Exists(config.OutputFile));
        }
    }
}