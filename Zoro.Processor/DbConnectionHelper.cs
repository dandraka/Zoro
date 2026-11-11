using System;
using System.Data;
using System.Data.Common;
using System.IO;

namespace Dandraka.Zoro.Processor
{
    internal static class DbConnectionHelper
    {
        internal static void ValidateConnectionSettings(MaskConfig config)
        {
            if (config.GetConnection() == null && (string.IsNullOrEmpty(config.ConnectionString) || string.IsNullOrEmpty(config.ConnectionType)))
            {
                throw new InvalidDataException("If no DbConnection is provided, ConnectionString and ConnectionType cannot be empty when using the database option");
            }
        }

        internal static char GetParamChar(MaskConfig config)
        {
            return config.GetConnection().GetType().ToString() == typeof(Microsoft.Data.SqlClient.SqlConnection).FullName ? '@' : '$';
        }

        /// <summary>Creates and opens a db connection, if needed.</summary>
        /// <returns>True if the connection was already open, false otherwise.</returns>
        internal static bool EnsureOpen(MaskConfig config)
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

        private static DbProviderFactory GetDbFactory(string connType)
        {
            switch (connType)
            {
                case "Microsoft.Data.SqlClient":
                    DbProviderFactories.RegisterFactory(connType, typeof(Microsoft.Data.SqlClient.SqlClientFactory));
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
    }
}
