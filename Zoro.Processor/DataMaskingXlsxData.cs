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
                // each masked on its own, so a match that spans several runs is not found.
                MaskInPlace(item.Text);
                foreach (var run in item.Elements<Run>())
                {
                    MaskInPlace(run.Text);
                }
            }
        }

        private void MaskInPlace(Text textNode)
        {
            if (textNode == null)
            {
                return;
            }

            string s = MaskText(textNode.Text);
            if (textNode.Text != s)
            {
                textNode.Text = s;
            }
        }
    }
}
