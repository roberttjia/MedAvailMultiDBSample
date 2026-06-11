using System.Data.Common;
using System.Data.Entity;
using MedAvail.DataAccess.Ef6.Entities;

namespace MedAvail.DataAccess.Ef6.Contexts
{
    /// <summary>
    /// EF6 context bound to MedAvailDB (the primary "Core" database). Maps a
    /// representative set of tables and the two views for schema-conversion
    /// coverage. Database-first; query-only.
    /// </summary>
    [DbConfigurationType(typeof(Ef6SqlServerConfiguration))]
    public class CoreEf6Context : DbContext
    {
        public CoreEf6Context(DbConnection connection, bool contextOwnsConnection)
            : base(connection, contextOwnsConnection) { }

        public DbSet<MedcenterEf6> Medcenters { get; set; } = null!;
        public DbSet<ResourceBinaryEf6> ResourceBinaries { get; set; } = null!;
        public DbSet<WorkflowProfileEf6> WorkflowProfiles { get; set; } = null!;
        public DbSet<UserEf6> Users { get; set; } = null!;
        public DbSet<KioskEf6> Kiosks { get; set; } = null!;
        public DbSet<PacardEf6> Pacards { get; set; } = null!;
        public DbSet<LegalDocumentEf6> LegalDocuments { get; set; } = null!;
        public DbSet<LanguageEf6> Languages { get; set; } = null!;
        public DbSet<PartEf6> Parts { get; set; } = null!;
        public DbSet<PatientProfileEf6> PatientProfiles { get; set; } = null!;

        public DbSet<AllDuplicatePartsEf6> AllDuplicateParts { get; set; } = null!;
        public DbSet<BinStatsEf6> BinStats { get; set; } = null!;

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer<CoreEf6Context>(null);
            base.OnModelCreating(modelBuilder);
        }
    }
}
