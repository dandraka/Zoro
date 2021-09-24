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
    public class DataMasking_Tests: IDisposable
    {
        private Utility utility = new Utility();

        public DataMasking_Tests()
        {
            utility.PrepareTestInstanceDir();
        }

        public void Dispose()
        {
            this.utility.Dispose();
        }

        [SkippableFact]
        public void T01_TestSecret()
        {
            // test github secrets
            var secret = Environment.GetEnvironmentVariable("TESTSECRET");
            Skip.If(string.IsNullOrWhiteSpace(secret), "No secret info found, is the environment variable 'TESTSECRET' set?");
            Assert.Equal("LALALA", secret);
        }

        [Fact]
        public void T02_Mask_CSV_Test()
        {
            var config = MaskConfig.ReadConfig(utility.TestInstanceConfigfile);
            Console.WriteLine($"Config: InputFile = {config.InputFile}");
            Console.WriteLine($"Config: OutputFile = {config.OutputFile}");
            var masker = new DataMasking(config);
            masker.Mask();

            Assert.True(File.Exists(config.OutputFile));
            var contents = new List<string>(File.ReadLines(config.OutputFile));            
            Assert.Equal(5, contents.Count);
        }

        [SkippableFact]
        public void T03_Mask_DB_Test()
        {
            var connstr = Environment.GetEnvironmentVariable("SQLCONNSTRING");

            // skip if no db config found
            Skip.If(string.IsNullOrWhiteSpace(connstr), "No database connection info found, is the environment variable 'SQLCONNSTRING' set?");

            var config = new MaskConfig()
            {
                ConnectionString = connstr,
                DataSource = DataSource.Database,
                SqlSelect = "SELECT * FROM testdata",
                OutputFile = Path.Combine(utility.TestInstanceDir, "maskeddata_db_02.csv"),
                FieldMasks = new List<FieldMask>()
            };
            config.FieldMasks.Add(new FieldMask() { FieldName = "name", MaskType = MaskType.Similar });
            config.FieldMasks.Add(new FieldMask() { FieldName = "iban", MaskType = MaskType.Asterisk });
            config.FieldMasks.Add(new FieldMask() { FieldName = "country", MaskType = MaskType.None });
            config.FieldMasks.Add(new FieldMask() { FieldName = "address", MaskType = MaskType.List });
            config.FieldMasks[3].ListOfPossibleReplacements = new List<Replacement>();
            config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement() 
                { Selector = "country=CH", ReplacementList="Bahnhofstrasse 41,Hauptstrasse 8,Berggasse 4" });
            config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement() 
                { Selector = "country=GR", ReplacementList="Evangelistrias 22,Thessalias 47,Eparhiaki Odos Lefkogion 6" });
            config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement() 
                { Selector = "", ReplacementList="Main Street 9,Fifth Avenue 104,Ranch rd. 1" });                

            var masker = new DataMasking(config);
            masker.Mask();

            Assert.True(File.Exists(config.OutputFile));
            var contents = new List<string>(File.ReadLines(config.OutputFile));            
            Assert.Equal(4, contents.Count);            
        }
    }
}