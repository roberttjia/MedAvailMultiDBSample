using System.Data.Common;
using System.Data.Entity;
using MedAvail.DataAccess.Ef6.Entities;

namespace MedAvail.DataAccess.Ef6.Contexts
{
    /// <summary>
    /// EF6 context bound to MedAvailPackageManagementDb. The provider is registered
    /// in code via Ef6SqlServerConfiguration. The context is constructed with an
    /// existing DbConnection (owned by this context) so the connection string is
    /// supplied by the shared connection-string provider.
    /// </summary>
    [DbConfigurationType(typeof(Ef6SqlServerConfiguration))]
    public class PackageManagementEf6Context : DbContext
    {
        public PackageManagementEf6Context(DbConnection connection, bool contextOwnsConnection)
            : base(connection, contextOwnsConnection) { }

        public DbSet<PackageDefinitionEf6> PackageDefinitions { get; set; } = null!;
        public DbSet<PackageIdMapEf6> PackageIdMaps { get; set; } = null!;

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // No EdmMetadata table / migrations history checks — database-first.
            Database.SetInitializer<PackageManagementEf6Context>(null);

            modelBuilder.Entity<PackageDefinitionEf6>()
                .Property(p => p.PackageSize).HasPrecision(12, 3);

            base.OnModelCreating(modelBuilder);
        }
    }
}
