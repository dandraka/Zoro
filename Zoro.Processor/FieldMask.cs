using System;
using System.Collections.Generic;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Information about the masking of a single field.
    /// </summary>
    [Serializable]
    public class FieldMask
    {
        /// <summary>
        /// Creates an instance of FieldMask class.
        /// </summary>
        public FieldMask()
        {
            this.MaskType = MaskType.None;
            this.Asterisk = "*";
            this.ListOfPossibleReplacements = new List<Replacement>();
        }

        /// <summary>
        /// The name of the field. Note that field names are case-insensitive for CSV files and DB queries, but case-sensitive for JSON files.
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// The type of masking to apply. The default is None.
        /// </summary>
        public MaskType MaskType { get; set; }

        /// <summary>
        /// In case of <c>MaskType.Asterisk</c>, the character to apply. The default is asterisk (*).
        /// </summary>
        public string Asterisk { get; set; }

        /// <summary>
        /// If filled, only matches from the regular expression are changed.
        /// If not, the whole field is changed.
        /// </summary>
        public string RegExMatch { get; set; }

        /// <summary>
        /// Valid only if <c>RegExMatch</c> is filled.
        /// </summary>
        public int RegExGroupToReplace { get; set; }

        /// <summary>
        /// In case of <c>MaskType.Query</c>, the SQL query to get the list of replacements from.
        /// </summary>
        public QueryReplacement QueryReplacement { get; set; }

        /// <summary>
        /// In case of <c>MaskType.List</c>, the comma-separated list of items to choose from.
        /// </summary>
        public List<Replacement> ListOfPossibleReplacements { get; set; }

        /// <summary>
        /// In case of <c>MaskType.Expression</c>, the expression to use.
        /// The field contents are substituted with a combination of a constant string and values from other fields.
        /// Must be filled with a constant string and field names enclosed in double curly brackets. 
        /// For example "Customer-{{CustomerID}}" (without the quotes).
        /// When the data source is Json, a JsonPath is expected in the place of field name. The JsonPath
        /// will be applied on the root of the Json.
        /// For example "Customer-{{$.CustomerID}}" (without the quotes).
        /// </summary>
        public string Expression { get; set; }
    }
}