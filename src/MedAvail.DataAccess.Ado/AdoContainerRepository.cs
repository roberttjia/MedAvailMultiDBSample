using System;
using System.Data;
using Microsoft.Data.SqlClient;
using MedAvail.Common;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>
    /// ADO.NET access to dbo.container via the CreateContainer / ModifyContainer
    /// stored procedures (CommandType.StoredProcedure). Both procs SELECT the
    /// affected row back, which we read from the result set.
    /// </summary>
    public sealed class AdoContainerRepository : AdoRepositoryBase, IContainerRepository
    {
        public AdoContainerRepository(IConnectionStringProvider connections)
            : base(connections, MedAvailDatabase.PackageManagement) { }

        public ContainerDto CreateContainer(int shape, decimal length, decimal width, decimal height,
            string changedBy, int? containerType = null, string? description = null)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.CreateContainer";
            cmd.Parameters.Add(Param("@shape", SqlDbType.Int, shape));
            cmd.Parameters.Add(Param("@length", SqlDbType.Decimal, length));
            cmd.Parameters.Add(Param("@width", SqlDbType.Decimal, width));
            cmd.Parameters.Add(Param("@height", SqlDbType.Decimal, height));
            cmd.Parameters.Add(Param("@changed_by", SqlDbType.NVarChar, changedBy));
            cmd.Parameters.Add(Param("@container_type", SqlDbType.Int, (object?)containerType));
            cmd.Parameters.Add(Param("@description", SqlDbType.NVarChar, (object?)description));

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                throw new InvalidOperationException("CreateContainer returned no row.");
            return MapFromResult(reader);
        }

        public ContainerDto ModifyContainer(string containerId, int shape, decimal length, decimal width,
            decimal height, string changedBy, int? containerType = null, string? description = null)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.ModifyContainer";
            cmd.Parameters.Add(Param("@container_id", SqlDbType.NVarChar, containerId));
            cmd.Parameters.Add(Param("@shape", SqlDbType.Int, shape));
            cmd.Parameters.Add(Param("@length", SqlDbType.Decimal, length));
            cmd.Parameters.Add(Param("@width", SqlDbType.Decimal, width));
            cmd.Parameters.Add(Param("@height", SqlDbType.Decimal, height));
            cmd.Parameters.Add(Param("@changed_by", SqlDbType.NVarChar, changedBy));
            cmd.Parameters.Add(Param("@container_type", SqlDbType.Int, (object?)containerType));
            cmd.Parameters.Add(Param("@description", SqlDbType.NVarChar, (object?)description));

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                throw new InvalidOperationException("ModifyContainer returned no row.");
            return MapFromContainerTable(reader);
        }

        public ContainerDto? GetById(string containerId)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT container_id, description, shape, length, width, height, " +
                "package_definition_type_id, changed_by, changed_on " +
                "FROM dbo.container WHERE container_id = @id;";
            cmd.Parameters.Add(Param("@id", SqlDbType.NVarChar, containerId));
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapFromContainerTable(reader) : null;
        }

        public int Delete(string containerId)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM dbo.container WHERE container_id = @id;";
            cmd.Parameters.Add(Param("@id", SqlDbType.NVarChar, containerId));
            return cmd.ExecuteNonQuery();
        }

        public int? GetAnyShapeId()
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TOP 1 shape_id FROM dbo.lookup_package_shape ORDER BY shape_id;";
            var result = cmd.ExecuteScalar();
            return result is null or DBNull ? null : Convert.ToInt32(result);
        }

        // CreateContainer SELECTs from its @inserted table variable (column set
        // matches the container table, in declared order).
        private static ContainerDto MapFromResult(SqlDataReader r) => ReadByName(r);

        // ModifyContainer / GetById SELECT from the container table.
        private static ContainerDto MapFromContainerTable(SqlDataReader r) => ReadByName(r);

        private static ContainerDto ReadByName(SqlDataReader r)
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

        private static bool HasColumn(SqlDataReader r, string name)
        {
            for (var i = 0; i < r.FieldCount; i++)
                if (string.Equals(r.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string GetString(SqlDataReader r, string name)
        {
            var o = r.GetOrdinal(name);
            return r.IsDBNull(o) ? string.Empty : r.GetValue(o).ToString() ?? string.Empty;
        }

        private static string? GetNullableStringByName(SqlDataReader r, string name)
        {
            if (!HasColumn(r, name)) return null;
            var o = r.GetOrdinal(name);
            return r.IsDBNull(o) ? null : r.GetValue(o).ToString();
        }

        private static int GetInt(SqlDataReader r, string name)
        {
            var o = r.GetOrdinal(name);
            return r.IsDBNull(o) ? 0 : Convert.ToInt32(r.GetValue(o));
        }

        private static decimal GetDecimal(SqlDataReader r, string name)
        {
            var o = r.GetOrdinal(name);
            return r.IsDBNull(o) ? 0m : Convert.ToDecimal(r.GetValue(o));
        }
    }
}
