using System;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using MedAvail.Common;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>
    /// ADO.NET invocation of a result-set-returning stored procedure in MedAvailDB
    /// (Core). medavaildb_dbo.generatemockpackagemovementxml is a PostgreSQL PROCEDURE
    /// with an INOUT REFCURSOR parameter. All three calling patterns open a transaction,
    /// CALL the procedure with a cursor name, then FETCH ALL from the cursor.
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
            try
            {
                using var callCmd = conn.CreateCommand();
                callCmd.Transaction = tx;
                callCmd.CommandText =
                    "CALL generatemockpackagemovementxml(@MedCenterSerialNumber, @NumberOfPackagesPerLoadSlot, @cursor)";
                callCmd.CommandTimeout = 60;
                callCmd.Parameters.Add(Param("@MedCenterSerialNumber", NpgsqlDbType.Varchar, medCenterSerialNumber));
                callCmd.Parameters.Add(Param("@NumberOfPackagesPerLoadSlot", NpgsqlDbType.Integer, packagesPerLoadSlot));
                callCmd.Parameters.Add(new NpgsqlParameter("@cursor", NpgsqlDbType.Refcursor) { Value = "result_cursor" });
                callCmd.ExecuteNonQuery();

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
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Same proc, but materialized manually via cursor fetch (replaces SqlDataAdapter.Fill).
        /// NpgsqlDataAdapter does not support stored procedures with REFCURSOR parameters.
        /// </summary>
        public StoredProcResultInfo GenerateMockPackageMovementDataTable(string medCenterSerialNumber,
            int packagesPerLoadSlot = 1)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using var callCmd = conn.CreateCommand();
                callCmd.Transaction = tx;
                callCmd.CommandText =
                    "CALL generatemockpackagemovementxml(@MedCenterSerialNumber, @NumberOfPackagesPerLoadSlot, @cursor)";
                callCmd.CommandTimeout = 60;
                callCmd.Parameters.Add(Param("@MedCenterSerialNumber", NpgsqlDbType.Varchar, medCenterSerialNumber));
                callCmd.Parameters.Add(Param("@NumberOfPackagesPerLoadSlot", NpgsqlDbType.Integer, packagesPerLoadSlot));
                callCmd.Parameters.Add(new NpgsqlParameter("@cursor", NpgsqlDbType.Refcursor) { Value = "result_cursor" });
                callCmd.ExecuteNonQuery();

                using var fetchCmd = conn.CreateCommand();
                fetchCmd.Transaction = tx;
                fetchCmd.CommandText = "FETCH ALL FROM result_cursor";
                using var reader = fetchCmd.ExecuteReader();

                var rowCount = 0;
                var fieldCount = reader.FieldCount;
                while (reader.Read()) rowCount++;

                tx.Commit();

                return new StoredProcResultInfo
                {
                    ReturnedResultSet = true,
                    FieldCount = fieldCount,
                    RowCount = rowCount
                };
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Calls the proc via explicit CALL text (mirrors the former EXEC text pattern).
        /// On PostgreSQL the procedure uses a REFCURSOR OUT parameter, so we must wrap
        /// the call in a transaction and FETCH from the cursor explicitly.
        /// </summary>
        public StoredProcResultInfo GenerateMockPackageMovementExec(string medCenterSerialNumber,
            int packagesPerLoadSlot = 1)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();
            try
            {
                using var callCmd = conn.CreateCommand();
                callCmd.Transaction = tx;
                callCmd.CommandText =
                    "CALL generatemockpackagemovementxml(@MedCenterSerialNumber, @NumberOfPackagesPerLoadSlot, @cursor)";
                callCmd.CommandTimeout = 60;
                callCmd.Parameters.Add(Param("@MedCenterSerialNumber", NpgsqlDbType.Varchar, medCenterSerialNumber));
                callCmd.Parameters.Add(Param("@NumberOfPackagesPerLoadSlot", NpgsqlDbType.Integer, packagesPerLoadSlot));
                callCmd.Parameters.Add(new NpgsqlParameter("@cursor", NpgsqlDbType.Refcursor) { Value = "result_cursor" });
                callCmd.ExecuteNonQuery();

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
            catch
            {
                tx.Rollback();
                throw;
            }
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
