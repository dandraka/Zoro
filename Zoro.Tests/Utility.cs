using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Text;
using System.Data.SQLite;
using System.Collections.Generic;

namespace Dandraka.Zoro.Tests
{
    internal class Utility : IDisposable
    {
        public string TestInstanceDir;
        public string TestInstanceConfigfile;
        public string TestDataDir => Path.Combine(Utility.AssemblyDirectory, "data");

        public DbConnection TestDbConnection;

        public Utility()
        {
            TestInstanceDir = Path.Combine(Path.GetTempPath(), "Zorotests_" + (Guid.NewGuid().ToString()));
            Directory.CreateDirectory(TestInstanceDir);
            TestInstanceConfigfile = Path.Combine(TestInstanceDir, "testconfig.xml");

            Console.WriteLine($"TestInstanceDir = {TestInstanceDir}");
        }

        public void PrepareLocalDb()
        {
            this.TestDbConnection = new SQLiteConnection("Data Source=:memory:");
            this.TestDbConnection.Open();

            var cmdCreateTable = this.TestDbConnection.CreateCommand();
            cmdCreateTable.CommandType = System.Data.CommandType.Text;
            cmdCreateTable.CommandText = "CREATE TABLE csvdata (ID int, Name nvarchar(100), BankAccount varchar(50))";
            cmdCreateTable.ExecuteNonQuery();

            var csvContents = new List<string>(File.ReadLines(Path.Combine(TestDataDir, "data1.csv")));
            var csvFields = csvContents[0].Split(';');

            string insertSql = $"INSERT INTO csvdata (ID, Name, BankAccount) VALUES ($ID, $Name, $BankAccount)";
            var cmdInsert = this.TestDbConnection.CreateCommand();
            cmdInsert.CommandType = System.Data.CommandType.Text;
            cmdInsert.CommandText = insertSql;
            foreach (string csvField in csvFields)
            {
                cmdInsert.Parameters.Add(new SQLiteParameter($"${csvField}"));
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
            cmdSelect.CommandText = "select * from csvdata";
            var rd = cmdSelect.ExecuteReader();
            var tbl = new DataTable();
            tbl.Load(rd);
            string tblContents = DumpDataTable(tbl);
            Console.WriteLine("============= Initial DB data =============");
            Console.Write(tblContents);
            Console.WriteLine("===========================================");
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
                    this.TestDbConnection.Close();
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

        private static string DumpDataTable(DataTable table)
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
