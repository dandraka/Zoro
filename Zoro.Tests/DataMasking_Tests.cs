using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Dandraka.Zoro.Processor;
using Newtonsoft.Json.Linq;

namespace Dandraka.Zoro.Tests
{
    /// <summary>
    /// Tests for the <c>DataMasking</c> class.
    /// </summary>
    public class DataMasking_Tests : IDisposable
    {
        private readonly Utility utility = new();
        private static readonly string[] CHcities = ["Geneva", "Bern", "Thun"];
        private static readonly string[] DEcities = ["Köln", "Berlin", "Hamburg"];
        private static readonly string[] ITcities = ["Roma", "Venezia", "Milano"];
        private static readonly string[] GRCities = ["Rethimno", "Trikala", "Patra"];

        public DataMasking_Tests()
        {
            utility.PrepareTestInstanceDir();
            utility.PrepareSqliteDb("DataMasking_Tests");
        }

        public void Dispose()
        {
            utility.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void T01_Mask_CSV_Test()
        {
            var config = MaskConfig.ReadConfig(utility.TestInstanceConfigCSVfile);
            //Console.WriteLine($"Config: InputFile = {config.InputFile}");
            //Console.WriteLine($"Config: OutputFile = {config.OutputFile}");
            var masker = new DataMasking(config);
            masker.Mask();

            Assert.True(File.Exists(config.OutputFile));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(5, contents.Count);
        }

        [SkippableFact]
        public void T02_Mask_DB_Test()
        {
            var config = new MaskConfig()
            {
                DataSource = DataSource.Database,
                SqlSelect = $"SELECT * FROM {utility.TestTableName}",
                OutputFile = Path.Combine(utility.TestInstanceDir, "maskeddata_db_02.csv")
            };
            config.SetConnection(utility.TestDbConnection);
            config.FieldMasks.Add(new FieldMask() { FieldName = "Name", MaskType = MaskType.Similar });
            config.FieldMasks.Add(new FieldMask() { FieldName = "BankAccount", MaskType = MaskType.Asterisk });
            config.FieldMasks.Add(new FieldMask() { FieldName = "Country", MaskType = MaskType.None });
            config.FieldMasks.Add(new FieldMask() { FieldName = "Address", MaskType = MaskType.List });
            config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement()
            { Selector = "country=CH", ReplacementList = "Bahnhofstrasse 41,Hauptstrasse 8,Berggasse 4" });
            config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement()
            { Selector = "country=GR", ReplacementList = "Evangelistrias 22,Thessalias 47,Eparhiaki Odos Lefkogion 6" });
            config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement()
            { Selector = "", ReplacementList = "Main Street 9,Fifth Avenue 104,Ranch rd. 1" });

            var masker = new DataMasking(config);
            try
            {
                masker.Mask();
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                // error 40 - could not open connection to sql server
                Skip.If(ex.Message.Contains("40"), $"Database seems not to respond, check if your SQL Server is running. {ex.Message}");
            }

