using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Zoro.Processor
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
        public string FieldValue { get; set; }

        /// <summary>
        /// Replacement list.
        /// </summary>
        [XmlAttribute(AttributeName = "List")]
        public string ReplacementList { get; set; }
    }

    [Serializable]
    public class FieldMask
    {
        public FieldMask()
        {
            this.MaskType = MaskType.None;
            this.Asterisk = "*";
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

        /// <summary>
        /// In case of <c>MaskType.List</c>, the comma-separated list of items to choose from.
        /// </summary>
        public List<Replacement> ListOfPossibleReplacements { get; set; }
    }
}