using System;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using MedAvail.Common;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>
    /// Base for ADO.NET repositories. Holds the target database and resolves a
    /// fresh connection per operation through the connection-string provider —
    /// the single point that binds a repository to a specific database.
    /// </summary>
    public abstract class AdoRepositoryBase
    {
        private readonly IConnectionStringProvider _connections;

        protected MedAvailDatabase TargetDatabase { get; }

        protected AdoRepositoryBase(IConnectionStringProvider connections, MedAvailDatabase database)
        {
            _connections = connections ?? throw new ArgumentNullException(nameof(connections));
            TargetDatabase = database;
        }

        protected NpgsqlConnection OpenConnection()
        {
            var connection = new NpgsqlConnection(_connections.GetConnectionString(TargetDatabase));
            connection.Open();
            return connection;
        }

        protected static NpgsqlParameter Param(string name, NpgsqlDbType type, object? value)
            => new(name, type) { Value = value ?? DBNull.Value };

        protected static T? GetNullable<T>(NpgsqlDataReader reader, int ordinal) where T : struct
            => reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<T>(ordinal);

        protected static string? GetNullableString(NpgsqlDataReader reader, int ordinal)
            => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
