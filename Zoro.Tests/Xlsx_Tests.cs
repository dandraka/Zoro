using System;
using Xunit;
using Dandraka.Zoro.Processor;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

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
                var masker = DataMasking.Create(config);
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
                var masker = DataMasking.Create(config);
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
                var masker = DataMasking.Create(config);
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
                var masker = DataMasking.Create(config);
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
                var masker = DataMasking.Create(config);
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

        [Theory]
        [InlineData("AT", "AT611904300234573201", "AT******************")]
        [InlineData("CH", "CH9300762011623852957", "CH*******************")]
        public void T06_Xlsx_RichText_Asterisk(string countryCode, string originalIban, string expectedContent)
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
                    InputFile = Path.Combine(utility.TestInstanceDir, "ibans_richtext.xlsx"),
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
                var masker = DataMasking.Create(config);
                masker.Mask();

                // === Assert ===

                // Manually examine                
                // Utility.OpenDocument(config.OutputFile);
                // does the file exist?
                Assert.True(File.Exists(config.OutputFile));

                // Examine the document: rich-text cells have runs instead of a direct Text
                var runTexts = new List<string>();
                using (var doc = SpreadsheetDocument.Open(config.OutputFile, false))
                {
                    var sstPart = doc.WorkbookPart.SharedStringTablePart;
                    foreach (var item in sstPart.SharedStringTable.Elements<DocumentFormat.OpenXml.Spreadsheet.SharedStringItem>())
                    {
                        foreach (var run in item.Elements<DocumentFormat.OpenXml.Spreadsheet.Run>())
                        {
                            runTexts.Add(run.Text?.Text);
                        }
                    }
                }

                // the test file must really contain rich text, otherwise this test proves nothing
                Assert.NotEmpty(runTexts);
                Assert.Contains(expectedContent, runTexts);
                Assert.DoesNotContain(originalIban, runTexts);
            }
        }

        [Fact]
        public void T07_Xlsx_ReplacementList_ConfigFile()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Console.WriteLine($"Starting {testName}");

            using (var utility = new Utility())
            {
                utility.PrepareTestInstanceDir();

                // the paths in the config file are relative, for running the exe manually
                var config = MaskConfig.ReadConfig(Path.Combine(utility.TestInstanceDir, "testconfigxlsx.xml"));
                config.InputFile = Path.Combine(utility.TestInstanceDir, "HR_data.xlsx");
                config.OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.xlsx");

                // === Act ===
                var masker = DataMasking.Create(config);
                masker.Mask();

                // === Assert ===
                Assert.True(File.Exists(config.OutputFile));

                var texts = new List<string>();
                using (var doc = SpreadsheetDocument.Open(config.OutputFile, false))
                {
                    foreach (var item in doc.WorkbookPart.SharedStringTablePart.SharedStringTable.Elements<DocumentFormat.OpenXml.Spreadsheet.SharedStringItem>())
                    {
                        texts.Add(item.Text?.Text);
                    }
                }

                // names and grades are replaced, everything else is left as is
                Assert.Equal(
                    ["Alice", "Bob", "Charlie", "Salary", "Grade", "Bodybuilder", "Neopas", "Sergeant"],
                    texts);
                Assert.Equal(0, masker.ValuesWithoutReplacement);
            }
        }

        [Fact]
        public void T08_Xlsx_ReplacementList_TextSplitAcrossRuns()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Console.WriteLine($"Starting {testName}");

            using (var utility = new Utility())
            {
                string inputFile = Path.Combine(utility.TestInstanceDir, "split.xlsx");
                using (var doc = SpreadsheetDocument.Create(inputFile, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = doc.AddWorkbookPart();
                    workbookPart.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook();
                    var sstPart = workbookPart.AddNewPart<SharedStringTablePart>();
                    sstPart.SharedStringTable = new DocumentFormat.OpenXml.Spreadsheet.SharedStringTable(
                        // one name in 3 runs
                        new DocumentFormat.OpenXml.Spreadsheet.SharedStringItem(
                            new DocumentFormat.OpenXml.Spreadsheet.Run(new DocumentFormat.OpenXml.Spreadsheet.Text("Nick ") { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve }),
                            new DocumentFormat.OpenXml.Spreadsheet.Run(new DocumentFormat.OpenXml.Spreadsheet.Text("Peter")),
                            new DocumentFormat.OpenXml.Spreadsheet.Run(new DocumentFormat.OpenXml.Spreadsheet.Text("son is here"))),
                        new DocumentFormat.OpenXml.Spreadsheet.SharedStringItem(new DocumentFormat.OpenXml.Spreadsheet.Text("Alice")));
                    sstPart.SharedStringTable.Save();
                    workbookPart.Workbook.Save();
                }

                var config = new MaskConfig()
                {
                    DataSource = DataSource.XlsXFile,
                    DataDestination = DataDestination.XlsXFile,
                    InputFile = inputFile,
                    OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.xlsx")
                };
                var mask = new FieldMask() { FieldName = "names", MaskType = MaskType.ReplacementList };
                var replacement = new Replacement() { Selector = "" };
                replacement.Mappings.Add(new ReplacementMap() { From = "Nick Peterson", To = "John Miller" });
                replacement.Mappings.Add(new ReplacementMap() { From = "Alice", To = "A" });
                mask.ListOfPossibleReplacements.Add(replacement);
                config.FieldMasks.Add(mask);

                // === Act ===
                var masker = DataMasking.Create(config);
                masker.Mask();

                // === Assert ===
                using (var doc = SpreadsheetDocument.Open(config.OutputFile, false))
                {
                    var items = doc.WorkbookPart.SharedStringTablePart.SharedStringTable
                        .Elements<DocumentFormat.OpenXml.Spreadsheet.SharedStringItem>().ToList();
                    Assert.Equal(2, items.Count);

                    // the replacement takes the run where the name starts; the others lose their part of the name
                    var runTexts = items[0].Elements<DocumentFormat.OpenXml.Spreadsheet.Run>().Select(r => r.Text.Text).ToList();
                    Assert.Equal(["John Miller", "", " is here"], runTexts);
                    Assert.Equal("A", items[1].Text.Text);
                }
            }
        }
    }
}