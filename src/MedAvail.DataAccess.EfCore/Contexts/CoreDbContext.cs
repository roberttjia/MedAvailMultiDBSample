using Microsoft.EntityFrameworkCore;
using MedAvail.DataAccess.EfCore.Entities;

namespace MedAvail.DataAccess.EfCore.Contexts;

/// <summary>
/// EF Core context bound to MedAvailDB (the "Core" database). Maps its own copy
/// of the package_id_map table — the same table name exists in three databases,
/// and each context must resolve to its own.
/// </summary>
public class CoreDbContext : DbContext
{
    public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options) { }

    public DbSet<PackageIdMapEntity> PackageIdMaps => Set<PackageIdMapEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PackageIdMapEntity>().HasNoKey().ToTable("package_id_map");
        base.OnModelCreating(modelBuilder);
    }
}
