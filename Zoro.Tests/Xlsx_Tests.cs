using System;
using Xunit;
using Dandraka.Zoro.Processor;
using System.IO;
using DocumentFormat.OpenXml.Packaging;

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
                Utility.OpenDocument(config.OutputFile);
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

            /*

            [Theory]
            [InlineData("secret", "My (\\w{6}) combination", 10)]
            [InlineData("mystery", "It's a (\\w{7}) for you", 5)]
            public void T04_Xlsx_Random(string wordToReplace, string expectedPhrase, int replacementsExpected)
            {
                // === Arrange ===
                string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                Console.WriteLine($"Starting {testName}");
                using (var utility = new Utility())
                {
                    utility.PrepareTestInstanceDir();

                    var config = new MaskConfig()
                    {
                        DataSource = DataSource.DocXFile,
                        DataDestination = DataDestination.DocXFile,
                        InputFile = Path.Combine(utility.TestInstanceDir, "SecretCombination.docx"),
                        OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.docx")
                    };
                    config.FieldMasks.Add(new FieldMask() { FieldName = wordToReplace, MaskType = MaskType.Random });

                    // === Act ===
                    var masker = new DataMasking(config);
                    masker.Mask();

                    // === Assert ===

                    // Manually examine                
                    // OpenDocument(config.OutputFile);
                    // does the file exist?
                    Assert.True(File.Exists(config.OutputFile));

                    // Examine the document
                    int replacementsToBeDone = replacementsExpected;
                    using (var doc = WordprocessingDocument.Open(config.OutputFile, false))
                    {
                        var body = doc.MainDocumentPart.Document.Body;
                        foreach (var textNode in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
                        {
                            if (Regex.IsMatch(textNode.Text, expectedPhrase))
                            {
                                Console.WriteLine($"Replacement found: {textNode.Text}");
                                replacementsToBeDone--;
                            }
                        }
                    }
                    Assert.Equal(0, replacementsToBeDone);
                }
            }

            [Theory]
            [InlineData("book", "tablet,newspaper,cellphone,", "An open %R%", 2)]
            [InlineData("heart", "soul,mind,thoughts,attention,sight,affection", "In the center of my %R%", 6)]
            public void T05_Xlsx_List(string wordToReplace, string replacements, string expected, int replacementsExpected)
            {
                // === Arrange ===
                string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                Console.WriteLine($"Starting {testName}");
                using (var utility = new Utility())
                {
                    utility.PrepareTestInstanceDir();

                    var config = new MaskConfig()
                    {
                        DataSource = DataSource.DocXFile,
                        DataDestination = DataDestination.DocXFile,
                        InputFile = Path.Combine(utility.TestInstanceDir, "SecretCombination.docx"),
                        OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.docx")
                    };
                    config.FieldMasks.Add(new FieldMask()
                    {
                        FieldName = wordToReplace,
                        MaskType = MaskType.List,
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
                    // OpenDocument(config.OutputFile);
                    // does the file exist?
                    Assert.True(File.Exists(config.OutputFile));

                    // Examine the document
                    int replacementsToBeDone = replacementsExpected;
                    using (var doc = WordprocessingDocument.Open(config.OutputFile, false))
                    {
                        var body = doc.MainDocumentPart.Document.Body;
                        foreach (var textNode in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
                        {
                            foreach (string possibleReplacement in replacements.Split(","))
                            {
                                string expectedPhrase = expected.Replace("%R%", possibleReplacement);
                                if (textNode.Text == expectedPhrase)
                                {
                                    Console.WriteLine($"Replacement found: {textNode.Text}");
                                    replacementsToBeDone--;
                                }
                            }
                        }
                    }
                    Assert.Equal(0, replacementsToBeDone);
                }
            }

            [Theory]
            [InlineData("imagination", "Address", "Use your %R%", 5)]
            [InlineData("center", "Country", "In the %R% of my heart", 6)]
            public void T06_Xlsx_Db(string wordToReplace, string dbfield, string expected, int replacementsExpected)
            {
                // === Arrange ===
                string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                Console.WriteLine($"Starting {testName}");

                string tblName = $"{testName}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                using (var utility = new Utility())
                {
                    utility.PrepareTestInstanceDir();
                    utility.PrepareSqliteDb(tblName);

                    var config = new MaskConfig()
                    {
                        DataSource = DataSource.DocXFile,
                        DataDestination = DataDestination.DocXFile,
                        InputFile = Path.Combine(utility.TestInstanceDir, "SecretCombination.docx"),
                        OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.docx")
                    };
                    config.SetConnection(utility.TestDbConnection);
                    config.FieldMasks.Add(new FieldMask()
                    {
                        FieldName = wordToReplace,
                        MaskType = MaskType.Query,
                        QueryReplacement = new QueryReplacement()
                        {
                            Query = $"SELECT {dbfield} FROM {tblName}",
                            ValueDbField = dbfield,
                            GroupDbField = string.Empty,
                            SelectorField = string.Empty
                        }
                    });

                    // === Act ===
                    var masker = new DataMasking(config);
                    masker.Mask();

                    // === Assert ===

                    // get the data we have in the db from the csv
                    var replacements = utility.GetCSVFieldValues("data1.csv", dbfield);

                    // Manually examine                
                    // OpenDocument(config.OutputFile);
                    // does the file exist?
                    Assert.True(File.Exists(config.OutputFile));

                    // Examine the document
                    int replacementsToBeDone = replacementsExpected;
                    using (var doc = WordprocessingDocument.Open(config.OutputFile, false))
                    {
                        var body = doc.MainDocumentPart.Document.Body;
                        foreach (var textNode in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
                        {
                            foreach (string possibleReplacement in replacements)
                            {
                                string expectedPhrase = expected.Replace("%R%", possibleReplacement);
                                if (textNode.Text == expectedPhrase)
                                {
                                    Console.WriteLine($"Replacement found: {textNode.Text}");
                                    replacementsToBeDone--;
                                }
                            }
                        }
                    }
                    Assert.Equal(0, replacementsToBeDone);
                }
            }

            [Fact(Skip = "Only for debugging")]
            //[Fact]
            public void T99_AdhocTestForDebugging()
            {
                // === Arrange ===
                string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                Console.WriteLine($"Starting {testName}");

                using (var utility = new Utility())
                {
                    var config = new MaskConfig()
                    {
                        DataSource = DataSource.DocXFile,
                        DataDestination = DataDestination.DocXFile,
                        InputFile = @"C:\temp\zoro\fake_ibans.docx",
                        OutputFile = @$"C:\temp\zoro\fake_ibans_{Guid.NewGuid().ToString().Split("-")[0]}.docx"
                    };
                    config.FieldMasks.Add(new FieldMask()
                    {
                        FieldName = "IBAN",
                        MaskType = MaskType.List,
                        RegExMatch = @"\b([A-Za-z]{2}\w{16})\b",
                        RegExGroupToReplace = 1
                    });
                    config.FieldMasks.Add(new FieldMask()
                    {
                        FieldName = "coffee",
                        MaskType = MaskType.Random
                    });                
                    config.FieldMasks[0].ListOfPossibleReplacements.Add(new Replacement()
                    {
                        Selector = "",
                        ReplacementList = "NL1111111111111111,GR2222222222222222,CH3333333333333333"
                    });

                    // === Act ===
                    var masker = new DataMasking(config);
                    masker.Mask();

                    // === Assert ===

                    // Manually examine                
                    // OpenDocument(config.OutputFile);
                    // does the file exist?
                    Assert.True(File.Exists(config.OutputFile));
                }
            }

            */
        }
    }
}