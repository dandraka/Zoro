using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Possible replacement, in case of <c>MaskType.List</c> or <c>MaskType.ReplacementList</c>.
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

        /// <summary>
        /// Explicit original value to replacement pairs, in case of <c>MaskType.ReplacementList</c>.
        /// They take precedence over <c>ReplacementList</c>, and their replacements are not picked again from it.
        /// </summary>
        [XmlElement(ElementName = "Map")]
        public List<ReplacementMap> Mappings { get; set; } = new List<ReplacementMap>();

        // Used instead of ReplacementList when the items come from a query, so values containing commas survive.
        internal List<string> Items { get; set; }
    }
}