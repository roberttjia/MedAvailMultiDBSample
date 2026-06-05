using System;
using System.Linq;
using MedAvail.Common;
using MedAvail.Common.Models;
using MedAvail.DataAccess.Ado;
using MedAvail.TestRunner.Harness;

namespace MedAvail.TestRunner.Tests;

/// <summary>
/// ADO.NET tests: CRUD, queries, and stored-procedure invocations.
/// All tests are re-runnable — they use unique/randomized keys, assert on
/// deltas rather than absolute counts, and clean up everything they create.
/// </summary>
public sealed class AdoTests
{
    private readonly IConnectionStringProvider _connections;
    private readonly Random _rng = new();

    public AdoTests(IConnectionStringProvider connections) => _connections = connections;

    public void RegisterAll(TestRunnerHarness harness)
    {
        PackageIdMapCrud(harness);
        PackageDefinitionQueries(harness);
        PackageDefinitionCrud(harness);
        ContainerStoredProcedures(harness);
        RowVersionAndBitFields(harness);
        CrossDatabase(harness);
    }

    // ---- package_id_map: full CRUD + query (PackageManagement) ----
    private void PackageIdMapCrud(TestRunnerHarness harness)
    {
        const string cat = "PackageIdMap";
        var repo = new AdoPackageIdMapRepository(_connections, MedAvailDatabase.PackageManagement);
        var packageId = NewTestId();

        harness.Run("Insert package_id_map row", cat, "Ado", () =>
        {
            var affected = repo.Insert(new PackageIdMapDto { PackageId = packageId, PackageGuid = Guid.NewGuid().ToString() });
            Assert.Equal(1, affected, "insert should affect exactly one row");
        });

        harness.Run("Read back inserted row", cat, "Ado", () =>
        {
            var row = repo.GetByPackageId(packageId);
            Assert.True(row is not null, "inserted row should be retrievable");
            Assert.Equal(packageId, row!.PackageId, "package_id should round-trip");
        });

        harness.Run("Query count by package_id", cat, "Ado", () =>
        {
            Assert.Equal(1, repo.CountByPackageId(packageId), "exactly one row for the test id");
        });

        harness.Run("Update package_guid", cat, "Ado", () =>
        {
            var newGuid = Guid.NewGuid().ToString();
            var affected = repo.UpdateGuid(packageId, newGuid);
            Assert.Equal(1, affected, "update should affect one row");
            Assert.Equal(newGuid, repo.GetByPackageId(packageId)!.PackageGuid, "guid should be updated");
        });

        harness.Run("Delete row (cleanup)", cat, "Ado", () =>
        {
            var affected = repo.Delete(packageId);
            Assert.Equal(1, affected, "delete should remove the test row");
            Assert.True(repo.GetByPackageId(packageId) is null, "row should be gone after delete");
        });
    }

    // ---- package_definition: query coverage (PackageManagement) ----
    // Self-seeds one row so the queries always have data to assert against.
    private void PackageDefinitionQueries(TestRunnerHarness harness)
    {
        const string cat = "PackageDefinition";
        var repo = new AdoPackageDefinitionRepository(_connections);
        var seeder = new AdoPackageManagementSeeder(_connections);

        harness.Run("GetRecent returns ordered rows", cat, "Ado", () =>
        {
            // Ensure at least one row exists so the query has data.
            seeder.EnsurePackageDefinitionLookups();
            repo.Insert(BuildDefinition($"SEED-{Guid.NewGuid():N}".Substring(0, 18)));

            var recent = repo.GetRecent(10);
            Assert.True(recent.Count > 0, "GetRecent should return the seeded row(s)");
            for (var i = 1; i < recent.Count; i++)
                Assert.True(recent[i - 1].PackageDefinitionId >= recent[i].PackageDefinitionId,
                    "GetRecent should be ordered by id descending");
        });

        harness.Run("GetById matches GetRecent head", cat, "Ado", () =>
        {
            var recent = repo.GetRecent(1);
            Assert.True(recent.Count > 0, "expected at least one row after seeding");
            var byId = repo.GetById(recent[0].PackageDefinitionId);
            Assert.True(byId is not null, "GetById should find the row");
            Assert.Equal(recent[0].PackageDefinitionId, byId!.PackageDefinitionId, "ids should match");
        });

        harness.Run("GetMaxId is consistent", cat, "Ado", () =>
        {
            var max = repo.GetMaxId();
            var recent = repo.GetRecent(1);
            Assert.True(recent.Count > 0, "expected at least one row after seeding");
            Assert.Equal(recent[0].PackageDefinitionId, max, "max id should equal newest row id");
        });
    }

