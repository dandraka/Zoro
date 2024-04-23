using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Dandraka.Zoro.Processor;

namespace Dandraka.Zoro.Tests
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
                "BIRTHDAY", "BANKACCOUNTNR", "FINANCIALINSTITUTE", "BANKZIPNR", "STREET1", "LAND",
                "ZIP1", "STREET2", "ZIP2", "STREET3", "ZIP3", "PHONEID1", "PHONEFIELDSTR1", "PHONEID2",
                "PHONEFIELDSTR2", "PHONEID3", "PHONEFIELDSTR3", "PHONEID4", "PHONEFIELDSTR4", "PHONEID5",
                "PHONEFIELDSTR5", "PHONEID6", "PHONEFIELDSTR6", "PHONEID7", "PHONEFIELDSTR7", "PHONEID8",
                "PHONEFIELDSTR8", "PHONEID9", "PHONEFIELDSTR9", "PHONEID10", "PHONEFIELDSTR10", "MAILFIELDSTR1",
                "MAILFIELDSTR2", "MAILFIELDSTR3", "MAILFIELDSTR4", "GWIBAN", "GWBIC", "GWTRADEREGISTER", "WCM_KURZNA",
                "ART_STREET_EXPORT", "ART_NAME_EXPORT", "ART_ZIP_EXPORT", "ART_CHRISTIANNAME_EXPORT"
            };
            string[] nameFields = new[]
            {
                "NAME", "CHRISTIANNAME"
            };
            string[] zipFields = new[]
            {
                "ZIP1", "ZIP2", "ZIP3"
            };            


            foreach (string field in fields.Where(x => !nameFields.Contains(x) && !zipFields.Contains(x)))
            {
                config.FieldMasks.Add(new FieldMask() { FieldName = field, MaskType = MaskType.Similar });
            }

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

            foreach (string field in zipFields)
            {
                config.FieldMasks.Add(new FieldMask()
                {
                    FieldName = field,
                    MaskType = MaskType.Query,
                    QueryReplacement = new QueryReplacement() 
                    {
                        Query = "SELECT postcode, country FROM postcode",
                        ValueDbField = "postcode",
                        GroupDbField = "country",
                        SelectorField = "LAND"
                    }
                });
            }            

            testConfigFile = Path.Combine(utility.TestInstanceDir, $"config_{Guid.NewGuid()}.xml");
            MaskConfig.SaveConfig(testConfigFile, config);

            // test writing
            Assert.True(File.Exists(utility.TestInstanceConfigCSVfile));

            // test reading
            var config2 = MaskConfig.ReadConfig(testConfigFile);
            //Console.WriteLine($"Config: InputFile = {config2.InputFile}");
            //Console.WriteLine($"Config: OutputFile = {config2.OutputFile}");
            Assert.Equal(45, config2.FieldMasks.Count);
        }

        [Fact]
        public void T02_Read_Config_Test()
        {
            var config = MaskConfig.ReadConfig(utility.TestInstanceConfigCSVfile);

            Assert.Equal(2, config.FieldMasks.Count);
        }
    }
}