using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zoro.Processor;

namespace Zoro.Tests
{
    /// <summary>
    /// Summary description for DataMasking_Tests
    /// </summary>
    [TestClass]
    public class DataMasking_Tests
    {
        private const string testDir = @"C:\temp\Zorotests\";
        private const string configfile = testDir + "test1.xml";

        [TestMethod]
        public void T01_Mask_Test()
        {
            var config = MaskConfig.ReadConfig(configfile);
            var masker = new DataMasking(config);
            masker.Mask();

            Assert.IsTrue(File.Exists(config.OutputFile));
        }
    }
}
