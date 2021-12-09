using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Text;
using System.Data.SQLite;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;

namespace Dandraka.Zoro.Tests
{
    internal class Utility : IDisposable
    {
        public string TestInstanceDir;
        public string TestInstanceConfigfile;
        public string TestDataDir => Path.Combine(Utility.AssemblyDirectory, "data");

        public DbConnection TestDbConnection;

        public char DbParamChar => this.TestDbConnection is SqlConnection ? '@' : '$';

        public string TestTableName { get; private set; }

        public string TestTablesToDrop { get; set; }

        public Utility()
        {
            TestInstanceDir = Path.Combine(Path.GetTempPath(), "Zorotests_" + (Guid.NewGuid().ToString()));
            Directory.CreateDirectory(TestInstanceDir);
            TestInstanceConfigfile = Path.Combine(TestInstanceDir, "testconfig.xml");

            Console.WriteLine($"TestInstanceDir = {TestInstanceDir}");
        }

        public void PrepareSqliteDb(string tableName)
        {
            this.TestDbConnection = new SQLiteConnection("Data Source=:memory:");
            this.PrepareDb(tableName);
        }

        public void PrepareSqlServerDb(string tableName, string connString)
        {
            this.TestDbConnection = new SqlConnection(connString);
            this.PrepareDb(tableName);
        }

        private void PrepareDb(string tableName = "testdata")
        {
            this.TestDbConnection.Open();
            this.TestTableName = tableName;

            var cmdCreateTable = this.TestDbConnection.CreateCommand();
            cmdCreateTable.CommandType = System.Data.CommandType.Text;
            cmdCreateTable.CommandText = $"CREATE TABLE {tableName} (ID int, Name nvarchar(100), BankAccount nvarchar(50), Address nvarchar(100), Country nvarchar(2))";
            cmdCreateTable.ExecuteNonQuery();

            var csvContents = new List<string>(File.ReadLines(Path.Combine(TestDataDir, "data1.csv")));
            var csvFields = csvContents[0].Split(';');

            string insertSql = $"INSERT INTO {tableName} (ID, Name, BankAccount,Address,Country) VALUES ({DbParamChar}ID, {DbParamChar}Name, {DbParamChar}BankAccount,{DbParamChar}Address,{DbParamChar}Country)";
            var cmdInsert = this.TestDbConnection.CreateCommand();
            cmdInsert.CommandType = System.Data.CommandType.Text;
            cmdInsert.CommandText = insertSql;
            foreach (string csvField in csvFields)
            {
                switch (this.TestDbConnection.GetType().ToString())
                {
                    case "System.Data.SqlClient.SqlConnection":
                        var dbType = csvField == "ID" ? SqlDbType.Int : SqlDbType.NVarChar;
                        cmdInsert.Parameters.Add(new SqlParameter($"{DbParamChar}{csvField}", dbType));
                        break;
                    default:
                        cmdInsert.Parameters.Add(new SQLiteParameter($"{DbParamChar}{csvField}"));
                        break;
                }                
            }

            for (int i = 1; i < csvContents.Count; i++)
            {
                string csvLine = csvContents[i];
                var csvLineFields = csvLine.Split(';');
                for (int j = 0; j < csvLineFields.Length; j++)
                {
                    cmdInsert.Parameters[j].Value = csvLineFields[j];
                }
                cmdInsert.ExecuteNonQuery();
            }

            var cmdSelect = this.TestDbConnection.CreateCommand();
            cmdSelect.CommandType = System.Data.CommandType.Text;
            cmdSelect.CommandText = $"SELECT * FROM {tableName}";
            var rd = cmdSelect.ExecuteReader();
            var tbl = new DataTable(tableName);
            tbl.Load(rd);
            /*
            string tblContents = DumpDataTable(tbl);
            Console.WriteLine("============= Initial DB data =============");
            Console.Write(tblContents);
            Console.WriteLine("===========================================");
            */
        }

        public void PrepareTestInstanceDir()
        {
            foreach (string filename in Directory.EnumerateFiles(TestDataDir))
            {
                File.Copy(filename, Path.Combine(TestInstanceDir, Path.GetFileName(filename)), true);
                //Console.WriteLine($"Copied {filename} to {TestInstanceDir}");
            }

            if (!File.Exists(TestInstanceConfigfile))
            {
                throw new FileNotFoundException(TestInstanceConfigfile);
            }
            string configContents = File.ReadAllText(TestInstanceConfigfile);
            configContents = configContents.Replace("%TestInstanceDir%", TestInstanceDir);
            File.WriteAllText(TestInstanceConfigfile, configContents);
        }

        public string CreateFileInTestInstanceDir(string contents, string ext)
        {
            string fileName = Path.Combine(this.TestInstanceDir, (Guid.NewGuid()) + "." + ext.Replace(".", ""));
            File.WriteAllText(fileName, contents);
            return fileName;
        }

        // perform clean up
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(this.TestInstanceDir))
                {
                    Directory.Delete(this.TestInstanceDir, true);
                }
                if (this.TestDbConnection != null
                    && this.TestDbConnection.State == System.Data.ConnectionState.Open)
                {
                    try
                    {
                        var tblsToDrop = new List<string>();
                        if (!string.IsNullOrEmpty(this.TestTableName)) { tblsToDrop.Add(this.TestTableName); }
                        if (!string.IsNullOrEmpty(this.TestTablesToDrop)) { this.TestTablesToDrop.Split(';').ToList().ForEach(x => tblsToDrop.Add(x)); }

                        foreach (string tblToDrop in tblsToDrop)
                        {
                            var cmdDropTable = this.TestDbConnection.CreateCommand();
                            cmdDropTable.CommandType = System.Data.CommandType.Text;
                            cmdDropTable.CommandText = $"DROP TABLE {tblToDrop}";
                            cmdDropTable.ExecuteNonQuery();
                            Console.WriteLine($"Dropped table {tblToDrop}");                            
                        }
                    }
                    catch
                    {
                        // meh
                    }
                    this.TestDbConnection.Close();
                    Console.WriteLine("Closed DB connection");
                }
            }
            catch
            {
                // ugh, never mind
            }
        }

        private static string AssemblyDirectory
        {
            get
            {
                string codebaseLocation = Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(codebaseLocation);
            }
        }

        internal static string DumpDataTable(DataTable table)
        {
            string data = string.Empty;
            StringBuilder sb = new StringBuilder();

            if (null != table && null != table.Rows)
            {
                foreach (DataRow dataRow in table.Rows)
                {
                    foreach (var item in dataRow.ItemArray)
                    {
                        sb.Append(item);
                        sb.Append(',');
                    }
                    sb.AppendLine();
                }

                data = sb.ToString();
            }
            return data;
        }
    }
}
