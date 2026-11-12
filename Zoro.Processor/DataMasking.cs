using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Main masking class, this is where the job is done.
    /// Use <see cref="Create(MaskConfig)"/> to get the implementation that matches the data source.
    /// </summary>
    public abstract class DataMasking
    {
        private protected readonly MaskConfig config;

        private readonly Random rnd = new Random(DateTime.Now.Millisecond);

        private static readonly Regex fieldsRegEx = new Regex("({{.*?}})");

        // Remembers the replacements given to each value; keyed by the Replacement (list) they came from.
        private readonly Dictionary<Replacement, ReplacementPool> replacementPools = new Dictionary<Replacement, ReplacementPool>();

        // Internal constructor: implementations are only created through Create().
        internal DataMasking(MaskConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Creates the masking implementation that matches <c>MaskConfig.DataSource</c>.
        /// </summary>
        /// <param name="config">The masking configuration.</param>
        /// <returns>A <c>DataMasking</c> instance, ready to call <c>Mask()</c>.</returns>
        public static DataMasking Create(MaskConfig config)
        {
            return config.DataSource switch
            {
                // flat types
                DataSource.CsvFile => (DataMasking)new DataMaskingFlatData(config),
                DataSource.Database => new DataMaskingFlatData(config),
                // nested types
                DataSource.JsonFile => new DataMaskingJSONData(config),
                // Documents
                DataSource.DocXFile => new DataMaskingDocxData(config),
                // Spreadsheets
                DataSource.XlsXFile => new DataMaskingXlsxData(config),
                _ => throw new NotImplementedException($"{Enum.GetName(typeof(DataSource), config.DataSource)} is not implemented"),
            };
        }

        /// <summary>
        /// Performs masking.
        /// </summary>
        public abstract void Mask();

        /// <summary>
        /// The number of distinct values that were masked with asterisks because a <c>ReplacementList</c> 
        /// or <c>ReplacementQuery</c> mask had no replacement left. Zero means all values got a replacement.
        /// </summary>
        public int ValuesWithoutReplacement => replacementPools.Values.Sum(p => p.FallbackCount);

        private protected string GetMaskedString(string data, FieldMask fieldMask, DataRow row, JProperty jsonNode = null)
        {
            if (string.IsNullOrEmpty(data))
                return data;
            if (!string.IsNullOrEmpty(fieldMask.RegExMatch))
            {
                if (fieldMask.MaskType == MaskType.ReplacementList || fieldMask.MaskType == MaskType.ReplacementQuery)
                {
                    return GetConsistentRegexString(data, fieldMask, row);
                }

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
                            MaskType.Random => GetRandomString(matchData),
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
                    MaskType.Random => GetRandomString(data),
                    MaskType.List => GetStringFromList(row, fieldMask.ListOfPossibleReplacements),
                    MaskType.Query => GetStringFromQuery(row, fieldMask.QueryReplacement),
                    MaskType.Expression => GetExpressionString(row, fieldMask.Expression, jsonNode),
                    MaskType.ReplacementList => GetConsistentString(data, fieldMask, row, fieldMask.ListOfPossibleReplacements),
                    MaskType.ReplacementQuery => GetConsistentString(data, fieldMask, row, GetReplacementQueryLists(fieldMask.QueryReplacement)),
                    MaskType.None => data,
                    _ => throw new NotImplementedException($"Mask type {fieldMask.MaskType} is not yet implemented"),
                };
            }
        }

        /// <summary>
        /// Resolves a field reference used in an Expression mask ({{fieldName}}).
        /// Only implemented by the data sources that support expressions.
        /// </summary>
        private protected virtual string GetFieldValue(string fieldName, DataRow row, JProperty jsonNode)
        {
            throw new NotSupportedException($"Data source {this.config.DataSource} is not supported.");
        }

        private string GetExpressionString(DataRow row, string expression, JProperty jsonNode)
        {
            foreach (Match match in fieldsRegEx.Matches(expression))
            {
                string fieldName = match.Value.Replace("{{", "").Replace("}}", "");
                expression = expression.Replace(match.Value, GetFieldValue(fieldName, row, jsonNode));
            }

            return expression;
        }

        private string GetRandomString(string data)
        {
            char method(char c)
            {
                if (Char.IsDigit(c))
                {
                    return rnd.Next(0, 10).ToString()[0];
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
            string replacementStr = SelectReplacement(row, listOfReplacements)?.ReplacementList;
            if (replacementStr == null)
            {
                // warning, nothing found
                throw new DataNotFoundException(row);
            }
            var list = replacementStr.Split(',').Select(x => x.Trim()).ToList();
            string str = list[rnd.Next(list.Count)];
            return str;
        }

        /// <summary>
        /// Picks the replacement whose selector matches the row; one with an empty selector matches always.
        /// </summary>
        private Replacement SelectReplacement(DataRow row, List<Replacement> listOfReplacements)
        {
            foreach (var r in listOfReplacements)
            {
                // to have as a default
                if (string.IsNullOrEmpty(r.Selector))
                {
                    return r;
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
                    return r;
                }
            }

            if (row == null)
            {
                throw new InvalidDataException("No replacement was found. Add a Replacement with an empty Selector.");
            }
            return null;
        }

        private string GetStringFromQuery(DataRow row, QueryReplacement queryReplacement)
        {
            if (queryReplacement.ListOfPossibleReplacements == null)
            {
                var dt = ReadReplacementTable(queryReplacement);
                queryReplacement.ListOfPossibleReplacements = new List<Replacement>();

                // generate lists
                if (string.IsNullOrWhiteSpace(queryReplacement.GroupDbField))
                {
                    var valueList = dt.Rows.OfType<DataRow>()
                        .Select(r => Convert.ToString(r[queryReplacement.ValueDbField]))
                        .ToList();
                    queryReplacement.ListOfPossibleReplacements.Add(new Replacement()
                    {
                        Selector = string.Empty,
                        ReplacementList = string.Join(',', valueList)
                    });
                }
                else
                {
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
            }

            return GetStringFromList(row, queryReplacement.ListOfPossibleReplacements);
        }

        private DataTable ReadReplacementTable(QueryReplacement queryReplacement)
        {
            bool wasOpen = DbConnectionHelper.EnsureOpen(config);

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

            return dt;
        }

        private protected List<Replacement> GetReplacementQueryLists(QueryReplacement queryReplacement)
        {
            if (queryReplacement == null)
            {
                throw new InvalidDataException("QueryReplacement must be filled when the mask type is ReplacementQuery.");
            }

            if (queryReplacement.ListOfPossibleReplacements == null)
            {
                var dt = ReadReplacementTable(queryReplacement);
                var rows = dt.Rows.OfType<DataRow>();
                var replacements = new List<Replacement>();

                if (string.IsNullOrWhiteSpace(queryReplacement.GroupDbField))
                {
                    replacements.Add(CreateReplacement(string.Empty, rows, queryReplacement));
                }
                else
                {
                    foreach (var group in rows.GroupBy(r => Convert.ToString(r[queryReplacement.GroupDbField])))
                    {
                        replacements.Add(CreateReplacement($"{queryReplacement.SelectorField}={group.Key}", group, queryReplacement));
                    }
                }

                queryReplacement.ListOfPossibleReplacements = replacements;
            }

            return queryReplacement.ListOfPossibleReplacements;
        }

        private static Replacement CreateReplacement(string selector, IEnumerable<DataRow> rows, QueryReplacement queryReplacement)
        {
            var replacement = new Replacement() { Selector = selector };

            if (string.IsNullOrWhiteSpace(queryReplacement.KeyDbField))
            {
                replacement.Items = rows.Select(r => Convert.ToString(r[queryReplacement.ValueDbField])).ToList();
                return replacement;
            }

            foreach (var row in rows)
            {
                string key = Convert.ToString(row[queryReplacement.KeyDbField]);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }
                replacement.Mappings.Add(new ReplacementMap()
                {
                    From = key,
                    To = Convert.ToString(row[queryReplacement.ValueDbField])
                });
            }
            return replacement;
        }

        /// <summary>
        /// Replaces the regex group of every match on its own, so each match is masked consistently.
        /// Only the group is replaced; the rest of the match is kept. Group 0 means the whole match.
        /// </summary>
        private string GetConsistentRegexString(string data, FieldMask fieldMask, DataRow row)
        {
            var lists = fieldMask.MaskType == MaskType.ReplacementQuery
                ? GetReplacementQueryLists(fieldMask.QueryReplacement)
                : fieldMask.ListOfPossibleReplacements;

            return new Regex(fieldMask.RegExMatch).Replace(data, match =>
            {
                Group group = fieldMask.RegExGroupToReplace == 0 ? match : match.Groups[fieldMask.RegExGroupToReplace];
                if (!group.Success)
                {
                    return match.Value;
                }

                string replacement = GetConsistentString(group.Value, fieldMask, row, lists);
                int offset = group.Index - match.Index;
                return match.Value.Substring(0, offset) + replacement + match.Value.Substring(offset + group.Length);
            });
        }

        private string GetConsistentString(string value, FieldMask fieldMask, DataRow row, List<Replacement> listOfReplacements)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var replacement = SelectReplacement(row, listOfReplacements);
            if (replacement == null)
            {
                throw new DataNotFoundException(row);
            }

            if (!replacementPools.TryGetValue(replacement, out var pool))
            {
                pool = new ReplacementPool(replacement, rnd);
                replacementPools.Add(replacement, pool);
            }

            if (pool.TryAssign(value, fieldMask.OnListExhausted == ListExhaustedAction.Reuse, out string assigned))
            {
                return assigned;
            }

            if (fieldMask.OnListExhausted == ListExhaustedAction.Error)
            {
                throw new ReplacementExhaustedException();
            }

            pool.RegisterFallback(value);
            char asterisk = string.IsNullOrEmpty(fieldMask.Asterisk) ? '*' : fieldMask.Asterisk[0];
            return GetAsteriskString(value, asterisk);
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
    }
}
