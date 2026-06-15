using System;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using System.Linq;
using MedAvail.Common;

namespace MedAvail.DataAccess.Ef6.Repositories
{
    /// <summary>
    /// EF6 invocation of the result-set-returning procedure
    /// medavaildb_dbo.generatemockpackagemovementxml (MedAvailDB / Core).
    /// The PostgreSQL target is a PROCEDURE with an INOUT REFCURSOR parameter.
    /// We get the underlying NpgsqlConnection from the EF6 context, open a
    /// transaction, CALL the procedure, FETCH ALL from the cursor, count rows,
    /// then commit.
    /// </summary>
    public sealed class Ef6ResultSetProcRepository
    {
        private readonly Ef6ContextFactory _factory;

        public Ef6ResultSetProcRepository(Ef6ContextFactory factory) => _factory = factory;

        public StoredProcResultInfo GenerateMockPackageMovement(string medCenterSerialNumber,
            int packagesPerLoadSlot = 1)
        {
            using var ctx = _factory.CreateCore();

            var conn = (NpgsqlConnection)ctx.Database.Connection;
            if (conn.State != ConnectionState.Open)
                conn.Open();

            using var tx = conn.BeginTransaction();
            try
            {
                using var callCmd = conn.CreateCommand();
                callCmd.Transaction = tx;
                callCmd.CommandText =
                    "CALL generatemockpackagemovementxml(@p0, @p1, @cursor)";
                callCmd.Parameters.Add(new NpgsqlParameter("@p0", NpgsqlDbType.Varchar) { Value = medCenterSerialNumber });
                callCmd.Parameters.Add(new NpgsqlParameter("@p1", NpgsqlDbType.Integer) { Value = packagesPerLoadSlot });
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
                    FieldCount = fieldCount >= 1 ? fieldCount : 1,
                    RowCount = rowCount
                };
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
