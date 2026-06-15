using System;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using MedAvail.Common;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>
    /// Exercises PostgreSQL cross-schema access from a single connection:
    ///   - a local query against package_id_map (the three-part-name cross-database
    ///     query is not supported in PostgreSQL's single-database model; the table
    ///     exists in the local schema), and
    ///   - a stored procedure (medavaildb_dbo.removemedcenter) that is a PROCEDURE
    ///     with an INOUT REFCURSOR parameter.
    ///
    /// Connected to MedAvailDB.
    /// </summary>
    public sealed class AdoCrossDatabaseRepository : AdoRepositoryBase
    {
        public AdoCrossDatabaseRepository(IConnectionStringProvider connections)
            : base(connections, MedAvailDatabase.Core) { }

        /// <summary>
        /// Counts rows in the local package_id_map table.
        /// (The original three-part-name cross-database query is not supported in
        /// PostgreSQL's single-database model; the table exists in the local schema.)
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
        /// Use a non-existent serial number so no rows match even before the rollback.
        /// Returns true if the proc executed without error.
        /// </summary>
        public bool InvokeRemoveMedCenterInRollback(string nonExistentSerial)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                // CALL the procedure with the cursor name as the INOUT REFCURSOR parameter.
                using var callCmd = conn.CreateCommand();
                callCmd.Transaction = tx;
                callCmd.CommandText =
                    "CALL removemedcenter(@MedCenterSerialNumber, @UserName, @TicketNumber, @cursor)";
                callCmd.CommandTimeout = 60;
                callCmd.Parameters.Add(Param("@MedCenterSerialNumber", NpgsqlDbType.Varchar, nonExistentSerial));
                callCmd.Parameters.Add(Param("@UserName", NpgsqlDbType.Varchar, "xdb-test"));
                callCmd.Parameters.Add(Param("@TicketNumber", NpgsqlDbType.Varchar, "xdb-test"));
                callCmd.Parameters.Add(new NpgsqlParameter("@cursor", NpgsqlDbType.Refcursor) { Value = "result_cursor" });
                callCmd.ExecuteNonQuery();

                // Drain the cursor result set.
                using var fetchCmd = conn.CreateCommand();
                fetchCmd.Transaction = tx;
                fetchCmd.CommandText = "FETCH ALL FROM result_cursor";
                using var reader = fetchCmd.ExecuteReader();
                while (reader.Read()) { /* drain */ }

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
