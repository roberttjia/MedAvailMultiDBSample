using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MedAvail.Common;

namespace MedAvail.DataAccess.EfCore.Repositories;

/// <summary>
/// EF Core invocation of the result-set-returning proc
/// dbo.GenerateMockPackageMovementXML (MedAvailDB / Core). EF Core 8 runs a
/// result-returning proc via Database.SqlQueryRaw&lt;T&gt;; the proc returns a
/// single (XML) column, so we read it as string?.
/// </summary>
public sealed class EfCoreResultSetProcRepository
{
    private readonly EfCoreContextFactory _factory;

    public EfCoreResultSetProcRepository(EfCoreContextFactory factory) => _factory = factory;

    public StoredProcResultInfo GenerateMockPackageMovement(string medCenterSerialNumber,
        int packagesPerLoadSlot = 1)
    {
        using var ctx = _factory.CreateCore();

        // EXEC the proc; the single FOR XML column comes back as the query value.
        var rows = ctx.Database
            .SqlQueryRaw<string?>(
                "EXEC dbo.GenerateMockPackageMovementXML @p0, @p1",
                new SqlParameter("@p0", medCenterSerialNumber),
                new SqlParameter("@p1", packagesPerLoadSlot))
            .ToList();

        return new StoredProcResultInfo
        {
            ReturnedResultSet = true,
            FieldCount = 1,            // single FOR XML column
            RowCount = rows.Count
        };
    }
}
