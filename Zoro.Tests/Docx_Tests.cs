using System;
using System.Data;
using System.Collections.Generic;
using Xunit;
using Dandraka.Zoro.Processor;
using Dandraka.Zoro.Tests;
using System.IO;
using System.Diagnostics;
using DocumentFormat.OpenXml.Packaging;

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

        [Theory]
        [InlineData("secret", "My ****** combination", 10)]
        [InlineData("mystery", "It's a ******* for you", 5)]
        public void T01_Docx_Base(string wordToReplace, string expectedPhrase, int replacementsExpected)
        {
            // Arrange
            string tblName = $"T01_Db2Csv_{Guid.NewGuid().ToString().Substring(0, 8)}";
            using (var utility = new Utility())
            {
                utility.PrepareTestInstanceDir();

                var config = new MaskConfig()
                {
                    DataSource = DataSource.DocXFile,
                    DataDestination = DataDestination.DocXFile,
                    InputFile = Path.Combine(utility.TestInstanceDir, "SecretCombination.docx"),
                    OutputFile = Path.Combine(utility.TestInstanceDir, "T01_Docx_Base.docx")
                };
                config.SetConnection(utility.TestDbConnection);
                config.FieldMasks.Add(new FieldMask() { FieldName = wordToReplace, MaskType = MaskType.Asterisk });

                // Act
                var masker = new DataMasking(config);
                masker.Mask();

                // Assert

                // Manually examine                
                //OpenDocument(config.OutputFile);
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
                            replacementsToBeDone--;
                        }
                    }
                }
                Assert.Equal(0, replacementsToBeDone);
            }
        }

        private static void OpenDocument(string filename)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = filename,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }
}