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

        // package_definition: match PostgreSQL precision/scale and map bool→numeric columns.
        modelBuilder.Entity<PackageDefinitionEntity>(e =>
        {
            e.Property(p => p.PackageHeight).HasColumnType("numeric(5,2)");
            e.Property(p => p.PackageWidth).HasColumnType("numeric(5,2)");
            e.Property(p => p.PackageLength).HasColumnType("numeric(5,2)");
            e.Property(p => p.CapDiameter).HasColumnType("numeric(5,2)");
            e.Property(p => p.CapLength).HasColumnType("numeric(5,2)");
            e.Property(p => p.Weight).HasColumnType("numeric(5,2)");
            e.Property(p => p.PackageSize).HasColumnType("numeric(12,3)");
            // cap and valid are NUMERIC in PostgreSQL but bool in C#; convert via int.
            e.Property(p => p.Cap).HasConversion<int>();
            e.Property(p => p.Valid).HasConversion<int>();
        });

        base.OnModelCreating(modelBuilder);
    }
}
