using System;
using System.Data;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Exception raised when a field is not found.
    /// </summary>
    public class DataNotFoundException : Exception
    {
        /// <summary>
        /// Creates a FieldNotFoundException.
        /// </summary>
        public DataNotFoundException()
        {
        }

        /// <summary>
        /// Creates a FieldNotFoundException specifying the field name.
        /// </summary>
        public DataNotFoundException(DataRow row)
            : base($"No match could be located for data row\r\n{string.Join(",", row.ItemArray)}")
        {
        }

        /// <summary>
        /// Creates a FieldNotFoundException specifying the field name and an inner exception.
        /// </summary>
        public DataNotFoundException(DataRow row, Exception inner)
            : base($"No match could be located for data row\r\n{string.Join(",", row.ItemArray)}", inner)
        {
        }
    }
}