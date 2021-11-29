using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dandraka.Zoro.Processor
{
    public enum DataDestination
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