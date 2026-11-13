using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using Dandraka.Zoro.Processor;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;

namespace Dandraka.Zoro.Tests
{
    /// <summary>
    /// Tests for the pptx support, using cavafy.pptx: 4 slides (the last one hidden), a table on slide 2,
    /// the poem of slide 3 in a group, text split across runs, speaker notes and a "Cavafy Poems" text in the master.
    /// </summary>
    public class Pptx_Tests : IDisposable
    {
        private readonly Utility utility = new();

        public Pptx_Tests()
        {
            utility.PrepareTestInstanceDir();
            utility.PrepareSqliteDb("Pptx_Tests");
        }

        public void Dispose()
        {
            utility.Dispose();
            GC.SuppressFinalize(this);
        }

        private MaskConfig Config(string testName, params FieldMask[] masks)
        {
            var config = new MaskConfig()
            {
                DataSource = DataSource.PptXFile,
                DataDestination = DataDestination.PptXFile,
                InputFile = Path.Combine(utility.TestInstanceDir, "cavafy.pptx"),
                OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.pptx")
            };
            config.SetConnection(utility.TestDbConnection);
            config.FieldMasks.AddRange(masks);
            return config;
        }

        private static FieldMask Mask(string fieldName, MaskType maskType)
        {
            return new FieldMask() { FieldName = fieldName, MaskType = maskType };
        }

        private static FieldMask ListMask(string fieldName, MaskType maskType, params Replacement[] replacements)
        {
            var mask = Mask(fieldName, maskType);
            mask.ListOfPossibleReplacements.AddRange(replacements);
            return mask;
        }

        private static Replacement Mapped(params (string from, string to)[] maps)
        {
            var replacement = new Replacement() { Selector = "" };
            foreach (var (from, to) in maps)
            {
                replacement.Mappings.Add(new ReplacementMap() { From = from, To = to });
            }
            return replacement;
        }

        private void CreateTable(string name, string selectSql)
        {
            var cmd = utility.TestDbConnection.CreateCommand();
            cmd.CommandText = $"CREATE TABLE {name} AS {selectSql}";
            cmd.ExecuteNonQuery();
        }

        // The text of every paragraph, with the runs joined, of the part that getPart returns for each slide.
        private static List<string> ReadTexts(string file, Func<SlidePart, OpenXmlPart> getPart)
        {
            using var doc = PresentationDocument.Open(file, false);
            return doc.PresentationPart.SlideParts
                .Select(getPart)
                .Where(p => p != null)
                .Distinct()
                .SelectMany(p => p.RootElement.Descendants<A.Paragraph>())
                .Select(p => string.Concat(p.Elements<A.Run>().Select(r => r.Text?.Text)))
                .Where(t => t.Length > 0)
                .ToList();
        }

        private static List<string> ReadSlideTexts(string file) => ReadTexts(file, s => s);

        private static List<string> ReadNotesTexts(string file) => ReadTexts(file, s => s.NotesSlidePart);

        private static List<string> ReadMasterTexts(string file) => ReadTexts(file, s => s.SlideLayoutPart?.SlideMasterPart);

        [Fact]
        public void T01_Pptx_InvalidOutputType()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var config = Config(testName, Mask("Cavafy", MaskType.Asterisk));
            config.DataDestination = DataDestination.CsvFile;
            config.OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.csv");

            // === Act & Assert ===
            var ex = Assert.Throws<NotSupportedException>(() => DataMasking.Create(config).Mask());
            Assert.Equal("For pptx source, only pptx destination is supported.", ex.Message);
        }

        [Fact]
        public void T02_Pptx_InvalidMaskType()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var config = Config(testName, new FieldMask() { FieldName = "Cavafy", MaskType = MaskType.Expression, Expression = "something" });

