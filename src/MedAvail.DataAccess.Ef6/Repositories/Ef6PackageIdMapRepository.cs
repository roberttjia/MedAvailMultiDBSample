using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using MedAvail.Common;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;

namespace MedAvail.DataAccess.Ef6.Repositories
{
    /// <summary>
    /// EF6 implementation of IPackageIdMapRepository against
    /// MedAvailPackageManagementDb. Reads use LINQ; writes use parameterized
    /// ExecuteSqlCommand (the table is effectively keyless).
    /// </summary>
    public sealed class Ef6PackageIdMapRepository : IPackageIdMapRepository
    {
        private readonly Ef6ContextFactory _factory;

        public Ef6PackageIdMapRepository(Ef6ContextFactory factory) => _factory = factory;

        public MedAvailDatabase Database => MedAvailDatabase.PackageManagement;

        public IReadOnlyList<PackageIdMapDto> GetAll()
        {
            using var ctx = _factory.CreatePackageManagement();
            return ctx.PackageIdMaps.AsNoTracking()
                .OrderBy(x => x.PackageId)
                .ToList()
                .Select(e => new PackageIdMapDto { PackageId = e.PackageId, PackageGuid = e.PackageGuid })
                .ToList();
        }

        public PackageIdMapDto? GetByPackageId(int packageId)
        {
            using var ctx = _factory.CreatePackageManagement();
            var e = ctx.PackageIdMaps.AsNoTracking().FirstOrDefault(x => x.PackageId == packageId);
            return e is null ? null : new PackageIdMapDto { PackageId = e.PackageId, PackageGuid = e.PackageGuid };
        }

        public int CountByPackageId(int packageId)
        {
            using var ctx = _factory.CreatePackageManagement();
            return ctx.PackageIdMaps.Count(x => x.PackageId == packageId);
        }

        public int Insert(PackageIdMapDto row)
        {
            using var ctx = _factory.CreatePackageManagement();
            return ctx.Database.ExecuteSqlCommand(
                "INSERT INTO dbo.package_id_map (package_id, package_guid) VALUES (@p0, @p1)",
                new SqlParameter("@p0", row.PackageId),
                new SqlParameter("@p1", (object?)row.PackageGuid ?? System.DBNull.Value));
        }

        public int UpdateGuid(int packageId, string? newGuid)
        {
            using var ctx = _factory.CreatePackageManagement();
            return ctx.Database.ExecuteSqlCommand(
                "UPDATE dbo.package_id_map SET package_guid = @p0 WHERE package_id = @p1",
                new SqlParameter("@p0", (object?)newGuid ?? System.DBNull.Value),
                new SqlParameter("@p1", packageId));
        }

        public int Delete(int packageId)
        {
            using var ctx = _factory.CreatePackageManagement();
            return ctx.Database.ExecuteSqlCommand(
                "DELETE FROM dbo.package_id_map WHERE package_id = @p0",
                new SqlParameter("@p0", packageId));
        }
    }
}