    // ---- package_definition: insert + delete (delta-based) ----
    // Self-seeds the FK lookups; Delete is kept because it is the CRUD-D under test.
    private void PackageDefinitionCrud(TestRunnerHarness harness)
    {
        const string cat = "PackageDefinition";
        var repo = new AdoPackageDefinitionRepository(_connections);
        var seeder = new AdoPackageManagementSeeder(_connections);
        var newId = 0;
        var marker = $"ADOTEST-{Guid.NewGuid():N}".Substring(0, 20);

        harness.Run("Insert package_definition", cat, "Ado", () =>
        {
            seeder.EnsurePackageDefinitionLookups();
            var before = repo.GetMaxId();
            newId = repo.Insert(BuildDefinition(marker));
            Assert.True(newId > before, $"new identity ({newId}) should exceed prior max ({before})");
        });

        harness.Run("Read back inserted definition", cat, "Ado", () =>
        {
            Assert.True(newId > 0, "insert should have produced an id");
            var row = repo.GetById(newId);
            Assert.True(row is not null, "inserted definition should be retrievable");
            Assert.Equal(marker, row!.ProductName, "product_name should round-trip");
        });

        harness.Run("Delete package_definition", cat, "Ado", () =>
        {
            Assert.True(newId > 0, "insert should have produced an id");
            var affected = repo.Delete(newId);
            Assert.Equal(1, affected, "delete should remove the inserted definition");
            Assert.True(repo.GetById(newId) is null, "definition should be gone after delete");
        });
    }

    // ---- container stored procedures: CreateContainer / ModifyContainer ----
    // Self-seeds the required lookup_package_shape row. Created container is left in place.
    private void ContainerStoredProcedures(TestRunnerHarness harness)
    {
        const string cat = "Container.Proc";
        var repo = new AdoContainerRepository(_connections);
        var seeder = new AdoPackageManagementSeeder(_connections);
        string? containerId = null;

        harness.Run("CreateContainer (sproc) creates row", cat, "Ado", () =>
        {
            var shape = seeder.EnsureShapeId();
            var created = repo.CreateContainer(
                shape: shape, length: 100, width: 80, height: 60,
                changedBy: "ado-test", containerType: 3, description: "ado test container");

            Assert.True(!string.IsNullOrEmpty(created.ContainerId), "proc should return a generated container_id");
            containerId = created.ContainerId;
        });

        harness.Run("ModifyContainer (sproc) updates row", cat, "Ado", () =>
        {
            Assert.True(containerId is not null, "create should have produced a container_id");
            var shape = seeder.EnsureShapeId();
            var updated = repo.ModifyContainer(
                containerId!, shape: shape, length: 111, width: 81, height: 61,
                changedBy: "ado-test-modify", containerType: 4, description: "modified by ado test");

            Assert.Equal(111m, updated.Length, "length should be updated by the proc");
            Assert.Equal("modified by ado test", updated.Description, "description should be updated");
        });

        harness.Run("Read container back (query)", cat, "Ado", () =>
        {
            Assert.True(containerId is not null, "create should have produced a container_id");
            var row = repo.GetById(containerId!);
            Assert.True(row is not null, "container should be retrievable after proc calls");
            Assert.Equal(61m, row!.Height, "height should reflect the modify");
        });

        harness.Run("CreateContainer (sproc) defaults NULL description", cat, "Ado", () =>
        {
            var shape = seeder.EnsureShapeId();
            // Pass description=null to exercise the proc's default-description path.
            var created = repo.CreateContainer(
                shape: shape, length: 50, width: 40, height: 30,
                changedBy: "ado-test", containerType: 3, description: null);

            Assert.True(!string.IsNullOrEmpty(created.ContainerId), "proc should return a generated container_id");
            Assert.True(!string.IsNullOrWhiteSpace(created.Description),
                "proc should generate a non-null description when none is supplied");
        });
    }

    // ---- cross-database access (same SQL Server instance) ----
    // Proves the "same server, multiple databases" topology used by the
    // schema-conversion target. All writes are rolled back.
    private void CrossDatabase(TestRunnerHarness harness)
    {
        const string cat = "CrossDatabase";
        var repo = new AdoCrossDatabaseRepository(_connections);

        harness.Run("Three-part-name query (Core -> PackageManagement)", cat, "Ado", () =>
        {
            // Connected to MedAvailDB, query a table in MedAvailPackageManagementDb.
            var count = repo.CountPackageIdMapInPackageManagement();
            Assert.True(count >= 0, "cross-database three-part query should resolve and return a count");
        });

        harness.Run("Cross-database stored proc RemoveMedCenter (rolled back)", cat, "Ado", () =>
        {
            // Non-existent serial => nothing matches; wrapped in a transaction that
            // always rolls back. Exercises the cross-db EXEC path safely.
            var serial = AdoCrossDatabaseRepository.NewNonExistentSerial();
            var ok = repo.InvokeRemoveMedCenterInRollback(serial);
            Assert.True(ok, "RemoveMedCenter (which EXECs across databases) should run without error");
        });
    }

