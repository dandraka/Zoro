using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zoro.Processor;

namespace Zoro.Tests
{
    [TestClass]
    public class MaskConfig_Tests
    {
        private const string testDir = @"C:\temp\Zorotests\";
        private const string configfile = testDir + "test1.xml";

        [ClassInitialize]
        public static void Prep(TestContext context)
        {
            if (!Directory.Exists(testDir))
            {
                Directory.CreateDirectory(testDir);
            }

            /*foreach (var file in Directory.GetFiles(testDir))
            {
                File.Delete(file);
            }*/
        }

        [TestMethod]
        public void T01_SaveConfig_Test()
        {
            var config = new MaskConfig()
            {
                InputFile = testDir + "data.csv",
                OutputFile = testDir + "maskeddata.csv",
                DataSource = DataSource.CsvFile,
                ConnectionString = "(none)",
                SqlSelect = "(none)"
            };

            config.FieldMasks = new List<FieldMask>();
            string[] fields = new[]
            {
                "BIRTHDAY", "BANKACCOUNTNR", "FINANCIALINSTITUTE", "BANKZIPNR", "STREET1",
                "ZIP1", "STREET2", "ZIP2", "STREET3", "ZIP3", "PHONEID1", "PHONEFIELDSTR1", "PHONEID2",
                "PHONEFIELDSTR2", "PHONEID3", "PHONEFIELDSTR3", "PHONEID4", "PHONEFIELDSTR4", "PHONEID5",
                "PHONEFIELDSTR5", "PHONEID6", "PHONEFIELDSTR6", "PHONEID7", "PHONEFIELDSTR7", "PHONEID8",
                "PHONEFIELDSTR8", "PHONEID9", "PHONEFIELDSTR9", "PHONEID10", "PHONEFIELDSTR10", "MAILFIELDSTR1",
                "MAILFIELDSTR2", "MAILFIELDSTR3", "MAILFIELDSTR4", "GWIBAN", "GWBIC", "GWTRADEREGISTER", "WCM_KURZNA",
                "ART_STREET_EXPORT", "ART_NAME_EXPORT", "ART_ZIP_EXPORT", "ART_CHRISTIANNAME_EXPORT"
            };
            foreach (string field in fields)
            {
                config.FieldMasks.Add(new FieldMask() { FieldName = field, MaskType = MaskType.Similar });
            }

            string[] nameFields = new[]
            {
                "NAME", "CHRISTIANNAME"
            };
            foreach (string field in nameFields)
            {
                config.FieldMasks.Add(new FieldMask()
                {
                    FieldName = field,
                    MaskType = MaskType.List,
                    // TODO
                    ListOfPossibleReplacements = new List<Replacement>()
                    {                        
                        new Replacement() { FieldValue = "GWGENDER=weiblich", ReplacementList = "Kerry,Laura" },
                        new Replacement() { FieldValue = "", ReplacementList = "Nick,John,Papadopoulos,Smith" }
                    }  
                });
            }
            MaskConfig.SaveConfig(configfile, config);

            Assert.IsTrue(File.Exists(configfile));
        }

        [TestMethod]
        public void T02_ReadConfig_Test()
        {
            var config = MaskConfig.ReadConfig(configfile);

            Assert.AreEqual(44, config.FieldMasks.Count);
        }
    }
}
