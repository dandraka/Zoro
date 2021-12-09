using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Possible replacement, in case of <c>MaskType.List</c>.
    /// </summary>
    [Serializable]
    public class Replacement
    {
        /// <summary>
        /// E.g. "Sex=F"
        /// </summary>
        [XmlAttribute(AttributeName = "Selector")]
        public string Selector { get; set; }

        /// <summary>
        /// Replacement list.
        /// </summary>
        [XmlAttribute(AttributeName = "List")]
        public string ReplacementList { get; set; }
    }

    /// <summary>
    /// Replacement query, in case of <c>MaskType.Query</c>.
    /// Sample usage:
    /// Query = "SELECT city, country FROM cities"
    ///
    /// This might return the following data:
    /// | city       | country |
    /// | Basel      | CH      |
    /// | Geneva     | CH      |
    /// | Bern       | CH      |
    /// | New York   | US      |
    /// | Helena     | US      |
    /// | London     | UK      |
    /// | Manchester | UK      |
    /// 
    /// GroupDbField = "country"
    /// ValueDbField = "city"
    /// SelectorField = "country"
    /// </summary>
    [Serializable]
    public class QueryReplacement
    {
        /// <summary>
        /// The field from the input data that will be used to select which 
        /// list to take a random item from.
        /// E.g. "country"
        /// </summary>
        [XmlAttribute(AttributeName = "SelectorField")]
        public string SelectorField { get; set; }

        /// <summary>
        /// The field from the db data (retrieved form the query) that will be used
        /// to create individual lists.
        /// E.g. "country"
        /// </summary>
        [XmlAttribute(AttributeName = "GroupField")]
        public string GroupDbField { get; set; }

        /// <summary>
        /// The field from the db data (retrieved form the query) that will be used
        /// to get the values.
        /// E.g. "city"
        /// </summary>
        [XmlAttribute(AttributeName = "ValueField")]
        public string ValueDbField { get; set; }        

        /// <summary>
        /// E.g. "SELECT city, country FROM cities"
        /// </summary>
        [XmlAttribute(AttributeName = "Query")]
        public string Query { get; set; }        

        internal List<Replacement> ListOfPossibleReplacements { get; set; }
    }    

    [Serializable]
    public class FieldMask
    {
        public FieldMask()
        {
            this.MaskType = MaskType.None;
            this.Asterisk = "*";
            this.ListOfPossibleReplacements = new List<Replacement>();
        }

        /// <summary>
        /// The name of the field.
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

        public QueryReplacement QueryReplacement { get; set; }

        /// <summary>
        /// In case of <c>MaskType.List</c>, the comma-separated list of items to choose from.
        /// </summary>
        public List<Replacement> ListOfPossibleReplacements { get; set; }
    }
}