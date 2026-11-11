using DocumentFormat.OpenXml.Packaging;

namespace Dandraka.Zoro.Processor
{
    internal class XlsxFileWriter : IDataWriter<SpreadsheetDocument>
    {
        internal XlsxFileWriter(MaskConfig config)
        {
        }

        public void Write(SpreadsheetDocument doc)
        {
            try
            {
                doc.WorkbookPart.SharedStringTablePart.SharedStringTable.Save();
            }
            finally
            {
                doc.Dispose();
            }
        }
    }
}
