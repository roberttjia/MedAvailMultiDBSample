using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using MedAvail.Common;
using MedAvail.Common.Models;
using MedAvail.DataAccess.Ado;
using MedAvail.DataAccess.Ef6;
using MedAvail.DataAccess.Ef6.Entities;
using MedAvail.DataAccess.Ef6.Repositories;
using MedAvail.TestRunner.Harness;

namespace MedAvail.TestRunner.Tests;

/// <summary>
/// EF6 (EntityFramework 6.5.x, running cross-platform on .NET 8) tests: CRUD,
/// queries, and rowversion-based optimistic concurrency (DbUpdateConcurrencyException).
/// Self-seeding and re-runnable; created rows are left in place.
/// </summary>
public sealed class Ef6Tests
{
    private readonly Ef6ContextFactory _factory;
    private readonly AdoPackageManagementSeeder _seeder;
    private readonly Random _rng = new();

    public Ef6Tests(IConnectionStringProvider connections)
    {
        _factory = new Ef6ContextFactory(connections);
        _seeder = new AdoPackageManagementSeeder(connections);
    }

    public void RegisterAll(TestRunnerHarness harness)
    {
        PackageDefinitionCrud(harness);
        PackageDefinitionQueries(harness);
        PackageIdMap(harness);
        RowVersionConcurrency(harness);
    }

    private void PackageDefinitionCrud(TestRunnerHarness harness)
    {
        const string cat = "PackageDefinition";
        var repo = new Ef6PackageDefinitionRepository(_factory);
        var id = 0;
        var marker = $"EF6-{Guid.NewGuid():N}".Substring(0, 16);

        harness.Run("Insert package_definition", cat, "Ef6", () =>
        {
            _seeder.EnsurePackageDefinitionLookups();
            var before = repo.GetMaxId();
            id = repo.Insert(BuildDefinition(marker));
            Assert.True(id > before, $"new identity ({id}) should exceed prior max ({before})");
        });

        harness.Run("GetById round-trips entity", cat, "Ef6", () =>
        {
            Assert.True(id > 0, "insert should have produced an id");
            var row = repo.GetById(id);
            Assert.True(row is not null, "inserted row should be retrievable");
            Assert.Equal(marker, row!.ProductName, "product_name should round-trip");
            Assert.True(row.AuditId is { Length: 8 }, "rowversion should be 8 bytes");
        });

        harness.Run("Delete package_definition", cat, "Ef6", () =>
        {
            Assert.True(id > 0, "insert should have produced an id");
            Assert.Equal(1, repo.Delete(id), "delete should remove the inserted row");
            Assert.True(repo.GetById(id) is null, "row should be gone after delete");
        });
    }

    private void PackageDefinitionQueries(TestRunnerHarness harness)
    {
        const string cat = "PackageDefinition";
        var repo = new Ef6PackageDefinitionRepository(_factory);
        var marker = $"EF6Q-{Guid.NewGuid():N}".Substring(0, 16);

        harness.Run("Insert + filtered LINQ query", cat, "Ef6", () =>
        {
            _seeder.EnsurePackageDefinitionLookups();
            repo.Insert(BuildDefinition(marker));

            var recent = repo.GetRecent(50);
            Assert.True(recent.Count > 0, "GetRecent should return rows");
            for (var i = 1; i < recent.Count; i++)
                Assert.True(recent[i - 1].PackageDefinitionId >= recent[i].PackageDefinitionId,
                    "GetRecent should be ordered by id descending");
            Assert.True(recent.Any(r => r.ProductName == marker),
                "the just-inserted row should be present in GetRecent");
        });

        harness.Run("GetMaxId consistent with newest", cat, "Ef6", () =>
        {
            var max = repo.GetMaxId();
            var newest = repo.GetRecent(1);
            Assert.True(newest.Count > 0, "should have at least one row");
            Assert.Equal(newest[0].PackageDefinitionId, max, "max id should equal newest row id");
        });
    }

    private void PackageIdMap(TestRunnerHarness harness)
    {
        const string cat = "PackageIdMap";
        var repo = new Ef6PackageIdMapRepository(_factory);
        var packageId = 2_000_000_000 + _rng.Next(1, 90_000_000);

        harness.Run("Insert row (ExecuteSqlCommand)", cat, "Ef6", () =>
        {
            Assert.Equal(1, repo.Insert(new PackageIdMapDto { PackageId = packageId, PackageGuid = Guid.NewGuid().ToString() }),
                "insert should affect one row");
        });

        harness.Run("Query row (LINQ)", cat, "Ef6", () =>
        {
            var row = repo.GetByPackageId(packageId);
            Assert.True(row is not null, "row should be queryable");
            Assert.Equal(packageId, row!.PackageId, "package_id should match");
            Assert.Equal(1, repo.CountByPackageId(packageId), "count should be 1");
        });

        harness.Run("Update + delete row", cat, "Ef6", () =>
        {
            var g = Guid.NewGuid().ToString();
            Assert.Equal(1, repo.UpdateGuid(packageId, g), "update should affect one row");
            Assert.Equal(g, repo.GetByPackageId(packageId)!.PackageGuid, "guid should update");
            Assert.Equal(1, repo.Delete(packageId), "delete should remove the row");
        });
    }

    private void RowVersionConcurrency(TestRunnerHarness harness)
    {
        const string cat = "TypeFidelity";
        var id = 0;

        harness.Run("EF6 throws on stale rowversion", cat, "Ef6", () =>
        {
            _seeder.EnsurePackageDefinitionLookups();

            using (var ctx = _factory.CreatePackageManagement())
            {
                var e = BuildEntity($"EF6RV-{Guid.NewGuid():N}".Substring(0, 16));
                ctx.PackageDefinitions.Add(e);
                ctx.SaveChanges();
                id = e.PackageDefinitionId;
            }
            Assert.True(id > 0, "should have inserted a row");

            using var ctxA = _factory.CreatePackageManagement();
            using var ctxB = _factory.CreatePackageManagement();
            var copyA = ctxA.PackageDefinitions.First(p => p.PackageDefinitionId == id);
            var copyB = ctxB.PackageDefinitions.First(p => p.PackageDefinitionId == id);

            copyA.Description = "ef6-update-A";
            ctxA.SaveChanges();

            copyB.Description = "ef6-update-B";
            var threw = false;
            try
            {
                ctxB.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                threw = true;
            }
            Assert.True(threw, "EF6 should throw DbUpdateConcurrencyException on a stale rowversion");
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
        ProductManufacturer = "EF6 Test Mfr",
        PackageSize = 1m,
        ChangedDate = DateTime.UtcNow,
        ChangedBy = "ef6-test",
        ControlledSubstance = false,
        CreatedBy = "ef6-test",
        CreatedOn = DateTime.UtcNow,
        IsDemoPackage = true
    };

    private static PackageDefinitionEf6 BuildEntity(string marker)
    {
        var d = BuildDefinition(marker);
        return new PackageDefinitionEf6
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
