using System;
using System.Data.Common;
using System.IO;
using System.Reflection;

namespace Dandraka.Zoro.Tests
{
    internal class Utility: IDisposable
    {
        public string TestInstanceDir;
        public string TestInstanceConfigfile;
        public string TestDataDir => Path.Combine(Utility.AssemblyDirectory, "data");

        public DbConnection TestDbConnection;

        private string compactDbFile;        

        public Utility()
        {
            TestInstanceDir = Path.Combine(Path.GetTempPath(), "Zorotests_" + (Guid.NewGuid().ToString()));
            Directory.CreateDirectory(TestInstanceDir);
            TestInstanceConfigfile = Path.Combine(TestInstanceDir, "testconfig.xml");

            Console.WriteLine($"TestInstanceDir = {TestInstanceDir}");
        }

        public void PrepareLocalDb()
        {
            this.compactDbFile = Path.GetTempFileName();
            string connectionString = $"Data Source={compactDbFile};Persist Security Info=False";

            Console.WriteLine($"Created Db file {this.compactDbFile}");

            this.TestDbConnection = DbProviderFactories.GetFactory("System.Data.SqlServerCe.4.0").CreateConnection();
            this.TestDbConnection.ConnectionString = connectionString;
            this.TestDbConnection.Open();

            var cmdCreateTable = this.TestDbConnection.CreateCommand();
            cmdCreateTable.CommandType = System.Data.CommandType.Text;
            cmdCreateTable.CommandText = "CREATE TABLE csvdata (ID int, Name nvarchar(100), BankAccount varchar(50))";
            cmdCreateTable.ExecuteNonQuery();
            
            var csvContents = File.ReadLines(Path.Combine(TestDataDir, "data1.csv"));
            foreach(string csvLine in csvContents)
            {
                if (csvLine.StartsWith("ID"))
                {
                    continue;
                }
                var csvFields = csvLine.Split(';');
                string insertSql = $"INSERT INTO csvdata (ID, Name, BankAccount) VALUES ({csvFields[0]},'{csvFields[1]}','{csvFields[2]}')";

                var cmdInsert = this.TestDbConnection.CreateCommand();
                cmdInsert.CommandType = System.Data.CommandType.Text;
                cmdInsert.CommandText = insertSql;
                cmdInsert.ExecuteNonQuery();                
            }
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
    }
}
