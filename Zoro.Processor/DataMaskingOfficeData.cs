using System;
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
        // The regex and group used for each mask; the masks of the configuration are left as they are.
        private readonly Dictionary<FieldMask, (Regex regex, int group)> textMasks = new Dictionary<FieldMask, (Regex regex, int group)>();

        protected DataMaskingOfficeData(MaskConfig config) : base(config)
        {
        }

        /// <summary>
        /// Without an explicit RegExMatch, the text to mask is the field name or, for masks that have
        /// mappings, all the mapped values.
        /// </summary>
        protected void PrepareTextMasks()
        {
            textMasks.Clear();
            foreach (var textMask in config.FieldMasks)
            {
                if (!string.IsNullOrWhiteSpace(textMask.RegExMatch))
                {
                    textMasks.Add(textMask, (new Regex(textMask.RegExMatch), textMask.RegExGroupToReplace));
                    continue;
                }

                var mappedValues = GetMappedValues(textMask);
                string pattern = mappedValues.Count > 0 ? BuildMappedValuesRegex(mappedValues) : $"({textMask.FieldName})";
                textMasks.Add(textMask, (new Regex(pattern), 1));
            }
        }

        private List<string> GetMappedValues(FieldMask textMask)
        {
            IEnumerable<Replacement> replacements = Enumerable.Empty<Replacement>();
            if (textMask.MaskType == MaskType.ReplacementList)
            {
                replacements = GetReplacementLists(textMask);
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

        /// <summary>
        /// Masks the texts of consecutive runs as one string, so a match can span several runs.
        /// A replaced match gets the formatting of the run where it starts; the rest of its characters
        /// are removed from the following runs.
        /// </summary>
        /// <returns>The new texts, one for each run.</returns>
        protected List<string> MaskRunTexts(IReadOnlyList<string> runTexts)
        {
            var texts = runTexts.Select(t => t ?? string.Empty).ToList();

            foreach (var textMask in config.FieldMasks)
            {
                var (regex, regexGroup) = textMasks[textMask];

                var edits = new List<(int start, int length, string replacement)>();
                foreach (Match match in regex.Matches(string.Concat(texts)))
                {
                    Group group = regexGroup == 0 ? match : match.Groups[regexGroup];
                    if (!group.Success || group.Length == 0)
                    {
                        continue;
                    }

                    string replacement = GetMaskedValue(group.Value, textMask, null);
                    if (replacement != group.Value)
                    {
                        edits.Add((group.Index, group.Length, replacement));
                    }
                }

                // from the end, so that the positions of the other edits stay valid
                for (int i = edits.Count - 1; i >= 0; i--)
                {
                    ApplyEdit(texts, edits[i].start, edits[i].length, edits[i].replacement);
                }
            }

            return texts;
        }

        private static void ApplyEdit(List<string> texts, int start, int length, string replacement)
        {
            int end = start + length;
            int offset = 0;
            bool inserted = false;

            for (int i = 0; i < texts.Count && offset < end; i++)
            {
                string text = texts[i];
                int runStart = offset;
                int runEnd = offset + text.Length;
                offset = runEnd;
                if (runEnd <= start)
                {
                    continue;
                }

                int localStart = Math.Max(start, runStart) - runStart;
                int localEnd = Math.Min(end, runEnd) - runStart;
                texts[i] = text.Substring(0, localStart) + (inserted ? string.Empty : replacement) + text.Substring(localEnd);
                inserted = true;
            }
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
