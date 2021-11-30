using System;
using System.Data;
using System.Collections.Generic;
using Xunit;
using Dandraka.Zoro.Processor;
using Dandraka.Zoro.Tests;
using System.IO;

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
            utility.PrepareTestInstanceDir();
            utility.PrepareLocalDb();            
        }

        public void Dispose()
        {
            this.utility.Dispose();
        }

        [Fact]
        public void T01_Db2Csv()
        {
            var config = new MaskConfig()
            {                
                DataSource = DataSource.Database,
                DataDestination = DataDestination.CsvFile,
                SqlSelect = $"SELECT * FROM {utility.TestTableName}",
                OutputFile = Path.Combine(utility.TestInstanceDir, "T01_Db2Csv.csv")
            };
            config.SetConnection(utility.TestDbConnection);
            config.FieldMasks.Add(new FieldMask() { FieldName = "Name", MaskType = MaskType.None });
            config.FieldMasks.Add(new FieldMask() { FieldName = "BankAccount", MaskType = MaskType.Asterisk });
            config.FieldMasks.Add(new FieldMask() { FieldName = "Country", MaskType = MaskType.None });
            config.FieldMasks.Add(new FieldMask() { FieldName = "Address", MaskType = MaskType.None });          

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
        public void T02_Csv2Db()
        {
            string tblName = "T02_Csv2Db";

            var config = new MaskConfig()
            {                
                DataSource = DataSource.CsvFile,
                DataDestination = DataDestination.Database,
                SqlCommand = $"INSERT INTO {tblName} (ID, Name, Bankaccount) VALUES ($ID, '$Name', '$BankAccount')",
                InputFile = Path.Combine(utility.TestInstanceDir, "data1.csv")
            };
            config.SetConnection(utility.TestDbConnection);
            config.FieldMasks.Add(new FieldMask() { FieldName = "ID", MaskType = MaskType.None });
            config.FieldMasks.Add(new FieldMask() { FieldName = "Name", MaskType = MaskType.None });
            config.FieldMasks.Add(new FieldMask() { FieldName = "BankAccount", MaskType = MaskType.Asterisk });

            var cmdTbl = utility.TestDbConnection.CreateCommand();
            cmdTbl.CommandText = $"CREATE TABLE {tblName} (ID int, Name nvarchar(100), BankAccount varchar(50))";
            cmdTbl.ExecuteNonQuery();

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

            var cmdSel = utility.TestDbConnection.CreateCommand();
            cmdSel.CommandText = $"SELECT * FROM {tblName}";
            var dt = new DataTable();
            dt.Load(cmdSel.ExecuteReader());

            Assert.Equal(4, dt.Rows.Count);
        }        
    }
}