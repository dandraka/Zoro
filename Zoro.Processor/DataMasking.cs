﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GenericParsing;
using Newtonsoft.Json;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Main masking class, this is where the job is done.
    /// </summary>
    public class DataMasking
    {
        private readonly MaskConfig config;

        private readonly Random rnd = new Random(DateTime.Now.Millisecond);

        private char DbParamChar => this.config.GetConnection().GetType().ToString() == "System.Data.SqlClient.SqlConnection" ? '@' : '$';

        /// <summary>
        /// Creates an instance of DataMasking class.
        /// </summary>
        /// <param name="config"></param>
        public DataMasking(MaskConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Performs masking.
        /// </summary>
        public void Mask()
        {
            var dt = GetData();

            AnonymizeTable(dt);

            SaveData(dt);
        }

        private DataTable GetData()
        {
            switch (config.DataSource)
            {
                case DataSource.CsvFile:
                    return ReadDataFromCSVFile();
                case DataSource.JsonFile:
                    return ReadDataFromJSONFile();
                case DataSource.Database:
                    return ReadDataFromDb();
                default:
                    throw new NotSupportedException();
            }
        }

        private void SaveData(DataTable dt)
        {
            switch (config.DataDestination)
            {
                case DataDestination.CsvFile:
                    SaveDataToCSVFile(dt);
                    break;
                case DataDestination.JsonFile:
                    SaveDataToJSONFile(dt);
                    break;
                case DataDestination.Database:
                    SaveDataToDb(dt);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private DataTable ReadDataFromDb()
        {
            if (config.GetConnection() == null && (string.IsNullOrEmpty(config.ConnectionString) || string.IsNullOrEmpty(config.ConnectionType)))
            {
                throw new InvalidDataException("If no DbConnection is provided, ConnectionString and ConnectionType cannot be empty when using the database option");
            }
            if (string.IsNullOrEmpty(config.SqlSelect))
            {
                throw new InvalidDataException("SQL Select statement must be filled when using the database option");
            }

            DataTable dt = new DataTable("data");

            Action doDbSelect = new Action(() =>
            {
                bool wasOpen = EnsureOpenDbConnection();

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
            });

            doDbSelect();

            return dt;
        }

        private void AnonymizeTable(DataTable dt)
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

        private string GetMaskedString(string data, FieldMask fieldMask, DataRow row)
        {
            if (string.IsNullOrEmpty(data))
                return data;
            if (!string.IsNullOrEmpty(fieldMask.RegExMatch))
            {
                Regex rgx = new Regex(fieldMask.RegExMatch);

                var match = rgx.Match(data);
                string matchData = match.Groups[fieldMask.RegExGroupToReplace].Value;
                string replaceStr = string.Empty;
                for (int i = 1; i < match.Groups.Count; i++)
                {
                    string s;
                    if (i != fieldMask.RegExGroupToReplace)
                    {
                        s = "${" + i + "}";
                    }
                    else
                    {
                        switch (fieldMask.MaskType)
                        {
                            case MaskType.Asterisk:
                                s = GetAsteriskString(matchData, fieldMask.Asterisk[0]);
                                break;
                            case MaskType.Similar:
                                s = GetSimilarString(matchData);
                                break;
                            case MaskType.List:
                                s = GetStringFromList(row, fieldMask.ListOfPossibleReplacements);
                                break;
                            case MaskType.Query:
                                s = GetStringFromQuery(row, fieldMask.QueryReplacement);
                                break;
                            case MaskType.None:
                                s = matchData;
                                break;
                            default:
                                throw new NotImplementedException($"Mask type {fieldMask.MaskType} is not yet implemented");
                        }
                    }
                    replaceStr += s;
                }
                string repl = rgx.Replace(data, replaceStr);
                return repl;
            }
            else
            {
                switch (fieldMask.MaskType)
                {
                    case MaskType.Asterisk:
                        return GetAsteriskString(data, fieldMask.Asterisk[0]);
                    case MaskType.Similar:
                        return GetSimilarString(data);
                    case MaskType.List:
                        return GetStringFromList(row, fieldMask.ListOfPossibleReplacements);
                    case MaskType.Query:
                        return GetStringFromQuery(row, fieldMask.QueryReplacement);
                    case MaskType.None:
                        return data;
                    default:
                        throw new NotImplementedException($"Mask type {fieldMask.MaskType} is not yet implemented");
                }
            }
        }

        private string GetSimilarString(string data)
        {
            Func<char, char> method = (c) =>
            {
                if (Char.IsDigit(c))
                {
                    return rnd.Next(0, 9).ToString()[0];
                }
                if (c.IsVowel())
                {
                    Char a = CharExtension.GetRandomVowel();
                    if (Char.IsUpper(c))
                    {
                        a = Char.ToUpper(a);
                    }
                    return a;
                }
                else
                {
                    Char a = CharExtension.GetRandomConsonant();
                    if (Char.IsUpper(c))
                    {
                        a = Char.ToUpper(a);
                    }
                    return a;
                }
            };
            return getReplacedString(data, method);
        }

        private string GetAsteriskString(string data, char asterisk)
        {
            Func<char, char> method = (c) => { return asterisk; };
            return getReplacedString(data, method);
        }

        private string GetStringFromList(DataRow row, List<Replacement> listOfReplacements)
        {
            // get the right list
            string replacementStr = null;
            foreach (var r in listOfReplacements)
            {
                // to have as a default
                if (string.IsNullOrEmpty(r.Selector))
                {
                    replacementStr = r.ReplacementList;
                    break;
                }

                string selectorField = r.Selector.Split('=')[0].Trim();
                string selectorValue = r.Selector.Split('=')[1].Trim().ToLower();

                if (!row.Table.Columns.Contains(selectorField))
                {
                    throw new DataException($"Field {selectorField} was not found in data.");
                }

                string dataValue = Convert.ToString(row[selectorField]).Trim().ToLower();
                if (dataValue == selectorValue)
                {
                    replacementStr = r.ReplacementList;
                    break;
                }
            }
            if (replacementStr == null)
            {
                // warning, nothing found
                throw new DataException($"No match could be located for data row\r\n{string.Join(",", row.ItemArray)}");
            }
            var list = replacementStr.Split(',').Select(x => x.Trim()).ToList();
            string str = list[rnd.Next(0, list.Count - 1)];
            return str;
        }

        private string GetStringFromQuery(DataRow row, QueryReplacement queryReplacement)
        {
            if (queryReplacement.ListOfPossibleReplacements == null)
            {
                bool wasOpen = EnsureOpenDbConnection();
                queryReplacement.ListOfPossibleReplacements = new List<Replacement>();

                var dbCmd = config.GetConnection().CreateCommand();
                dbCmd.CommandType = CommandType.Text;
                dbCmd.CommandText = queryReplacement.Query;

                var dt = new DataTable(queryReplacement.ValueDbField);
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

                // generate lists
                var groupList = dt.Rows.OfType<DataRow>()
                    .Select(r => Convert.ToString(r[queryReplacement.GroupDbField]))
                    .Distinct()
                    .ToList();
                foreach (string group in groupList)
                {
                    var valueList = dt.Rows.OfType<DataRow>()
                        .Where(r => Convert.ToString(r[queryReplacement.GroupDbField]) == group)
                        .Select(r => Convert.ToString(r[queryReplacement.ValueDbField]))
                        .ToList();
                    queryReplacement.ListOfPossibleReplacements.Add(new Replacement()
                    {
                        Selector = $"{queryReplacement.SelectorField}={group}",
                        ReplacementList = string.Join(',', valueList)
                    });
                }
            }

            return GetStringFromList(row, queryReplacement.ListOfPossibleReplacements);
        }

        private string getReplacedString(string data, Func<char, char> method)
        {
            string anon = string.Empty;

            for (int i = 0; i < data.Length; i++)
            {
                Char c = data[i];
                if (!Char.IsLetterOrDigit(c))
                {
                    anon += c;
                    continue;
                }

                anon += method(c);
            }

            return anon;
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

        private DataTable ReadDataFromJSONFile()
        {
            if (!File.Exists(config.InputFile))
            {
                throw new FileNotFoundException(config.InputFile);
            }

            var tbl = new DataTable();

            string json = File.ReadAllText(config.InputFile);

            var jsonData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json);

            switch (jsonData.ValueKind)
            {
                case JsonValueKind.Object:
                    // generate 1 row
                    AddJSONData(jsonData, ref tbl);
                    break;
                case JsonValueKind.Array:
                    // generate multiple rows
                    var itemsList = jsonData.EnumerateArray().ToList();
                    foreach (var item in itemsList)
                    {
                        AddJSONData(item, ref tbl);
                    }
                    break;
                default:
                    throw new NotSupportedException($"JSON type {Enum.GetName(typeof(JsonValueKind), jsonData.ValueKind)} is not supported at this level.");
            }

            return tbl;
        }

        private void AddJSONData(JsonElement jsonNode, ref DataTable tbl)
        {
            if (jsonNode.ValueKind != JsonValueKind.Object)
            {
                throw new NotSupportedException();
            }

            if (tbl.Columns.Count == 0)
            {
                // do setup
                List<JsonProperty> childrenListForColumns = null;
                var topNodeForColumns = jsonNode.EnumerateObject().ToList()[0].Value;
                if (topNodeForColumns.ValueKind == JsonValueKind.Object)
                {
                    childrenListForColumns = jsonNode.EnumerateObject().ToList()[0].Value.EnumerateObject().ToList();
                }
                else
                {
                    childrenListForColumns = jsonNode.EnumerateObject().ToList();
                }
                foreach (var child in childrenListForColumns)
                {
                    switch (child.Value.ValueKind)
                    {
                        /*
                        case JsonValueKind.String:
                        case JsonValueKind.Null:
                        case JsonValueKind.Number:
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            tbl.Columns.Add(child.Name, typeof(string));
                            break;
                        default:
                            // skip
                            break;
                        */
                        case JsonValueKind.String:
                        case JsonValueKind.Null:
                            tbl.Columns.Add(child.Name, typeof(string));
                            break;
                        case JsonValueKind.Number:
                            // int or decimal?                            
                            try
                            {
                                decimal nd = child.Value.GetDecimal();
                                int ni = child.Value.GetInt32();
                                if (nd - ni == 0)
                                {
                                    tbl.Columns.Add(child.Name, typeof(int));
                                }
                                else
                                {
                                    tbl.Columns.Add(child.Name, typeof(decimal));
                                }
                            }
                            catch (Exception)
                            {
                                tbl.Columns.Add(child.Name, typeof(decimal));
                            }                                                        
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            tbl.Columns.Add(child.Name, typeof(bool));
                            break;
                        default:
                            // skip
                            break;
                    }                    
                }
            }

            // add row
            var row = tbl.NewRow();
            List<JsonProperty> childrenList = null;
            var topNode = jsonNode.EnumerateObject().ToList()[0].Value;
            if (topNode.ValueKind == JsonValueKind.Object)
            {
                childrenList = jsonNode.EnumerateObject().ToList()[0].Value.EnumerateObject().ToList();
            }
            else
            {
                childrenList = jsonNode.EnumerateObject().ToList();
            }
            foreach (var child in childrenList)
            {
                switch (child.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        row[child.Name] = child.Value.GetString();
                        break;
                    case JsonValueKind.Null:
                        row[child.Name] = string.Empty;
                        break;
                    case JsonValueKind.Number:
                        row[child.Name] = child.Value.GetDecimal().ToString();
                        break;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        row[child.Name] = child.Value.GetBoolean().ToString();
                        break;
                    default:
                        // skip
                        break;
                }
            }
            tbl.Rows.Add(row);
        }

        private void SaveDataToCSVFile(DataTable dt)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                sb.AppendFormat("{0}{1}",
                    dt.Columns[i].ColumnName,
                    i < dt.Columns.Count - 1 ? config.Delimiter : string.Empty);
            }
            sb.Append("\r\n");

            for (int r = 0; r < dt.Rows.Count; r++)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    string content = Convert.ToString(dt.Rows[r][dt.Columns[i].ColumnName]);
                    if (content.Contains(config.Delimiter))
                    {
                        content = string.Format("\"{0}\"", content);
                    }

                    sb.AppendFormat("{0}{1}",
                            content,
                            i < dt.Columns.Count - 1 ? config.Delimiter : string.Empty);
                }
                sb.Append("\r\n");
            }

            File.WriteAllText(config.OutputFile, sb.ToString(), Encoding.UTF8);

        }

        private void SaveDataToJSONFile(DataTable dt)
        {
            File.WriteAllText(config.OutputFile, JsonConvert.SerializeObject(dt, Formatting.Indented), Encoding.UTF8);
        }

        private void SaveDataToDb(DataTable dt)
        {
            if (config.GetConnection() == null && (string.IsNullOrEmpty(config.ConnectionString) || string.IsNullOrEmpty(config.ConnectionType)))
            {
                throw new InvalidDataException("If no DbConnection is provided, ConnectionString and ConnectionType cannot be empty when using the database option");
            }
            if (string.IsNullOrWhiteSpace(config.SqlCommand))
            {
                throw new ArgumentException($"Sql Command statement must be filled when using the database option");
            }
            int numParams = config.SqlCommand.Count(c => c == this.DbParamChar);
            if (numParams != config.FieldMasks.Count)
            {
                throw new ArgumentException($"Sql Command parameter mismatch: '{config.SqlCommand}' does not contain the same number of parameters $field ({numParams}) as the number of FieldMasks ({config.FieldMasks.Count})");
            }

            bool wasOpen = EnsureOpenDbConnection();

            var cmd = config.GetConnection().CreateCommand();
            cmd.CommandText = config.SqlCommand;
            foreach (var m in config.FieldMasks)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = m.FieldName;
                cmd.Parameters.Add(p);
            }

            foreach (DataRow r in dt.Rows)
            {
                foreach (var m in config.FieldMasks)
                {
                    cmd.Parameters[m.FieldName].Value = r[m.FieldName];
                }
                cmd.ExecuteNonQuery();
            }

            if (!wasOpen)
            {
                config.GetConnection().Close();
            }
        }

        /// <summary>Creates and opens a db connection, if needed.</summary>
        /// <returns>True if the connection was already open, false otherwise.</returns>
        private bool EnsureOpenDbConnection()
        {
            bool wasOpen = false;
            if (config.GetConnection() == null)
            {
                DbProviderFactory factory = DbProviderFactories.GetFactory(config.ConnectionType);
                config.SetConnection(factory.CreateConnection());
                config.GetConnection().ConnectionString = config.ConnectionString;
                config.GetConnection().Open();
            }
            else
            {
                if (config.GetConnection().State != ConnectionState.Open)
                {
                    config.GetConnection().Open();
                }
                else
                {
                    wasOpen = true;
                }
            }
            return wasOpen;
        }
    }
}