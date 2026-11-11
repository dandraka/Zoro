using DocumentFormat.OpenXml.Packaging;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Masking for Word documents (.docx).
    /// </summary>
    internal class DataMaskingDocxData : DataMaskingOfficeData<WordprocessingDocument>
    {
        internal DataMaskingDocxData(MaskConfig config) : base(config)
        {
        }

        protected override WordprocessingDocument GetData()
        {
            CopyInputToOutput();
            return WordprocessingDocument.Open(config.OutputFile, true);
        }

        protected override void Anonymize(WordprocessingDocument doc)
        {
            PrepareTextMasks();

            var body = doc.MainDocumentPart.Document.Body;

            // Iterate through all "Text" elements in the document
            foreach (var textNode in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
            {
                string s = MaskText(textNode.Text);
                if (textNode.Text != s)
                {
                    textNode.Text = s;
                }
            }
        }
    }
}
