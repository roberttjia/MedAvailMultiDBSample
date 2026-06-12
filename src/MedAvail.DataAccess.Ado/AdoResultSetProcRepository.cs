using System;
using System.Data;
using Microsoft.Data.SqlClient;
using MedAvail.Common;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>
    /// ADO.NET invocation of a result-set-returning stored procedure in MedAvailDB
    /// (Core). dbo.GenerateMockPackageMovementXML returns a single result set (one
    /// FOR XML column) and is read-only (uses table variables), so it is safe to
    /// call repeatedly against a shared database.
    ///
    /// This is the SQL Server "result set" calling convention; the migration tool
    /// converts such procs to PostgreSQL procedures with a refcursor OUT param.
    /// </summary>
    public sealed class AdoResultSetProcRepository : AdoRepositoryBase
    {
        public AdoResultSetProcRepository(IConnectionStringProvider connections)
            : base(connections, MedAvailDatabase.Core) { }

        public StoredProcResultInfo GenerateMockPackageMovement(string medCenterSerialNumber,
            int packagesPerLoadSlot = 1)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.GenerateMockPackageMovementXML";
            cmd.CommandTimeout = 60;
            cmd.Parameters.Add(Param("@MedCenterSerialNumber", SqlDbType.VarChar, medCenterSerialNumber));
            cmd.Parameters.Add(Param("@NumberOfPackagesPerLoadSlot", SqlDbType.Int, packagesPerLoadSlot));

            using var reader = cmd.ExecuteReader();
            var info = new StoredProcResultInfo
            {
                ReturnedResultSet = true,     // ExecuteReader always yields a reader
                FieldCount = reader.FieldCount
            };
            while (reader.Read()) info.RowCount++;
            return info;
        }

        /// <summary>
        /// Same proc, but materialized into a DataTable via SqlDataAdapter.Fill
        /// (the classic disconnected ADO.NET pattern) instead of a SqlDataReader.
        /// </summary>
        public StoredProcResultInfo GenerateMockPackageMovementDataTable(string medCenterSerialNumber,
            int packagesPerLoadSlot = 1)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "dbo.GenerateMockPackageMovementXML";
            cmd.CommandTimeout = 60;
            cmd.Parameters.Add(Param("@MedCenterSerialNumber", SqlDbType.VarChar, medCenterSerialNumber));
            cmd.Parameters.Add(Param("@NumberOfPackagesPerLoadSlot", SqlDbType.Int, packagesPerLoadSlot));

            var table = new DataTable();
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(table);

            return new StoredProcResultInfo
            {
                ReturnedResultSet = true,     // Fill always produces a table
                FieldCount = table.Columns.Count,
                RowCount = table.Rows.Count
            };
        }

        /// <summary>
        /// Calls the proc via raw SQL (EXEC text) rather than
        /// CommandType.StoredProcedure. On SQL Server this returns the result set
        /// directly. On PostgreSQL the converted procedure uses a REFCURSOR OUT
        /// parameter, and because this call does NOT declare itself as a stored
        /// procedure, Npgsql will not auto-dereference the cursor — the migrated
        /// code must wrap the call in a transaction and FETCH from the refcursor
        /// explicitly. This mirrors legacy code that builds SQL text or uses
        /// helpers that never set CommandType.StoredProcedure.
        /// </summary>
        public StoredProcResultInfo GenerateMockPackageMovementExec(string medCenterSerialNumber,
            int packagesPerLoadSlot = 1)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            // Note: CommandType stays Text (the default) — deliberately not StoredProcedure.
            cmd.CommandText =
                "EXEC dbo.GenerateMockPackageMovementXML @MedCenterSerialNumber, @NumberOfPackagesPerLoadSlot;";
            cmd.CommandTimeout = 60;
            cmd.Parameters.Add(Param("@MedCenterSerialNumber", SqlDbType.VarChar, medCenterSerialNumber));
            cmd.Parameters.Add(Param("@NumberOfPackagesPerLoadSlot", SqlDbType.Int, packagesPerLoadSlot));

            using var reader = cmd.ExecuteReader();
            var info = new StoredProcResultInfo
            {
                ReturnedResultSet = reader.HasRows,
                FieldCount = reader.FieldCount
            };
            while (reader.Read()) info.RowCount++;
            return info;
        }

        /// <summary>Returns any existing med center serial number, or null if none.</summary>
        public string? GetAnyMedCenterSerial()
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TOP 1 serial_number FROM dbo.medcenter WHERE serial_number IS NOT NULL;";
            var result = cmd.ExecuteScalar();
            return result is null or DBNull ? null : result.ToString();
        }
    }
}
