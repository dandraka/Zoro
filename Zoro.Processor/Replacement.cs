using System;
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
}