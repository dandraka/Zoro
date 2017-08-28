using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using GenericParsing;

namespace Zoro.Processor
{
    public static class CharExtension
    {
        private static Random rnd = new Random(DateTime.Now.Millisecond);

        public static bool IsVowel(this Char c)
        {
            return c == 'e' ||
                   c == 'a' ||
                   c == 'o' ||
                   c == 'i' ||
                   c == 'u' ||
                   c == 'y' ||
                   c == 'ä' ||
                   c == 'ö' ||
                   c == 'ü';
        }

        public static bool IsConsonant(this Char c)
        {
            return !c.IsVowel();
        }

        public static Char GetRandomVowel()
        {
            Char[] vowels = new Char[] { 'e', 'a', 'o', 'i', 'u', 'ä', 'ö', 'ü' };
            return vowels[rnd.Next(0, vowels.Length)];
        }

        public static Char GetRandomConsonant()
        {
            Char[] consonants = new Char[] { 'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'z' };
            return consonants[rnd.Next(0, consonants.Length)];
        }
    }

    public class DataMasking
    {
        private readonly MaskConfig config;

        private readonly Random rnd = new Random(DateTime.Now.Millisecond);

        public DataMasking(MaskConfig config)
        {
            this.config = config;
        }

        public void Mask()
        {
            var dt = GetData();

            AnonymizeTable(dt);

            SaveDataToFile(dt);
        }

        private DataTable GetData()
        {
            switch (config.DataSource)
            {
                case DataSource.CsvFile:
                    return ReadDataFromFile();
                case DataSource.Database:
                    return ReadDataFromDb();
                default:
                    throw new NotSupportedException();
            }
        }

        private DataTable ReadDataFromDb()
        {
            if (string.IsNullOrEmpty(config.ConnectionString))
            {
                throw new InvalidDataException("Connection string must be filled when using the DB option");
            }

            if (string.IsNullOrEmpty(config.SqlSelect))
            {
                throw new InvalidDataException("SQL Select statement must be filled when using the DB option");
            }

            DataTable dt = new DataTable("data");

            Action doDbSelect = new Action(() =>
            {
                using (var dbConn = new SqlConnection(config.ConnectionString))
                {
                    dbConn.Open();
                    var dbCmd = dbConn.CreateCommand();
                    dbCmd.CommandType = CommandType.Text;
                    dbCmd.CommandText = config.SqlSelect;

                    using (var dbReader = dbCmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        dt.Load(dbReader);
                        dbReader.Close();
                    }
                }

                foreach (DataColumn dataColumn in dt.Columns)
                {
                    dataColumn.ReadOnly = false;
                }
            });

            Console.WriteLine("Enter username (Enter to use currently logged in user or connection string credentials):");
            string user = Console.ReadLine();

            if (string.IsNullOrEmpty(user))
            {
                doDbSelect();
            }
            else
            {
                Console.WriteLine("Enter domain (hit Enter to use the current one):");
                string domain = Console.ReadLine();
                if (string.IsNullOrEmpty(domain))
                {
                    domain = Environment.UserDomainName;
                }

                Console.WriteLine("Enter password:");
                string pwd = Console.ReadLine();
                Console.Clear();

                using (var impersonator = new XperiCode.Impersonator.Impersonator(domain, user, pwd))
                {
                    doDbSelect();
                }
            }            
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

                    dt.Rows[r][colName] = GetMaskedString(Convert.ToString(dt.Rows[r][colName]), config.FieldMasks[c]);
                }
            }
        }

        private string GetMaskedString(string data, FieldMask fieldMask)
        {
            if (string.IsNullOrEmpty(data))
                return data;
            if (!string.IsNullOrEmpty(fieldMask.RegExMatch))
            {
                Regex rgx = new Regex(fieldMask.RegExMatch);
                switch (fieldMask.MaskType)
                {
                    case MaskType.Asterisk:
                        return rgx.Replace(data, fieldMask.Asterisk);
                    case MaskType.Similar:
                        var match = rgx.Match(data);
                        string matchData = match.Groups[fieldMask.RegExGroupToReplace].Value;
                        string replaceStr = string.Empty;
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            string s;
                            if (i != fieldMask.RegExGroupToReplace)
                            {
                                s = "${"+ i + "}";
                            }
                            else
                            {
                                s = GetSimilarString(matchData);
                            }
                            replaceStr += s;
                        }
                        string repl = rgx.Replace(data, replaceStr);
                        return repl;
                    default:
                        return data;
                }
            }
            else
            {
                switch (fieldMask.MaskType)
                {
                    case MaskType.Asterisk:
                        return new string(fieldMask.Asterisk[0], data.Length);
                    case MaskType.Similar:
                        return GetSimilarString(data);
                    default:
                        return data;
                }
            }            
        }

        private string GetSimilarString(string data)
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

                if (Char.IsDigit(c))
                {
                    anon += rnd.Next(0, 9).ToString()[0];
                    continue;
                }
                if (c.IsVowel())
                {
                    Char a = CharExtension.GetRandomVowel();
                    if (Char.IsUpper(c))
                    {
                        a = Char.ToUpper(a);
                    }
                    anon += a;
                }
                else
                {
                    Char a = CharExtension.GetRandomConsonant();
                    if (Char.IsUpper(c))
                    {
                        a = Char.ToUpper(a);
                    }
                    anon += a;
                }
            }

            return anon;
        }

        private DataTable ReadDataFromFile()
        {
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

        private void SaveDataToFile(DataTable dt)
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
    }
}