            // === Act & Assert ===
            var ex = Assert.Throws<NotSupportedException>(() => DataMasking.Create(config).Mask());
            Assert.Equal("For Office file types, the Expression mask type is not supported.", ex.Message);
        }

        [Fact]
        public void T03_Pptx_Asterisk_TextSplitAcrossRuns()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            // "Spartan" is split in 5 runs
            var config = Config(testName, Mask("Spartan", MaskType.Asterisk));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var texts = ReadSlideTexts(config.OutputFile);
            Assert.Contains(texts, t => t.Contains("comprehend the ******* spirit;"));
            Assert.DoesNotContain(texts, t => t.Contains("Spartan"));
            // the other texts are untouched
            Assert.Contains("In Sparta", texts);
        }

        [Fact]
        public void T04_Pptx_Asterisk_Table()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            // "Alexandrian" is split in 3 runs, inside a table cell
            var config = Config(testName, Mask("Thermopylae", MaskType.Asterisk), Mask("Alexandrian", MaskType.Asterisk));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var texts = ReadSlideTexts(config.OutputFile);
            Assert.Contains("Walls (1896), Candles (1899), ***********, and Waiting for the Barbarians (1904)", texts);
            Assert.Contains("Ithaca (1911), *********** Kings, and The God Abandons Antony.", texts);
        }

        [Fact]
        public void T05_Pptx_Asterisk_Group()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            // the poem is in a group; the word is split in 3 runs the first time and in 3 different ones the second
            var config = Config(testName, Mask("Laistrygonians", MaskType.Asterisk));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var texts = ReadSlideTexts(config.OutputFile);
            Assert.Equal(2, texts.Count(t => t == "**************, Cyclops,"));
            Assert.DoesNotContain(texts, t => t.Contains("Laistrygonians"));
        }

        [Fact]
        public void T06_Pptx_Asterisk_Master()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var config = Config(testName, Mask("Cavafy Poems", MaskType.Asterisk));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            Assert.Contains("****** *****", ReadMasterTexts(config.OutputFile));
            Assert.DoesNotContain(ReadMasterTexts(config.OutputFile), t => t.Contains("Cavafy Poems"));
        }

        [Fact]
        public void T07_Pptx_Asterisk_SpeakerNotes()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            // in the notes, "Translated" and " By Edmund Keeley & Philip Sherrard" are separate runs
            var config = Config(testName, Mask("Edmund Keeley", MaskType.Asterisk));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var notes = ReadNotesTexts(config.OutputFile);
            Assert.Equal(2, notes.Count(t => t == "Translated By ****** ****** & Philip Sherrard"));
            Assert.Contains("Translated By ****** ****** & Philip Sherrard", ReadSlideTexts(config.OutputFile));
            Assert.DoesNotContain(notes, t => t.Contains("Keeley"));
        }

        [Fact]
        public void T08_Pptx_Asterisk_HiddenSlideAndEverywhere()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var config = Config(testName, Mask("Cavafy", MaskType.Asterisk));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var slides = ReadSlideTexts(config.OutputFile);
            // the hidden slide
            Assert.Contains("More ****** poems to be added", slides);
            Assert.Equal(4, slides.Count(t => t == "C.P. ******"));
            Assert.DoesNotContain(slides, t => t.Contains("Cavafy"));
            Assert.DoesNotContain(ReadNotesTexts(config.OutputFile), t => t.Contains("Cavafy"));
            Assert.Contains("****** Poems", ReadMasterTexts(config.OutputFile));
        }

        [Fact]
        public void T09_Pptx_Random()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var config = Config(testName, Mask("Cavafy", MaskType.Random));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var slides = ReadSlideTexts(config.OutputFile);
            Assert.Contains(slides, t => Regex.IsMatch(t, @"^More \w{6} poems to be added$") && !t.Contains("Cavafy"));
            Assert.DoesNotContain(slides, t => t.Contains("Cavafy"));
        }

        [Fact]
        public void T10_Pptx_List()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var config = Config(testName,
                ListMask("Cavafy", MaskType.List, new Replacement() { Selector = "", ReplacementList = "Homer,Sappho" }));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var slides = ReadSlideTexts(config.OutputFile);
            Assert.Contains(slides, t => Regex.IsMatch(t, @"^More (Homer|Sappho) poems to be added$"));
            Assert.DoesNotContain(slides, t => t.Contains("Cavafy"));
        }

        [Fact]
        public void T11_Pptx_ReplacementList_Mapping()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            // mapped values are searched without a regex, ignoring case, also when split across runs
            var config = Config(testName,
                ListMask("all", MaskType.ReplacementList, Mapped(("cavafy", "Seferis"), ("Spartan", "Laconian"))));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var slides = ReadSlideTexts(config.OutputFile);
            Assert.Equal(4, slides.Count(t => t == "C.P. Seferis"));
            Assert.Contains("More Seferis poems to be added", slides);
            Assert.Contains(slides, t => t.Contains("comprehend the Laconian spirit;"));
            Assert.Contains("Seferis Poems", ReadMasterTexts(config.OutputFile));
            Assert.Equal(2, ReadNotesTexts(config.OutputFile).Count(t => t == "By C. P. Seferis"));
        }

        [Fact]
        public void T12_Pptx_ReplacementList_RegEx_Pool()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            // "Sparta" is split in 2 runs the second time; "Spartan" must not match
            var mask = ListMask("city", MaskType.ReplacementList, new Replacement() { Selector = "", ReplacementList = "Lacedaemon" });
            mask.RegExMatch = @"\b(Sparta)\b";
            mask.RegExGroupToReplace = 1;
            var config = Config(testName, mask);

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var slides = ReadSlideTexts(config.OutputFile);
            Assert.Contains("In Lacedaemon", slides);
            Assert.Contains("she could be useful to Lacedaemon still.", slides);
            Assert.Contains(slides, t => t.Contains("comprehend the Spartan spirit;"));
        }

        [Fact]
        public void T13_Pptx_ReplacementQuery_MappingTable()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            CreateTable("poet_mapping", "SELECT 'Cavafy' AS originalName, 'Seferis' AS pseudonym UNION ALL SELECT 'Keeley' AS originalName, 'Smith' AS pseudonym");
            var config = Config(testName, new FieldMask()
            {
                FieldName = "all",
                MaskType = MaskType.ReplacementQuery,
                QueryReplacement = new QueryReplacement()
                {
                    Query = "SELECT originalName, pseudonym FROM poet_mapping",
                    KeyDbField = "originalName",
                    ValueDbField = "pseudonym"
                }
            });

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var slides = ReadSlideTexts(config.OutputFile);
            Assert.Equal(4, slides.Count(t => t == "C.P. Seferis"));
            Assert.Contains("More Seferis poems to be added", slides);
            Assert.Contains("Translated By Edmund Smith & Philip Sherrard", slides);
        }

        [Fact]
        public void T14_Pptx_ReplacementQuery_Pool_IsConsistent()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            CreateTable("poet_pool", "SELECT 'Homer' AS poet UNION ALL SELECT 'Sappho' AS poet");
            var config = Config(testName, new FieldMask()
            {
                FieldName = "Cavafy",
                MaskType = MaskType.ReplacementQuery,
                QueryReplacement = new QueryReplacement() { Query = "SELECT poet FROM poet_pool", ValueDbField = "poet" }
            });

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var titles = ReadSlideTexts(config.OutputFile).Where(t => t.StartsWith("C.P. ")).ToList();
            Assert.Equal(4, titles.Count);
            Assert.Single(titles.Distinct());
            Assert.Matches(@"^C\.P\. (Homer|Sappho)$", titles[0]);
        }
    }
}
