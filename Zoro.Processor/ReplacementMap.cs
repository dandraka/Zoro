using System;
using System.Xml.Serialization;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// An explicit replacement of an original value, in case of <c>MaskType.ReplacementList</c>.
    /// Matching of the original value ignores case and surrounding spaces.
    /// </summary>
    [Serializable]
    public class ReplacementMap
    {
        /// <summary>
        /// The original value, e.g. "Athens"
        /// </summary>
        [XmlAttribute(AttributeName = "From")]
        public string From { get; set; }

        /// <summary>
        /// The replacement, e.g. "CityA"
        /// </summary>
        [XmlAttribute(AttributeName = "To")]
        public string To { get; set; }
    }
}
