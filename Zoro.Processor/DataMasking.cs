using GenericParsing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Main masking class, this is where the job is done.
    /// </summary>
    public class DataMasking
    {
        private readonly MaskConfig config;

        private readonly Random rnd = new Random(DateTime.Now.Millisecond);

        private readonly Regex fieldsRegEx = new Regex("{{.*?}}");

        private char DbParamChar => this.config.GetConnection().GetType().ToString() == "System.Data.SqlClient.SqlConnection" ? '@' : '$';

        /// <summary>
        /// Creates an instance of DataMasking class.
        /// </summary>
        /// <param name="config"></param>
        public DataMasking(MaskConfig config)
        {            
            this.config = config;            
        }

        private static DbProviderFactory GetDbFactory(string connType)
        {
            switch(connType)
            {
                case "System.Data.SqlClient":
                    DbProviderFactories.RegisterFactory(connType, typeof(System.Data.SqlClient.SqlClientFactory));
                    break;
                case "System.Data.OleDb":
                    DbProviderFactories.RegisterFactory(connType, typeof(System.Data.OleDb.OleDbFactory));
                    break; 
                /*                    
                case "System.Data.Odbc":
                    DbProviderFactories.RegisterFactory(connType, typeof(System.Data.Odbc.OdbcFactory));
                    break;
                */                                      
                default:
                    throw new NotSupportedException($"Connection type {connType} is not yet supported");
            }            
            return DbProviderFactories.GetFactory(connType);
        }        

        /// <summary>
        /// Performs masking.
        /// </summary>
        public void Mask()
        {
            object dt = GetData();

            switch (config.DataSource)
            {
                // flat types
                case DataSource.CsvFile:
                case DataSource.Database:
                    AnonymizeFlatData((DataTable)dt);
                    break;
                // nested types
                case DataSource.JsonFile:
                    AnonymizeJSONData((JContainer)dt);
                    break;
                default:
                    throw new NotImplementedException($"{Enum.GetName(typeof(DataSource), config.DataSource)} is not implemented");
            }

            SaveData(dt);
        }

        private void AnonymizeJSONData(JContainer jsonContainer)
        {
            switch (jsonContainer.GetType().Name)
            {
                case "JArray":
                    foreach (var jsonItem in (JArray)jsonContainer)
                    {
                        foreach (JProperty c in (JToken)jsonItem)
                        {
                            AnonymizeJSONProperty(c);
                        }
                    }
                    break;
                case "JObject":
                    foreach (JProperty c in (JToken)jsonContainer)
                    {
                        AnonymizeJSONProperty(c);
                    }
                    break;
                default:
                    throw new NotImplementedException(jsonContainer.GetType().FullName);
            }
        }

        private void AnonymizeJSONProperty(JProperty c)
        {
            string colName = c.Name;
            string value = Convert.ToString(c.Value);
            var fieldMask = config.FieldMasks.FirstOrDefault(x => x.FieldName == colName);
            if (fieldMask != null)
            {
                switch (c.Value.Type)
                {
                    case JTokenType.Integer:
                        string i = GetMaskedString(value, fieldMask, null, c);
                        c.Value = Convert.ToInt32(i);
                        break;
                    case JTokenType.Float:
                        string f = GetMaskedString(value, fieldMask, null, c);
                        c.Value = Convert.ToDecimal(f);
                        break;
                    default:
                        // treat non-numbers as strings
                        string s = GetMaskedString(value, fieldMask, null, c);
                        c.Value = s;
                        break;
                }
            }

            if (c.HasValues)
            {
                foreach (var jsonChild in c.Children().OfType<JContainer>())
                {
                    AnonymizeJSONData((JContainer)jsonChild);
                }
            }
        }

        private object GetData()
        {
            return config.DataSource switch
            {
                DataSource.CsvFile => ReadDataFromCSVFile(),
                DataSource.JsonFile => ReadDataFromJSONFile(),
                DataSource.Database => ReadDataFromDb(),
                _ => throw new NotSupportedException(),
            };
        }

        private void SaveData(object dt)
        {
            switch (config.DataDestination)
            {
                case DataDestination.CsvFile:
                    SaveDataToCSVFile((DataTable)dt);
                    break;
                case DataDestination.JsonFile:
                    SaveDataToJSONFile((JContainer)dt);
                    break;
                case DataDestination.Database:
                    SaveDataToDb((DataTable)dt);
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

        private void AnonymizeFlatData(DataTable dt)
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

        private string GetMaskedString(string data, FieldMask fieldMask, DataRow row, JProperty jsonNode = null)
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
                        s = fieldMask.MaskType switch
                        {
                            MaskType.Asterisk => GetAsteriskString(matchData, fieldMask.Asterisk[0]),
                            MaskType.Similar => GetSimilarString(matchData),
                            MaskType.List => GetStringFromList(row, fieldMask.ListOfPossibleReplacements),
                            MaskType.Query => GetStringFromQuery(row, fieldMask.QueryReplacement),
                            MaskType.Expression => GetExpressionString(row, fieldMask.Expression, jsonNode),
                            MaskType.None => matchData,
                            _ => throw new NotImplementedException($"Mask type {fieldMask.MaskType} is not yet implemented"),
                        };
                    }
                    replaceStr += s;
                }
                string repl = rgx.Replace(data, replaceStr);
                return repl;
            }
            else
            {
                return fieldMask.MaskType switch
                {
                    MaskType.Asterisk => GetAsteriskString(data, fieldMask.Asterisk[0]),
                    MaskType.Similar => GetSimilarString(data),
                    MaskType.List => GetStringFromList(row, fieldMask.ListOfPossibleReplacements),
                    MaskType.Query => GetStringFromQuery(row, fieldMask.QueryReplacement),
                    MaskType.Expression => GetExpressionString(row, fieldMask.Expression, jsonNode),
                    MaskType.None => data,
                    _ => throw new NotImplementedException($"Mask type {fieldMask.MaskType} is not yet implemented"),
                };
            }
        }

        private string GetExpressionString(DataRow row, string expression, JProperty jsonNode)
        {
            var r = fieldsRegEx.Match(expression);

            foreach (Group rxGroup in r.Groups)
            {
                string fieldName = rxGroup.Value.Replace("{{", "").Replace("}}", "").ToLower();

                switch (this.config.DataSource)
                {
                    case DataSource.CsvFile:
                    case DataSource.Database:
                        if (!row.Table.Columns.Contains(fieldName))
                        {
                            throw new FieldNotFoundException(fieldName);
                        }
                        string fieldValue = Convert.ToString(row[fieldName]);
                        expression = expression.Replace(rxGroup.Value, fieldValue);
                        break;
                    case DataSource.JsonFile:
                        // field name is supposed to be a JsonPath
                        var jsonPathResult = jsonNode.Parent.SelectToken(fieldName); //jsonNode.Root.SelectToken(fieldName);
                        if (jsonPathResult == null || jsonPathResult.Value<object>() == null)
                        {
                            jsonPathResult = jsonNode.Root.SelectToken(fieldName);
                            if (jsonPathResult == null || jsonPathResult.Value<object>() == null)
                            {
                                throw new FieldNotFoundException(fieldName);
                            }
                        }
                        fieldValue = jsonPathResult.Value<string>();
                        expression = expression.Replace(rxGroup.Value, fieldValue);
                        break;
                    default:
                        throw new NotSupportedException($"Data source {this.config.DataSource} is not supported.");
                }
            }

            return expression;
        }

        private string GetSimilarString(string data)
        {
            char method(char c)
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
            }
            return GetReplacedString(data, method);
        }

        private string GetAsteriskString(string data, char asterisk)
        {
            char method(char c) { return asterisk; }
            return GetReplacedString(data, method);
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
                throw new DataNotFoundException(row);
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

        private string GetReplacedString(string data, Func<char, char> method)
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

        private JContainer ReadDataFromJSONFile()
        {
            if (!File.Exists(config.InputFile))
            {
                throw new FileNotFoundException(config.InputFile);
            }

            string json = File.ReadAllText(config.InputFile);

            JContainer jsonObj;
            try
            {
                jsonObj = JObject.Parse(json);
            }
            catch (JsonReaderException e)
            {
                e.ToString();
                jsonObj = JArray.Parse(json);
            }

            return jsonObj;
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

        private void SaveDataToJSONFile(JContainer dt)
        {
            File.WriteAllText(config.OutputFile, dt.ToString(), Encoding.UTF8);
            config.OutputFile.ToString();
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
                throw new ArgumentException($"Sql Command parameter mismatch: '{config.SqlCommand}' does not contain the same number of parameters {this.DbParamChar}field ({numParams}) as the number of FieldMasks ({config.FieldMasks.Count})");
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
                var dbFactory = GetDbFactory(config.ConnectionType);
                config.SetConnection(dbFactory.CreateConnection());
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