using System;
using System.Data.SqlClient;
using MedAvail.Common;
using MedAvail.DataAccess.Ef6.Contexts;

namespace MedAvail.DataAccess.Ef6
{
    /// <summary>
    /// Creates EF6 contexts wired to the correct database via the shared
    /// connection-string provider. Each context gets its own SqlConnection, which
    /// it owns and disposes. This is the single point binding EF6 to a database.
    /// </summary>
    public sealed class Ef6ContextFactory
    {
        private readonly IConnectionStringProvider _connections;

        public Ef6ContextFactory(IConnectionStringProvider connections)
        {
            _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        }

        public PackageManagementEf6Context CreatePackageManagement()
        {
            var cs = _connections.GetConnectionString(MedAvailDatabase.PackageManagement);
            var connection = new SqlConnection(AdaptForLegacyClient(cs));
            return new PackageManagementEf6Context(connection, contextOwnsConnection: true);
        }

        public Contexts.CoreEf6Context CreateCore()
        {
            var cs = _connections.GetConnectionString(MedAvailDatabase.Core);
            var connection = new SqlConnection(AdaptForLegacyClient(cs));
            return new Contexts.CoreEf6Context(connection, contextOwnsConnection: true);
        }

        /// <summary>
        /// EF6 uses the legacy System.Data.SqlClient, which does not understand the
        /// modern "TrustServerCertificate" keyword (introduced in Microsoft.Data.SqlClient).
        /// Translate it to the legacy-compatible "Encrypt=False" so a self-signed
        /// server certificate does not block the connection.
        /// </summary>
        private static string AdaptForLegacyClient(string modernConnectionString)
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(modernConnectionString);

            var legacy = new System.Data.SqlClient.SqlConnectionStringBuilder
            {
                DataSource = builder.DataSource,
                InitialCatalog = builder.InitialCatalog,
                UserID = builder.UserID,
                Password = builder.Password,
                ConnectTimeout = builder.ConnectTimeout
            };

            // TrustServerCertificate=true (modern) => Encrypt=false (legacy) for a
            // self-signed cert test server.
            if (builder.TrustServerCertificate)
                legacy.Encrypt = false;

            return legacy.ConnectionString;
        }
    }
}
