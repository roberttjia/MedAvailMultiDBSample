using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MedAvail.Common;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;
using MedAvail.DataAccess.EfCore.Contexts;
using MedAvail.DataAccess.EfCore.Entities;

namespace MedAvail.DataAccess.EfCore.Repositories;

/// <summary>
/// EF Core implementation of IPackageIdMapRepository. package_id_map exists in
/// Core, DataAcquisition and PackageManagement; this repository is bound to one
/// of them and resolves the matching DbContext. Reads use LINQ; writes use
/// parameterized ExecuteSql (the table is keyless).
/// </summary>
public sealed class EfCorePackageIdMapRepository : IPackageIdMapRepository
{
    private readonly EfCoreContextFactory _factory;

    public EfCorePackageIdMapRepository(EfCoreContextFactory factory, MedAvailDatabase database)
    {
        _factory = factory;
        Database = database;
    }

    public MedAvailDatabase Database { get; }

    /// <summary>Creates the DbContext for this repository's bound database.</summary>
    private DbContext CreateContext() => Database switch
    {
        MedAvailDatabase.Core => _factory.CreateCore(),
        MedAvailDatabase.DataAcquisition => _factory.CreateDataAcquisition(),
        MedAvailDatabase.PackageManagement => _factory.CreatePackageManagement(),
        _ => throw new NotSupportedException($"package_id_map does not exist in {Database}.")
    };

    private static DbSet<PackageIdMapEntity> Maps(DbContext ctx) => ctx switch
    {
        CoreDbContext c => c.PackageIdMaps,
        DataAcquisitionDbContext d => d.PackageIdMaps,
        PackageManagementDbContext p => p.PackageIdMaps,
        _ => throw new NotSupportedException()
    };

    public IReadOnlyList<PackageIdMapDto> GetAll()
    {
        using var ctx = CreateContext();
        return Maps(ctx).AsNoTracking().OrderBy(x => x.PackageId).ToList().Select(ToDto).ToList();
    }

    public PackageIdMapDto? GetByPackageId(int packageId)
    {
        using var ctx = CreateContext();
        var entity = Maps(ctx).AsNoTracking().FirstOrDefault(x => x.PackageId == packageId);
        return entity is null ? null : ToDto(entity);
    }

    public int CountByPackageId(int packageId)
    {
        using var ctx = CreateContext();
        return Maps(ctx).Count(x => x.PackageId == packageId);
    }

    public int Insert(PackageIdMapDto row)
    {
        using var ctx = CreateContext();
        return ctx.Database.ExecuteSqlInterpolated(
            $"INSERT INTO dbo.package_id_map (package_id, package_guid) VALUES ({row.PackageId}, {row.PackageGuid})");
    }

    public int UpdateGuid(int packageId, string? newGuid)
    {
        using var ctx = CreateContext();
        return ctx.Database.ExecuteSqlInterpolated(
            $"UPDATE dbo.package_id_map SET package_guid = {newGuid} WHERE package_id = {packageId}");
    }

    public int Delete(int packageId)
    {
        using var ctx = CreateContext();
        return ctx.Database.ExecuteSqlInterpolated(
            $"DELETE FROM dbo.package_id_map WHERE package_id = {packageId}");
    }

    private static PackageIdMapDto ToDto(PackageIdMapEntity e) =>
        new() { PackageId = e.PackageId, PackageGuid = e.PackageGuid };
}
