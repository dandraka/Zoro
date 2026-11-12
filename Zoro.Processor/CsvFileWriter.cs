using System;
using System.Data;
using System.IO;
using System.Text;

namespace Dandraka.Zoro.Processor
{
    internal class CsvFileWriter : IDataWriter<DataTable>
    {
        private readonly MaskConfig config;

        internal CsvFileWriter(MaskConfig config)
        {
            this.config = config;
        }

        public void Write(DataTable dt)
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
