using System;
using Microsoft.EntityFrameworkCore;
using MedAvail.Common;
using MedAvail.Common.Models;
using MedAvail.DataAccess.Ado;
using MedAvail.DataAccess.EfCore;
using MedAvail.DataAccess.EfCore.Entities;
using MedAvail.TestRunner.Harness;

namespace MedAvail.TestRunner.Tests;

/// <summary>
/// EF Core tests: CRUD, queries, keyless-entity access, and the rowversion-based
/// optimistic concurrency that EF Core enforces natively (DbUpdateConcurrencyException).
/// Self-seeding and re-runnable; created rows are left in place.
/// </summary>
public sealed class EfCoreTests
{
    private readonly EfCoreContextFactory _factory;
    private readonly AdoPackageManagementSeeder _seeder;
    private readonly Random _rng = new();

    public EfCoreTests(IConnectionStringProvider connections)
    {
        _factory = new EfCoreContextFactory(connections);
        _seeder = new AdoPackageManagementSeeder(connections);
    }

    public void RegisterAll(TestRunnerHarness harness)
    {
        PackageDefinitionCrud(harness);
        PackageDefinitionTrackingUpdate(harness);
        PackageDefinitionQueryShapes(harness);
        PackageIdMapKeyless(harness);
        PackageIdMapAssociationAcrossDatabases(harness);
        RowVersionConcurrency(harness);
    }

    // ---- package_definition CRUD + query via EF Core ----
    private void PackageDefinitionCrud(TestRunnerHarness harness)
    {
        const string cat = "PackageDefinition";
        var repo = new MedAvail.DataAccess.EfCore.Repositories.EfCorePackageDefinitionRepository(_factory);
        var id = 0;
        var marker = $"EFTEST-{Guid.NewGuid():N}".Substring(0, 18);

        harness.Run("Insert package_definition", cat, "EfCore", () =>
        {
            _seeder.EnsurePackageDefinitionLookups();
            var before = repo.GetMaxId();
            id = repo.Insert(BuildDefinition(marker));
            Assert.True(id > before, $"new identity ({id}) should exceed prior max ({before})");
        });

        harness.Run("GetById round-trips entity", cat, "EfCore", () =>
        {
            Assert.True(id > 0, "insert should have produced an id");
            var row = repo.GetById(id);
            Assert.True(row is not null, "inserted row should be retrievable");
            Assert.Equal(marker, row!.ProductName, "product_name should round-trip");
            Assert.True(row.AuditId is { Length: 8 }, "rowversion should be materialized as 8 bytes");
        });

        harness.Run("GetRecent ordered descending", cat, "EfCore", () =>
        {
            var recent = repo.GetRecent(10);
            Assert.True(recent.Count > 0, "should return at least the inserted row");
            for (var i = 1; i < recent.Count; i++)
                Assert.True(recent[i - 1].PackageDefinitionId >= recent[i].PackageDefinitionId,
                    "GetRecent should be ordered by id descending");
        });

        harness.Run("Delete package_definition", cat, "EfCore", () =>
        {
            Assert.True(id > 0, "insert should have produced an id");
            Assert.Equal(1, repo.Delete(id), "delete should remove the inserted row");
            Assert.True(repo.GetById(id) is null, "row should be gone after delete");
        });
    }

    // ---- keyless entity (package_id_map) via EF Core ----
    private void PackageIdMapKeyless(TestRunnerHarness harness)
    {
        const string cat = "PackageIdMap";
        var repo = new MedAvail.DataAccess.EfCore.Repositories.EfCorePackageIdMapRepository(
            _factory, MedAvailDatabase.PackageManagement);
        var packageId = 2_000_000_000 + _rng.Next(1, 90_000_000);

        harness.Run("Insert keyless row (ExecuteSql)", cat, "EfCore", () =>
        {
            Assert.Equal(1, repo.Insert(new PackageIdMapDto { PackageId = packageId, PackageGuid = Guid.NewGuid().ToString() }),
                "insert should affect one row");
        });

        harness.Run("Query keyless entity (LINQ)", cat, "EfCore", () =>
        {
            var row = repo.GetByPackageId(packageId);
            Assert.True(row is not null, "keyless row should be queryable via LINQ");
            Assert.Equal(packageId, row!.PackageId, "package_id should match");
            Assert.Equal(1, repo.CountByPackageId(packageId), "count should be 1");
        });

        harness.Run("Update + delete keyless row", cat, "EfCore", () =>
        {
            var g = Guid.NewGuid().ToString();
            Assert.Equal(1, repo.UpdateGuid(packageId, g), "update should affect one row");
            Assert.Equal(g, repo.GetByPackageId(packageId)!.PackageGuid, "guid should update");
            Assert.Equal(1, repo.Delete(packageId), "delete should remove the row");
        });
    }

