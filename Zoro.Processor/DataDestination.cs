namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Specifies where the data should be sent to.
    /// </summary>
    public enum DataDestination
    {
        /// <summary>
        /// The data will be written to a csv file.
        /// </summary>
        ///
        CsvFile,

        /// <summary>
        /// The data will be INSERTed in a db using an SQL query.
        /// </summary>
        Database,

        /// <summary>
        /// The data will be written to a JSON file.
        /// </summary>
        JsonFile,

        /// <summary>
        /// The data will be written to a DOCX file.
        /// </summary>
        DocXFile,

        /// <summary>
        /// The data will be written to an XLSX file.
        /// </summary>
        XlsXFile                       
    }
}