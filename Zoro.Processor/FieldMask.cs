using System;

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
    }
}
