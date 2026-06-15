using System;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using Microsoft.EntityFrameworkCore;
using MedAvail.Common;

namespace MedAvail.DataAccess.EfCore.Repositories;

/// <summary>
/// EF Core invocation of the result-set-returning procedure
/// medavaildb_dbo.generatemockpackagemovementxml (MedAvailDB / Core).
/// The PostgreSQL target is a PROCEDURE with an INOUT REFCURSOR parameter.
/// We open a transaction, CALL the procedure with a cursor name, then
/// FETCH ALL from the cursor to read the result rows.
/// </summary>
public sealed class EfCoreResultSetProcRepository
{
    private readonly EfCoreContextFactory _factory;

    public EfCoreResultSetProcRepository(EfCoreContextFactory factory) => _factory = factory;

    public StoredProcResultInfo GenerateMockPackageMovement(string medCenterSerialNumber,
        int packagesPerLoadSlot = 1)
    {
        using var ctx = _factory.CreateCore();
        var conn = (NpgsqlConnection)ctx.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        using var tx = conn.BeginTransaction();
        try
        {
            // CALL the procedure, passing the cursor name as the INOUT REFCURSOR parameter.
            using var callCmd = conn.CreateCommand();
            callCmd.Transaction = tx;
            callCmd.CommandText = "CALL medavaildb_dbo.generatemockpackagemovementxml(@p0, @p1, @cursor)";
            callCmd.Parameters.Add(new NpgsqlParameter("@p0", NpgsqlDbType.Varchar) { Value = medCenterSerialNumber });
            callCmd.Parameters.Add(new NpgsqlParameter("@p1", NpgsqlDbType.Integer) { Value = packagesPerLoadSlot });
            callCmd.Parameters.Add(new NpgsqlParameter("@cursor", NpgsqlDbType.Refcursor) { Value = "result_cursor" });
            callCmd.ExecuteNonQuery();

            // FETCH ALL from the cursor to read the result set.
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
