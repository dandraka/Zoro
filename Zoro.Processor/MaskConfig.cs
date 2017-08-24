using System;
using System.Collections.Generic;

namespace Zoro.Processor
{
    [Serializable]
    public class MaskConfig
    {
        /// <summary>
        /// Save the configuration to a file.
        /// </summary>
        /// <param name="configfile"></param>
        /// <param name="config"></param>
        public static void SaveConfig(string configfile, MaskConfig config)
        {
            var ser = new System.Xml.Serialization.XmlSerializer(typeof(MaskConfig));
            var file = System.IO.File.Create(configfile);
            ser.Serialize(file, config);
            file.Close();
        }

        /// <summary>
        /// Read the configuration from a file.
        /// </summary>
        /// <param name="configfile"></param>
        /// <param name="config"></param>
        public static MaskConfig ReadConfig(string configfile)
        {
            var ser = new System.Xml.Serialization.XmlSerializer(typeof(MaskConfig));
            var file = System.IO.File.OpenRead(configfile);
            var config = (MaskConfig)ser.Deserialize(file);
            file.Close();

            return config;
        }

        public MaskConfig()
        {
            this.Delimiter = ";";
        }

        /// <summary>
        /// The input file.
        /// </summary>
        public string InputFile { get; set; }

        /// <summary>
        /// The ourput file.
        /// </summary>
        public string OutputFile { get; set; }

        /// <summary>
        /// The CSV file delimiter. By default, a semicolon.
        /// </summary>
        public string Delimiter { get; set; }

        /// <summary>
        /// The type of masking for every field. If a field is not found, <c>MaskType.None</c> is implied.
        /// </summary>
        public List<FieldMask> FieldMasks;
    }
}
