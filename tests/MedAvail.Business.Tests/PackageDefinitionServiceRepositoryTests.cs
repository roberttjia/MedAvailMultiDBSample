using System;
using System.Collections.Generic;
using MedAvail.Business;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;
using Moq;
using Xunit;

namespace MedAvail.Business.Tests;

/// <summary>
/// Unit tests for the service's interaction with the data-access boundary.
/// The repository is fully mocked (no database), letting us assert exactly how
/// the business layer calls into data access — what it passes, what it returns,
/// and that invalid input never reaches the repository.
/// </summary>
public class PackageDefinitionServiceRepositoryTests
{
    private static PackageDefinitionDto ValidDefinition() => new()
    {
        ProductName = "Ibuprofen 200mg",
        PackageCode = "099999999901",
        PackageHeight = 2m,
        PackageWidth = 2m,
        PackageLength = 2m,
        Weight = 0.5m
    };

    [Fact]
    public void CreateValidated_ValidInput_InsertsAndReturnsNewId()
    {
        var repo = new Mock<IPackageDefinitionRepository>();
        repo.Setup(r => r.Insert(It.IsAny<PackageDefinitionDto>())).Returns(4242);

        var service = new PackageDefinitionService(repo.Object);
        var now = DateTime.UtcNow;

        var id = service.CreateValidated(ValidDefinition(), "creator", now);

        Assert.Equal(4242, id);
        // The repository was called exactly once, with audit defaults applied.
        repo.Verify(r => r.Insert(It.Is<PackageDefinitionDto>(d =>
            d.CreatedBy == "creator" && d.ChangedBy == "creator" &&
            d.CreatedOn == now && d.Valid)), Times.Once);
    }

    [Fact]
    public void CreateValidated_InvalidInput_ThrowsAndNeverHitsRepository()
    {
        var repo = new Mock<IPackageDefinitionRepository>(MockBehavior.Strict);
        // Strict mock: any unexpected call fails the test. Insert is never set up.
        var service = new PackageDefinitionService(repo.Object);

        var invalid = ValidDefinition();
        invalid.ProductName = ""; // fails validation

        Assert.Throws<InvalidOperationException>(
            () => service.CreateValidated(invalid, "creator", DateTime.UtcNow));

        repo.Verify(r => r.Insert(It.IsAny<PackageDefinitionDto>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 1)]       // below floor -> clamped up to 1
    [InlineData(-50, 1)]     // negative -> clamped to 1
    [InlineData(100, 100)]   // within range -> unchanged
    [InlineData(9999, 500)]  // above ceiling -> clamped to 500
    public void GetRecent_ClampsPageSizeBeforeCallingRepository(int requested, int expected)
    {
        var repo = new Mock<IPackageDefinitionRepository>();
        repo.Setup(r => r.GetRecent(It.IsAny<int>()))
            .Returns(new List<PackageDefinitionDto>());

        var service = new PackageDefinitionService(repo.Object);
        service.GetRecent(requested);

        repo.Verify(r => r.GetRecent(expected), Times.Once);
    }

    [Fact]
    public void GetRecent_ReturnsRepositoryResults()
    {
        var rows = new List<PackageDefinitionDto>
        {
            new() { PackageDefinitionId = 1, ProductName = "A" },
            new() { PackageDefinitionId = 2, ProductName = "B" },
        };
        var repo = new Mock<IPackageDefinitionRepository>();
        repo.Setup(r => r.GetRecent(It.IsAny<int>())).Returns(rows);

        var service = new PackageDefinitionService(repo.Object);
        var result = service.GetRecent(10);

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].ProductName);
    }

    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PackageDefinitionService(null!));
    }
}
