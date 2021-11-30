using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Dandraka.Zoro.Processor;

namespace Dandraka.Zoro.Tests
{
    /// <summary>
    /// Tests for the <c>DataMasking</c> class.
    /// </summary>
    public class DataMasking_Tests: IDisposable
    {
        private Utility utility = new Utility();

        public DataMasking_Tests()
        {
            utility.PrepareTestInstanceDir();
            utility.PrepareLocalDb();
        }

        public void Dispose()
        {
            this.utility.Dispose();
        }

        [Fact]
        public void T01_Mask_CSV_Test()
        {
            var config = MaskConfig.ReadConfig(utility.TestInstanceConfigfile);
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
                { Selector = "country=CH", ReplacementList="Bahnhofstrasse 41,Hauptstrasse 8,Berggasse 4" });
            config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement() 
                { Selector = "country=GR", ReplacementList="Evangelistrias 22,Thessalias 47,Eparhiaki Odos Lefkogion 6" });
            config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement() 
                { Selector = "", ReplacementList="Main Street 9,Fifth Avenue 104,Ranch rd. 1" });                

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
            for(int i = 0; i < contents.Count; i++)
            {
                Assert.Equal(maskContents[i], contents[i]);
            }
        }

        [Fact]
        public void T05_Mask_MaskType_Asterisk_Test()
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
            for(int i = 0; i < contents.Count; i++)
            {
                Assert.Equal(maskContents[i], contents[i]);
            }
        }

        [Fact]
        public void T06_Mask_MaskType_List_Test()
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
            for(int i = 0; i < contents.Count; i++)
            {
                Assert.Equal(maskContents[i], contents[i]);
            }
        }    

        [Fact]
        public void T07_Mask_MaskType_Similar_Test()
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
            config.FieldMasks.Add(new FieldMask() { FieldName = "name", MaskType = MaskType.Similar });

            // Act
            var masker = new DataMasking(config);
            masker.Mask();

            // Assert
            Assert.True(File.Exists(config.OutputFile));
            var contents = new List<string>(File.ReadLines(config.OutputFile));
            Assert.Equal(4, contents.Count);
            for(int i = 1; i < contents.Count; i++)
            {
                var items = contents[i].Split(';');
                // id
                Assert.Equal(i.ToString(), items[0]);
            }
        }                    
    }
}