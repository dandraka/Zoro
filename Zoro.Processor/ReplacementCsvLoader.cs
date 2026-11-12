using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GenericParsing;

namespace Dandraka.Zoro.Processor
{
    internal static class ReplacementCsvLoader
    {
        /// <summary>
        /// Reads the csv file of a ReplacementCSV into a Replacement with Maps.
        /// Error messages do not contain data from the file.
        /// </summary>
        internal static Replacement Load(ReplacementCSV csv, string fieldName)
        {
            string path = csv.CSVFileName
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"The csv file of the ReplacementCSV of field '{fieldName}' was not found.", path);
            }

            var replacement = new Replacement() { Selector = string.Empty };
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var parser = new GenericParser())
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.ColumnDelimiter = string.IsNullOrEmpty(csv.Delimiter) ? ',' : csv.Delimiter[0];
                parser.FirstRowHasHeader = true;
                parser.FirstRowSetsExpectedColumnCount = true;
                parser.TrimResults = false;
                parser.MaxBufferSize = 1048576;
                parser.TextQualifier = '\"';
                parser.EscapeCharacter = null;
                parser.SetDataSource(path, Encoding.UTF8);

                int fromIndex = -1;
                int toIndex = -1;
                while (parser.Read())
                {
                    if (fromIndex < 0)
                    {
                        fromIndex = FindColumn(parser, csv.FromFieldName, "FromFieldName", fieldName);
                        toIndex = FindColumn(parser, csv.ToFieldName, "ToFieldName", fieldName);
                    }

                    string from = parser[fromIndex]?.Trim();
                    if (string.IsNullOrEmpty(from))
                    {
                        continue;
                    }
                    if (!seen.Add(from))
                    {
                        throw new InvalidDataException($"The csv file of the ReplacementCSV of field '{fieldName}' contains the same value in '{csv.FromFieldName}' more than once (case and spaces are ignored).");
                    }

                    replacement.Mappings.Add(new ReplacementMap() { From = from, To = parser[toIndex] ?? string.Empty });
                }

                if (fromIndex < 0)
                {
                    throw new InvalidDataException($"The csv file of the ReplacementCSV of field '{fieldName}' contains no data.");
                }
            }

            return replacement;
        }

        private static int FindColumn(GenericParser parser, string columnName, string attributeName, string fieldName)
        {
            for (int i = 0; i < parser.ColumnCount; i++)
            {
                if (string.Equals(parser.GetColumnName(i)?.Trim(), columnName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            throw new InvalidDataException($"The csv file of the ReplacementCSV of field '{fieldName}' has no column named '{columnName}' ({attributeName}).");
        }
    }
}