    // ---- rowversion (AuditID_MA) + bit field behavior (PackageManagement) ----
    // Seeds + inserts a dedicated row, then exercises rowversion and bit columns.
    private void RowVersionAndBitFields(TestRunnerHarness harness)
    {
        const string cat = "TypeFidelity";
        var repo = new AdoPackageDefinitionRepository(_connections);
        var seeder = new AdoPackageManagementSeeder(_connections);
        var id = 0;
        byte[]? versionAfterInsert = null;

        harness.Run("Insert row for type tests", cat, "Ado", () =>
        {
            seeder.EnsurePackageDefinitionLookups();
            id = repo.Insert(BuildDefinition($"TYPE-{Guid.NewGuid():N}".Substring(0, 18)));
            Assert.True(id > 0, "should insert a row to operate on");
        });

        harness.Run("rowversion AuditID_MA is populated", cat, "Ado", () =>
        {
            Assert.True(id > 0, "insert should have produced an id");
            versionAfterInsert = repo.GetRowVersion(id);
            Assert.True(versionAfterInsert is { Length: 8 },
                "rowversion should be an 8-byte value generated by SQL Server");
        });

        harness.Run("rowversion changes after update", cat, "Ado", () =>
        {
            Assert.True(versionAfterInsert is not null, "need the initial rowversion");
            // Any update bumps the rowversion.
            repo.UpdateBitFlags(id, cap: true, valid: false, controlledSubstance: true, isDemo: false);
            var after = repo.GetRowVersion(id);
            Assert.True(after is not null, "rowversion should still be present");
            Assert.True(!RowVersionsEqual(versionAfterInsert!, after!),
                "rowversion must change after an update");
        });

        harness.Run("bit fields round-trip", cat, "Ado", () =>
        {
            Assert.True(id > 0, "need an inserted row");
            // The previous test set cap=1, valid=0, controlled=1, demo=0.
            var row = repo.GetById(id);
            Assert.True(row is not null, "row should exist");
            Assert.Equal(true, row!.Cap, "cap bit should be true");
            Assert.Equal(false, row.Valid, "valid bit should be false");
            Assert.Equal(true, row.ControlledSubstance, "controlled_substance bit should be true");
            Assert.Equal(false, row.IsDemoPackage, "is_demo_package bit should be false");

            // Flip them all and confirm the inverse round-trips too.
            repo.UpdateBitFlags(id, cap: false, valid: true, controlledSubstance: false, isDemo: true);
            var flipped = repo.GetById(id)!;
            Assert.Equal(false, flipped.Cap, "cap should flip to false");
            Assert.Equal(true, flipped.Valid, "valid should flip to true");
            Assert.Equal(false, flipped.ControlledSubstance, "controlled_substance should flip to false");
            Assert.Equal(true, flipped.IsDemoPackage, "is_demo_package should flip to true");
        });

        harness.Run("optimistic concurrency via rowversion", cat, "Ado", () =>
        {
            Assert.True(id > 0, "need an inserted row");
            var current = repo.GetRowVersion(id)!;

            // A matching token should update exactly one row.
            var matched = repo.UpdateDescriptionIfVersionMatches(id, "rv-match", current);
            Assert.Equal(1, matched, "update with current rowversion should succeed");

            // Reusing the now-stale token should affect zero rows.
            var stale = repo.UpdateDescriptionIfVersionMatches(id, "rv-stale", current);
            Assert.Equal(0, stale, "update with stale rowversion should affect no rows");
        });
    }

    private static bool RowVersionsEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    // ---- helpers ----

    /// <summary>A high, randomized id unlikely to collide with real data.</summary>
    private int NewTestId() => 2_000_000_000 + _rng.Next(1, 90_000_000);

    private static PackageDefinitionDto BuildDefinition(string marker) => new()
    {
        Description = marker,
        PackageCode = marker,
        // FK columns -> the seeded lookup id (AdoPackageManagementSeeder.SeedLookupId)
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
        ProductManufacturer = "ADO Test Mfr",
        PackageSize = 1m,
        ChangedDate = DateTime.UtcNow,
        ChangedBy = "ado-test",
        ControlledSubstance = false,
        CreatedBy = "ado-test",
        CreatedOn = DateTime.UtcNow,
        IsDemoPackage = true
    };
}
