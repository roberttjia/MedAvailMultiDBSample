using System;
using MedAvail.Common;
using MedAvail.DataAccess.Ado;
using MedAvail.DataAccess.EfCore;
using MedAvail.DataAccess.EfCore.Repositories;
using MedAvail.DataAccess.Ef6;
using MedAvail.DataAccess.Ef6.Repositories;
using MedAvail.TestRunner.Harness;

namespace MedAvail.TestRunner.Tests;

/// <summary>
/// Coverage for a result-set-returning stored procedure
/// (dbo.GenerateMockPackageMovementXML in MedAvailDB). In SQL Server the proc
/// returns a result set; the schema-migration tool converts such procs to a
/// PostgreSQL procedure with a refcursor OUT parameter — a calling-convention
/// change. These tests exercise the SQL Server form through all three
/// technologies so the post-migration equivalents can be compared.
///
/// The proc is read-only (table variables only) and returns a readable result
/// set even when the med center serial matches nothing, so it is safe to call
/// against a shared database.
/// </summary>
public sealed class ResultSetProcTests
{
    private readonly IConnectionStringProvider _connections;

    public ResultSetProcTests(IConnectionStringProvider connections) => _connections = connections;

    public void RegisterAll(TestRunnerHarness harness)
    {
        const string cat = "ResultSetProc";

        // Resolve a real serial if one exists; otherwise use a synthetic one
        // (the proc still returns an empty result set — fine for a signature test).
        var ado = new AdoResultSetProcRepository(_connections);
        var serial = ado.GetAnyMedCenterSerial() ?? "MOCK-SERIAL-0";

        harness.Run("GenerateMockPackageMovementXML returns result set", cat, "Ado", () =>
        {
            var info = ado.GenerateMockPackageMovement(serial);
            Assert.True(info.ReturnedResultSet, "ADO.NET should get a result set from the proc");
            Assert.True(info.FieldCount >= 1, "result set should expose at least one column");
            Assert.True(info.RowCount >= 0, "row count should be non-negative");
        });

        harness.Run("GenerateMockPackageMovementXML via DataTable", cat, "Ado", () =>
        {
            // Classic disconnected ADO.NET path: SqlDataAdapter.Fill -> DataTable.
            var info = ado.GenerateMockPackageMovementDataTable(serial);
            Assert.True(info.ReturnedResultSet, "DataTable fill should produce a table");
            Assert.True(info.FieldCount >= 1, "DataTable should expose at least one column");
            Assert.True(info.RowCount >= 0, "row count should be non-negative");
        });

        harness.Run("GenerateMockPackageMovementXML via raw EXEC text", cat, "Ado", () =>
        {
            // Raw EXEC (CommandType.Text), NOT CommandType.StoredProcedure. Forces
            // explicit REFCURSOR/transaction handling after PostgreSQL conversion.
            var info = ado.GenerateMockPackageMovementExec(serial);
            // ReturnedResultSet reflects HasRows, which may be false for an empty
            // result set (no matching med center) — both are valid here.
            Assert.True(info.FieldCount >= 1, "EXEC result should expose at least one column");
            Assert.True(info.RowCount >= 0, "row count should be non-negative");
        });

        harness.Run("GenerateMockPackageMovementXML returns result set", cat, "EfCore", () =>
        {
            var repo = new EfCoreResultSetProcRepository(new EfCoreContextFactory(_connections));
            var info = repo.GenerateMockPackageMovement(serial);
            Assert.True(info.ReturnedResultSet, "EF Core should get a result set from the proc");
            Assert.True(info.FieldCount >= 1, "result set should expose at least one column");
            Assert.True(info.RowCount >= 0, "row count should be non-negative");
        });

        harness.Run("GenerateMockPackageMovementXML returns result set", cat, "Ef6", () =>
        {
            var repo = new Ef6ResultSetProcRepository(new Ef6ContextFactory(_connections));
            var info = repo.GenerateMockPackageMovement(serial);
            Assert.True(info.ReturnedResultSet, "EF6 should get a result set from the proc");
            Assert.True(info.FieldCount >= 1, "result set should expose at least one column");
            Assert.True(info.RowCount >= 0, "row count should be non-negative");
        });
    }
}
