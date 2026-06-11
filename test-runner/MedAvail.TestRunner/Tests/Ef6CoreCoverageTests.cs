using System;
using System.Data.Entity;
using System.Linq;
using MedAvail.Common;
using MedAvail.DataAccess.Ef6;
using MedAvail.TestRunner.Harness;

namespace MedAvail.TestRunner.Tests;

/// <summary>
/// Schema-conversion coverage for MedAvailDB (Core) using EF6 (EntityFramework
/// 6.5.x on .NET 8). Each test queries a mapped DbSet for one table or view,
/// proving EF6's database-first mapping resolves the object and its columns
/// against the (possibly migrated) schema. Query-only; no seeding required.
/// </summary>
public sealed class Ef6CoreCoverageTests
{
    private readonly Ef6ContextFactory _factory;

    public Ef6CoreCoverageTests(IConnectionStringProvider connections)
    {
        _factory = new Ef6ContextFactory(connections);
    }

    public void RegisterAll(TestRunnerHarness harness)
    {
        // Each lambda issues a TOP-style EF6 query and reads at most one row,
        // forcing EF6 to materialize the entity (exercises the column mapping).
        Probe(harness, "table medcenter", ctx => ctx.Medcenters.OrderBy(x => x.MedcenterId).Take(1).ToList().Count);
        Probe(harness, "table ResourceBinary", ctx => ctx.ResourceBinaries.OrderBy(x => x.ResourceInfoId).Take(1).ToList().Count);
        Probe(harness, "table workflow_profile", ctx => ctx.WorkflowProfiles.OrderBy(x => x.WorkflowProfileId).Take(1).ToList().Count);
        Probe(harness, "table user", ctx => ctx.Users.OrderBy(x => x.UserId).Take(1).ToList().Count);
        Probe(harness, "table kiosk", ctx => ctx.Kiosks.OrderBy(x => x.KioskId).Take(1).ToList().Count);
        Probe(harness, "table pacard", ctx => ctx.Pacards.OrderBy(x => x.PacardId).Take(1).ToList().Count);
        Probe(harness, "table legal_document", ctx => ctx.LegalDocuments.OrderBy(x => x.LegalDocumentId).Take(1).ToList().Count);
        Probe(harness, "table language", ctx => ctx.Languages.OrderBy(x => x.LanguageId).Take(1).ToList().Count);
        Probe(harness, "table part", ctx => ctx.Parts.OrderBy(x => x.PartId).Take(1).ToList().Count);
        Probe(harness, "table PatientProfile", ctx => ctx.PatientProfiles.OrderBy(x => x.PatientProfileId).Take(1).ToList().Count);
        Probe(harness, "view AllDuplicateParts", ctx => ctx.AllDuplicateParts.Take(1).ToList().Count);
        Probe(harness, "view bin_stats", ctx => ctx.BinStats.Take(1).ToList().Count);
    }

    private void Probe(TestRunnerHarness harness, string label,
        Func<MedAvail.DataAccess.Ef6.Contexts.CoreEf6Context, int> query)
    {
        harness.Run($"Probe {label}", "CoreCoverage", "Ef6", () =>
        {
            using var ctx = _factory.CreateCore();
            // Executing the query proves EF6 can map the entity and translate the
            // LINQ to SQL against the live schema. 0 or 1 rows are both valid.
            var count = query(ctx);
            Assert.True(count >= 0 && count <= 1, $"{label}: query should return 0 or 1 row");
        });
    }
}
