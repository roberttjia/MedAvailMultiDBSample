using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using Npgsql;
using NpgsqlTypes;
using MedAvail.Common;

namespace MedAvail.DataAccess.Ef6.Repositories
{
    /// <summary>
    /// EF6 invocation of the result-set-returning proc
    /// generatemockpackagemovementxml (MedAvailDB / Core). The PostgreSQL target
    /// is a PROCEDURE with an INOUT REFCURSOR parameter, so we use raw ADO.NET
    /// via ctx.Database.Connection with CALL + FETCH ALL inside a transaction.
    /// </summary>
    public sealed class Ef6ResultSetProcRepository
    {
        private readonly Ef6ContextFactory _factory;

        public Ef6ResultSetProcRepository(Ef6ContextFactory factory) => _factory = factory;

        public StoredProcResultInfo GenerateMockPackageMovement(string medCenterSerialNumber,
            int packagesPerLoadSlot = 1)
        {
            using var ctx = _factory.CreateCore();
            ctx.Database.Connection.Open();
            using var tx = ctx.Database.Connection.BeginTransaction();
            using var cmd = (NpgsqlCommand)ctx.Database.Connection.CreateCommand();
            cmd.Transaction = (NpgsqlTransaction)tx;
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

            using var fetchCmd = (NpgsqlCommand)ctx.Database.Connection.CreateCommand();
            fetchCmd.Transaction = (NpgsqlTransaction)tx;
            fetchCmd.CommandText = "FETCH ALL FROM result_cursor";
            using var reader = fetchCmd.ExecuteReader();
            var rows = new List<string?>();
            while (reader.Read()) rows.Add(reader.IsDBNull(0) ? null : reader.GetString(0));
            tx.Commit();

            return new StoredProcResultInfo
            {
                ReturnedResultSet = true,
                FieldCount = 1,
                RowCount = rows.Count
            };
        }
    }
}
