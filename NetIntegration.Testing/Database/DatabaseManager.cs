using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Threading;

namespace NetIntegration.Testing.Database
{
    public static class DatabaseManager
    {
        #region Properties
        public static string ConnectionString { get; private set; }

        private static Lazy<IDatabase> _database = new Lazy<IDatabase>(() =>
        {
            if (String.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new InvalidOperationException("You must setup the connection string first.");
            }
            return GetDatabase(ConnectionString);
        });
        public static IDatabase Database { get { return _database.Value; } }
        #endregion

        #region Public Methods
        public static void SetConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public static void SetDataDirectory()
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", AppDomain.CurrentDomain.BaseDirectory);
        }

        public static IDatabase GetDatabase(string connectionString)
        {
            return new DatabaseImpl(connectionString);
        }
        #endregion

        #region Private Types
        private class DatabaseImpl : IDatabase
        {
            private string _connectionString;

            public DatabaseImpl(string connectionString)
            {
                _connectionString = connectionString;
            }

            #region IDatabase Implementation
            public void GrantFileAccessForAttach()
            {
                var databaseFilename = GetDatabaseFilename(_connectionString);
                GrantFileAccess(databaseFilename);
                GrantFileAccess(databaseFilename.Replace(".mdf", "_log.ldf"));
            }

            public void Deploy(string dacPackageFile)
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var dacServices = new DacServices(GetDacFriendlyConnectionString(_connectionString));
                    dacServices.Message += (sender, args) => Debug.WriteLineIf(Debugger.IsAttached, args.Message);
                    dacServices.ProgressChanged += (sender, args) => Debug.WriteLineIf(Debugger.IsAttached, String.Format("[{0}] {1} - {2}", args.OperationId, args.Status, args.Message));

                    var package = DacPackage.Load(dacPackageFile);
                    CancellationToken? cancellationToken = new CancellationToken();

                    dacServices.Deploy(package, connection.Database, true, null, cancellationToken);
                    connection.Close();
                }
            }

            public void DeployLocalDB(string dacPackageFile)
            {
                GrantFileAccessForAttach();
                try
                {
                    Deploy(dacPackageFile);
                }
                finally
                {
                    DetachDatabase();
                }

            }

            public void DetachDatabase()
            {
                var connectionStringBuilder = new SqlConnectionStringBuilder(_connectionString);

                var serverName = connectionStringBuilder.DataSource;
                var databaseName = connectionStringBuilder.InitialCatalog;

                var server = new Server(serverName);
                server.KillAllProcesses(databaseName);
                var database = server.Databases[databaseName];
                if (database != null)
                {
                    database.DatabaseOptions.UserAccess = DatabaseUserAccess.Single;
                    database.Alter(TerminationClause.RollbackTransactionsImmediately);
                    server.DetachDatabase(databaseName, true);
                }
            }

            public int ExecuteNonQuery(Action<SqlCommand> prepare)
            {
                var rows = 0;
                ProcessCommand(
                    command =>
                    {
                        prepare(command);
                        rows = command.ExecuteNonQuery();
                    });

                return rows;
            }

            public TResult ExecuteReader<TResult>(Action<SqlCommand> prepare, Func<SqlDataReader, TResult> function)
            {
                var result = default(TResult);

                ProcessCommand(command =>
                {
                    prepare(command);
                    using (var reader = command.ExecuteReader())
                    {
                        result = function(reader);
                    }
                });

                return result;
            }

            public TResult ExecuteReader<TResult>(Action<SqlCommand> prepare, Func<SqlConnection, SqlDataReader, TResult> function)
            {
                var result = default(TResult);

                ProcessCommand((connection, command) =>
                {
                    prepare(command);
                    using (var reader = command.ExecuteReader())
                    {
                        result = function(connection, reader);
                    }
                });

                return result;
            }

            public IEnumerable<TResult> GetResults<TResult>(Action<SqlCommand> prepare, Func<BasicReader, TResult> function)
            {
                return ExecuteReader(prepare, reader => reader.ToList(function));
            }

            public Dictionary<TKey, TValue> GetResults<TKey, TValue>(Action<SqlCommand> prepare, Func<BasicReader, TValue> function, Func<TValue, TKey> keySelector)
            {
                return ExecuteReader(prepare, reader => reader.ToDictionary(function, keySelector));
            }

            public TResult GetSingleResult<TResult>(Action<SqlCommand> prepare, Func<SqlDataReader, TResult> function)
            {
                return ExecuteReader(prepare, reader => reader.Read() ? function(reader) : default(TResult));
            }
            #endregion

            #region Private Methods
            private void ProcessCommand(Action<SqlCommand> function)
            {
                ProcessCommand((connection, command) => function(command));
            }

            private void ProcessCommand(Action<SqlConnection, SqlCommand> function)
            {
                if (function == null)
                {
                    throw new ArgumentNullException("function");
                }

                ProcessConnection(connection =>
                {
                    using (var command = connection.CreateCommand())
                    {
                        function(connection, command);
                    }
                });
            }

            private void ProcessConnection(Action<SqlConnection> function)
            {
                if (function == null)
                {
                    throw new ArgumentNullException("function");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    function(connection);
                }
            }

            private T ProcessConnection<T>(Func<SqlConnection, T> function)
            {
                if (function == null)
                {
                    throw new ArgumentNullException("function");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    return function(connection);
                }
            }

            private static string GetDatabaseFilename(string connectionString)
            {
                var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                string filename = String.Empty;
                if (connectionStringBuilder.AttachDBFilename.Contains("|DataDirectory|"))
                {
                    filename = connectionStringBuilder.AttachDBFilename.Replace("|DataDirectory|", AppDomain.CurrentDomain.BaseDirectory);
                }
                else
                {
                    filename = connectionStringBuilder.AttachDBFilename;
                }

                return filename;
            }

            private static void GrantFileAccess(string filename)
            {
                var fs = new FileSecurity();
                fs.AddAccessRule(new FileSystemAccessRule(Thread.CurrentPrincipal.Identity.Name, FileSystemRights.FullControl, AccessControlType.Allow));
                fs.AddAccessRule(new FileSystemAccessRule("NT Service\\MSSQL$SQLEXPRESS", FileSystemRights.FullControl, AccessControlType.Allow));

                File.SetAccessControl(filename, fs);
            }

            private static string GetDacFriendlyConnectionString(string connectionString)
            {
                var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString) { AttachDBFilename = String.Empty, Pooling = false };
                return connectionStringBuilder.ConnectionString;
            }
            #endregion
        }
        #endregion
    }
}
