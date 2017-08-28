using System;
using System.Xml.Schema;

namespace Zoro.Processor
{
    [Serializable]
    public class FieldMask
    {
        public FieldMask()
        {
            this.MaskType = MaskType.None;
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
    }
}