            Assert.True(File.Exists(config.OutputFile));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Console.WriteLine($"Contents of {config.OutputFile}");
            contents.ForEach(x => Console.WriteLine(x));
            Assert.Equal(5, contents.Count);
        }

        [Fact]
        public void T03_Mask_MaskType_None_Test()
        {
            // Arrange
            string csvContent = "id;name\r\n1;Carol Danvers\r\n2;Bruce Banner\r\n3;Peter Parker\r\n";
            string csvFilename = this.utility.CreateFileInTestInstanceDir(csvContent, "csv");
            var config = new MaskConfig()
            {
                InputFile = csvFilename,
                OutputFile = csvFilename.Replace(".csv", "_out.csv")
            };
            config.FieldMasks.Add(new FieldMask() { FieldName = "id", MaskType = MaskType.None });
            config.FieldMasks.Add(new FieldMask() { FieldName = "name", MaskType = MaskType.None });

            // Act
            var masker = new DataMasking(config);
            masker.Mask();

            // Assert
            Assert.True(File.Exists(config.OutputFile));
            var maskContents = new List<string>(csvContent.Split("\r\n"));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(4, contents.Count);
            for (int i = 0; i < contents.Count; i++)
            {
                Assert.Equal(maskContents[i], contents[i]);
            }
        }

        [Fact]
        public void T04_Mask_MaskType_Asterisk_Test()
        {
            // Arrange
            string csvContent = "id;name\r\n1;Carol Danvers\r\n2;Bruce Banner\r\n3;Peter Parker\r\n";
            string csvMaskedContent = "id;name\r\n1;***** *******\r\n2;***** ******\r\n3;***** ******\r\n";
            string csvFilename = this.utility.CreateFileInTestInstanceDir(csvContent, "csv");
            var config = new MaskConfig()
            {
                InputFile = csvFilename,
                OutputFile = csvFilename.Replace(".csv", "_out.csv")
            };
            config.FieldMasks.Add(new FieldMask() { FieldName = "id", MaskType = MaskType.None });
            config.FieldMasks.Add(new FieldMask() { FieldName = "name", MaskType = MaskType.Asterisk });

            // Act
            var masker = new DataMasking(config);
            masker.Mask();

            // Assert
            Assert.True(File.Exists(config.OutputFile));
            var maskContents = new List<string>(csvMaskedContent.Split("\r\n"));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(4, contents.Count);
            for (int i = 0; i < contents.Count; i++)
            {
                Assert.Equal(maskContents[i], contents[i]);
            }
        }

        [Fact]
        public void T05_Mask_MaskType_List_Test()
        {
            // Arrange
            string csvContent = "id;name\r\n1;Carol Danvers\r\n2;Bruce Banner\r\n3;Peter Parker\r\n";
            string csvMaskedContent = "id;name\r\n1;Charles Xavier\r\n2;Jean Grey\r\n3;Charles Xavier\r\n";
            string csvFilename = this.utility.CreateFileInTestInstanceDir(csvContent, "csv");
            var config = new MaskConfig()
            {
                InputFile = csvFilename,
                OutputFile = csvFilename.Replace(".csv", "_out.csv")
            };
            config.FieldMasks.Add(new FieldMask() { FieldName = "id", MaskType = MaskType.None });
            config.FieldMasks.Add(new FieldMask() { FieldName = "name", MaskType = MaskType.List });
            config.FieldMasks[1].ListOfPossibleReplacements.Add(new Replacement() { Selector = "id=2", ReplacementList = "Jean Grey" });
            config.FieldMasks[1].ListOfPossibleReplacements.Add(new Replacement() { Selector = "", ReplacementList = "Charles Xavier" });

            // Act
            var masker = new DataMasking(config);
            masker.Mask();

            // Assert
            Assert.True(File.Exists(config.OutputFile));
            var maskContents = new List<string>(csvMaskedContent.Split("\r\n"));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(4, contents.Count);
            for (int i = 0; i < contents.Count; i++)
            {
                Assert.Equal(maskContents[i], contents[i]);
            }
        }

        [Fact]
        public void T06_Mask_MaskType_Similar_Test()
        {
            // ===== Arrange
            string csvContent = "id;name\r\n1;Carol Danvers\r\n2;Bruce Banner\r\n3;Peter Parker\r\n";
            string csvFilename = this.utility.CreateFileInTestInstanceDir(csvContent, "csv");
            var config = new MaskConfig()
            {
                InputFile = csvFilename,
                OutputFile = csvFilename.Replace(".csv", "_out.csv")
            };
            config.FieldMasks.Add(new FieldMask() { FieldName = "id", MaskType = MaskType.None });
            config.FieldMasks.Add(new FieldMask() { FieldName = "name", MaskType = MaskType.Similar });

            // ===== Act
            var masker = new DataMasking(config);
            masker.Mask();

            // ===== Assert
            Assert.True(File.Exists(config.OutputFile));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(4, contents.Count);
            for (int i = 1; i < contents.Count; i++)
            {
                var items = contents[i].Split(';');
                // id
                Assert.Equal(i.ToString(), items[0]);
            }
        }

        [Fact]
        public void T07_Mask_MaskType_Query_Test()
        {
            // ===== Arrange
            string csvContent = "ID;Name;City;Country\r\n1;Roche;Basel;CH\r\n2;ABB;Baden;CH\r\n3;BMW;München;DE\r\n4;Barilla;Parma;IT\r\n5;FAGE;Athens;GR";
            string csvFilename = this.utility.CreateFileInTestInstanceDir(csvContent, "csv");
            var config = new MaskConfig()
            {
                InputFile = csvFilename,
                OutputFile = csvFilename.Replace(".csv", "_out.csv")
            };
            config.SetConnection(utility.TestDbConnection);
            config.FieldMasks.Add(new FieldMask() { FieldName = "ID", MaskType = MaskType.None });
            config.FieldMasks.Add(new FieldMask() { FieldName = "Name", MaskType = MaskType.Asterisk });
            config.FieldMasks.Add(new FieldMask() { FieldName = "City", MaskType = MaskType.Query });
            config.FieldMasks.Add(new FieldMask() { FieldName = "Country", MaskType = MaskType.None });

            config.FieldMasks[2].QueryReplacement = new QueryReplacement()
            {
                Query = "SELECT city, country FROM cities",
                GroupDbField = "country",
                ValueDbField = "city",
                SelectorField = "Country"
            };

            // prepare lookup table
            var cmdTbl = utility.TestDbConnection.CreateCommand();
            cmdTbl.CommandText = @"CREATE TABLE cities AS 
                SELECT 'Geneva'   AS city, 'CH' as country UNION ALL 
                SELECT 'Bern'     AS city, 'CH' as country UNION ALL 
                SELECT 'Thun'     AS city, 'CH' as country UNION ALL 
                SELECT 'Köln'     AS city, 'DE' as country UNION ALL                 
                SELECT 'Berlin'   AS city, 'DE' as country UNION ALL 
                SELECT 'Hamburg'  AS city, 'DE' as country UNION ALL 
                SELECT 'Roma'     AS city, 'IT' as country UNION ALL 
                SELECT 'Venezia'  AS city, 'IT' as country UNION ALL 
                SELECT 'Milano'   AS city, 'IT' as country UNION ALL 
                SELECT 'Rethimno' AS city, 'GR' as country UNION ALL 
                SELECT 'Trikala'  AS city, 'GR' as country UNION ALL 
                SELECT 'Patra'    AS city, 'GR' as country";
            cmdTbl.ExecuteNonQuery();
            utility.TestTablesToDrop = "cities";

            // ===== Act
            var masker = new DataMasking(config);
            masker.Mask();

            // ===== Assert
            Assert.True(File.Exists(config.OutputFile));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(6, contents.Count);
            for (int i = 1; i < contents.Count; i++)
            {
                var items = contents[i].Split(';');

                string id = items[0];
                string city = items[2];
                string country = items[3];

                // id
                Assert.Equal(i.ToString(), id);

                // city
                switch (country)
                {
                    case "CH":
                        Assert.Contains<string>(city, CHcities);
                        break;
                    case "DE":
                        Assert.Contains<string>(city, DEcities);
                        break;
                    case "IT":
                        Assert.Contains<string>(city, ITcities);
                        break;
                    case "GR":
                        Assert.Contains<string>(city, GRCities);
                        break;
                    default:
                        throw new NotSupportedException($"Unexpected country {country} found");
                }
            }
        }        

        [Fact]
        public void T08_Mask_JSON_Array_Test()
        {
            // === Arrange ===
            var config = MaskConfig.ReadConfig(utility.TestInstanceConfigJSONfile);

            // === Act ===
            var masker = new DataMasking(config);
            masker.Mask();

            // === Assert ===
            Assert.True(File.Exists(config.OutputFile));
            var contentsOrig = new List<string>(File.ReadLines(config.InputFile));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(contentsOrig.Count, contents.Count);

            var jsonObjOrig = JArray.Parse(File.ReadAllText(config.InputFile));
            var jsonObjMasked = JArray.Parse(File.ReadAllText(config.OutputFile));
            string orig = jsonObjOrig[0]["employee"].Value<string>("name");
            string masked = jsonObjMasked[0]["employee"].Value<string>("name");
            Assert.NotEqual(orig, masked);
        }

        [Fact]
        public void T09_Mask_JSON_SingleElement_Test()
        {
            // === Arrange ===
            var config = MaskConfig.ReadConfig(utility.TestInstanceConfigJSONfile);
            config.InputFile = config.InputFile.Replace("data2.json", "data3.json");
            config.OutputFile = config.OutputFile.Replace("data2.json", "data3.json");

            // === Act ===
            var masker = new DataMasking(config);
            masker.Mask();

            // === Assert ===
            Assert.True(File.Exists(config.OutputFile));
            var contentsOrig = new List<string>(File.ReadLines(config.InputFile));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(contentsOrig.Count, contents.Count);

            var jsonObjOrig = JObject.Parse(File.ReadAllText(config.InputFile));
            var jsonObjMasked = JObject.Parse(File.ReadAllText(config.OutputFile));
            string orig = jsonObjOrig["employee"].Value<string>("name");
            string masked = jsonObjMasked["employee"].Value<string>("name");
            Assert.NotEqual(orig, masked);
        }

        [Fact]
        public void T10_Mask_JSON_SingleElement_NoHeader_Test()
        {
            // === Arrange ===
            var config = MaskConfig.ReadConfig(utility.TestInstanceConfigJSONfile);
            config.InputFile = config.InputFile.Replace("data2.json", "data4.json");
            config.OutputFile = config.OutputFile.Replace("data2.json", "data4.json");

            // === Act ===
            var masker = new DataMasking(config);
            masker.Mask();

            // === Assert ===
            var contentsOrig = new List<string>(File.ReadLines(config.InputFile));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(contentsOrig.Count, contents.Count);

            var jsonObjOrig = JObject.Parse(File.ReadAllText(config.InputFile));
            var jsonObjMasked = JObject.Parse(File.ReadAllText(config.OutputFile));
            string orig = jsonObjOrig.Value<string>("name");
            string masked = jsonObjMasked.Value<string>("name");
            Assert.NotEqual(orig, masked);
        }

        [Fact]
        public void T11_Mask_JSON_Deep_Test()
        {
            // === Arrange ===
            var config = MaskConfig.ReadConfig(utility.TestInstanceConfigJSONfile);
            config.InputFile = config.InputFile.Replace("data2.json", "nested2WithArray.json");
            config.OutputFile = config.OutputFile.Replace("data2.json", "nested2WithArray.json");

            // === Act ===
            var masker = new DataMasking(config);
            masker.Mask();

            // === Assert ===
            var contentsOrig = new List<string>(File.ReadLines(config.InputFile));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(contentsOrig.Count, contents.Count);

            var jsonObjOrig = JObject.Parse(File.ReadAllText(config.InputFile));
            var jsonObjMasked = JObject.Parse(File.ReadAllText(config.OutputFile));
            string orig = jsonObjOrig["data"][0]["categories"][0].Value<string>("CategoryName");
            string masked = jsonObjMasked["data"][0]["categories"][0].Value<string>("CategoryName");
            Assert.NotEqual(orig, masked);
        }

        [Fact]
        public void T12_Mask_MaskType_Expression_Test()
        {
            // Arrange
            string csvContent = "Id;Name\r\n1;Carol Danvers\r\n2;Bruce Banner\r\n3;Peter Parker\r\n";
            string csvMaskedContent = "Id;Name\r\n1;Hero-1\r\n2;Hero-2\r\n3;Hero-3\r\n";
            string csvFilename = this.utility.CreateFileInTestInstanceDir(csvContent, "csv");
            var config = new MaskConfig()
            {
                InputFile = csvFilename,
                OutputFile = csvFilename.Replace(".csv", "_out.csv")
            };
            config.FieldMasks.Add(new FieldMask() { FieldName = "id", MaskType = MaskType.None });
            config.FieldMasks.Add(new FieldMask() 
            {
                FieldName = "name", 
                MaskType = MaskType.Expression,
                Expression = "Hero-{{id}}"
            });

            // Act
            var masker = new DataMasking(config);
            masker.Mask();

            // Assert
            Assert.True(File.Exists(config.OutputFile));
            var maskContents = new List<string>(csvMaskedContent.Split("\r\n"));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(4, contents.Count);
            for (int i = 0; i < contents.Count; i++)
            {
                Assert.Equal(maskContents[i], contents[i]);
            }
        }        

        [Fact]
        public void T13_Mask_MaskType_Expression_Regex_Test()
        {
            // Arrange
            string csvContent = "Id;Name\r\n1;Carol Danvers\r\n2;Bruce Banner\r\n3;Peter Parker\r\n";
            string csvMaskedContent = "Id;Name\r\n1;CarHero-1\r\n2;BruHero-2\r\n3;PetHero-3\r\n";
            string csvFilename = this.utility.CreateFileInTestInstanceDir(csvContent, "csv");
            var config = new MaskConfig()
            {
                InputFile = csvFilename,
                OutputFile = csvFilename.Replace(".csv", "_out.csv")
            };
            config.FieldMasks.Add(new FieldMask() { FieldName = "id", MaskType = MaskType.None });
            config.FieldMasks.Add(new FieldMask() 
            {
                FieldName = "name", 
                MaskType = MaskType.Expression,
                Expression = "Hero-{{id}}",
                RegExMatch = "(...)(.*)",
                RegExGroupToReplace = 2
            });

            // Act
            var masker = new DataMasking(config);
            masker.Mask();

            // Assert
            Assert.True(File.Exists(config.OutputFile));
            var maskContents = new List<string>(csvMaskedContent.Split("\r\n"));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(4, contents.Count);
            for (int i = 0; i < contents.Count; i++)
            {
                Assert.Equal(maskContents[i], contents[i]);
            }
        }   

        [Fact]
        public void T14_Mask_JSON_Expression_Test()
        {
            // === Arrange ===
            var config = MaskConfig.ReadConfig(utility.TestInstanceConfigJSONfile);
            config.FieldMasks.Clear();
            config.FieldMasks.Add(new FieldMask()
            {
                FieldName = "name",
                MaskType = MaskType.Expression,
                Expression = "Customer-{{$.id}}"
            });
            config.InputFile = config.InputFile.Replace("data2.json", "data5.json");
            config.OutputFile = config.OutputFile.Replace("data2.json", "data5.json");

            // === Act ===
            var masker = new DataMasking(config);
            masker.Mask();

            // === Assert ===
            var contentsOrig = new List<string>(File.ReadLines(config.InputFile));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(contentsOrig.Count, contents.Count);

            var jsonObjOrig = JObject.Parse(File.ReadAllText(config.InputFile));
            var jsonObjMasked = JObject.Parse(File.ReadAllText(config.OutputFile));
            string origId = jsonObjOrig.Value<string>("id");
            string maskedName = jsonObjMasked.Value<string>("name");
            Assert.Equal($"Customer-{origId}", maskedName);
        }       

        [Fact]
        public void T15_Mask_JSON_Expression_WrongJsonPath_Test()
        {
            // === Arrange ===
            var config = MaskConfig.ReadConfig(utility.TestInstanceConfigJSONfile);
            config.FieldMasks.Clear();
            config.FieldMasks.Add(new FieldMask()
            {
                FieldName = "name",
                MaskType = MaskType.Expression,
                Expression = "Customer-{{$.i2d}}"
            });
            config.InputFile = config.InputFile.Replace("data2.json", "data5.json");
            config.OutputFile = config.OutputFile.Replace("data2.json", "data5.json");

            // === Act ===
            bool hasException = false;
            var masker = new DataMasking(config);
            try 
            {
                masker.Mask();
            }
            catch(FieldNotFoundException)
            {
                hasException = true;
            }
            
            // === Assert ===
            Assert.True(hasException, "FieldNotFoundException was expected");
        }                               
    }
}