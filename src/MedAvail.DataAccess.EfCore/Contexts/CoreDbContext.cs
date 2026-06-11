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

    // Schema-conversion coverage entities (10 tables + 2 views).
    public DbSet<MedcenterCore> Medcenters => Set<MedcenterCore>();
    public DbSet<ResourceBinaryCore> ResourceBinaries => Set<ResourceBinaryCore>();
    public DbSet<WorkflowProfileCore> WorkflowProfiles => Set<WorkflowProfileCore>();
    public DbSet<UserCore> Users => Set<UserCore>();
    public DbSet<KioskCore> Kiosks => Set<KioskCore>();
    public DbSet<PacardCore> Pacards => Set<PacardCore>();
    public DbSet<LegalDocumentCore> LegalDocuments => Set<LegalDocumentCore>();
    public DbSet<LanguageCore> Languages => Set<LanguageCore>();
    public DbSet<PartCore> Parts => Set<PartCore>();
    public DbSet<PatientProfileCore> PatientProfiles => Set<PatientProfileCore>();
    public DbSet<AllDuplicatePartsCore> AllDuplicateParts => Set<AllDuplicatePartsCore>();
    public DbSet<BinStatsCore> BinStats => Set<BinStatsCore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PackageIdMapEntity>().HasNoKey().ToTable("package_id_map");

        // Views are keyless (already annotated [Keyless], reaffirm the table map).
        modelBuilder.Entity<AllDuplicatePartsCore>().HasNoKey().ToView("AllDuplicateParts");
        modelBuilder.Entity<BinStatsCore>().HasNoKey().ToView("bin_stats");

        base.OnModelCreating(modelBuilder);
    }
}
