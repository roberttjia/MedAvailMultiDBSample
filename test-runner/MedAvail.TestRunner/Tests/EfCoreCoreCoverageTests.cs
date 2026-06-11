using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MedAvail.Common;
using MedAvail.DataAccess.EfCore;
using MedAvail.DataAccess.EfCore.Contexts;
using MedAvail.TestRunner.Harness;

namespace MedAvail.TestRunner.Tests;

/// <summary>
/// Schema-conversion coverage for MedAvailDB (Core) using EF Core 8. Each test
/// queries a mapped DbSet for one table or view, proving EF Core's database-first
/// mapping resolves the object and its columns against the (possibly migrated)
/// schema. Query-only; no seeding required. Mirrors the ADO.NET and EF6 sets.
/// </summary>
public sealed class EfCoreCoreCoverageTests
{
    private readonly EfCoreContextFactory _factory;

    public EfCoreCoreCoverageTests(IConnectionStringProvider connections)
    {
        _factory = new EfCoreContextFactory(connections);
    }

    public void RegisterAll(TestRunnerHarness harness)
    {
        Probe(harness, "table medcenter", ctx => ctx.Medcenters.OrderBy(x => x.MedcenterId).Take(1).Count());
        Probe(harness, "table ResourceBinary", ctx => ctx.ResourceBinaries.OrderBy(x => x.ResourceInfoId).Take(1).Count());
        Probe(harness, "table workflow_profile", ctx => ctx.WorkflowProfiles.OrderBy(x => x.WorkflowProfileId).Take(1).Count());
        Probe(harness, "table user", ctx => ctx.Users.OrderBy(x => x.UserId).Take(1).Count());
        Probe(harness, "table kiosk", ctx => ctx.Kiosks.OrderBy(x => x.KioskId).Take(1).Count());
        Probe(harness, "table pacard", ctx => ctx.Pacards.OrderBy(x => x.PacardId).Take(1).Count());
        Probe(harness, "table legal_document", ctx => ctx.LegalDocuments.OrderBy(x => x.LegalDocumentId).Take(1).Count());
        Probe(harness, "table language", ctx => ctx.Languages.OrderBy(x => x.LanguageId).Take(1).Count());
        Probe(harness, "table part", ctx => ctx.Parts.OrderBy(x => x.PartId).Take(1).Count());
        Probe(harness, "table PatientProfile", ctx => ctx.PatientProfiles.OrderBy(x => x.PatientProfileId).Take(1).Count());
        Probe(harness, "view AllDuplicateParts", ctx => ctx.AllDuplicateParts.Take(1).Count());
        Probe(harness, "view bin_stats", ctx => ctx.BinStats.Take(1).Count());
    }

    private void Probe(TestRunnerHarness harness, string label, Func<CoreDbContext, int> query)
    {
        harness.Run($"Probe {label}", "CoreCoverage", "EfCore", () =>
        {
            using var ctx = _factory.CreateCore();
            // Executing the query proves EF Core maps the entity and translates the
            // LINQ to SQL against the live schema. 0 or 1 rows are both valid.
            var count = query(ctx);
            Assert.True(count >= 0 && count <= 1, $"{label}: query should return 0 or 1 row");
        });
    }
}
