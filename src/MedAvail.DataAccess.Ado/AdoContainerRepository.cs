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
    /// stored procedures. Both are PostgreSQL PROCEDUREs with an INOUT REFCURSOR
    /// parameter. We open a transaction, CALL the procedure with a cursor name,
    /// then FETCH ALL from the cursor to read the result rows.
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
            try
            {
                using var callCmd = conn.CreateCommand();
                callCmd.Transaction = tx;
                callCmd.CommandText =
                    "CALL createcontainer(@shape, @length, @width, @height, @changed_by, @container_type, @description, NULL, @cursor)";
                callCmd.Parameters.Add(Param("@shape", NpgsqlDbType.Integer, shape));
                callCmd.Parameters.Add(Param("@length", NpgsqlDbType.Numeric, length));
                callCmd.Parameters.Add(Param("@width", NpgsqlDbType.Numeric, width));
                callCmd.Parameters.Add(Param("@height", NpgsqlDbType.Numeric, height));
                callCmd.Parameters.Add(Param("@changed_by", NpgsqlDbType.Text, changedBy));
                callCmd.Parameters.Add(Param("@container_type", NpgsqlDbType.Integer, (object?)containerType));
                callCmd.Parameters.Add(Param("@description", NpgsqlDbType.Text, (object?)description));
                callCmd.Parameters.Add(new NpgsqlParameter("@cursor", NpgsqlDbType.Refcursor) { Value = "result_cursor" });
                callCmd.ExecuteNonQuery();

                using var fetchCmd = conn.CreateCommand();
                fetchCmd.Transaction = tx;
                fetchCmd.CommandText = "FETCH ALL FROM result_cursor";
                using var reader = fetchCmd.ExecuteReader();
                if (!reader.Read())
                    throw new InvalidOperationException("createcontainer returned no row.");
                var result = MapFromResult(reader);
                tx.Commit();
                return result;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public ContainerDto ModifyContainer(string containerId, int shape, decimal length, decimal width,
            decimal height, string changedBy, int? containerType = null, string? description = null)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using var callCmd = conn.CreateCommand();
                callCmd.Transaction = tx;
                callCmd.CommandText =
                    "CALL modifycontainer(@container_id, @shape, @length, @width, @height, @changed_by, @container_type, @description, NULL, @cursor)";
                callCmd.Parameters.Add(Param("@container_id", NpgsqlDbType.Varchar, containerId));
                callCmd.Parameters.Add(Param("@shape", NpgsqlDbType.Integer, shape));
                callCmd.Parameters.Add(Param("@length", NpgsqlDbType.Numeric, length));
                callCmd.Parameters.Add(Param("@width", NpgsqlDbType.Numeric, width));
                callCmd.Parameters.Add(Param("@height", NpgsqlDbType.Numeric, height));
                callCmd.Parameters.Add(Param("@changed_by", NpgsqlDbType.Text, changedBy));
                callCmd.Parameters.Add(Param("@container_type", NpgsqlDbType.Integer, (object?)containerType));
                callCmd.Parameters.Add(Param("@description", NpgsqlDbType.Text, (object?)description));
                callCmd.Parameters.Add(new NpgsqlParameter("@cursor", NpgsqlDbType.Refcursor) { Value = "result_cursor" });
                callCmd.ExecuteNonQuery();

                using var fetchCmd = conn.CreateCommand();
                fetchCmd.Transaction = tx;
                fetchCmd.CommandText = "FETCH ALL FROM result_cursor";
                using var reader = fetchCmd.ExecuteReader();
                if (!reader.Read())
                    throw new InvalidOperationException("modifycontainer returned no row.");
                var result = MapFromContainerTable(reader);
                tx.Commit();
                return result;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public ContainerDto? GetById(string containerId)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT container_id, description, shape, length, width, height, " +
                "package_definition_type_id, changed_by, changed_on " +
                "FROM container WHERE container_id = @id;";
            cmd.Parameters.Add(Param("@id", NpgsqlDbType.Varchar, containerId));
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromContainerTable(reader) : null;
        }

        public int Delete(string containerId)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM container WHERE container_id = @id;";
            cmd.Parameters.Add(Param("@id", NpgsqlDbType.Varchar, containerId));
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

        // CreateContainer SELECTs from its temp_inserted table variable (column set
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
