using System;
using System.Collections.Generic;
using MedAvail.Business;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;
using Moq;
using Xunit;

namespace MedAvail.Business.Tests;

/// <summary>
/// Unit tests for PackageDefinitionService. No database: the
/// IPackageDefinitionRepository is mocked with Moq, so these tests exercise the
/// business logic and the repository boundary in isolation. This validates the
/// app's behavior independent of the SQL Server -> PostgreSQL migration.
/// </summary>
public class PackageDefinitionServiceTests
{
    private static PackageDefinitionDto ValidDefinition() => new()
    {
        ProductName = "Amoxicillin 500mg",
        PackageCode = "012345678905",
        PackageHeight = 5m,
        PackageWidth = 4m,
        PackageLength = 3m,
        Weight = 1m,
        Cap = false
    };

    // ---------- pure logic: Validate ----------

    [Fact]
    public void Validate_ValidDefinition_ReturnsValid()
    {
        var service = new PackageDefinitionService(Mock.Of<IPackageDefinitionRepository>());
        var result = service.Validate(ValidDefinition());
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_MissingProductName_ReportsError()
    {
        var service = new PackageDefinitionService(Mock.Of<IPackageDefinitionRepository>());
        var dto = ValidDefinition();
        dto.ProductName = "  ";
        var result = service.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Product name"));
    }

    [Theory]
    [InlineData(0, 4, 3)]
    [InlineData(5, 0, 3)]
    [InlineData(5, 4, 0)]
    [InlineData(-1, 4, 3)]
    public void Validate_NonPositiveDimensions_ReportsError(decimal h, decimal w, decimal l)
    {
        var service = new PackageDefinitionService(Mock.Of<IPackageDefinitionRepository>());
        var dto = ValidDefinition();
        dto.PackageHeight = h; dto.PackageWidth = w; dto.PackageLength = l;
        var result = service.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("dimensions"));
    }

    [Fact]
    public void Validate_CappedWithoutCapDimensions_ReportsError()
    {
        var service = new PackageDefinitionService(Mock.Of<IPackageDefinitionRepository>());
        var dto = ValidDefinition();
        dto.Cap = true; dto.CapDiameter = 0m; dto.CapLength = 0m;
        var result = service.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("cap"));
    }

    [Fact]
    public void Validate_NegativeWeight_ReportsError()
    {
        var service = new PackageDefinitionService(Mock.Of<IPackageDefinitionRepository>());
        var dto = ValidDefinition();
        dto.Weight = -0.5m;
        var result = service.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Weight"));
    }

    // ---------- pure logic: CalculateVolume ----------

    [Fact]
    public void CalculateVolume_MultipliesDimensions()
    {
        var service = new PackageDefinitionService(Mock.Of<IPackageDefinitionRepository>());
        var dto = ValidDefinition(); // 5 * 4 * 3
        Assert.Equal(60m, service.CalculateVolume(dto));
    }

    // ---------- pure logic: ApplyCreationDefaults ----------

    [Fact]
    public void ApplyCreationDefaults_SetsAuditFieldsAndValidFlag()
    {
        var service = new PackageDefinitionService(Mock.Of<IPackageDefinitionRepository>());
        var dto = ValidDefinition();
        dto.Valid = false;
        var now = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var result = service.ApplyCreationDefaults(dto, "tester", now);

        Assert.Equal("tester", result.CreatedBy);
        Assert.Equal("tester", result.ChangedBy);
        Assert.Equal(now, result.CreatedOn);
        Assert.Equal(now, result.ChangedDate);
        Assert.True(result.Valid);
    }

    [Fact]
    public void ApplyCreationDefaults_NullCreatedBy_Throws()
    {
        var service = new PackageDefinitionService(Mock.Of<IPackageDefinitionRepository>());
        Assert.Throws<ArgumentException>(
            () => service.ApplyCreationDefaults(ValidDefinition(), "", DateTime.UtcNow));
    }
}
