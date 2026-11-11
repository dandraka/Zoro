using System;
using System.Data;
using System.Linq;

namespace Dandraka.Zoro.Processor
{
    internal class DbWriter : IDataWriter<DataTable>
    {
        private readonly MaskConfig config;

        internal DbWriter(MaskConfig config)
        {
            this.config = config;
        }

        public void Write(DataTable dt)
        {
            DbConnectionHelper.ValidateConnectionSettings(config);
            if (string.IsNullOrWhiteSpace(config.SqlCommand))
            {
                throw new ArgumentException($"Sql Command statement must be filled when using the database option");
            }
            char dbParamChar = DbConnectionHelper.GetParamChar(config);
            int numParams = config.SqlCommand.Count(c => c == dbParamChar);
            if (numParams != config.FieldMasks.Count)
            {
                throw new ArgumentException($"Sql Command parameter mismatch: '{config.SqlCommand}' does not contain the same number of parameters {dbParamChar}field ({numParams}) as the number of FieldMasks ({config.FieldMasks.Count})");
            }

            bool wasOpen = DbConnectionHelper.EnsureOpen(config);

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
    }
}
