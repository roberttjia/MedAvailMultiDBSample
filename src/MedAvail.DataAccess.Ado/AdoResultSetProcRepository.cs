using System;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using MedAvail.Common;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>
    /// ADO.NET invocation of a result-set-returning stored procedure in MedAvailDB
    /// (Core). generatemockpackagemovementxml is a PostgreSQL PROCEDURE with an
    /// INOUT REFCURSOR parameter. All three calling patterns use CALL + FETCH ALL
    /// inside a transaction to dereference the cursor.
    /// </summary>
    public sealed class AdoResultSetProcRepository : AdoRepositoryBase
    {
        public AdoResultSetProcRepository(IConnectionStringProvider connections)
            : base(connections, MedAvailDatabase.Core) { }

        public StoredProcResultInfo GenerateMockPackageMovement(string medCenterSerialNumber,
            int packagesPerLoadSlot = 1)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "CALL generatemockpackagemovementxml(@p_medcenterserialnumber, @p_numberofpackagesperloadslot, @p_result_cursor)";
            cmd.CommandTimeout = 60;
            cmd.Parameters.Add(new NpgsqlParameter("@p_medcenterserialnumber", NpgsqlDbType.Citext) { Value = medCenterSerialNumber });
            cmd.Parameters.AddWithValue("@p_numberofpackagesperloadslot", packagesPerLoadSlot);
            cmd.Parameters.Add(new NpgsqlParameter("@p_result_cursor", NpgsqlDbType.Refcursor)
            {
                Direction = ParameterDirection.InputOutput,
                Value = "result_cursor"
            });
            cmd.ExecuteNonQuery();

            // Fetch from the cursor
            using var fetchCmd = conn.CreateCommand();
            fetchCmd.Transaction = tx;
            fetchCmd.CommandText = "FETCH ALL FROM result_cursor";
            using var reader = fetchCmd.ExecuteReader();
            var info = new StoredProcResultInfo
            {
                ReturnedResultSet = true,
                FieldCount = reader.FieldCount
            };
            while (reader.Read()) info.RowCount++;
            tx.Commit();
            return info;
        }

        /// <summary>
        /// Same proc, but materialized into a DataTable via NpgsqlDataAdapter.Fill
        /// (the classic disconnected ADO.NET pattern) instead of a NpgsqlDataReader.
        /// </summary>
        public StoredProcResultInfo GenerateMockPackageMovementDataTable(string medCenterSerialNumber,
            int packagesPerLoadSlot = 1)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "CALL generatemockpackagemovementxml(@p_medcenterserialnumber, @p_numberofpackagesperloadslot, @p_result_cursor)";
            cmd.CommandTimeout = 60;
            cmd.Parameters.Add(new NpgsqlParameter("@p_medcenterserialnumber", NpgsqlDbType.Citext) { Value = medCenterSerialNumber });
            cmd.Parameters.AddWithValue("@p_numberofpackagesperloadslot", packagesPerLoadSlot);
            cmd.Parameters.Add(new NpgsqlParameter("@p_result_cursor", NpgsqlDbType.Refcursor)
            {
                Direction = ParameterDirection.InputOutput,
                Value = "result_cursor"
            });
            cmd.ExecuteNonQuery();

            // Fetch from the cursor into a DataTable via NpgsqlDataAdapter
            using var fetchCmd = conn.CreateCommand();
            fetchCmd.Transaction = tx;
            fetchCmd.CommandText = "FETCH ALL FROM result_cursor";
            var table = new System.Data.DataTable();
            using var adapter = new NpgsqlDataAdapter(fetchCmd);
            adapter.Fill(table);
            tx.Commit();

            return new StoredProcResultInfo
            {
                ReturnedResultSet = true,
                FieldCount = table.Columns.Count,
                RowCount = table.Rows.Count
            };
        }

        /// <summary>
        /// Calls the proc via CALL text (CommandType stays Text — deliberately not
        /// StoredProcedure). Uses CALL + FETCH ALL inside a transaction to
        /// dereference the REFCURSOR returned by the PostgreSQL procedure.
        /// Note: the word "text" here refers to CommandType.Text (the ADO.NET enum
        /// value), not a procedure name — there is no procedure called "text".
        /// </summary>
        public StoredProcResultInfo GenerateMockPackageMovementExec(string medCenterSerialNumber,
            int packagesPerLoadSlot = 1)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            // Note: CommandType stays Text (the default) — deliberately not StoredProcedure.
            cmd.CommandText = "CALL generatemockpackagemovementxml(@p_medcenterserialnumber, @p_numberofpackagesperloadslot, @p_result_cursor)";
            cmd.CommandTimeout = 60;
            cmd.Parameters.Add(new NpgsqlParameter("@p_medcenterserialnumber", NpgsqlDbType.Citext) { Value = medCenterSerialNumber });
            cmd.Parameters.AddWithValue("@p_numberofpackagesperloadslot", packagesPerLoadSlot);
            cmd.Parameters.Add(new NpgsqlParameter("@p_result_cursor", NpgsqlDbType.Refcursor)
            {
                Direction = ParameterDirection.InputOutput,
                Value = "result_cursor"
            });
            cmd.ExecuteNonQuery();

            using var fetchCmd = conn.CreateCommand();
            fetchCmd.Transaction = tx;
            fetchCmd.CommandText = "FETCH ALL FROM result_cursor";
            using var reader = fetchCmd.ExecuteReader();
            var info = new StoredProcResultInfo
            {
                ReturnedResultSet = reader.HasRows,
                FieldCount = reader.FieldCount
            };
            while (reader.Read()) info.RowCount++;
            tx.Commit();
            return info;
        }

        /// <summary>Returns any existing med center serial number, or null if none.</summary>
        public string? GetAnyMedCenterSerial()
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT serial_number FROM medcenter WHERE serial_number IS NOT NULL LIMIT 1;";
            var result = cmd.ExecuteScalar();
            return result is null or DBNull ? null : result.ToString();
        }
    }
}
