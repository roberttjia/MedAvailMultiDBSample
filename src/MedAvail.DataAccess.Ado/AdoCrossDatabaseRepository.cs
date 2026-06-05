using System;
using System.Data;
using Microsoft.Data.SqlClient;
using MedAvail.Common;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>
    /// Exercises SQL Server cross-database access from a single connection:
    ///   - a three-part-name query (MedAvailDB -> PackageManagementDb), and
    ///   - a stored procedure (MedAvailDB.dbo.RemoveMedCenter) that internally
    ///     EXECs MedAvailPackageManagementDB.dbo.PermanentlyDeleteAllPackagesForMedCenter.
    ///
    /// This is the "same server, multiple databases" topology the schema-conversion
    /// team targets (later as multiple schemas in one Postgres database).
    /// Connected to MedAvailDB.
    /// </summary>
    public sealed class AdoCrossDatabaseRepository : AdoRepositoryBase
    {
        public AdoCrossDatabaseRepository(IConnectionStringProvider connections)
            : base(connections, MedAvailDatabase.Core) { }

        /// <summary>
        /// Runs a read-only query that references a table in another database via
        /// a three-part name, while connected to MedAvailDB. Returns the row count
        /// it could read (proves cross-database name resolution works).
        /// </summary>
        public int CountPackageIdMapInPackageManagement()
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT COUNT(*) FROM MedAvailPackageManagementDb.dbo.package_id_map;";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Invokes dbo.RemoveMedCenter inside a transaction that is ALWAYS rolled
        /// back, so the cross-database stored-procedure path executes but nothing
        /// is persisted. Use a non-existent serial number so no rows match even
        /// before the rollback. Returns true if the proc executed without error.
        /// </summary>
        public bool InvokeRemoveMedCenterInRollback(string nonExistentSerial)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.RemoveMedCenter";
                cmd.CommandTimeout = 60;
                cmd.Parameters.Add(Param("@MedCenterSerialNumber", SqlDbType.NVarChar, nonExistentSerial));
                cmd.Parameters.Add(Param("@UserName", SqlDbType.NVarChar, "xdb-test"));
                cmd.Parameters.Add(Param("@TicketNumber", SqlDbType.NVarChar, "xdb-test"));

                // RemoveMedCenter emits result sets (PRINT + possible SELECTs); drain them.
                using (var reader = cmd.ExecuteReader())
                {
                    do { while (reader.Read()) { /* drain */ } } while (reader.NextResult());
                }

                return true;
            }
            finally
            {
                // Always roll back — this is a test of the code path, not a real delete.
                tx.Rollback();
            }
        }

        /// <summary>Generates a serial number that will not match any med center.</summary>
        public static string NewNonExistentSerial() => $"XDBTEST-{Guid.NewGuid():N}".Substring(0, 24);
    }
}
