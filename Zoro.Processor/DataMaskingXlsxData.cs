using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Masking for Excel spreadsheets (.xlsx).
    /// </summary>
    internal class DataMaskingXlsxData : DataMaskingOfficeData<SpreadsheetDocument>
    {
        internal DataMaskingXlsxData(MaskConfig config) : base(config)
        {
        }

        protected override SpreadsheetDocument GetData()
        {
            CopyInputToOutput();
            return SpreadsheetDocument.Open(config.OutputFile, true);
        }

        protected override void Anonymize(SpreadsheetDocument doc)
        {
            PrepareTextMasks();

            var sharedStrings = doc.WorkbookPart.SharedStringTablePart.SharedStringTable;

            foreach (var item in sharedStrings.Elements<SharedStringItem>())
            {
                // Plain strings have a direct Text child; rich-text strings (mixed formatting) only have runs,
                // which are masked together as one string, so a match can span several runs.
                if (item.Text != null)
                {
                    MaskInPlace(new List<Text>() { item.Text });
                }
                MaskInPlace(item.Elements<Run>().Where(r => r.Text != null).Select(r => r.Text).ToList());
            }
        }

        private void MaskInPlace(List<Text> textNodes)
        {
            if (textNodes.Count == 0)
            {
                return;
            }

            var original = textNodes.Select(t => t.Text ?? string.Empty).ToList();
            var masked = MaskRunTexts(original);
            for (int i = 0; i < textNodes.Count; i++)
            {
                if (masked[i] != original[i])
                {
                    textNodes[i].Text = masked[i];
                }
            }
        }
    }
}
