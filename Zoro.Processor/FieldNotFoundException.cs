using System;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Exception raised when a field is not found.
    /// </summary>
    public class FieldNotFoundException : Exception
    {
        /// <summary>
        /// Creates a FieldNotFoundException.
        /// </summary>
        public FieldNotFoundException()
        {
        }

        /// <summary>
        /// Creates a FieldNotFoundException specifying the field name.
        /// </summary>
        public FieldNotFoundException(string field)
            : base($"Field or JsonPath {field} was not found. Note that field names are case-insensitive for CSV files & DB queries, but case-sensitive for JSON files.")
        {
        }

        /// <summary>
        /// Creates a FieldNotFoundException specifying the field name and an inner exception.
        /// </summary>
        public FieldNotFoundException(string field, Exception inner)
            : base($"Field or JsonPath {field} was not found. Note that field names are case-insensitive for CSV files & DB queries, but case-sensitive for JSON files.", inner)
        {
        }
    }
}