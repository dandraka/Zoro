using System;
using System.IO;
using System.Reflection;

namespace Zoro.Tests
{
    internal class Utility: IDisposable
    {
        public string TestInstanceDir;
        public string TestInstanceConfigfile;
        public string TestDataDir => Path.Combine(Utility.AssemblyDirectory, "data");

        public Utility()
        {
            TestInstanceDir = Path.Combine(Path.GetTempPath(), "Zorotests_" + (Guid.NewGuid().ToString()));
            Directory.CreateDirectory(TestInstanceDir);
            TestInstanceConfigfile = Path.Combine(TestInstanceDir, "testconfig.xml");

            Console.WriteLine($"TestInstanceDir = {TestInstanceDir}");
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
