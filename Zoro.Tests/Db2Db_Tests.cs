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
        public Db2Db_Tests()
        {
            //
        }

        public void Dispose()
        {
            //
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
        public void T02_Db2Csv_Sqlite()
        {
            string tblName = $"T01_Db2Csv_{Guid.NewGuid().ToString().Substring(0, 8)}";
            using (var utility = new Utility())
            {
                utility.PrepareTestInstanceDir();
                utility.PrepareSqliteDb(tblName);

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
        }

        [Fact]
        public void T03_Csv2Db_Sqlite()
        {
            string tblName = $"T02_Csv2Db_{Guid.NewGuid().ToString().Substring(0, 8)}";
            string tblName2 = $"T02_Csv2Db_{Guid.NewGuid().ToString().Substring(0, 8)}";
            using (var utility = new Utility())
            {
                utility.PrepareTestInstanceDir();
                utility.PrepareSqliteDb(tblName);

                var config = new MaskConfig()
                {
                    DataSource = DataSource.CsvFile,
                    DataDestination = DataDestination.Database,
                    SqlCommand = $"INSERT INTO {tblName2} (ID, Name, Bankaccount) VALUES ($ID, '$Name', '$BankAccount')",
                    InputFile = Path.Combine(utility.TestInstanceDir, "data1.csv")
                };
                config.SetConnection(utility.TestDbConnection);
                config.FieldMasks.Add(new FieldMask() { FieldName = "ID", MaskType = MaskType.None });
                config.FieldMasks.Add(new FieldMask() { FieldName = "Name", MaskType = MaskType.None });
                config.FieldMasks.Add(new FieldMask() { FieldName = "BankAccount", MaskType = MaskType.Asterisk });

                var cmdTbl = utility.TestDbConnection.CreateCommand();
                cmdTbl.CommandText = $"CREATE TABLE {tblName2} (ID int, Name nvarchar(100), BankAccount varchar(50))";
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
                cmdSel.CommandText = $"SELECT * FROM {tblName2}";
                var dt = new DataTable();
                dt.Load(cmdSel.ExecuteReader());

                Assert.Equal(4, dt.Rows.Count);
            }
        }

        [Fact]
        public void T04_Db2Db_Sqlite()
        {
            string tblName = $"T03_Db2Db_{Guid.NewGuid().ToString().Substring(0, 8)}";
            using (var utility = new Utility())
            {
                utility.PrepareTestInstanceDir();
                utility.PrepareSqliteDb(tblName);

                // debug: get list of tables
                /*
                var testCmd = utility.TestDbConnection.CreateCommand();
                testCmd.CommandText = "SELECT name FROM sqlite_schema WHERE type ='table' AND name NOT LIKE 'sqlite_%'";
                var tbl = new DataTable();
                using (var rd = testCmd.ExecuteReader())
                {
                    tbl.Load(rd);
                }
                string tablesStr = Utility.DumpDataTable(tbl);
                Console.Write($"========== List of tables: {tablesStr} ==========");
                */

                var config = new MaskConfig()
                {
                    DataSource = DataSource.Database,
                    DataDestination = DataDestination.Database,
                    SqlSelect = $"SELECT * FROM {utility.TestTableName}",
                    SqlCommand = $"UPDATE {utility.TestTableName} SET Name=$Name, Bankaccount=$Bankaccount, Address=$Address WHERE ID = $ID",
                    OutputFile = Path.Combine(utility.TestInstanceDir, "T01_Db2Csv.csv")
                };
                config.SetConnection(utility.TestDbConnection);
                config.FieldMasks.Add(new FieldMask() { FieldName = "ID", MaskType = MaskType.None });
                config.FieldMasks.Add(new FieldMask() { FieldName = "Name", MaskType = MaskType.None });
                config.FieldMasks.Add(new FieldMask() { FieldName = "BankAccount", MaskType = MaskType.Asterisk });
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
            }
        }

        [SkippableFact]
        public void T05_Db2Csv_SqlServer()
        {
            string tblName = $"T05_Db2Csv_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var connstr = Environment.GetEnvironmentVariable("SQLCONNSTRING");

            // skip if no db config found
            Skip.If(string.IsNullOrWhiteSpace(connstr), "No database connection info found, is the environment variable 'SQLCONNSTRING' set?");

            using (var utility = new Utility())
            {
                utility.PrepareTestInstanceDir();
                utility.PrepareSqlServerDb(tblName, connstr);

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
        }

        [SkippableFact]
        public void T06_Csv2Db_SqlServer()
        {
            string tblName = $"T06_Csv2Db_{Guid.NewGuid().ToString().Substring(0, 8)}";
            string tblName2 = $"T06_Csv2Db_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var connstr = Environment.GetEnvironmentVariable("SQLCONNSTRING");

            // skip if no db config found
            Skip.If(string.IsNullOrWhiteSpace(connstr), "No database connection info found, is the environment variable 'SQLCONNSTRING' set?");

            using (var utility = new Utility())
            {
                utility.PrepareTestInstanceDir();
                utility.PrepareSqlServerDb(tblName, connstr);

                var config = new MaskConfig()
                {
                    DataSource = DataSource.CsvFile,
                    DataDestination = DataDestination.Database,
                    SqlCommand = $"INSERT INTO {tblName2} (ID, Name, Bankaccount) VALUES (@ID, '@Name', '@BankAccount')",
                    InputFile = Path.Combine(utility.TestInstanceDir, "data1.csv")
                };
                config.SetConnection(utility.TestDbConnection);
                config.FieldMasks.Add(new FieldMask() { FieldName = "ID", MaskType = MaskType.None });
                config.FieldMasks.Add(new FieldMask() { FieldName = "Name", MaskType = MaskType.None });
                config.FieldMasks.Add(new FieldMask() { FieldName = "BankAccount", MaskType = MaskType.Asterisk });

                var cmdTbl = utility.TestDbConnection.CreateCommand();
                cmdTbl.CommandText = $"CREATE TABLE {tblName2} (ID int, Name nvarchar(100), BankAccount varchar(50))";
                cmdTbl.ExecuteNonQuery();
                utility.TestTablesToDrop = tblName2;

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
                cmdSel.CommandText = $"SELECT * FROM {tblName2}";
                var dt = new DataTable();
                dt.Load(cmdSel.ExecuteReader());

                Assert.Equal(4, dt.Rows.Count);
            }
        }

        [SkippableFact]
        public void T07_Db2Db_SqlServer()
        {
            string tblName = $"T03_Db2Db_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var connstr = Environment.GetEnvironmentVariable("SQLCONNSTRING");

            // skip if no db config found
            Skip.If(string.IsNullOrWhiteSpace(connstr), "No database connection info found, is the environment variable 'SQLCONNSTRING' set?");

            using (var utility = new Utility())
            {
                utility.PrepareTestInstanceDir();
                utility.PrepareSqlServerDb(tblName, connstr);

                // debug: get list of tables
                /*
                var testCmd = utility.TestDbConnection.CreateCommand();
                testCmd.CommandText = "SELECT name FROM sqlite_schema WHERE type ='table' AND name NOT LIKE 'sqlite_%'";
                var tbl = new DataTable();
                using (var rd = testCmd.ExecuteReader())
                {
                    tbl.Load(rd);
                }
                string tablesStr = Utility.DumpDataTable(tbl);
                Console.Write($"========== List of tables: {tablesStr} ==========");
                */

                var config = new MaskConfig()
                {
                    DataSource = DataSource.Database,
                    DataDestination = DataDestination.Database,
                    SqlSelect = $"SELECT * FROM {utility.TestTableName}",
                    SqlCommand = $"UPDATE {utility.TestTableName} SET Name=@Name, Bankaccount=@Bankaccount, Address=@Address WHERE ID = @ID",
                    OutputFile = Path.Combine(utility.TestInstanceDir, "T01_Db2Csv.csv")
                };
                config.SetConnection(utility.TestDbConnection);
                config.FieldMasks.Add(new FieldMask() { FieldName = "ID", MaskType = MaskType.None });
                config.FieldMasks.Add(new FieldMask() { FieldName = "Name", MaskType = MaskType.None });
                config.FieldMasks.Add(new FieldMask() { FieldName = "BankAccount", MaskType = MaskType.Asterisk });
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
            }
        }                
    }
}