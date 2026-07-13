namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Specifies where the data is coming from.
    /// </summary>
    public enum DataSource
    {
        /// <summary>
        /// The data comes from a csv file.
        /// </summary>
        CsvFile,

        /// <summary>
        /// The data is obtained by executing a DB query.
        /// </summary>
        Database,

        /// <summary>
        /// The data comes from a JSON file.
        /// </summary>
        JsonFile,

        /// <summary>
        /// The data comes from a DOCX file.
        /// </summary>
        DocXFile,

        /// <summary>
        /// The data comes from an XLSX file.
        /// </summary>
        XlsXFile        
    }
}