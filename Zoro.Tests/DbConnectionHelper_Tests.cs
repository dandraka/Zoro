using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using Xunit;
using Dandraka.Zoro.Processor;

namespace Dandraka.Zoro.Tests
{
    /// <summary>
    /// Tests for the connection handling, without needing a SQL Server.
    /// </summary>
    public class DbConnectionHelper_Tests
    {
        [Fact]
        public void T01_ValidateConnectionSettings_NoConnectionNoSettings_Throws()
        {
            var config = new MaskConfig() { DataSource = DataSource.Database };

            Assert.Throws<InvalidDataException>(() => DbConnectionHelper.ValidateConnectionSettings(config));
        }

        [Theory]
        [InlineData("Server=x;", "")]
        [InlineData("", "Microsoft.Data.SqlClient")]
        public void T02_ValidateConnectionSettings_IncompleteSettings_Throws(string connectionString, string connectionType)
        {
            var config = new MaskConfig() { ConnectionString = connectionString, ConnectionType = connectionType };

            Assert.Throws<InvalidDataException>(() => DbConnectionHelper.ValidateConnectionSettings(config));
        }

        [Fact]
        public void T03_ValidateConnectionSettings_ConnectionStringAndType_Passes()
        {
            var config = new MaskConfig() { ConnectionString = "Server=x;", ConnectionType = "Microsoft.Data.SqlClient" };

            DbConnectionHelper.ValidateConnectionSettings(config);
        }

        [Fact]
        public void T04_ValidateConnectionSettings_ProvidedConnection_Passes()
        {
            using var connection = new SQLiteConnection("Data Source=:memory:");
            var config = new MaskConfig();
            config.SetConnection(connection);

            DbConnectionHelper.ValidateConnectionSettings(config);
        }

        [Fact]
        public void T05_GetParamChar_SqlServer_UsesAt()
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection();
            var config = new MaskConfig();
            config.SetConnection(connection);

            Assert.Equal('@', DbConnectionHelper.GetParamChar(config));
        }

        [Fact]
        public void T06_GetParamChar_Other_UsesDollar()
        {
            using var connection = new SQLiteConnection("Data Source=:memory:");
            var config = new MaskConfig();
            config.SetConnection(connection);

            Assert.Equal('$', DbConnectionHelper.GetParamChar(config));
        }

        [Fact]
        public void T07_EnsureOpen_ClosedConnection_OpensIt()
        {
            using var connection = new SQLiteConnection("Data Source=:memory:");
            var config = new MaskConfig();
            config.SetConnection(connection);

            bool wasOpen = DbConnectionHelper.EnsureOpen(config);

            Assert.False(wasOpen);
            Assert.Equal(ConnectionState.Open, connection.State);
        }

        [Fact]
        public void T08_EnsureOpen_OpenConnection_ReportsItWasOpen()
        {
            using var connection = new SQLiteConnection("Data Source=:memory:");
            connection.Open();
            var config = new MaskConfig();
            config.SetConnection(connection);

            bool wasOpen = DbConnectionHelper.EnsureOpen(config);

            Assert.True(wasOpen);
            Assert.Equal(ConnectionState.Open, connection.State);
        }

        [Fact]
        public void T09_EnsureOpen_UnsupportedConnectionType_Throws()
        {
            var config = new MaskConfig() { ConnectionString = "Data Source=:memory:", ConnectionType = "System.Data.SQLite" };

            var ex = Assert.Throws<NotSupportedException>(() => DbConnectionHelper.EnsureOpen(config));
            Assert.Contains("System.Data.SQLite", ex.Message);
        }
    }
}
