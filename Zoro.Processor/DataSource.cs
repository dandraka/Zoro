using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        JsonFile
    }
}