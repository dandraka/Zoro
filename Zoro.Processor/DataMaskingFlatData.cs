using System;
using System.Data;
using System.IO;
using System.Text;
using GenericParsing;
using Newtonsoft.Json.Linq;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Masking for flat (tabular) data: CSV files and database queries.
    /// </summary>
    internal class DataMaskingFlatData : DataMasking<DataTable>
    {
        internal DataMaskingFlatData(MaskConfig config) : base(config)
        {
        }

        protected override DataTable GetData()
        {
            return config.DataSource switch
            {
                DataSource.CsvFile => ReadDataFromCSVFile(),
                DataSource.Database => ReadDataFromDb(),
                _ => throw new NotSupportedException(),
            };
        }

        protected override void Anonymize(DataTable dt)
        {
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                for (int c = 0; c < config.FieldMasks.Count; c++)
                {
                    string colName = config.FieldMasks[c].FieldName;
                    if (r == 0)
                    {
                        if (!dt.Columns.Contains(colName))
                        {
                            throw new InvalidDataException(string.Format("File '{0}' does not contain field '{1}'",
                                config.InputFile, colName));
                        }
                    }

                    dt.Rows[r][colName] = GetMaskedString(Convert.ToString(dt.Rows[r][colName]), config.FieldMasks[c], dt.Rows[r]);
                }
            }
        }

        private protected override string GetFieldValue(string fieldName, DataRow row, JProperty jsonNode)
        {
            if (!row.Table.Columns.Contains(fieldName))
            {
                throw new FieldNotFoundException(fieldName);
            }
            return Convert.ToString(row[fieldName]);
        }

        private DataTable ReadDataFromDb()
        {
            DbConnectionHelper.ValidateConnectionSettings(config);
            if (string.IsNullOrEmpty(config.SqlSelect))
            {
                throw new InvalidDataException("SQL Select statement must be filled when using the database option");
            }

            DataTable dt = new DataTable("data");

            bool wasOpen = DbConnectionHelper.EnsureOpen(config);

            var dbCmd = config.GetConnection().CreateCommand();
            dbCmd.CommandType = CommandType.Text;
            dbCmd.CommandText = config.SqlSelect;

            var behaviour = wasOpen ? CommandBehavior.Default : CommandBehavior.CloseConnection;
            using (var dbReader = dbCmd.ExecuteReader(behaviour))
            {
                dt.Load(dbReader);
                dbReader.Close();
            }

            foreach (DataColumn dataColumn in dt.Columns)
            {
                dataColumn.ReadOnly = false;
            }

            return dt;
        }

        private DataTable ReadDataFromCSVFile()
        {
            if (!File.Exists(config.InputFile))
            {
                throw new FileNotFoundException(config.InputFile);
            }

            var tbl = new DataTable();

            using (var parser = new GenericParser())
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.ColumnDelimiter = config.Delimiter[0];
                parser.FirstRowHasHeader = true;
                parser.FirstRowSetsExpectedColumnCount = true;
                parser.TrimResults = false;
                //parser.SkipStartingDataRows = 10;
                parser.MaxBufferSize = 1048576;
                //parser.MaxRows = 500;
                parser.TextQualifier = '\"';
                parser.EscapeCharacter = null;

                parser.SetDataSource(config.InputFile, Encoding.UTF8);

                bool doSetup = true;

                while (parser.Read())
                {

                    if (doSetup)
                    {
                        for (int i = 0; i < parser.ColumnCount; i++)
                        {
                            string colName = parser.GetColumnName(i);
                            tbl.Columns.Add(colName, typeof(string));
                        }
                        doSetup = false;
                    }

                    var row = tbl.NewRow();
                    for (int i = 0; i < parser.ColumnCount; i++)
                    {
                        string colName = parser.GetColumnName(i);
                        row[colName] = parser[colName];
                    }
                    tbl.Rows.Add(row);
                }
            }

            return tbl;
        }
    }
}
