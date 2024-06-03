using System;
using System.IO;
using System.Runtime.InteropServices;
using Dandraka.Zoro.Processor;

namespace Dandraka.Zoro
{
    class Program
    {
        private static string configFile = null;

        private static string inFile = null;

        private static string outFile = null;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(@"Usage: Zoro.exe <path to config file> [<optional path to input file>] [<optional path to output file>]");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.WriteLine(@"E.g. Zoro.exe c:\zoro\mask.xml");
                    Console.WriteLine(@"     Zoro.exe c:\zoro\mask.xml c:\data\original.csv c:\data\anonymized.csv");
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Console.WriteLine(@"E.g. ./zoro /home/jim/zoro/mask.xml");
                    Console.WriteLine(@"     ./zoro /home/jim/zoro/mask.xml /home/jim/data/original.csv /home/jim/data/anonymized.csv");
                }
                Console.WriteLine(@"     Input & Output files are optional, but if specified they");
                Console.WriteLine(@"     take precedence over (i.e. are used instead of) the config file.");
                Console.WriteLine(@"Sample config file:");
                Console.WriteLine("<?xml version=\"1.0\"?>");
                Console.WriteLine("<MaskConfig xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
                Console.WriteLine(@"  <FieldMasks>");
                Console.WriteLine(@"    <FieldMask>");
                Console.WriteLine(@"      <FieldName>Name</FieldName>");
                Console.WriteLine(@"      <MaskType>Similar</MaskType>");
                Console.WriteLine(@"    </FieldMask>");
                Console.WriteLine(@"    <FieldMask>");
                Console.WriteLine(@"      <FieldName>MainPhone</FieldName>");
                Console.WriteLine(@"      <MaskType>Similar</MaskType>");
                Console.WriteLine(@"      <RegExMatch>^(\+\d\d)?(.*)$</RegExMatch>");
                Console.WriteLine(@"      <RegExGroupToReplace>2</RegExGroupToReplace>");
                Console.WriteLine(@"    </FieldMask>");
                Console.WriteLine(@"    <FieldMask>");
                Console.WriteLine(@"      <FieldName>Fax</FieldName>");
                Console.WriteLine(@"      <MaskType>Asterisk</MaskType>");
                Console.WriteLine(@"      <Asterisk>9</Asterisk>");
                Console.WriteLine(@"      <RegExMatch>^(\+\d\d)?(.*)$</RegExMatch>");
                Console.WriteLine(@"      <RegExGroupToReplace>2</RegExGroupToReplace>");
                Console.WriteLine(@"    </FieldMask>");
                Console.WriteLine(@"    <FieldMask>");
                Console.WriteLine(@"      <FieldName>BankAccount</FieldName>");
                Console.WriteLine(@"      <MaskType>Asterisk</MaskType>");
                Console.WriteLine(@"      <Asterisk>9</Asterisk>");
                Console.WriteLine(@"    </FieldMask>");
                Console.WriteLine(@"    <FieldMask>");
                Console.WriteLine(@"      <FieldName>Street</FieldName>");
                Console.WriteLine(@"      <MaskType>List</MaskType>");
                Console.WriteLine(@"      <ListOfPossibleReplacements>");
                Console.WriteLine(@"        <Replacement Selector=""Country=Netherlands"" List=""Nootdorpstraat,Nolensstraat,Statensingel"" />");
                Console.WriteLine(@"        <Replacement Selector=""Country=Germany"" List=""Bahnhofstraße,Freigaße,Hauptstraße"" />");
                Console.WriteLine(@"        <Replacement Selector=""Country=France"" List=""Rue Nationale,Boulevard Vauban,Rue des Stations"" />");
                Console.WriteLine(@"        <Replacement Selector="""" List=""Bedford Gardens,Sheffield Terrace,Kensington Palace Gardens"" />");
                Console.WriteLine(@"      </ListOfPossibleReplacements>");
                Console.WriteLine(@"    </FieldMask>");
                Console.WriteLine(@"  </FieldMasks>");
                Console.WriteLine(@"  <InputFile>C:\\temp\\Zorotests\\data.csv</InputFile>");
                Console.WriteLine(@"  <OutputFile>C:\\temp\\Zorotests\\maskeddata.csv</OutputFile>");
                Console.WriteLine(@"  <Delimiter>;</Delimiter>");
                Console.WriteLine(@"</MaskConfig>");

                return;
            }

            // cross platform
            configFile = Path.GetFullPath(args[0]
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar));

            if (args.Length >= 2)
            {
                inFile = Path.GetFullPath(args[1]
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar));
            }
            if (args.Length >= 3)
            {
                outFile = Path.GetFullPath(args[2]
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar));
            }

            if (!File.Exists(configFile))
            {
                Console.WriteLine($"WARNING: Config file {configFile} was not found, exiting.");
                Console.WriteLine(@"Usage: Zoro.exe <path to config file> [<optional path to input file>] [<optional path to output file>]");              
                return;
            }

            try
            {
                var config = MaskConfig.ReadConfig(configFile);
                if (!string.IsNullOrEmpty(inFile)) 
                { 
                    config.InputFile = inFile;
                }
                if (!string.IsNullOrEmpty(outFile))
                {
                    config.OutputFile = outFile;
                }

                var masker = new DataMasking(config);
                masker.Mask();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured:");
                Console.WriteLine("{0}: {1}\r\n{2}", ex.GetType(), ex.Message, ex.StackTrace);
            }
        }
    }
}