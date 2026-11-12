using System;
using System.Collections.Generic;
using Xunit;
using Dandraka.Zoro.Processor;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using System.Text.RegularExpressions;
using System.Linq;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Dandraka.Zoro.Tests
{
    /// <summary>
    /// Tests for the <c>DataMasking</c> class where both data source and data destination are database.
    /// </summary>
    public class Docx_Tests : IDisposable
    {
        public Docx_Tests()
        {
            //
        }

        public void Dispose()
        {
            //
        }

        [Fact]
        public void T01_Docx_InvalidOutputType()
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
                    DataDestination = DataDestination.CsvFile,
                    InputFile = Path.Combine(utility.TestInstanceDir, "SecretCombination.docx"),
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
                Assert.Equal("For docx source, only docx destination is supported.", ex.Message);
            }
        }

        [Fact]
        public void T02_Docx_InvalidMaskType()
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
        [InlineData("secret", "My ****** combination", 10)]
        [InlineData("mystery", "It's a ******* for you", 5)]
        public void T03_Docx_Asterisk(string wordToReplace, string expectedPhrase, int replacementsExpected)
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
                config.FieldMasks.Add(new FieldMask() { FieldName = wordToReplace, MaskType = MaskType.Asterisk });

                // === Act ===
                var masker = DataMasking.Create(config);
                masker.Mask();

                // === Assert ===

                // Manually examine                
                // Utility.OpenDocument(config.OutputFile);
                // does the file exist?
                Assert.True(File.Exists(config.OutputFile));

                // Examine the document
                int replacementsToBeDone = replacementsExpected;
                using (var doc = WordprocessingDocument.Open(config.OutputFile, false))
                {
                    var body = doc.MainDocumentPart.Document.Body;
                    foreach (var textNode in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
                    {
                        if (textNode.Text == expectedPhrase)
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
        [InlineData("secret", "My (\\w{6}) combination", 10)]
        [InlineData("mystery", "It's a (\\w{7}) for you", 5)]
        public void T04_Docx_Random(string wordToReplace, string expectedPhrase, int replacementsExpected)
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
                var masker = DataMasking.Create(config);
                masker.Mask();

                // === Assert ===

                // Manually examine                
                // Utility.OpenDocument(config.OutputFile);
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
        public void T05_Docx_List(string wordToReplace, string replacements, string expected, int replacementsExpected)
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
                var masker = DataMasking.Create(config);
                masker.Mask();

                // === Assert ===

                // Manually examine                
                // Utility.OpenDocument(config.OutputFile);
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
        public void T06_Docx_Db(string wordToReplace, string dbfield, string expected, int replacementsExpected)
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
                var masker = DataMasking.Create(config);
                masker.Mask();

                // === Assert ===

                // get the data we have in the db from the csv
                var replacements = utility.GetCSVFieldValues("data1.csv", dbfield);

                // Manually examine                
                // Utility.OpenDocument(config.OutputFile);
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
                var masker = DataMasking.Create(config);
                masker.Mask();

                // === Assert ===

                // Manually examine                
                // Utility.OpenDocument(config.OutputFile);
                // does the file exist?
                Assert.True(File.Exists(config.OutputFile));
            }
        }

        [Fact]
        public void T08_Docx_ReplacementList_TextSplitAcrossRuns()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            Console.WriteLine($"Starting {testName}");
            using (var utility = new Utility())
            {
                string inputFile = Path.Combine(utility.TestInstanceDir, "split.docx");
                using (var doc = WordprocessingDocument.Create(inputFile, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                {
                    var mainPart = doc.AddMainDocumentPart();
                    mainPart.Document = new W.Document(new W.Body(
                        // one name in 3 runs, the 2nd one bold
                        new W.Paragraph(
                            new W.Run(new W.Text("My friend Ni") { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve }),
                            new W.Run(new W.RunProperties(new W.Bold()), new W.Text("ck Pe")),
                            new W.Run(new W.Text("terson")),
                            new W.Run(new W.Text(" is here") { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve })),
                        // a tab between 2 names: they are 2 values, not 1 glued together
                        new W.Paragraph(new W.Run(new W.Text("Alice"), new W.TabChar(), new W.Text("Bob"))),
                        new W.Paragraph(new W.Run(new W.Text("Ali")), new W.Run(new W.Text("ce")))));
                    mainPart.Document.Save();
                }

                var config = new MaskConfig()
                {
                    DataSource = DataSource.DocXFile,
                    DataDestination = DataDestination.DocXFile,
                    InputFile = inputFile,
                    OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.docx")
                };
                var mask = new FieldMask() { FieldName = "names", MaskType = MaskType.ReplacementList };
                var replacement = new Replacement() { Selector = "" };
                replacement.Mappings.Add(new ReplacementMap() { From = "Nick Peterson", To = "John Miller" });
                replacement.Mappings.Add(new ReplacementMap() { From = "Alice", To = "A" });
                replacement.Mappings.Add(new ReplacementMap() { From = "Bob", To = "B" });
                mask.ListOfPossibleReplacements.Add(replacement);
                config.FieldMasks.Add(mask);

                // === Act ===
                var masker = DataMasking.Create(config);
                masker.Mask();

                // === Assert ===
                using (var doc = WordprocessingDocument.Open(config.OutputFile, false))
                {
                    var paragraphs = doc.MainDocumentPart.Document.Body.Descendants<W.Paragraph>().ToList();
                    Assert.Equal(3, paragraphs.Count);

                    // the replacement takes the run where the name starts; the others lose their part of the name
                    var texts = paragraphs[0].Descendants<W.Text>().Select(t => t.Text).ToList();
                    Assert.Equal(["My friend John Miller", "", "", " is here"], texts);

                    Assert.Equal(["A", "B"], paragraphs[1].Descendants<W.Text>().Select(t => t.Text));
                    Assert.Equal(["A", ""], paragraphs[2].Descendants<W.Text>().Select(t => t.Text));
                }
            }
        }
    }
}