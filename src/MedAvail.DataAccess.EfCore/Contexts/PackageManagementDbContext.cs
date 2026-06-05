using Microsoft.EntityFrameworkCore;
using MedAvail.DataAccess.EfCore.Entities;

namespace MedAvail.DataAccess.EfCore.Contexts;

/// <summary>
/// EF Core context bound to MedAvailPackageManagementDb. One context per database
/// — the entity-to-database association lives in the (context, connection) pair.
/// </summary>
public class PackageManagementDbContext : DbContext
{
    public PackageManagementDbContext(DbContextOptions<PackageManagementDbContext> options)
        : base(options) { }

    public DbSet<PackageDefinitionEntity> PackageDefinitions => Set<PackageDefinitionEntity>();
    public DbSet<PackageIdMapEntity> PackageIdMaps => Set<PackageIdMapEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // package_id_map is keyless (no PK in the DB); query-only via the model.
        modelBuilder.Entity<PackageIdMapEntity>().HasNoKey().ToTable("package_id_map");

        // package_definition decimals: match the SQL precision/scale to avoid
        // silent truncation warnings.
        modelBuilder.Entity<PackageDefinitionEntity>(e =>
        {
            e.Property(p => p.PackageHeight).HasColumnType("decimal(5,2)");
            e.Property(p => p.PackageWidth).HasColumnType("decimal(5,2)");
            e.Property(p => p.PackageLength).HasColumnType("decimal(5,2)");
            e.Property(p => p.CapDiameter).HasColumnType("decimal(5,2)");
            e.Property(p => p.CapLength).HasColumnType("decimal(5,2)");
            e.Property(p => p.Weight).HasColumnType("decimal(5,2)");
            e.Property(p => p.PackageSize).HasColumnType("decimal(12,3)");
        });

        base.OnModelCreating(modelBuilder);
    }
}
