using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using MedAvail.Common;

namespace MedAvail.DataAccess.EfCore.Repositories;

/// <summary>
/// EF Core invocation of the result-set-returning proc
/// generatemockpackagemovementxml (MedAvailDB / Core). The PostgreSQL target is
/// a PROCEDURE with an INOUT REFCURSOR parameter, so we use raw ADO.NET via
/// ctx.Database.GetDbConnection() with CALL + FETCH ALL inside a transaction.
/// </summary>
public sealed class EfCoreResultSetProcRepository
{
    private readonly EfCoreContextFactory _factory;

    public EfCoreResultSetProcRepository(EfCoreContextFactory factory) => _factory = factory;

    public StoredProcResultInfo GenerateMockPackageMovement(string medCenterSerialNumber,
        int packagesPerLoadSlot = 1)
    {
        using var ctx = _factory.CreateCore();
        var npgsqlConn = (NpgsqlConnection)ctx.Database.GetDbConnection();
        npgsqlConn.Open();
        using var tx = npgsqlConn.BeginTransaction();

        using var cmd = npgsqlConn.CreateCommand();
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

        using var fetchCmd = npgsqlConn.CreateCommand();
        fetchCmd.Transaction = tx;
        fetchCmd.CommandText = "FETCH ALL FROM result_cursor";
        using var reader = fetchCmd.ExecuteReader();
        var info = new StoredProcResultInfo { ReturnedResultSet = true, FieldCount = reader.FieldCount };
        while (reader.Read()) info.RowCount++;
        tx.Commit();
        return info;
    }
}
