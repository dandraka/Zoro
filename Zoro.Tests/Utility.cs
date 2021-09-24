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

        public void PrepareTestInstanceDir()
        {
            TestInstanceDir = Path.Combine(Path.GetTempPath(), "Zorotests_" + (Guid.NewGuid().ToString()));
            Directory.CreateDirectory(TestInstanceDir);

            foreach (string filename in Directory.EnumerateFiles(TestDataDir))
            {
                File.Copy(filename, Path.Combine(TestInstanceDir, Path.GetFileName(filename)), true);
                Console.WriteLine($"Copied {filename} to {TestInstanceDir}");
            }

            TestInstanceConfigfile = Path.Combine(TestInstanceDir, "testconfig.xml");
            if (!File.Exists(TestInstanceConfigfile))
            {
                throw new FileNotFoundException(TestInstanceConfigfile);
            }
            string configContents = File.ReadAllText(TestInstanceConfigfile);
            configContents = configContents.Replace("%TestInstanceDir%", TestInstanceDir);
            File.WriteAllText(TestInstanceConfigfile, configContents);
        }

        // clean up
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
                // ugh, never mind, nbd
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
