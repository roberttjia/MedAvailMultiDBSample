using System.Collections.Generic;
using System.Data;
using MedAvail.Common;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>
    /// ADO.NET access to dbo.package_id_map, bound to a specific database so the
    /// same table name in Core / DataAcquisition / PackageManagement is reached
    /// through separate, explicitly-targeted instances.
    /// </summary>
    public sealed class AdoPackageIdMapRepository : AdoRepositoryBase, IPackageIdMapRepository
    {
        public AdoPackageIdMapRepository(IConnectionStringProvider connections, MedAvailDatabase database)
            : base(connections, database) { }

        public MedAvailDatabase Database => TargetDatabase;

        public IReadOnlyList<PackageIdMapDto> GetAll()
        {
            var list = new List<PackageIdMapDto>();
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT package_id, package_guid FROM dbo.package_id_map ORDER BY package_id;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new PackageIdMapDto
                {
                    PackageId = reader.GetInt32(0),
                    PackageGuid = GetNullableString(reader, 1)
                });
            }
            return list;
        }

        public PackageIdMapDto? GetByPackageId(int packageId)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT TOP 1 package_id, package_guid FROM dbo.package_id_map WHERE package_id = @id;";
            cmd.Parameters.Add(Param("@id", SqlDbType.Int, packageId));
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            return new PackageIdMapDto
            {
                PackageId = reader.GetInt32(0),
                PackageGuid = GetNullableString(reader, 1)
            };
        }

        public int CountByPackageId(int packageId)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM dbo.package_id_map WHERE package_id = @id;";
            cmd.Parameters.Add(Param("@id", SqlDbType.Int, packageId));
            return (int)cmd.ExecuteScalar();
        }

        public int Insert(PackageIdMapDto row)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "INSERT INTO dbo.package_id_map (package_id, package_guid) VALUES (@id, @guid);";
            cmd.Parameters.Add(Param("@id", SqlDbType.Int, row.PackageId));
            cmd.Parameters.Add(Param("@guid", SqlDbType.NVarChar, row.PackageGuid));
            return cmd.ExecuteNonQuery();
        }

        public int UpdateGuid(int packageId, string? newGuid)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "UPDATE dbo.package_id_map SET package_guid = @guid WHERE package_id = @id;";
            cmd.Parameters.Add(Param("@guid", SqlDbType.NVarChar, newGuid));
            cmd.Parameters.Add(Param("@id", SqlDbType.Int, packageId));
            return cmd.ExecuteNonQuery();
        }

        public int Delete(int packageId)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM dbo.package_id_map WHERE package_id = @id;";
            cmd.Parameters.Add(Param("@id", SqlDbType.Int, packageId));
            return cmd.ExecuteNonQuery();
        }
    }
}
