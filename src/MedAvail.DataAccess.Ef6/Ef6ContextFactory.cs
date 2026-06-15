using System;
using Npgsql;
using MedAvail.Common;
using MedAvail.DataAccess.Ef6.Contexts;

namespace MedAvail.DataAccess.Ef6
{
    /// <summary>
    /// Creates EF6 contexts wired to the correct database via the shared
    /// connection-string provider. Each context gets its own NpgsqlConnection, which
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
            var connection = new NpgsqlConnection(cs);
            return new PackageManagementEf6Context(connection, contextOwnsConnection: true);
        }

        public Contexts.CoreEf6Context CreateCore()
        {
            var cs = _connections.GetConnectionString(MedAvailDatabase.Core);
            var connection = new NpgsqlConnection(cs);
            return new Contexts.CoreEf6Context(connection, contextOwnsConnection: true);
        }
    }
}
