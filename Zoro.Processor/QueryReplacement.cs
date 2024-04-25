using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Dandraka.Zoro.Processor
{
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
}