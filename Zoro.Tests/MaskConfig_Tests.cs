using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Zoro.Processor;

namespace Zoro.Tests
{
    public class MaskConfig_Tests : IDisposable
    {
        private static string testConfigFile;

        private Utility utility = new Utility();

        public MaskConfig_Tests()
        {
            utility.PrepareTestInstanceDir();
        }

        public void Dispose()
        {
            this.utility.Dispose();
        }

        [Fact]
        public void T01_Save_Read_Config_Test()
        {
            var config = new MaskConfig()
            {
                InputFile = Path.Combine(utility.TestInstanceDir, "data2.csv"),
                OutputFile = Path.Combine(utility.TestInstanceDir, "maskeddata2.csv"),
                DataSource = DataSource.CsvFile,
                ConnectionString = "(none)",
                SqlSelect = "(none)"
            };

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
                    ListOfPossibleReplacements = new List<Replacement>()
                    {
                        new Replacement() { Selector = "GWGENDER=weiblich", ReplacementList = "Kerry,Laura" },
                        new Replacement() { Selector = "", ReplacementList = "Nick,John,Papadopoulos,Smith" }
                    }
                });
            }

            testConfigFile = Path.Combine(utility.TestInstanceDir, "testconfig2.xml");
            MaskConfig.SaveConfig(testConfigFile, config);

            // test writing
            Assert.True(File.Exists(utility.TestInstanceConfigfile));

            // test reading
            var config2 = MaskConfig.ReadConfig(testConfigFile);
            //Console.WriteLine($"Config: InputFile = {config2.InputFile}");
            //Console.WriteLine($"Config: OutputFile = {config2.OutputFile}");
            Assert.Equal(44, config2.FieldMasks.Count);
        }

        [Fact]
        public void T02_Read_Config_Test()
        {
            var config = MaskConfig.ReadConfig(utility.TestInstanceConfigfile);

            Assert.Equal(2, config.FieldMasks.Count);
        }
    }
}