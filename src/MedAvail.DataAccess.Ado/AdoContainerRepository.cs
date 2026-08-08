using System;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using MedAvail.Common;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>
    /// ADO.NET access to container via the createcontainer / modifycontainer
    /// PostgreSQL procedures (INOUT REFCURSOR). Both procs return the affected row
    /// via a cursor, which we dereference with FETCH ALL inside a transaction.
    /// </summary>
    public sealed class AdoContainerRepository : AdoRepositoryBase, IContainerRepository
    {
        public AdoContainerRepository(IConnectionStringProvider connections)
            : base(connections, MedAvailDatabase.PackageManagement) { }

        public ContainerDto CreateContainer(int shape, decimal length, decimal width, decimal height,
            string changedBy, int? containerType = null, string? description = null)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "CALL createcontainer(@p_shape, @p_length, @p_width, @p_height, @p_changed_by, @p_container_type, @p_description, @p_changed_on, @p_result_cursor)";
            cmd.Parameters.AddWithValue("@p_shape", shape);
            cmd.Parameters.AddWithValue("@p_length", (long)length);
            cmd.Parameters.AddWithValue("@p_width", (long)width);
            cmd.Parameters.AddWithValue("@p_height", (long)height);
            cmd.Parameters.AddWithValue("@p_changed_by", changedBy);
            cmd.Parameters.Add(new NpgsqlParameter("@p_container_type", NpgsqlDbType.Integer)
            {
                Value = (object?)containerType ?? DBNull.Value
            });
            cmd.Parameters.Add(new NpgsqlParameter("@p_description", NpgsqlDbType.Text)
            {
                Value = (object?)description ?? DBNull.Value
            });
            cmd.Parameters.Add(new NpgsqlParameter("@p_changed_on", NpgsqlDbType.Timestamp)
            {
                Value = DBNull.Value
            });
            cmd.Parameters.Add(new NpgsqlParameter("@p_result_cursor", NpgsqlDbType.Refcursor)
            {
                Direction = ParameterDirection.InputOutput,
                Value = "result_cursor"
            });
            cmd.ExecuteNonQuery();

            using var fetchCmd = conn.CreateCommand();
            fetchCmd.Transaction = tx;
            fetchCmd.CommandText = "FETCH ALL FROM result_cursor";
            using var reader = fetchCmd.ExecuteReader();
            if (!reader.Read())
                throw new InvalidOperationException("CreateContainer returned no row.");
            var result = MapFromResult(reader);
            tx.Commit();
            return result;
        }

        public ContainerDto ModifyContainer(string containerId, int shape, decimal length, decimal width,
            decimal height, string changedBy, int? containerType = null, string? description = null)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "CALL modifycontainer(@p_container_id, @p_shape, @p_length, @p_width, @p_height, @p_changed_by, @p_container_type, @p_description, @p_changed_on, @p_result_cursor)";
            cmd.Parameters.AddWithValue("@p_container_id", containerId);
            cmd.Parameters.AddWithValue("@p_shape", shape);
            cmd.Parameters.AddWithValue("@p_length", (long)length);
            cmd.Parameters.AddWithValue("@p_width", (long)width);
            cmd.Parameters.AddWithValue("@p_height", (long)height);
            cmd.Parameters.AddWithValue("@p_changed_by", changedBy);
            cmd.Parameters.Add(new NpgsqlParameter("@p_container_type", NpgsqlDbType.Integer)
            {
                Value = (object?)containerType ?? DBNull.Value
            });
            cmd.Parameters.Add(new NpgsqlParameter("@p_description", NpgsqlDbType.Text)
            {
                Value = (object?)description ?? DBNull.Value
            });
            cmd.Parameters.Add(new NpgsqlParameter("@p_changed_on", NpgsqlDbType.Timestamp)
            {
                Value = DBNull.Value
            });
            cmd.Parameters.Add(new NpgsqlParameter("@p_result_cursor", NpgsqlDbType.Refcursor)
            {
                Direction = ParameterDirection.InputOutput,
                Value = "result_cursor"
            });
            cmd.ExecuteNonQuery();

            using var fetchCmd = conn.CreateCommand();
            fetchCmd.Transaction = tx;
            fetchCmd.CommandText = "FETCH ALL FROM result_cursor";
            using var reader = fetchCmd.ExecuteReader();
            if (!reader.Read())
                throw new InvalidOperationException("ModifyContainer returned no row.");
            var result = MapFromContainerTable(reader);
            tx.Commit();
            return result;
        }

        public ContainerDto? GetById(string containerId)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT container_id, description, shape, length, width, height, " +
                "package_definition_type_id, changed_by, changed_on " +
                "FROM container WHERE container_id = @id;";
            cmd.Parameters.Add(Param("@id", NpgsqlDbType.Text, containerId));
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromContainerTable(reader) : null;
        }

        public int Delete(string containerId)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM container WHERE container_id = @id;";
            cmd.Parameters.Add(Param("@id", NpgsqlDbType.Text, containerId));
            return cmd.ExecuteNonQuery();
        }

        public int? GetAnyShapeId()
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT shape_id FROM lookup_package_shape ORDER BY shape_id LIMIT 1;";
            var result = cmd.ExecuteScalar();
            return result is null or DBNull ? null : Convert.ToInt32(result);
        }

        // CreateContainer SELECTs from its result cursor (column set
        // matches the container table, in declared order).
        private static ContainerDto MapFromResult(NpgsqlDataReader r) => ReadByName(r);

        // ModifyContainer / GetById SELECT from the container table.
        private static ContainerDto MapFromContainerTable(NpgsqlDataReader r) => ReadByName(r);

        private static ContainerDto ReadByName(NpgsqlDataReader r)
        {
            return new ContainerDto
            {
                ContainerId = GetString(r, "container_id"),
                Description = GetNullableStringByName(r, "description"),
                Shape = GetInt(r, "shape"),
                Length = GetDecimal(r, "length"),
                Width = GetDecimal(r, "width"),
                Height = GetDecimal(r, "height"),
                PackageDefinitionTypeId = GetInt(r, "package_definition_type_id"),
                ChangedBy = GetNullableStringByName(r, "changed_by"),
                ChangedOn = HasColumn(r, "changed_on") && !r.IsDBNull(r.GetOrdinal("changed_on"))
                    ? r.GetDateTime(r.GetOrdinal("changed_on"))
                    : default
            };
        }

        private static bool HasColumn(NpgsqlDataReader r, string name)
        {
            for (var i = 0; i < r.FieldCount; i++)
                if (string.Equals(r.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string GetString(NpgsqlDataReader r, string name)
        {
            var o = r.GetOrdinal(name);
            return r.IsDBNull(o) ? string.Empty : r.GetValue(o).ToString() ?? string.Empty;
        }

        private static string? GetNullableStringByName(NpgsqlDataReader r, string name)
        {
            if (!HasColumn(r, name)) return null;
            var o = r.GetOrdinal(name);
            return r.IsDBNull(o) ? null : r.GetValue(o).ToString();
        }

        private static int GetInt(NpgsqlDataReader r, string name)
        {
            var o = r.GetOrdinal(name);
            return r.IsDBNull(o) ? 0 : Convert.ToInt32(r.GetValue(o));
        }

        private static decimal GetDecimal(NpgsqlDataReader r, string name)
        {
            var o = r.GetOrdinal(name);
            return r.IsDBNull(o) ? 0m : Convert.ToDecimal(r.GetValue(o));
        }
    }
}
