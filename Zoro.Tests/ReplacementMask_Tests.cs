using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using Dandraka.Zoro.Processor;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Dandraka.Zoro.Tests
{
    /// <summary>
    /// Tests for the <c>ReplacementList</c> and <c>ReplacementQuery</c> mask types (consistent masking).
    /// </summary>
    public class ReplacementMask_Tests : IDisposable
    {
        private readonly Utility utility = new();

        public ReplacementMask_Tests()
        {
            utility.PrepareTestInstanceDir();
            utility.PrepareSqliteDb("ReplacementMask_Tests");
        }

        public void Dispose()
        {
            utility.Dispose();
            GC.SuppressFinalize(this);
        }

        private static FieldMask ListMask(string fieldName, params Replacement[] replacements)
        {
            var mask = new FieldMask() { FieldName = fieldName, MaskType = MaskType.ReplacementList };
            mask.ListOfPossibleReplacements.AddRange(replacements);
            return mask;
        }

        private static Replacement Pool(string items, string selector = "")
        {
            return new Replacement() { Selector = selector, ReplacementList = items };
        }

        private static Replacement Mapped(string selector, params (string from, string to)[] maps)
        {
            var replacement = new Replacement() { Selector = selector };
            foreach (var (from, to) in maps)
            {
                replacement.Mappings.Add(new ReplacementMap() { From = from, To = to });
            }
            return replacement;
        }

        private (List<string[]> rows, DataMasking masker) MaskCsv(string csvContent, params FieldMask[] masks)
        {
            string csvFilename = utility.CreateFileInTestInstanceDir(csvContent, "csv");
            var config = new MaskConfig()
            {
                InputFile = csvFilename,
                OutputFile = csvFilename.Replace(".csv", "_out.csv")
            };
            config.SetConnection(utility.TestDbConnection);
            config.FieldMasks.AddRange(masks);

            var masker = DataMasking.Create(config);
            masker.Mask();

            Assert.True(File.Exists(config.OutputFile));
            var rows = File.ReadLines(config.OutputFile).Skip(1).Select(x => x.Split(';')).ToList();
            return (rows, masker);
        }

        [Fact]
        public void T01_ReplacementList_Pool_IsConsistentAndUnique()
        {
            // === Arrange ===
            string csv = "id;city\r\n1;Athens\r\n2;London\r\n3;athens \r\n4;Paris\r\n5;LONDON";
            string[] pool = ["CityA", "CityB", "CityC"];

            // === Act ===
            var (rows, masker) = MaskCsv(csv,
                new FieldMask() { FieldName = "id", MaskType = MaskType.None },
                ListMask("city", Pool(string.Join(",", pool))));

            // === Assert ===
            var cities = rows.Select(r => r[1]).ToList();
            Assert.Equal(5, cities.Count);
            Assert.All(cities, c => Assert.Contains(c, pool));
            // same value (ignoring case and spaces) gets the same replacement
            Assert.Equal(cities[0], cities[2]);
            Assert.Equal(cities[1], cities[4]);
            // different values get different replacements
            Assert.Equal(3, cities.Distinct().Count());
            Assert.Equal(0, masker.ValuesWithoutReplacement);
        }

        [Fact]
        public void T02_ReplacementList_Mapping_UsesGivenPairs()
        {
            // === Arrange ===
            string csv = "id;city\r\n1;Athens\r\n2;London\r\n3; LONDON \r\n4;Athens";

            // === Act ===
            var (rows, masker) = MaskCsv(csv,
                new FieldMask() { FieldName = "id", MaskType = MaskType.None },
                ListMask("city", Mapped("", ("Athens", "CityA"), ("london", "CityB"))));

            // === Assert ===
            Assert.Equal(["CityA", "CityB", "CityB", "CityA"], rows.Select(r => r[1]));
            Assert.Equal(0, masker.ValuesWithoutReplacement);
        }

        [Fact]
        public void T03_ReplacementList_MappingAndPool_WithSelectors()
        {
            // === Arrange ===
            string csv = "id;city;country\r\n1;Athens;GR\r\n2;Patra;GR\r\n3;London;UK\r\n4;Athens;UK\r\n5;Athens;GR";
            var cityMask = ListMask("city",
                new Replacement() { Selector = "country=GR", ReplacementList = "P1,P2", Mappings = { new ReplacementMap() { From = "Athens", To = "Capital" } } },
                Pool("T1,T2"));

            // === Act ===
            var (rows, _) = MaskCsv(csv,
                new FieldMask() { FieldName = "id", MaskType = MaskType.None },
                cityMask,
                new FieldMask() { FieldName = "country", MaskType = MaskType.None });

            // === Assert ===
            var cities = rows.Select(r => r[1]).ToList();
            Assert.Equal("Capital", cities[0]);
            Assert.Equal("Capital", cities[4]);
            Assert.Contains(cities[1], new[] { "P1", "P2" });
            // other country: separate list, so Athens is not "Capital" there
            Assert.Contains(cities[2], new[] { "T1", "T2" });
            Assert.Contains(cities[3], new[] { "T1", "T2" });
            Assert.NotEqual(cities[2], cities[3]);
        }

        [Fact]
        public void T04_ReplacementList_RegEx_ReplacesOnlyTheGroupAndEveryMatch()
        {
            // === Arrange ===
            string csv = "id;phone\r\n1;+41 785412798\r\n2;+41 785412798\r\n3;+49 17612345\r\n4;+49 17699999";

            // === Act ===
            var (rows, _) = MaskCsv(csv,
                new FieldMask() { FieldName = "id", MaskType = MaskType.None },
                new FieldMask()
                {
                    FieldName = "phone",
                    MaskType = MaskType.ReplacementList,
                    RegExMatch = @"^(\+\d\d) (.*)$",
                    RegExGroupToReplace = 2,
                    ListOfPossibleReplacements = { Pool("111,222,333,444") }
                });

            // === Assert ===
            var phones = rows.Select(r => r[1]).ToList();
            Assert.StartsWith("+41 ", phones[0]);
            Assert.StartsWith("+41 ", phones[1]);
            Assert.StartsWith("+49 ", phones[2]);
            Assert.Equal(phones[0], phones[1]);
            // two different numbers, plus the repeated one
            Assert.Equal(3, phones.Distinct().Count());
        }

        [Fact]
        public void T05_ReplacementList_Exhausted_Asterisk_IsDefault()
        {
            // === Arrange ===
            string csv = "id;city\r\n1;Athens\r\n2;Madrid\r\n3;Athens\r\n4;Madrid";

            // === Act ===
            var (rows, masker) = MaskCsv(csv,
                new FieldMask() { FieldName = "id", MaskType = MaskType.None },
                ListMask("city", Pool("X")));

            // === Assert ===
            var cities = rows.Select(r => r[1]).ToList();
            Assert.Equal(["X", "******"], cities.Distinct().OrderByDescending(c => c, StringComparer.Ordinal));
            Assert.Equal(cities[0], cities[2]);
            Assert.Equal(cities[1], cities[3]);
            Assert.Equal(1, masker.ValuesWithoutReplacement);
        }

        [Fact]
        public void T06_ReplacementList_Exhausted_Asterisk_CustomCharacter()
        {
            // === Arrange ===
            string csv = "id;city\r\n1;Athens\r\n2;Madrid";
            var cityMask = ListMask("city", Mapped("", ("Athens", "CityA")));
            cityMask.Asterisk = "#";

            // === Act ===
            var (rows, masker) = MaskCsv(csv, new FieldMask() { FieldName = "id", MaskType = MaskType.None }, cityMask);

            // === Assert ===
            Assert.Equal(["CityA", "######"], rows.Select(r => r[1]));
            Assert.Equal(1, masker.ValuesWithoutReplacement);
        }

        [Fact]
        public void T07_ReplacementList_Exhausted_Error()
        {
            // === Arrange ===
            string csv = "id;city\r\n1;Athens\r\n2;Madrid";
            var cityMask = ListMask("city", Pool("X"));
            cityMask.OnListExhausted = ListExhaustedAction.Error;

            string csvFilename = utility.CreateFileInTestInstanceDir(csv, "csv");
            var config = new MaskConfig() { InputFile = csvFilename, OutputFile = csvFilename.Replace(".csv", "_out.csv") };
            config.FieldMasks.Add(cityMask);

            // === Act & Assert ===
            Assert.Throws<ReplacementExhaustedException>(() => DataMasking.Create(config).Mask());
        }

        [Fact]
        public void T08_ReplacementList_Exhausted_Reuse()
        {
            // === Arrange ===
            string csv = "id;city\r\n1;Athens\r\n2;Madrid\r\n3;Athens";
            var cityMask = ListMask("city", Pool("X"));
            cityMask.OnListExhausted = ListExhaustedAction.Reuse;

            // === Act ===
            var (rows, masker) = MaskCsv(csv, new FieldMask() { FieldName = "id", MaskType = MaskType.None }, cityMask);

            // === Assert ===
            Assert.All(rows.Select(r => r[1]), c => Assert.Equal("X", c));
            Assert.Equal(0, masker.ValuesWithoutReplacement);
        }

        [Fact]
        public void T09_ReplacementList_DuplicateMapping_Throws()
        {
            // === Arrange ===
            string csvFilename = utility.CreateFileInTestInstanceDir("id;city\r\n1;Athens", "csv");
            var config = new MaskConfig() { InputFile = csvFilename, OutputFile = csvFilename.Replace(".csv", "_out.csv") };
            config.FieldMasks.Add(ListMask("city", Mapped("", ("Athens", "CityA"), (" ATHENS ", "CityB"))));

            // === Act & Assert ===
            Assert.Throws<InvalidDataException>(() => DataMasking.Create(config).Mask());
        }

        [Fact]
        public void T10_ReplacementList_JSON()
        {
            // === Arrange ===
            string json = "[{\"name\":\"Nick\"},{\"name\":\"Eva\"},{\"name\":\"nick\"}]";
            string jsonFilename = utility.CreateFileInTestInstanceDir(json, "json");
            var config = new MaskConfig()
            {
                DataSource = DataSource.JsonFile,
                DataDestination = DataDestination.JsonFile,
                InputFile = jsonFilename,
                OutputFile = jsonFilename.Replace(".json", "_out.json")
            };
            config.FieldMasks.Add(ListMask("name", Mapped("", ("Nick", "John"), ("Eva", "Jane"))));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var result = JArray.Parse(File.ReadAllText(config.OutputFile));
            Assert.Equal(["John", "Jane", "John"], result.Select(x => (string)x["name"]));
        }

        [Fact]
        public void T11_ReplacementQuery_Pool_PerGroup()
        {
            // === Arrange ===
            string csv = "ID;City;Country\r\n1;Athens;GR\r\n2;Chania;GR\r\n3;Athens;GR\r\n4;Basel;CH";
            var cmd = utility.TestDbConnection.CreateCommand();
            cmd.CommandText = @"CREATE TABLE cities AS 
                SELECT 'Rethimno' AS city, 'GR' AS country UNION ALL 
                SELECT 'Trikala'  AS city, 'GR' AS country UNION ALL 
                SELECT 'Geneva'   AS city, 'CH' AS country UNION ALL 
                SELECT 'Bern'     AS city, 'CH' AS country";
            cmd.ExecuteNonQuery();
            utility.TestTablesToDrop = "cities";

            // === Act ===
            var (rows, masker) = MaskCsv(csv,
                new FieldMask() { FieldName = "ID", MaskType = MaskType.None },
                new FieldMask()
                {
                    FieldName = "City",
                    MaskType = MaskType.ReplacementQuery,
                    QueryReplacement = new QueryReplacement()
                    {
                        Query = "SELECT city, country FROM cities",
                        ValueDbField = "city",
                        GroupDbField = "country",
                        SelectorField = "Country"
                    }
                },
                new FieldMask() { FieldName = "Country", MaskType = MaskType.None });

            // === Assert ===
            var cities = rows.Select(r => r[1]).ToList();
            Assert.Contains(cities[0], new[] { "Rethimno", "Trikala" });
            Assert.Contains(cities[1], new[] { "Rethimno", "Trikala" });
            Assert.NotEqual(cities[0], cities[1]);
            Assert.Equal(cities[0], cities[2]);
            Assert.Contains(cities[3], new[] { "Geneva", "Bern" });
            Assert.Equal(0, masker.ValuesWithoutReplacement);
        }

        [Fact]
        public void T12_ReplacementQuery_MappingTable_PerGroup()
        {
            // === Arrange ===
            string csv = "ID;Name;Country\r\n1;Nick;GR\r\n2;Nick;CH\r\n3;eva;GR\r\n4;Zed;GR";
            var cmd = utility.TestDbConnection.CreateCommand();
            cmd.CommandText = @"CREATE TABLE name_mapping AS 
                SELECT 'Nick' AS originalName, 'John'    AS pseudonym, 'GR' AS country UNION ALL 
                SELECT 'Nick' AS originalName, 'Giorgos' AS pseudonym, 'CH' AS country UNION ALL 
                SELECT 'Eva'  AS originalName, 'Maria'   AS pseudonym, 'GR' AS country";
            cmd.ExecuteNonQuery();
            utility.TestTablesToDrop = "name_mapping";

            // === Act ===
            var (rows, masker) = MaskCsv(csv,
                new FieldMask() { FieldName = "ID", MaskType = MaskType.None },
                new FieldMask()
                {
                    FieldName = "Name",
                    MaskType = MaskType.ReplacementQuery,
                    QueryReplacement = new QueryReplacement()
                    {
                        Query = "SELECT originalName, pseudonym, country FROM name_mapping",
                        KeyDbField = "originalName",
                        ValueDbField = "pseudonym",
                        GroupDbField = "country",
                        SelectorField = "Country"
                    }
                },
                new FieldMask() { FieldName = "Country", MaskType = MaskType.None });

            // === Assert ===
            Assert.Equal(["John", "Giorgos", "Maria", "***"], rows.Select(r => r[1]));
            Assert.Equal(1, masker.ValuesWithoutReplacement);
        }

        [Fact]
        public void T13_ReplacementList_Docx_Mapping()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var config = new MaskConfig()
            {
                DataSource = DataSource.DocXFile,
                DataDestination = DataDestination.DocXFile,
                InputFile = Path.Combine(utility.TestInstanceDir, "SecretCombination.docx"),
                OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.docx")
            };
            config.FieldMasks.Add(ListMask("heart", Mapped("", ("heart", "soul"))));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var texts = ReadDocxTexts(config.OutputFile);
            Assert.Equal(6, texts.Count(t => t == "In the center of my soul"));
            Assert.DoesNotContain(texts, t => t.Contains("heart"));
        }

        [Fact]
        public void T14_ReplacementList_Docx_RegEx_Pool()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var config = new MaskConfig()
            {
                DataSource = DataSource.DocXFile,
                DataDestination = DataDestination.DocXFile,
                InputFile = Path.Combine(utility.TestInstanceDir, "SecretCombination.docx"),
                OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.docx")
            };
            config.FieldMasks.Add(new FieldMask()
            {
                FieldName = "words",
                MaskType = MaskType.ReplacementList,
                RegExMatch = @"\b(secret|mystery)\b",
                RegExGroupToReplace = 1,
                ListOfPossibleReplacements = { Pool("alpha,beta") }
            });

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var texts = ReadDocxTexts(config.OutputFile);
            var secretWords = texts.Select(t => Regex.Match(t, @"^My (\w+) combination$")).Where(m => m.Success).Select(m => m.Groups[1].Value).ToList();
            var mysteryWords = texts.Select(t => Regex.Match(t, @"^It's a (\w+) for you$")).Where(m => m.Success).Select(m => m.Groups[1].Value).ToList();

            Assert.Equal(10, secretWords.Count);
            Assert.Equal(5, mysteryWords.Count);
            Assert.Single(secretWords.Distinct());
            Assert.Single(mysteryWords.Distinct());
            Assert.NotEqual(secretWords[0], mysteryWords[0]);
            Assert.Contains(secretWords[0], new[] { "alpha", "beta" });
            Assert.Contains(mysteryWords[0], new[] { "alpha", "beta" });
        }

        [Fact]
        public void T15_ReplacementList_Xlsx_Exhausted()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var config = new MaskConfig()
            {
                DataSource = DataSource.XlsXFile,
                DataDestination = DataDestination.XlsXFile,
                InputFile = Path.Combine(utility.TestInstanceDir, "ibans.xlsx"),
                OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.xlsx")
            };
            // the file has 8 different IBANs, but only 3 replacements
            config.FieldMasks.Add(new FieldMask()
            {
                FieldName = "IBAN",
                MaskType = MaskType.ReplacementList,
                RegExMatch = @"\b([A-Z]{2}\d{2}\w{10,30})\b",
                RegExGroupToReplace = 1,
                ListOfPossibleReplacements = { Pool("IBAN-1,IBAN-2,IBAN-3") }
            });

            // === Act ===
            var masker = DataMasking.Create(config);
            masker.Mask();

            // === Assert ===
            var texts = new List<string>();
            using (var doc = SpreadsheetDocument.Open(config.OutputFile, false))
            {
                foreach (var item in doc.WorkbookPart.SharedStringTablePart.SharedStringTable.Elements<DocumentFormat.OpenXml.Spreadsheet.SharedStringItem>())
                {
                    texts.Add(item.Text?.Text);
                }
            }

            Assert.Equal(3, texts.Count(t => t != null && t.StartsWith("IBAN-")));
            Assert.Equal(5, texts.Count(t => !string.IsNullOrEmpty(t) && t.All(c => c == '*')));
            Assert.Equal(5, masker.ValuesWithoutReplacement);
        }

        [Fact]
        public void T16_Config_XmlRoundTrip()
        {
            // === Arrange ===
            var config = new MaskConfig();
            var cityMask = ListMask("city", new Replacement() { Selector = "country=GR", ReplacementList = "P1,P2", Mappings = { new ReplacementMap() { From = "Athens", To = "Capital" } } }, Pool("T1,T2"));
            cityMask.OnListExhausted = ListExhaustedAction.Error;
            var nameMask = new FieldMask()
            {
                FieldName = "name",
                MaskType = MaskType.ReplacementQuery,
                QueryReplacement = new QueryReplacement() { Query = "SELECT a, b FROM t", KeyDbField = "a", ValueDbField = "b" }
            };
            config.FieldMasks.Add(cityMask);
            config.FieldMasks.Add(nameMask);
            string file = Path.Combine(utility.TestInstanceDir, $"config_{Guid.NewGuid()}.xml");

            // === Act ===
            MaskConfig.SaveConfig(file, config);
            var config2 = MaskConfig.ReadConfig(file);

            // === Assert ===
            string xml = File.ReadAllText(file);
            Assert.Contains("<Map From=\"Athens\" To=\"Capital\" />", xml);
            // the default is not written
            Assert.Single(Regex.Matches(xml, "<OnListExhausted>"));

            var city2 = config2.FieldMasks[0];
            Assert.Equal(MaskType.ReplacementList, city2.MaskType);
            Assert.Equal(ListExhaustedAction.Error, city2.OnListExhausted);
            Assert.Equal("Capital", city2.ListOfPossibleReplacements[0].Mappings[0].To);
            Assert.Empty(city2.ListOfPossibleReplacements[1].Mappings);

            var name2 = config2.FieldMasks[1];
            Assert.Equal(MaskType.ReplacementQuery, name2.MaskType);
            Assert.Equal(ListExhaustedAction.Asterisk, name2.OnListExhausted);
            Assert.Equal("a", name2.QueryReplacement.KeyDbField);
        }

        [Fact]
        public void T17_ReplacementList_Docx_MappedValuesAreFoundWithoutRegEx()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var config = new MaskConfig()
            {
                DataSource = DataSource.DocXFile,
                DataDestination = DataDestination.DocXFile,
                InputFile = Path.Combine(utility.TestInstanceDir, "SecretCombination.docx"),
                OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.docx")
            };
            // the field name is just a label here; the mapped values are searched, ignoring case
            config.FieldMasks.Add(ListMask("all", Mapped("", ("HEART", "soul"), ("book", "tablet"))));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var texts = ReadDocxTexts(config.OutputFile);
            Assert.Equal(6, texts.Count(t => t == "In the center of my soul"));
            Assert.Equal(2, texts.Count(t => t == "An open tablet"));
            Assert.DoesNotContain(texts, t => t.Contains("heart") || t.Contains("book"));
            Assert.Contains("Use your imagination", texts);
        }

        [Fact]
        public void T18_ReplacementList_Xlsx_MappedValuesAreFoundWithoutRegEx()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var config = new MaskConfig()
            {
                DataSource = DataSource.XlsXFile,
                DataDestination = DataDestination.XlsXFile,
                InputFile = Path.Combine(utility.TestInstanceDir, "ibans.xlsx"),
                OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.xlsx")
            };
            // values with spaces and brackets are matched as they are
            config.FieldMasks.Add(ListMask("all", Mapped("", ("Austria (AT)", "Land A"), ("AT483200000012345864", "IBAN-A"))));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var texts = ReadXlsxTexts(config.OutputFile);
            Assert.Contains("Land A", texts);
            Assert.Contains("IBAN-A", texts);
            Assert.DoesNotContain("Austria (AT)", texts);
            Assert.DoesNotContain("AT483200000012345864", texts);
            Assert.Contains("Germany (DE)", texts);
        }

        [Fact]
        public void T19_ReplacementQuery_Xlsx_MappingTableValuesAreFoundWithoutRegEx()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            var cmd = utility.TestDbConnection.CreateCommand();
            cmd.CommandText = @"CREATE TABLE code_mapping AS 
                SELECT 'Germany (DE)' AS originalValue, 'Land D' AS replacement UNION ALL 
                SELECT 'NL18ABNA0484869868' AS originalValue, 'IBAN-NL' AS replacement";
            cmd.ExecuteNonQuery();
            utility.TestTablesToDrop = "code_mapping";

            var config = new MaskConfig()
            {
                DataSource = DataSource.XlsXFile,
                DataDestination = DataDestination.XlsXFile,
                InputFile = Path.Combine(utility.TestInstanceDir, "ibans.xlsx"),
                OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.xlsx")
            };
            config.SetConnection(utility.TestDbConnection);
            config.FieldMasks.Add(new FieldMask()
            {
                FieldName = "all",
                MaskType = MaskType.ReplacementQuery,
                QueryReplacement = new QueryReplacement()
                {
                    Query = "SELECT originalValue, replacement FROM code_mapping",
                    KeyDbField = "originalValue",
                    ValueDbField = "replacement"
                }
            });

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var texts = ReadXlsxTexts(config.OutputFile);
            Assert.Contains("Land D", texts);
            Assert.Contains("IBAN-NL", texts);
            Assert.DoesNotContain("Germany (DE)", texts);
            Assert.Contains("Austria (AT)", texts);
        }

        private static ReplacementCSV Csv(string file, string from, string to, string delimiter = null)
        {
            var csv = new ReplacementCSV() { CSVFileName = file, FromFieldName = from, ToFieldName = to };
            if (delimiter != null)
            {
                csv.Delimiter = delimiter;
            }
            return csv;
        }

        private static FieldMask CsvMask(string fieldName, ReplacementCSV csv)
        {
            var mask = new FieldMask() { FieldName = fieldName, MaskType = MaskType.ReplacementList };
            mask.ListOfPossibleReplacements.Add(csv);
            return mask;
        }

        private Exception MaskAndCatch(FieldMask mask)
        {
            string csvFilename = utility.CreateFileInTestInstanceDir("id;city\r\n1;Athens", "csv");
            var config = new MaskConfig() { InputFile = csvFilename, OutputFile = csvFilename.Replace(".csv", "_out.csv") };
            config.FieldMasks.Add(mask);
            return Record.Exception(() => DataMasking.Create(config).Mask());
        }

        [Fact]
        public void T20_ReplacementCSV_Flat_DefaultDelimiter_NoRegEx()
        {
            // === Arrange ===
            string mapFile = utility.CreateFileInTestInstanceDir("original,pseudonym\r\nAthens,CityA\r\nlondon,CityB\r\n", "csv");
            string csv = "id;city\r\n1;Athens\r\n2;London\r\n3;Berlin\r\n4;athens";

            // === Act ===
            var (rows, masker) = MaskCsv(csv,
                new FieldMask() { FieldName = "id", MaskType = MaskType.None },
                CsvMask("city", Csv(mapFile, "original", "pseudonym")));

            // === Assert ===
            Assert.Equal(["CityA", "CityB", "******", "CityA"], rows.Select(r => r[1]));
            Assert.Equal(1, masker.ValuesWithoutReplacement);
        }

        [Fact]
        public void T21_ReplacementCSV_Flat_CustomDelimiter_WithRegEx()
        {
            // === Arrange ===
            string mapFile = utility.CreateFileInTestInstanceDir("original;pseudonym\r\n785412798;111\r\n17612345;222\r\n", "csv");
            string csv = "id;phone\r\n1;+41 785412798\r\n2;+49 17612345";
            var phoneMask = CsvMask("phone", Csv(mapFile, "original", "pseudonym", ";"));
            phoneMask.RegExMatch = @"^(\+\d\d) (.*)$";
            phoneMask.RegExGroupToReplace = 2;

            // === Act ===
            var (rows, _) = MaskCsv(csv, new FieldMask() { FieldName = "id", MaskType = MaskType.None }, phoneMask);

            // === Assert ===
            Assert.Equal(["+41 111", "+49 222"], rows.Select(r => r[1]));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void T22_ReplacementCSV_Docx(bool withRegEx)
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string mapFile = utility.CreateFileInTestInstanceDir("from,to\r\nheart,soul\r\nBOOK,tablet\r\n", "csv");
            var config = new MaskConfig()
            {
                DataSource = DataSource.DocXFile,
                DataDestination = DataDestination.DocXFile,
                InputFile = Path.Combine(utility.TestInstanceDir, "SecretCombination.docx"),
                OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}_{withRegEx}.docx")
            };
            var mask = CsvMask("all", Csv(mapFile, "from", "to"));
            if (withRegEx)
            {
                mask.RegExMatch = @"\b(heart|book)\b";
                mask.RegExGroupToReplace = 1;
            }
            config.FieldMasks.Add(mask);

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var texts = ReadDocxTexts(config.OutputFile);
            Assert.Equal(6, texts.Count(t => t == "In the center of my soul"));
            Assert.Equal(2, texts.Count(t => t == "An open tablet"));
            Assert.DoesNotContain(texts, t => t.Contains("heart") || t.Contains("book"));
            // the configuration is not changed by the masking
            Assert.True(string.IsNullOrEmpty(config.FieldMasks[0].RegExMatch) == !withRegEx);
        }

        [Fact]
        public void T23_ReplacementCSV_Xlsx_NoRegEx()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            string mapFile = utility.CreateFileInTestInstanceDir("from,to\r\n\"Austria (AT)\",Land A\r\nAT483200000012345864,IBAN-A\r\n", "csv");
            var config = new MaskConfig()
            {
                DataSource = DataSource.XlsXFile,
                DataDestination = DataDestination.XlsXFile,
                InputFile = Path.Combine(utility.TestInstanceDir, "ibans.xlsx"),
                OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.xlsx")
            };
            config.FieldMasks.Add(CsvMask("all", Csv(mapFile, "from", "to")));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var texts = ReadXlsxTexts(config.OutputFile);
            Assert.Contains("Land A", texts);
            Assert.Contains("IBAN-A", texts);
            Assert.DoesNotContain("Austria (AT)", texts);
            Assert.Contains("Germany (DE)", texts);
        }

        [Fact]
        public void T24_ReplacementCSV_And_Replacement_Throws()
        {
            // === Arrange ===
            string mapFile = utility.CreateFileInTestInstanceDir("from,to\r\nAthens,CityA\r\n", "csv");
            var mask = CsvMask("city", Csv(mapFile, "from", "to"));
            mask.ListOfPossibleReplacements.Add(Pool("X"));

            // === Act ===
            var ex = MaskAndCatch(mask);

            // === Assert ===
            Assert.IsType<InvalidDataException>(ex);
            Assert.Contains("together", ex.Message);
        }

        [Theory]
        [InlineData("CSVFileName")]
        [InlineData("FromFieldName")]
        [InlineData("ToFieldName")]
        public void T25_ReplacementCSV_MissingAttribute_Throws(string missing)
        {
            // === Arrange ===
            string mapFile = utility.CreateFileInTestInstanceDir("from,to\r\nAthens,CityA\r\n", "csv");
            var csv = Csv(mapFile, "from", "to");
            switch (missing)
            {
                case "CSVFileName": csv.CSVFileName = null; break;
                case "FromFieldName": csv.FromFieldName = ""; break;
                case "ToFieldName": csv.ToFieldName = null; break;
            }

            // === Act ===
            var ex = MaskAndCatch(CsvMask("city", csv));

            // === Assert ===
            Assert.IsType<InvalidDataException>(ex);
            Assert.Contains(missing, ex.Message);
        }

        [Fact]
        public void T26_ReplacementCSV_FileNotFound_Throws()
        {
            // === Act ===
            var ex = MaskAndCatch(CsvMask("city", Csv(Path.Combine(utility.TestInstanceDir, "nothing.csv"), "from", "to")));

            // === Assert ===
            Assert.IsType<FileNotFoundException>(ex);
        }

        [Theory]
        [InlineData("original,pseudonym\r\nAthens,CityA\r\n", "nope", "pseudonym", "FromFieldName")]
        [InlineData("original,pseudonym\r\nAthens,CityA\r\n", "original", "nope", "ToFieldName")]
        [InlineData("original,pseudonym\r\nAthens,CityA\r\nathens ,CityB\r\n", "original", "pseudonym", "more than once")]
        [InlineData("original,pseudonym\r\n", "original", "pseudonym", "no data")]
        public void T27_ReplacementCSV_InvalidFile_Throws(string content, string from, string to, string expectedInMessage)
        {
            // === Arrange ===
            string mapFile = utility.CreateFileInTestInstanceDir(content, "csv");

            // === Act ===
            var ex = MaskAndCatch(CsvMask("city", Csv(mapFile, from, to)));

            // === Assert ===
            Assert.IsType<InvalidDataException>(ex);
            Assert.Contains(expectedInMessage, ex.Message);
            // data from the file is not part of the message
            Assert.DoesNotContain("Athens", ex.Message);
        }

        [Fact]
        public void T28_ReplacementCSV_WithOtherMaskType_Throws()
        {
            // === Arrange ===
            string mapFile = utility.CreateFileInTestInstanceDir("from,to\r\nAthens,CityA\r\n", "csv");
            var mask = CsvMask("city", Csv(mapFile, "from", "to"));
            mask.MaskType = MaskType.List;

            // === Act ===
            var ex = MaskAndCatch(mask);

            // === Assert ===
            Assert.IsType<InvalidDataException>(ex);
            Assert.Contains("ReplacementList", ex.Message);
        }

        [Fact]
        public void T29_ReplacementCSV_XmlRoundTrip()
        {
            // === Arrange ===
            var config = new MaskConfig();
            config.FieldMasks.Add(CsvMask("city", Csv("map.csv", "a", "b")));
            config.FieldMasks.Add(CsvMask("name", Csv("names.csv", "x", "y", ";")));
            config.FieldMasks.Add(ListMask("other", Pool("X,Y")));
            string file = Path.Combine(utility.TestInstanceDir, $"config_{Guid.NewGuid()}.xml");

            // === Act ===
            MaskConfig.SaveConfig(file, config);
            var config2 = MaskConfig.ReadConfig(file);

            // === Assert ===
            string xml = File.ReadAllText(file);
            Assert.Contains("<ReplacementCSV CSVFileName=\"map.csv\" FromFieldName=\"a\" ToFieldName=\"b\" />", xml);
            Assert.Contains("Delimiter=\";\"", xml);
            Assert.Single(Regex.Matches(xml, "Delimiter="));

            var csv1 = Assert.IsType<ReplacementCSV>(Assert.Single(config2.FieldMasks[0].ListOfPossibleReplacements));
            Assert.Equal("map.csv", csv1.CSVFileName);
            Assert.Equal("a", csv1.FromFieldName);
            Assert.Equal("b", csv1.ToFieldName);
            Assert.Equal(",", csv1.Delimiter);

            var csv2 = Assert.IsType<ReplacementCSV>(Assert.Single(config2.FieldMasks[1].ListOfPossibleReplacements));
            Assert.Equal(";", csv2.Delimiter);

            var plain = Assert.Single(config2.FieldMasks[2].ListOfPossibleReplacements);
            Assert.IsNotType<ReplacementCSV>(plain);
            Assert.Equal("X,Y", plain.ReplacementList);
        }

        private void CreateTable(string name, string selectSql)
        {
            var cmd = utility.TestDbConnection.CreateCommand();
            cmd.CommandText = $"CREATE TABLE {name} AS {selectSql}";
            cmd.ExecuteNonQuery();
        }

        private static FieldMask QueryMask(string fieldName, string query, string valueField, string keyField = null)
        {
            return new FieldMask()
            {
                FieldName = fieldName,
                MaskType = MaskType.ReplacementQuery,
                QueryReplacement = new QueryReplacement() { Query = query, ValueDbField = valueField, KeyDbField = keyField }
            };
        }

        private MaskConfig DocxConfig(string testName)
        {
            var config = new MaskConfig()
            {
                DataSource = DataSource.DocXFile,
                DataDestination = DataDestination.DocXFile,
                InputFile = Path.Combine(utility.TestInstanceDir, "SecretCombination.docx"),
                OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.docx")
            };
            config.SetConnection(utility.TestDbConnection);
            return config;
        }

        private MaskConfig CsvConfig(string csvContent, params FieldMask[] masks)
        {
            string csvFilename = utility.CreateFileInTestInstanceDir(csvContent, "csv");
            var config = new MaskConfig() { InputFile = csvFilename, OutputFile = csvFilename.Replace(".csv", "_out.csv") };
            config.SetConnection(utility.TestDbConnection);
            config.FieldMasks.AddRange(masks);
            return config;
        }

        [Fact]
        public void T30_ReplacementQuery_Docx_Pool()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            CreateTable("word_pool", "SELECT 'soul' AS word UNION ALL SELECT 'spirit' AS word");
            var config = DocxConfig(testName);
            config.FieldMasks.Add(QueryMask("heart", "SELECT word FROM word_pool", "word"));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var texts = ReadDocxTexts(config.OutputFile);
            var replacements = texts.Select(t => Regex.Match(t, @"^In the center of my (\w+)$")).Where(m => m.Success).Select(m => m.Groups[1].Value).ToList();
            Assert.Equal(6, replacements.Count);
            Assert.Single(replacements.Distinct());
            Assert.Contains(replacements[0], new[] { "soul", "spirit" });
            Assert.DoesNotContain(texts, t => t.Contains("heart"));
        }

        [Fact]
        public void T31_ReplacementQuery_Docx_MappingTable()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            CreateTable("word_mapping", "SELECT 'HEART' AS originalWord, 'soul' AS replacement UNION ALL SELECT 'book' AS originalWord, 'tablet' AS replacement");
            var config = DocxConfig(testName);
            config.FieldMasks.Add(QueryMask("all", "SELECT originalWord, replacement FROM word_mapping", "replacement", "originalWord"));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var texts = ReadDocxTexts(config.OutputFile);
            Assert.Equal(6, texts.Count(t => t == "In the center of my soul"));
            Assert.Equal(2, texts.Count(t => t == "An open tablet"));
            Assert.DoesNotContain(texts, t => t.Contains("heart") || t.Contains("book"));
            Assert.Contains("Use your imagination", texts);
        }

        [Fact]
        public void T32_ReplacementQuery_Docx_RegEx_Pool()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            CreateTable("word_pool", "SELECT 'alpha' AS word UNION ALL SELECT 'beta' AS word");
            var config = DocxConfig(testName);
            var mask = QueryMask("words", "SELECT word FROM word_pool", "word");
            mask.RegExMatch = @"\b(secret|mystery)\b";
            mask.RegExGroupToReplace = 1;
            config.FieldMasks.Add(mask);

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var texts = ReadDocxTexts(config.OutputFile);
            var secretWords = texts.Select(t => Regex.Match(t, @"^My (\w+) combination$")).Where(m => m.Success).Select(m => m.Groups[1].Value).ToList();
            var mysteryWords = texts.Select(t => Regex.Match(t, @"^It's a (\w+) for you$")).Where(m => m.Success).Select(m => m.Groups[1].Value).ToList();

            Assert.Equal(10, secretWords.Count);
            Assert.Equal(5, mysteryWords.Count);
            Assert.Single(secretWords.Distinct());
            Assert.Single(mysteryWords.Distinct());
            Assert.NotEqual(secretWords[0], mysteryWords[0]);
            Assert.Contains(secretWords[0], new[] { "alpha", "beta" });
            Assert.Contains(mysteryWords[0], new[] { "alpha", "beta" });
        }

        [Fact]
        public void T33_ReplacementQuery_Xlsx_Pool_Exhausted()
        {
            // === Arrange ===
            string testName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            CreateTable("iban_pool", "SELECT 'IBAN-1' AS code UNION ALL SELECT 'IBAN-2' AS code UNION ALL SELECT 'IBAN-3' AS code");
            var config = new MaskConfig()
            {
                DataSource = DataSource.XlsXFile,
                DataDestination = DataDestination.XlsXFile,
                InputFile = Path.Combine(utility.TestInstanceDir, "ibans.xlsx"),
                OutputFile = Path.Combine(utility.TestInstanceDir, $"{testName}.xlsx")
            };
            config.SetConnection(utility.TestDbConnection);
            // the file has 8 different IBANs, but only 3 replacements
            var mask = QueryMask("IBAN", "SELECT code FROM iban_pool", "code");
            mask.RegExMatch = @"\b([A-Z]{2}\d{2}\w{10,30})\b";
            mask.RegExGroupToReplace = 1;
            config.FieldMasks.Add(mask);

            // === Act ===
            var masker = DataMasking.Create(config);
            masker.Mask();

            // === Assert ===
            var texts = ReadXlsxTexts(config.OutputFile);
            Assert.Equal(3, texts.Count(t => t != null && t.StartsWith("IBAN-")));
            Assert.Equal(5, texts.Count(t => !string.IsNullOrEmpty(t) && t.All(c => c == '*')));
            Assert.Equal(5, masker.ValuesWithoutReplacement);
        }

        [Fact]
        public void T34_ReplacementQuery_RegEx_ReplacesOnlyTheGroupAndEveryMatch()
        {
            // === Arrange ===
            string csv = "id;phone\r\n1;+41 785412798\r\n2;+41 785412798\r\n3;+49 17612345\r\n4;+49 17699999";
            CreateTable("phone_pool", "SELECT '111' AS num UNION ALL SELECT '222' AS num UNION ALL SELECT '333' AS num UNION ALL SELECT '444' AS num");
            var phoneMask = QueryMask("phone", "SELECT num FROM phone_pool", "num");
            phoneMask.RegExMatch = @"^(\+\d\d) (.*)$";
            phoneMask.RegExGroupToReplace = 2;

            // === Act ===
            var (rows, _) = MaskCsv(csv, new FieldMask() { FieldName = "id", MaskType = MaskType.None }, phoneMask);

            // === Assert ===
            var phones = rows.Select(r => r[1]).ToList();
            Assert.StartsWith("+41 ", phones[0]);
            Assert.StartsWith("+41 ", phones[1]);
            Assert.StartsWith("+49 ", phones[2]);
            Assert.Equal(phones[0], phones[1]);
            // two different numbers, plus the repeated one
            Assert.Equal(3, phones.Distinct().Count());
        }

        [Fact]
        public void T35_ReplacementQuery_JSON_MappingTable()
        {
            // === Arrange ===
            CreateTable("name_mapping", "SELECT 'Nick' AS originalName, 'John' AS pseudonym UNION ALL SELECT 'Eva' AS originalName, 'Jane' AS pseudonym");
            string json = "[{\"name\":\"Nick\"},{\"name\":\"Eva\"},{\"name\":\"nick\"}]";
            string jsonFilename = utility.CreateFileInTestInstanceDir(json, "json");
            var config = new MaskConfig()
            {
                DataSource = DataSource.JsonFile,
                DataDestination = DataDestination.JsonFile,
                InputFile = jsonFilename,
                OutputFile = jsonFilename.Replace(".json", "_out.json")
            };
            config.SetConnection(utility.TestDbConnection);
            config.FieldMasks.Add(QueryMask("name", "SELECT originalName, pseudonym FROM name_mapping", "pseudonym", "originalName"));

            // === Act ===
            DataMasking.Create(config).Mask();

            // === Assert ===
            var result = JArray.Parse(File.ReadAllText(config.OutputFile));
            Assert.Equal(["John", "Jane", "John"], result.Select(x => (string)x["name"]));
        }

        [Fact]
        public void T36_ReplacementQuery_Pool_Exhausted_Asterisk_IsDefault()
        {
            // === Arrange ===
            string csv = "id;city\r\n1;Athens\r\n2;Madrid\r\n3;Athens\r\n4;Madrid";
            CreateTable("city_pool", "SELECT 'X' AS city");

            // === Act ===
            var (rows, masker) = MaskCsv(csv,
                new FieldMask() { FieldName = "id", MaskType = MaskType.None },
                QueryMask("city", "SELECT city FROM city_pool", "city"));

            // === Assert ===
            var cities = rows.Select(r => r[1]).ToList();
            Assert.Equal(["X", "******"], cities.Distinct().OrderByDescending(c => c, StringComparer.Ordinal));
            Assert.Equal(cities[0], cities[2]);
            Assert.Equal(cities[1], cities[3]);
            Assert.Equal(1, masker.ValuesWithoutReplacement);
        }

        [Fact]
        public void T37_ReplacementQuery_MappingTable_Exhausted_Asterisk_CustomCharacter()
        {
            // === Arrange ===
            string csv = "id;city\r\n1;Athens\r\n2;Madrid";
            CreateTable("city_mapping", "SELECT 'Athens' AS originalCity, 'CityA' AS replacement");
            var cityMask = QueryMask("city", "SELECT originalCity, replacement FROM city_mapping", "replacement", "originalCity");
            cityMask.Asterisk = "#";

            // === Act ===
            var (rows, masker) = MaskCsv(csv, new FieldMask() { FieldName = "id", MaskType = MaskType.None }, cityMask);

            // === Assert ===
            Assert.Equal(["CityA", "######"], rows.Select(r => r[1]));
            Assert.Equal(1, masker.ValuesWithoutReplacement);
        }

        [Fact]
        public void T38_ReplacementQuery_Exhausted_Error()
        {
            // === Arrange ===
            CreateTable("city_pool", "SELECT 'X' AS city");
            var cityMask = QueryMask("city", "SELECT city FROM city_pool", "city");
            cityMask.OnListExhausted = ListExhaustedAction.Error;
            var config = CsvConfig("id;city\r\n1;Athens\r\n2;Madrid", cityMask);

            // === Act & Assert ===
            Assert.Throws<ReplacementExhaustedException>(() => DataMasking.Create(config).Mask());
        }

        [Fact]
        public void T39_ReplacementQuery_Exhausted_Reuse()
        {
            // === Arrange ===
            string csv = "id;city\r\n1;Athens\r\n2;Madrid\r\n3;Athens";
            CreateTable("city_pool", "SELECT 'X' AS city");
            var cityMask = QueryMask("city", "SELECT city FROM city_pool", "city");
            cityMask.OnListExhausted = ListExhaustedAction.Reuse;

            // === Act ===
            var (rows, masker) = MaskCsv(csv, new FieldMask() { FieldName = "id", MaskType = MaskType.None }, cityMask);

            // === Assert ===
            Assert.All(rows.Select(r => r[1]), c => Assert.Equal("X", c));
            Assert.Equal(0, masker.ValuesWithoutReplacement);
        }

        [Fact]
        public void T40_ReplacementQuery_MissingQueryReplacement_Throws()
        {
            // === Arrange ===
            var config = CsvConfig("id;city\r\n1;Athens",
                new FieldMask() { FieldName = "city", MaskType = MaskType.ReplacementQuery });

            // === Act & Assert ===
            var ex = Assert.Throws<InvalidDataException>(() => DataMasking.Create(config).Mask());
            Assert.Contains("QueryReplacement", ex.Message);
        }

        private static List<string> ReadXlsxTexts(string file)
        {
            using var doc = SpreadsheetDocument.Open(file, false);
            return doc.WorkbookPart.SharedStringTablePart.SharedStringTable
                .Elements<DocumentFormat.OpenXml.Spreadsheet.SharedStringItem>()
                .Select(i => i.Text?.Text)
                .ToList();
        }

        private static List<string> ReadDocxTexts(string file)
        {
            using var doc = WordprocessingDocument.Open(file, false);
            return doc.MainDocumentPart.Document.Body
                .Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
                .Select(t => t.Text)
                .ToList();
        }
    }
}
