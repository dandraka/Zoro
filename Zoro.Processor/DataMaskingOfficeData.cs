using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Shared logic for Office files, where masks are applied to the text found in the document.
    /// </summary>
    internal abstract class DataMaskingOfficeData<T> : DataMasking<T>
    {
        protected DataMaskingOfficeData(MaskConfig config) : base(config)
        {
        }

        /// <summary>
        /// Without an explicit RegExMatch, the text to mask is the field name or, for masks that have
        /// mappings, all the mapped values.
        /// </summary>
        protected void PrepareTextMasks()
        {
            foreach (var textMask in config.FieldMasks)
            {
                if (!string.IsNullOrWhiteSpace(textMask.RegExMatch))
                {
                    continue;
                }

                var mappedValues = GetMappedValues(textMask);
                textMask.RegExMatch = mappedValues.Count > 0 ? BuildMappedValuesRegex(mappedValues) : $"({textMask.FieldName})";
                textMask.RegExGroupToReplace = 1;
            }
        }

        private List<string> GetMappedValues(FieldMask textMask)
        {
            IEnumerable<Replacement> replacements = Enumerable.Empty<Replacement>();
            if (textMask.MaskType == MaskType.ReplacementList)
            {
                replacements = textMask.ListOfPossibleReplacements;
            }
            else if (textMask.MaskType == MaskType.ReplacementQuery && !string.IsNullOrWhiteSpace(textMask.QueryReplacement?.KeyDbField))
            {
                replacements = GetReplacementQueryLists(textMask.QueryReplacement);
            }

            return replacements
                .SelectMany(r => r.Mappings ?? new List<ReplacementMap>())
                .Select(m => m.From?.Trim())
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Longest first so that e.g. "Senior Manager" wins over "Senior"; no letter or digit may touch the match.
        private static string BuildMappedValuesRegex(List<string> values)
        {
            string alternatives = string.Join("|", values.OrderByDescending(v => v.Length).Select(Regex.Escape));
            return $@"(?i)(?<!\w)({alternatives})(?!\w)";
        }

        protected string MaskText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            foreach (var textMask in config.FieldMasks)
            {
                if (!Regex.IsMatch(text, textMask.RegExMatch))
                {
                    continue;
                }
                text = GetMaskedString(text, textMask, null);
            }
            return text;
        }

        protected void CopyInputToOutput()
        {
            if (!File.Exists(config.InputFile))
            {
                throw new FileNotFoundException(config.InputFile);
            }

            // copy the original to make sure we don't change it
            File.Copy(config.InputFile, config.OutputFile);
        }
    }
}
