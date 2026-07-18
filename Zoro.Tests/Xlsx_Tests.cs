using System;
using Xunit;
using Dandraka.Zoro.Processor;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Dandraka.Zoro.Tests
{
    /// <summary>
    /// Tests for the <c>DataMasking</c> class where both data source and data destination are database.
    /// </summary>
    public class Xlsx_Tests : IDisposable
    {
        public Xlsx_Tests()
        {
            //
        }

        public void Dispose()
        {
            //
        }

        [Fact]
        public void T01_Xlsx_InvalidOutputType()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Console.WriteLine($"Starting {testName}");
            using (var utility = new Utility())
            {
                utility.PrepareTestInstanceDir();

                var config = new MaskConfig()
                {
                    DataSource = DataSource.XlsXFile,
                    DataDestination = DataDestination.CsvFile,
                    InputFile = Path.Combine(utility.TestInstanceDir, "ibans.xlsx"),
                    OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.csv")
                };
                config.FieldMasks.Add(new FieldMask() { FieldName = "book", MaskType = MaskType.Asterisk });

                // === Act ===
                Exception ex = null;
                var masker = new DataMasking(config);
                try
                {
                    masker.Mask();
                }
                catch (System.Exception e)
                {
                    ex = e;
                }

                // === Assert ===

                // Examine exception
                Assert.NotNull(ex);
                Assert.IsType<NotSupportedException>(ex);
                Assert.Equal("For xlsx source, only xlsx destination is supported.", ex.Message);
            }
        }

        [Fact]
        public void T02_Xlsx_InvalidMaskType()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Console.WriteLine($"Starting {testName}");
            using (var utility = new Utility())
            {
                utility.PrepareTestInstanceDir();

                var config = new MaskConfig()
                {
                    DataSource = DataSource.XlsXFile,
                    DataDestination = DataDestination.XlsXFile,
                    InputFile = Path.Combine(utility.TestInstanceDir, "ibans.xlsx"),
                    OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.xlsx")
                };
                config.FieldMasks.Add(new FieldMask() { FieldName = "book", MaskType = MaskType.Expression, Expression = "something" });

                // === Act ===
                Exception ex = null;
                var masker = new DataMasking(config);
                try
                {
                    masker.Mask();
                }
                catch (System.Exception e)
                {
                    ex = e;
                }

                // === Assert ===

                // Examine exception
                Assert.NotNull(ex);
                Assert.IsType<NotSupportedException>(ex);
                Assert.Equal("For Office file types, the Expression mask type is not supported.", ex.Message);
            }
        }

        [Theory]
        [InlineData("AT", "AT******************")]
        [InlineData("CH", "CH*******************")]
        [InlineData("NO", "NO*************")]
        public void T03_Xlsx_Asterisk(string countryCode, string expectedContent)
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Console.WriteLine($"Starting {testName}");

            using (var utility = new Utility())
            {
                utility.PrepareTestInstanceDir();

                var config = new MaskConfig()
                {
                    DataSource = DataSource.XlsXFile,
                    DataDestination = DataDestination.XlsXFile,
                    InputFile = Path.Combine(utility.TestInstanceDir, "ibans.xlsx"),
                    OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.xlsx")
                };
                config.FieldMasks.Add(new FieldMask()
                {
                    FieldName = "IBAN",
                    MaskType = MaskType.Asterisk,
                    RegExMatch = @$"\b({countryCode})(\w{{12,30}})\b",
                    RegExGroupToReplace = 2
                });

                // === Act ===
                var masker = new DataMasking(config);
                masker.Mask();

                // === Assert ===

                // Manually examine                
                // Utility.OpenDocument(config.OutputFile);
                // does the file exist?
                Assert.True(File.Exists(config.OutputFile));

                // Examine the document
                bool found = false;
                using (var doc = SpreadsheetDocument.Open(config.OutputFile, false))
                {
                    var sstPart = doc.WorkbookPart.SharedStringTablePart;
                    if (sstPart != null)
                    {
                        foreach (var text in sstPart.SharedStringTable.Elements<DocumentFormat.OpenXml.Spreadsheet.SharedStringItem>())
                        {
                            if (text.Text.Text == expectedContent)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                }

                Assert.True(found);
            }
        }
        
        [Theory]
        [InlineData("AT", "AT[A-Za-z0-9]{18}")]
        [InlineData("CH", "CH[A-Za-z0-9]{19}")]
        [InlineData("NO", "NO[A-Za-z0-9]{13}")]
        public void T04_Xlsx_Random(string countryCode, string expectedRegEx)
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Console.WriteLine($"Starting {testName}");

            using (var utility = new Utility())
            {
                utility.PrepareTestInstanceDir();

                var config = new MaskConfig()
                {
                    DataSource = DataSource.XlsXFile,
                    DataDestination = DataDestination.XlsXFile,
                    InputFile = Path.Combine(utility.TestInstanceDir, "ibans.xlsx"),
                    OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.xlsx")
                };
                config.FieldMasks.Add(new FieldMask()
                {
                    FieldName = "IBAN",
                    MaskType = MaskType.Random,
                    RegExMatch = @$"\b({countryCode})(\w{{12,30}})\b",
                    RegExGroupToReplace = 2
                });

                // === Act ===
                var masker = new DataMasking(config);
                masker.Mask();

                // === Assert ===

                // Manually examine                
                // Utility.OpenDocument(config.OutputFile);
                // does the file exist?
                Assert.True(File.Exists(config.OutputFile));

                // Examine the document
                bool found = false;
                using (var doc = SpreadsheetDocument.Open(config.OutputFile, false))
                {
                    var sstPart = doc.WorkbookPart.SharedStringTablePart;
                    if (sstPart != null)
                    {
                        foreach (var cell in sstPart.SharedStringTable.Elements<DocumentFormat.OpenXml.Spreadsheet.SharedStringItem>())
                        {
                            if (Regex.IsMatch(cell.Text.Text, expectedRegEx))
                            {
                                found = true;
                                Console.WriteLine($"Replacement found: {cell.Text.Text}");
                                break;
                            }
                        }
                    }
                }

                Assert.True(found);
            }
        }

        [Theory]
        [InlineData("AT", "11111111,2222222,333333")]
        [InlineData("Be", "lize (BZ),larus (BR),nin (BN)")]
        public void T05_Xlsx_List(string startofString, string replacements)
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Console.WriteLine($"Starting {testName}");

            using (var utility = new Utility())
            {
                utility.PrepareTestInstanceDir();

                var config = new MaskConfig()
                {
                    DataSource = DataSource.XlsXFile,
                    DataDestination = DataDestination.XlsXFile,
                    InputFile = Path.Combine(utility.TestInstanceDir, "ibans.xlsx"),
                    OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.xlsx")
                };
                config.FieldMasks.Add(new FieldMask()
                {
                    FieldName = "IBAN or Country",
                    MaskType = MaskType.List,
                    RegExMatch = @$"({startofString})(.{{8,20}})",
                    RegExGroupToReplace = 2,
                    ListOfPossibleReplacements = new List<Replacement>()
                    {
                        new Replacement() { Selector = "", ReplacementList = replacements }
                    }                    
                });

                // === Act ===
                var masker = new DataMasking(config);
                masker.Mask();

                // === Assert ===

                // Manually examine                
                // Utility.OpenDocument(config.OutputFile);
                // does the file exist?
                Assert.True(File.Exists(config.OutputFile));

                // Examine the document
                bool found = false;
                using (var doc = SpreadsheetDocument.Open(config.OutputFile, false))
                {
                    var sstPart = doc.WorkbookPart.SharedStringTablePart;
                    if (sstPart != null)
                    {
                        foreach (var cell in sstPart.SharedStringTable.Elements<DocumentFormat.OpenXml.Spreadsheet.SharedStringItem>())
                        {
                            Console.WriteLine($"Cell: {cell.Text.Text}");
                            foreach (string possibleReplacement in replacements.Split(","))
                            {                                
                                if (cell.Text.Text == (startofString + possibleReplacement))
                                {
                                    found = true;
                                    Console.WriteLine($"Replacement found: {cell.Text.Text}");
                                    break;
                                }
                            }                            
                        }
                    }
                }

                Assert.True(found);
            }            
        }
    }
}