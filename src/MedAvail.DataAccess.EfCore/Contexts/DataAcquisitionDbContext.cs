using Microsoft.EntityFrameworkCore;
using MedAvail.DataAccess.EfCore.Entities;

namespace MedAvail.DataAccess.EfCore.Contexts;

/// <summary>
/// EF Core context bound to MedAvailDataAcquisitionDb. Like the other contexts it
/// maps its own copy of package_id_map; data must stay isolated from the Core and
/// PackageManagement copies.
/// </summary>
public class DataAcquisitionDbContext : DbContext
{
    public DataAcquisitionDbContext(DbContextOptions<DataAcquisitionDbContext> options) : base(options) { }

    public DbSet<PackageIdMapEntity> PackageIdMaps => Set<PackageIdMapEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PackageIdMapEntity>().HasNoKey().ToTable("package_id_map");
        base.OnModelCreating(modelBuilder);
    }
}
