using System;
using MedAvail.Business;
using MedAvail.Common;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;
using MedAvail.DataAccess.Ado;
using MedAvail.DataAccess.EfCore;
using MedAvail.DataAccess.EfCore.Repositories;
using MedAvail.TestRunner.Harness;

namespace MedAvail.TestRunner.Tests;

/// <summary>
/// Full-stack tests through the business layer: runner -> PackageDefinitionService
/// -> IPackageDefinitionRepository -> live database. Exercises the same service
/// the unit tests mock, but here against real ADO.NET and EF Core repositories,
/// confirming the business orchestration works end-to-end against SQL Server.
///
/// Self-seeding (FK lookups) and re-runnable; created rows are deleted because
/// the create/read/delete cycle is the scenario under test.
/// </summary>
public sealed class BusinessLayerTests
{
    private readonly IConnectionStringProvider _connections;

    public BusinessLayerTests(IConnectionStringProvider connections) => _connections = connections;

    public void RegisterAll(TestRunnerHarness harness)
    {
        var seeder = new AdoPackageManagementSeeder(_connections);

        // Drive the SAME business service with each technology's real repository.
        RunFor(harness, "Ado",
            new PackageDefinitionService(new AdoPackageDefinitionRepository(_connections)), seeder);

        RunFor(harness, "EfCore",
            new PackageDefinitionService(
                new EfCorePackageDefinitionRepository(new EfCoreContextFactory(_connections))), seeder);
    }

    private void RunFor(TestRunnerHarness harness, string tech,
        PackageDefinitionService service, AdoPackageManagementSeeder seeder)
    {
        const string cat = "BusinessLayer";
        var newId = 0;
        var marker = $"BIZ-{tech}-{Guid.NewGuid():N}".Substring(0, 18);

        harness.Run($"Service.CreateValidated persists ({tech})", cat, tech, () =>
        {
            seeder.EnsurePackageDefinitionLookups();
            var dto = BuildDefinition(marker);
            // Business layer applies defaults, validates, then inserts via the repo.
            newId = service.CreateValidated(dto, createdBy: "biz-test", utcNow: DateTime.UtcNow);
            Assert.True(newId > 0, "service should return the new identity from the repository");
        });

        harness.Run($"Service.GetRecent returns inserted row ({tech})", cat, tech, () =>
        {
            Assert.True(newId > 0, "create step should have produced an id");
            var recent = service.GetRecent(50);
            Assert.True(recent.Count > 0, "GetRecent should return rows");
            Assert.True(System.Linq.Enumerable.Any(recent, p => p.PackageDefinitionId == newId),
                "GetRecent should include the row created via the service");
        });

        harness.Run($"Service.GetRecent clamps page size ({tech})", cat, tech, () =>
        {
            // Business rule: requests above 500 are clamped. The call should
            // succeed and return no more than the clamp ceiling.
            var recent = service.GetRecent(100000);
            Assert.True(recent.Count <= 500, "GetRecent should clamp the page size to <= 500");
        });

        harness.Run($"Service.CreateValidated rejects invalid ({tech})", cat, tech, () =>
        {
            var invalid = BuildDefinition(marker);
            invalid.ProductName = "";  // violates a business rule
            var threw = false;
            try
            {
                service.CreateValidated(invalid, "biz-test", DateTime.UtcNow);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert.True(threw, "service should reject an invalid definition before inserting");
        });

        harness.Run($"Cleanup inserted row ({tech})", cat, tech, () =>
        {
            if (newId <= 0) return;
            // Delete via the repository directly (cleanup, not the scenario).
            new AdoPackageDefinitionRepository(_connections).Delete(newId);
        });
    }

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
        ProductManufacturer = "Business Test Mfr",
        PackageSize = 1m,
        ControlledSubstance = false,
        IsDemoPackage = true
    };
}
