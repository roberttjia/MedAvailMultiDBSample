using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using MedAvail.Common;

namespace MedAvail.DataAccess.Ef6.Repositories
{
    /// <summary>
    /// EF6 invocation of the result-set-returning proc
    /// dbo.GenerateMockPackageMovementXML (MedAvailDB / Core). EF6 runs a
    /// result-returning proc via Database.SqlQuery&lt;T&gt;; the proc returns a
    /// single (XML) column, read here as string.
    /// </summary>
    public sealed class Ef6ResultSetProcRepository
    {
        private readonly Ef6ContextFactory _factory;

        public Ef6ResultSetProcRepository(Ef6ContextFactory factory) => _factory = factory;

        public StoredProcResultInfo GenerateMockPackageMovement(string medCenterSerialNumber,
            int packagesPerLoadSlot = 1)
        {
            using var ctx = _factory.CreateCore();

            var rows = ctx.Database
                .SqlQuery<string>(
                    "EXEC dbo.GenerateMockPackageMovementXML @p0, @p1",
                    new SqlParameter("@p0", medCenterSerialNumber),
                    new SqlParameter("@p1", packagesPerLoadSlot))
                .ToList();

            return new StoredProcResultInfo
            {
                ReturnedResultSet = true,
                FieldCount = 1,
                RowCount = rows.Count
            };
        }
    }
}
