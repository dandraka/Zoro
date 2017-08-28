using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zoro.Processor
{
    public enum DataSource
    {
        /// <summary>
        /// The data comes from a csv file.
        /// </summary>
        CsvFile,

        /// <summary>
        /// The data is obtained by executing a DB query.
        /// </summary>
        Database
    }
}
