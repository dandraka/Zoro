using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Takes the explicit replacements of <c>MaskType.ReplacementList</c> from a csv file instead of
    /// <c>Map</c> items, so that a long list can be kept (and changed) outside of the configuration.
    /// It must be the only item of <c>FieldMask.ListOfPossibleReplacements</c>.
    /// The file is read when the masking starts. The csv file name is relative to the current directory,
    /// like <c>MaskConfig.InputFile</c>.
    /// Sample usage:
    /// &lt;ReplacementCSV CSVFileName="names.csv" FromFieldName="original" ToFieldName="pseudonym" /&gt;
    /// </summary>
    [Serializable]
    public class ReplacementCSV : Replacement
    {
        /// <summary>
        /// The csv file. Mandatory, and the file must exist.
        /// </summary>
        [XmlAttribute(AttributeName = "CSVFileName")]
        public string CSVFileName { get; set; }

        /// <summary>
        /// The column of the csv file (as named in its first row) that holds the original value. Mandatory.
        /// </summary>
        [XmlAttribute(AttributeName = "FromFieldName")]
        public string FromFieldName { get; set; }

        /// <summary>
        /// The column of the csv file (as named in its first row) that holds the replacement. Mandatory.
        /// </summary>
        [XmlAttribute(AttributeName = "ToFieldName")]
        public string ToFieldName { get; set; }

        /// <summary>
        /// The delimiter of the csv file. The default is a comma.
        /// </summary>
        [XmlAttribute(AttributeName = "Delimiter")]
        [DefaultValue(",")]
        public string Delimiter { get; set; } = ",";
    }
}
