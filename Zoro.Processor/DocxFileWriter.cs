using DocumentFormat.OpenXml.Packaging;

namespace Dandraka.Zoro.Processor
{
    internal class DocxFileWriter : IDataWriter<WordprocessingDocument>
    {
        internal DocxFileWriter(MaskConfig config)
        {
        }

        public void Write(WordprocessingDocument doc)
        {
            try
            {
                doc.MainDocumentPart.Document.Save();
            }
            finally
            {
                doc.Dispose();
            }
        }
    }
}
