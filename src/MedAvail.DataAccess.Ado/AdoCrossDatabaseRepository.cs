using System;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using MedAvail.Common;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>
    /// Exercises PostgreSQL cross-schema access from a single connection:
    ///   - a direct query against package_id_map (PostgreSQL has no cross-database
    ///     three-part-name queries; the table is accessed via SearchPath), and
    ///   - a stored procedure (removemedcenter) that is a plain PROCEDURE (no
    ///     REFCURSOR), called via CALL inside a transaction.
    ///
    /// Connected to MedAvailDB (Core).
    /// </summary>
    public sealed class AdoCrossDatabaseRepository : AdoRepositoryBase
    {
        public AdoCrossDatabaseRepository(IConnectionStringProvider connections)
            : base(connections, MedAvailDatabase.Core) { }

        /// <summary>
        /// Queries package_id_map directly (PostgreSQL does not support cross-database
        /// three-part-name queries; the SearchPath resolves the table). Returns the
        /// row count to confirm connectivity and table accessibility.
        /// </summary>
        public int CountPackageIdMapInPackageManagement()
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM package_id_map;";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Invokes removemedcenter inside a transaction that is ALWAYS rolled
        /// back, so the stored-procedure path executes but nothing is persisted.
        /// Use a non-existent serial number so no rows match even before the
        /// rollback. Returns true if the proc executed without error.
        /// </summary>
        public bool InvokeRemoveMedCenterInRollback(string nonExistentSerial)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "CALL removemedcenter(@p_medcenterserialnumber, @p_username, @p_ticketnumber)";
                cmd.CommandTimeout = 60;
                cmd.Parameters.AddWithValue("@p_medcenterserialnumber", nonExistentSerial);
                cmd.Parameters.AddWithValue("@p_username", "xdb-test");
                cmd.Parameters.AddWithValue("@p_ticketnumber", "xdb-test");
                cmd.ExecuteNonQuery();

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
