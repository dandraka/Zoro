using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        JsonFile
    }
}