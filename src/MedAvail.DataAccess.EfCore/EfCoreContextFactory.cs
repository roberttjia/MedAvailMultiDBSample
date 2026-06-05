using System;
using Microsoft.EntityFrameworkCore;
using MedAvail.Common;
using MedAvail.DataAccess.EfCore.Contexts;

namespace MedAvail.DataAccess.EfCore;

/// <summary>
/// Creates EF Core contexts, each wired to the correct database's connection
/// string via the shared <see cref="IConnectionStringProvider"/>. This is the
/// single point that binds an EF Core context to a database.
/// </summary>
public sealed class EfCoreContextFactory
{
    private readonly IConnectionStringProvider _connections;

    public EfCoreContextFactory(IConnectionStringProvider connections)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
    }

    public PackageManagementDbContext CreatePackageManagement()
    {
        var options = new DbContextOptionsBuilder<PackageManagementDbContext>()
            .UseSqlServer(_connections.GetConnectionString(MedAvailDatabase.PackageManagement))
            .Options;
        return new PackageManagementDbContext(options);
    }

    public CoreDbContext CreateCore()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseSqlServer(_connections.GetConnectionString(MedAvailDatabase.Core))
            .Options;
        return new CoreDbContext(options);
    }

    public DataAcquisitionDbContext CreateDataAcquisition()
    {
        var options = new DbContextOptionsBuilder<DataAcquisitionDbContext>()
            .UseSqlServer(_connections.GetConnectionString(MedAvailDatabase.DataAcquisition))
            .Options;
        return new DataAcquisitionDbContext(options);
    }
}
