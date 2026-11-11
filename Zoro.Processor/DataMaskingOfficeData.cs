using System.IO;
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
        /// Without an explicit RegExMatch, the field name itself is the text to mask.
        /// </summary>
        protected void PrepareTextMasks()
        {
            foreach (var textMask in config.FieldMasks)
            {
                if (string.IsNullOrWhiteSpace(textMask.RegExMatch))
                {
                    textMask.RegExMatch = $"({textMask.FieldName})";
                    textMask.RegExGroupToReplace = 1;
                }
            }
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
