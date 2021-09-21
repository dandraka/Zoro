using System;
using Zoro.Processor;

namespace Zoro
{
    class Program
    {
        private static string configfile;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(@"Usage: Zoro.exe <path to config file>");
                Console.WriteLine(@"E.g. Zoro.exe c:\temp\mask.xml");
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
                Console.WriteLine(@"Press Enter to exit...");
                Console.ReadLine();

                return;
            }

            configfile = args[0];

            try
            {
                var config = MaskConfig.ReadConfig(configfile);
                var masker = new DataMasking(config);
                masker.Mask();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured:");
                Console.WriteLine("{0}: {1}\r\n{2}", ex.GetType(), ex.Message, ex.StackTrace);
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
            }
        }
    }
}