using System.Linq;
using MedAvail.Common;
using MedAvail.DataAccess.Ado;
using MedAvail.TestRunner.Harness;

namespace MedAvail.TestRunner.Tests;

/// <summary>
/// Schema-conversion coverage for the primary database (MedAvailDB / Core) using
/// ADO.NET. Each test probes one table or view: it runs a read, pulls the column
/// schema the live database reports, and asserts the object exists, is queryable,
/// and exposes columns. This is the behavior that must survive a SQL Server ->
/// PostgreSQL schema + code migration.
///
/// Read-only and dependency-free: no seeding, works whether or not the object has
/// rows. Tables/views were chosen for column-type diversity (bit, datetime,
/// decimal, varbinary, int families, etc.).
/// </summary>
public sealed class AdoCoreCoverageTests
{
    private readonly IConnectionStringProvider _connections;

    public AdoCoreCoverageTests(IConnectionStringProvider connections) => _connections = connections;

    // 10 tables + 2 views in MedAvailDB, with an expected column known to exist.
    private static readonly (string Name, string Kind, string ExpectedColumn)[] Objects =
    {
        ("medcenter",        "table", "medcenter_id"),
        ("ResourceBinary",   "table", "resourceInfo_id"),
        ("workflow_profile", "table", "workflow_type"),
        ("user",             "table", "user_id"),
        ("kiosk",            "table", "name"),
        ("pacard",           "table", "pacard_id"),
        ("legal_document",   "table", "legal_document_id"),
        ("language",         "table", "language_id"),
        ("part",             "table", "part_id"),
        ("PatientProfile",   "table", "ExternalPatientId"),
        ("AllDuplicateParts","view",  "part_id"),
        ("bin_stats",        "view",  "bin_definition_id"),
    };

    public void RegisterAll(TestRunnerHarness harness)
    {
        var probe = new AdoSchemaProbeRepository(_connections, MedAvailDatabase.Core);

        foreach (var (name, kind, expectedColumn) in Objects)
        {
            harness.Run($"Probe {kind} {name}", "CoreCoverage", "Ado", () =>
            {
                var result = probe.ProbeWithCount(name, sampleSize: 5);

                Assert.True(result.Columns.Count > 0,
                    $"{kind} '{name}' should expose at least one column");

                Assert.True(
                    result.Columns.Any(c => string.Equals(c.Name, expectedColumn, System.StringComparison.OrdinalIgnoreCase)),
                    $"{kind} '{name}' should contain expected column '{expectedColumn}'");

                // Every probed column should report a SQL type name and a mapped CLR type.
                Assert.True(
                    result.Columns.All(c => !string.IsNullOrEmpty(c.DataTypeName) && c.ClrType != null),
                    $"all columns of '{name}' should report a data type");

                // Sample read should not exceed what we asked for, and not exceed total.
                Assert.True(result.SampleRowCount <= 5, "sample row count should respect TOP(5)");
                Assert.True(result.TotalRowCount >= result.SampleRowCount,
                    "total row count should be >= sampled rows");
            });
        }
    }
}
