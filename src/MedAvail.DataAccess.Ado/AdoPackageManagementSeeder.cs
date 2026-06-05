using System;
using System.Data;
using Microsoft.Data.SqlClient;
using MedAvail.Common;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>
    /// Idempotently ensures the minimal lookup data exists in
    /// MedAvailPackageManagementDb so FK-dependent inserts (package_definition)
    /// and the container stored procedures have valid references.
    ///
    /// Lookup PKs are not identity columns, so we insert explicit ids. Every
    /// insert is guarded by NOT EXISTS, so seeding is safe to run repeatedly and
    /// never duplicates rows. Seed data is intentionally left in place.
    /// </summary>
    public sealed class AdoPackageManagementSeeder : AdoRepositoryBase
    {
        /// <summary>The fixed lookup id seeded into every lookup table.</summary>
        public const int SeedLookupId = 1;

        public AdoPackageManagementSeeder(IConnectionStringProvider connections)
            : base(connections, MedAvailDatabase.PackageManagement) { }

        /// <summary>Ensures all lookups needed for a package_definition insert exist.</summary>
        public void EnsurePackageDefinitionLookups()
        {
            using var conn = OpenConnection();

            EnsureLookup(conn, "lookup_code_absolute_location", "code_abs_location_id");
            EnsureLookup(conn, "lookup_code_relative_location", "code_rel_location_id");
            EnsureLookup(conn, "lookup_definition_state", "definition_state_id");
            EnsureLookup(conn, "lookup_expiration_method", "expiration_method_id");
            EnsureLookup(conn, "lookup_lot_code_source", "lotcode_source_id");
            EnsureLookup(conn, "lookup_package_shape", "shape_id");
            EnsureLookup(conn, "lookup_package_size_uom", "package_size_uom_id");
            EnsureLookup(conn, "lookup_product_category", "product_category_id");
            EnsureLookup(conn, "lookup_product_code_type", "product_code_type_id");
            EnsureDrugSchedule(conn);
        }

        /// <summary>Ensures the single lookup needed by the container procs exists.</summary>
        public void EnsureContainerLookups()
        {
            using var conn = OpenConnection();
            EnsureLookup(conn, "lookup_package_shape", "shape_id");
        }

        /// <summary>Returns the seeded shape id (after ensuring it exists).</summary>
        public int EnsureShapeId()
        {
            using var conn = OpenConnection();
            EnsureLookup(conn, "lookup_package_shape", "shape_id");
            return SeedLookupId;
        }

        private static void EnsureLookup(SqlConnection conn, string table, string idColumn)
        {
            // alias is the only other NOT NULL column on these lookups.
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                $"IF NOT EXISTS (SELECT 1 FROM dbo.[{table}] WHERE [{idColumn}] = @id) " +
                $"INSERT INTO dbo.[{table}] ([{idColumn}], [alias]) VALUES (@id, @alias);";
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = SeedLookupId });
            cmd.Parameters.Add(new SqlParameter("@alias", SqlDbType.VarChar, 50) { Value = "seed" });
            cmd.ExecuteNonQuery();
        }

        private static void EnsureDrugSchedule(SqlConnection conn)
        {
            // lookup_drug_schedule has controlled_substance bit NOT NULL (no description).
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "IF NOT EXISTS (SELECT 1 FROM dbo.lookup_drug_schedule WHERE drug_schedule_id = @id) " +
                "INSERT INTO dbo.lookup_drug_schedule (drug_schedule_id, alias, controlled_substance) " +
                "VALUES (@id, @alias, 0);";
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = SeedLookupId });
            cmd.Parameters.Add(new SqlParameter("@alias", SqlDbType.VarChar, 256) { Value = "seed" });
            cmd.ExecuteNonQuery();
        }
    }
}
