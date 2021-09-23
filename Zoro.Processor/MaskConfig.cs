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
            this.DataSource = DataSource.CsvFile;
            this.ConnectionString = string.Empty;
            this.SqlSelect = string.Empty;
            this.Delimiter = ";";
        }

        /// <summary>
        /// Determines how the data is obtained. 
        /// If the source is a file, the <c>InputFile</c> field needs to be filled.
        /// If the source is a DB query, the <c>ConnectionString</c> and <c>SqlSelect</c> fields need to be filled.
        /// </summary>
        public DataSource DataSource { get; set; }

        private string _inputFile;
        /// <summary>
        /// The input file.
        /// </summary>
        public string InputFile 
        {
            get => _inputFile;
            set 
            { 
                _inputFile = value
                    .Replace('\\', System.IO.Path.DirectorySeparatorChar)
                    .Replace('/', System.IO.Path.DirectorySeparatorChar);
            }
        }

        private string _outputFile;
        /// <summary>
        /// The output file.
        /// </summary>
        public string OutputFile 
        {
            get => _outputFile;
            set 
            { 
                _outputFile = value
                    .Replace('\\', System.IO.Path.DirectorySeparatorChar)
                    .Replace('/', System.IO.Path.DirectorySeparatorChar);
            }
        }

        /// <summary>
        /// The DB connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The DB query statement.
        /// </summary>
        public string SqlSelect { get; set; }

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