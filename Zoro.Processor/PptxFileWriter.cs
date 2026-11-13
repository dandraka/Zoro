using DocumentFormat.OpenXml.Packaging;

namespace Dandraka.Zoro.Processor
{
    internal class PptxFileWriter : IDataWriter<PresentationDocument>
    {
        internal PptxFileWriter(MaskConfig config)
        {
        }

        public void Write(PresentationDocument doc)
        {
            // the parts that were loaded for masking are saved when the document is disposed
            doc.Dispose();
        }
    }
}
