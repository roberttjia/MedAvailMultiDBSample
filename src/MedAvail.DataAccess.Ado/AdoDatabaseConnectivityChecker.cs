using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using MedAvail.Common;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>
    /// ADO.NET connectivity probe. Opens a connection per database and runs a
    /// trivial query that returns DB_NAME() so we can confirm the connection
    /// landed in the database we intended (not just that the server is up).
    /// </summary>
    public sealed class AdoDatabaseConnectivityChecker : IDatabaseConnectivityChecker
    {
        private readonly IConnectionStringProvider _connections;

        public AdoDatabaseConnectivityChecker(IConnectionStringProvider connections)
        {
            _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        }

        public ConnectivityResult Check(MedAvailDatabase database)
        {
            // Resolve the expected name from the connection string itself so the
            // result is meaningful even if provider config changes.
            string expected;
            string connectionString;
            try
            {
                connectionString = _connections.GetConnectionString(database);
                expected = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
            }
            catch (Exception ex)
            {
                return ConnectivityResult.Failure(database, expectedDatabaseName: database.ToString(),
                    error: ex.Message, elapsedMs: 0);
            }

            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT DB_NAME();";
                var reported = Convert.ToString(command.ExecuteScalar());

                sw.Stop();
                return ConnectivityResult.Success(
                    database, expected, reported ?? string.Empty,
                    connection.ServerVersion, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return ConnectivityResult.Failure(database, expected, ex.Message, sw.ElapsedMilliseconds);
            }
        }

        public IReadOnlyList<ConnectivityResult> CheckAll()
        {
            var results = new List<ConnectivityResult>();
            foreach (MedAvailDatabase db in Enum.GetValues(typeof(MedAvailDatabase)))
            {
                results.Add(Check(db));
            }
            return results;
        }
    }
}