    // ---- change-tracked update via EF Core ----
    private void PackageDefinitionTrackingUpdate(TestRunnerHarness harness)
    {
        const string cat = "PackageDefinition";
        var id = 0;

        harness.Run("Tracked update persists changes", cat, "EfCore", () =>
        {
            _seeder.EnsurePackageDefinitionLookups();

            // Insert via a tracked context.
            using (var ctx = _factory.CreatePackageManagement())
            {
                var e = BuildEntity($"EFUPD-{Guid.NewGuid():N}".Substring(0, 18));
                ctx.PackageDefinitions.Add(e);
                ctx.SaveChanges();
                id = e.PackageDefinitionId;
            }
            Assert.True(id > 0, "row should be inserted");

            // Load, mutate, save — change tracker should issue an UPDATE.
            using (var ctx = _factory.CreatePackageManagement())
            {
                var e = ctx.PackageDefinitions.First(p => p.PackageDefinitionId == id);
                e.ProductManufacturer = "Updated Mfr";
                e.Weight = 3.50m;
                var affected = ctx.SaveChanges();
                Assert.Equal(1, affected, "SaveChanges should update one row");
            }

            // Verify in a fresh context.
            using (var ctx = _factory.CreatePackageManagement())
            {
                var e = ctx.PackageDefinitions.AsNoTracking().First(p => p.PackageDefinitionId == id);
                Assert.Equal("Updated Mfr", e.ProductManufacturer, "manufacturer should persist");
                Assert.Equal(3.50m, e.Weight, "weight should persist");
            }
        });
    }

    // ---- LINQ query shapes via EF Core ----
    private void PackageDefinitionQueryShapes(TestRunnerHarness harness)
    {
        const string cat = "PackageDefinition";
        var marker = $"EFQRY-{Guid.NewGuid():N}".Substring(0, 18);

        harness.Run("Filtered query + projection", cat, "EfCore", () =>
        {
            _seeder.EnsurePackageDefinitionLookups();
            using (var ctx = _factory.CreatePackageManagement())
            {
                ctx.PackageDefinitions.Add(BuildEntity(marker));
                ctx.SaveChanges();
            }

            using var read = _factory.CreatePackageManagement();
            // WHERE + projection to an anonymous-free shape (just the id).
            var ids = read.PackageDefinitions.AsNoTracking()
                .Where(p => p.ProductName == marker)
                .Select(p => p.PackageDefinitionId)
                .ToList();
            Assert.Equal(1, ids.Count, "exactly one row should match the unique marker");
        });

        harness.Run("Count and Any predicates", cat, "EfCore", () =>
        {
            using var ctx = _factory.CreatePackageManagement();
            Assert.True(ctx.PackageDefinitions.Any(p => p.ProductName == marker),
                "Any() should find the seeded row");
            Assert.Equal(1, ctx.PackageDefinitions.Count(p => p.ProductName == marker),
                "Count() should be 1 for the unique marker");
        });
    }

    // ---- multi-database association/isolation: package_id_map in 3 databases ----
    // Same table name in Core, DataAcquisition, PackageManagement. Proves each
    // EF Core context binds to its own database and the data stays isolated.
    private void PackageIdMapAssociationAcrossDatabases(TestRunnerHarness harness)
    {
        const string cat = "Association";
        var Repo = (MedAvailDatabase db) =>
            new MedAvail.DataAccess.EfCore.Repositories.EfCorePackageIdMapRepository(_factory, db);

        var pm = Repo(MedAvailDatabase.PackageManagement);
        var core = Repo(MedAvailDatabase.Core);
        var daq = Repo(MedAvailDatabase.DataAcquisition);
        var packageId = 2_100_000_000 + _rng.Next(1, 40_000_000);

        harness.Run("Insert isolated to PackageManagement", cat, "EfCore", () =>
        {
            pm.Insert(new PackageIdMapDto { PackageId = packageId, PackageGuid = "pm-only" });
            Assert.Equal(1, pm.CountByPackageId(packageId), "row should exist in PackageManagement");
        });

        harness.Run("Row absent in Core database", cat, "EfCore", () =>
        {
            Assert.Equal(0, core.CountByPackageId(packageId),
                "the PackageManagement row must NOT appear in Core (separate database)");
        });

        harness.Run("Row absent in DataAcquisition database", cat, "EfCore", () =>
        {
            Assert.Equal(0, daq.CountByPackageId(packageId),
                "the PackageManagement row must NOT appear in DataAcquisition (separate database)");
        });

        harness.Run("Same id distinct per database", cat, "EfCore", () =>
        {
            // Insert the same package_id into Core with a different guid; confirm
            // each database holds its own value (no bleed-through).
            core.Insert(new PackageIdMapDto { PackageId = packageId, PackageGuid = "core-only" });
            Assert.Equal("pm-only", pm.GetByPackageId(packageId)!.PackageGuid,
                "PackageManagement should still read its own value");
            Assert.Equal("core-only", core.GetByPackageId(packageId)!.PackageGuid,
                "Core should read its own value");
            // cleanup the Core insert (PackageManagement row left in place like other tests)
            core.Delete(packageId);
        });
    }

