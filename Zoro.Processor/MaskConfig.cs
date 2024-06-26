﻿using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Class that keeps all configuration info for a masking job, 
    /// including the collection of <c>FieldMask</c> objects.
    /// </summary>
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
        public static MaskConfig ReadConfig(string configfile)
        {
            var ser = new System.Xml.Serialization.XmlSerializer(typeof(MaskConfig));
            var file = System.IO.File.OpenRead(configfile);
            var config = (MaskConfig)ser.Deserialize(file);
            file.Close();

            return config;
        }

        /// <summary>
        /// Creates an instance of MaskConfig class.
        /// </summary>
        public MaskConfig()
        {
            this.DataSource = DataSource.CsvFile;
            this.DataDestination = DataDestination.CsvFile;
            this.ConnectionString = string.Empty;
            this.SqlSelect = string.Empty;
            this.Delimiter = ";";
            this.FieldMasks = new List<FieldMask>();
        }

        /// <summary>
        /// Determines how the data is obtained. 
        /// If the source is a file, the <c>InputFile</c> field needs to be filled.
        /// If the source is a DB query, the <c>ConnectionString</c> and <c>SqlSelect</c> fields need to be filled.
        /// </summary>
        public DataSource DataSource;

        /// <summary>
        /// Determines where the masked data will be written. 
        /// If the destination is a file, the <c>OutputFile</c> field needs to be filled.
        /// If the destination is a database, the <c>ConnectionString</c> and <c>SqlCommand</c> fields need to be filled.
        /// </summary>
        public DataDestination DataDestination;

        private string _inputFile;
        /// <summary>
        /// The input file, used when <c>DataSource</c> is File.
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
        /// The output file, used when <c>DataDestination</c> is File.
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
        /// The DB connection string, used when either <c>DataSource</c> is Database
        /// or when <c>DataDestination</c> is Database.
        /// </summary>
        public string ConnectionString;

        /// <summary>
        /// The type of DB connection to instantiate, used when either <c>DataSource</c> is Database
        /// or when <c>DataDestination</c> is Database.
        /// This needs to be available to the DbProviderFactories class, which reads from
        /// the System.Data section of Machine.Config.
        /// Examples:
        /// System.Data.Odbc, System.Data.OleDb, System.Data.SQLite, 
        /// System.Data.OracleClient, System.Data.SqlClient.
        /// See also https://downloads.teradata.com/blog/netfx/2010/12/dbproviderfactories-demystified
        /// for more info.
        /// </summary>
        // TODO test for different db types
        public string ConnectionType;

        private DbConnection _connection;

        /// <summary>
        /// The DB connection, which can be provided directly instead of a connection string and type.
        /// Used when either <c>DataSource</c> is Database
        /// or when <c>DataDestination</c> is Database.
        /// </summary>
        public DbConnection GetConnection() => _connection;

        /// <summary>
        /// The DB connection, which can be provided directly instead of a connection string.
        /// Used when either <c>DataSource</c> is Database
        /// or when <c>DataDestination</c> is Database.
        /// </summary>
        public void SetConnection(DbConnection connection) => _connection = connection;

        /// <summary>
        /// The DB query statement, used when <c>DataSource</c> is Database.
        /// </summary>
        public string SqlSelect;

        /// <summary>
        /// The DB statement executed to write the data to the DB, used when <c>DataSource</c> is Database.
        /// It can be any sql for example insert, update or merge.
        /// </summary>
        public string SqlCommand;

        /// <summary>
        /// The CSV file delimiter. By default, a semicolon.
        /// </summary>
        public string Delimiter;

        /// <summary>
        /// The type of masking for every field. If a field is not found, <c>MaskType.None</c> is implied.
        /// </summary>
        public List<FieldMask> FieldMasks;
    }
}