    // ---- rowversion optimistic concurrency (EF Core native) ----
    private void RowVersionConcurrency(TestRunnerHarness harness)
    {
        const string cat = "TypeFidelity";
        var id = 0;

        harness.Run("EF Core throws on stale rowversion", cat, "EfCore", () =>
        {
            _seeder.EnsurePackageDefinitionLookups();

            // Insert a row to operate on.
            using (var ctx = _factory.CreatePackageManagement())
            {
                var entity = BuildEntity($"EFRV-{Guid.NewGuid():N}".Substring(0, 18));
                ctx.PackageDefinitions.Add(entity);
                ctx.SaveChanges();
                id = entity.PackageDefinitionId;
            }
            Assert.True(id > 0, "should have inserted a row");

            // Load the same row in two separate contexts (two copies, same rowversion).
            using var ctxA = _factory.CreatePackageManagement();
            using var ctxB = _factory.CreatePackageManagement();
            var copyA = ctxA.PackageDefinitions.First(p => p.PackageDefinitionId == id);
            var copyB = ctxB.PackageDefinitions.First(p => p.PackageDefinitionId == id);

            // First update succeeds and bumps the rowversion.
            copyA.Description = "ef-update-A";
            ctxA.SaveChanges();

            // Second update uses the now-stale rowversion -> EF Core detects the conflict.
            copyB.Description = "ef-update-B";
            var threw = false;
            try
            {
                ctxB.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                threw = true;
            }
            Assert.True(threw, "EF Core should throw DbUpdateConcurrencyException on a stale rowversion");
        });
    }

    // ---- helpers ----

    private static PackageDefinitionDto BuildDefinition(string marker) => new()
    {
        Description = marker,
        PackageCode = marker,
        PackageCodeAbsLocationId = AdoPackageManagementSeeder.SeedLookupId,
        Shape = AdoPackageManagementSeeder.SeedLookupId,
        LotCodeAbsLocation = AdoPackageManagementSeeder.SeedLookupId,
        LotCodeRelLocation = AdoPackageManagementSeeder.SeedLookupId,
        ExpiryAbsLocation = AdoPackageManagementSeeder.SeedLookupId,
        ExpiryRelLocation = AdoPackageManagementSeeder.SeedLookupId,
        DefinitionStateId = AdoPackageManagementSeeder.SeedLookupId,
        ProductCategoryId = AdoPackageManagementSeeder.SeedLookupId,
        ProductCodeTypeId = AdoPackageManagementSeeder.SeedLookupId,
        PackageSizeUomId = AdoPackageManagementSeeder.SeedLookupId,
        LotCodeSource = AdoPackageManagementSeeder.SeedLookupId,
        PackageDefinitionTypeId = AdoPackageManagementSeeder.SeedLookupId,
        PackageHeight = 10m,
        PackageWidth = 10m,
        PackageLength = 10m,
        Cap = false,
        CapDiameter = 0m,
        CapLength = 0m,
        Weight = 1m,
        FragileScale = 0,
        Valid = true,
        ProductName = marker,
        ProductCode = marker,
        ProductManufacturer = "EF Core Test Mfr",
        PackageSize = 1m,
        ChangedDate = DateTime.UtcNow,
        ChangedBy = "efcore-test",
        ControlledSubstance = false,
        CreatedBy = "efcore-test",
        CreatedOn = DateTime.UtcNow,
        IsDemoPackage = true
    };

    private static PackageDefinitionEntity BuildEntity(string marker)
    {
        var d = BuildDefinition(marker);
        return new PackageDefinitionEntity
        {
            Description = d.Description,
            PackageCode = d.PackageCode,
            PackageCodeAbsLocationId = d.PackageCodeAbsLocationId,
            PackageHeight = d.PackageHeight,
            PackageWidth = d.PackageWidth,
            PackageLength = d.PackageLength,
            Shape = d.Shape,
            Cap = d.Cap,
            CapDiameter = d.CapDiameter,
            CapLength = d.CapLength,
            Weight = d.Weight,
            FragileScale = d.FragileScale,
            LotCodeAbsLocation = d.LotCodeAbsLocation,
            LotCodeRelLocation = d.LotCodeRelLocation,
            ExpiryAbsLocation = d.ExpiryAbsLocation,
            ExpiryRelLocation = d.ExpiryRelLocation,
            Valid = d.Valid,
            DefinitionStateId = d.DefinitionStateId,
            ProductCategoryId = d.ProductCategoryId,
            ProductName = d.ProductName,
            ProductCodeTypeId = d.ProductCodeTypeId,
            ProductCode = d.ProductCode,
            ProductManufacturer = d.ProductManufacturer,
            PackageSize = d.PackageSize,
            PackageSizeUomId = d.PackageSizeUomId,
            ChangedDate = d.ChangedDate,
            ChangedBy = d.ChangedBy,
            ControlledSubstance = d.ControlledSubstance,
            CreatedBy = d.CreatedBy,
            CreatedOn = d.CreatedOn,
            LotCodeSource = d.LotCodeSource,
            PackageDefinitionTypeId = d.PackageDefinitionTypeId,
            IsDemoPackage = d.IsDemoPackage
        };
    }
}
