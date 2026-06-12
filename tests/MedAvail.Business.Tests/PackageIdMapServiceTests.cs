using System;
using MedAvail.Business;
using MedAvail.Common;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;
using Moq;
using Xunit;

namespace MedAvail.Business.Tests;

/// <summary>
/// Unit tests for PackageIdMapService. The IPackageIdMapRepository is mocked,
/// so these tests exercise the GUID normalization, register/upsert rules, and
/// duplicate guarding without a database.
/// </summary>
public class PackageIdMapServiceTests
{
    // ---------- NormalizeGuid (pure) ----------

    [Fact]
    public void NormalizeGuid_NullOrWhitespace_ReturnsNull()
    {
        Assert.Null(PackageIdMapService.NormalizeGuid(null));
        Assert.Null(PackageIdMapService.NormalizeGuid("   "));
    }

    [Fact]
    public void NormalizeGuid_BracedUppercase_ReturnsCanonicalLowercase()
    {
        var g = Guid.NewGuid();
        var input = g.ToString("B").ToUpperInvariant();   // {AAAA-...} uppercase
        var normalized = PackageIdMapService.NormalizeGuid(input);
        Assert.Equal(g.ToString("D").ToLowerInvariant(), normalized);
    }

    [Fact]
    public void NormalizeGuid_Invalid_Throws()
    {
        Assert.Throws<FormatException>(() => PackageIdMapService.NormalizeGuid("not-a-guid"));
    }

    // ---------- Register ----------

    [Fact]
    public void Register_NewId_InsertsNormalizedGuid()
    {
        var repo = new Mock<IPackageIdMapRepository>();
        repo.Setup(r => r.CountByPackageId(10)).Returns(0);
        repo.Setup(r => r.Insert(It.IsAny<PackageIdMapDto>())).Returns(1);

        var service = new PackageIdMapService(repo.Object);
        var raw = Guid.NewGuid().ToString("B").ToUpperInvariant();

        var affected = service.Register(10, raw);

        Assert.Equal(1, affected);
        repo.Verify(r => r.Insert(It.Is<PackageIdMapDto>(d =>
            d.PackageId == 10 &&
            d.PackageGuid == raw.Trim('{', '}').ToLowerInvariant())), Times.Once);
    }

    [Fact]
    public void Register_ExistingId_ThrowsAndDoesNotInsert()
    {
        var repo = new Mock<IPackageIdMapRepository>();
        repo.Setup(r => r.CountByPackageId(10)).Returns(1);

        var service = new PackageIdMapService(repo.Object);

        Assert.Throws<InvalidOperationException>(() => service.Register(10, null));
        repo.Verify(r => r.Insert(It.IsAny<PackageIdMapDto>()), Times.Never);
    }

    [Fact]
    public void Register_NegativeId_Throws()
    {
        var service = new PackageIdMapService(Mock.Of<IPackageIdMapRepository>());
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Register(-1, null));
    }

    // ---------- RegisterOrUpdate (upsert) ----------

    [Fact]
    public void RegisterOrUpdate_NewId_Inserts()
    {
        var repo = new Mock<IPackageIdMapRepository>();
        repo.Setup(r => r.CountByPackageId(20)).Returns(0);
        repo.Setup(r => r.Insert(It.IsAny<PackageIdMapDto>())).Returns(1);

        var service = new PackageIdMapService(repo.Object);
        var affected = service.RegisterOrUpdate(20, Guid.NewGuid().ToString());

        Assert.Equal(1, affected);
        repo.Verify(r => r.Insert(It.IsAny<PackageIdMapDto>()), Times.Once);
        repo.Verify(r => r.UpdateGuid(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public void RegisterOrUpdate_ExistingId_Updates()
    {
        var repo = new Mock<IPackageIdMapRepository>();
        repo.Setup(r => r.CountByPackageId(20)).Returns(1);
        repo.Setup(r => r.UpdateGuid(20, It.IsAny<string?>())).Returns(1);

        var service = new PackageIdMapService(repo.Object);
        var g = Guid.NewGuid().ToString();
        var affected = service.RegisterOrUpdate(20, g);

        Assert.Equal(1, affected);
        repo.Verify(r => r.UpdateGuid(20, g.ToLowerInvariant()), Times.Once);
        repo.Verify(r => r.Insert(It.IsAny<PackageIdMapDto>()), Times.Never);
    }

    // ---------- query helpers ----------

    [Fact]
    public void IsRegistered_ReflectsRepositoryCount()
    {
        var repo = new Mock<IPackageIdMapRepository>();
        repo.Setup(r => r.CountByPackageId(5)).Returns(1);
        repo.Setup(r => r.CountByPackageId(6)).Returns(0);

        var service = new PackageIdMapService(repo.Object);
        Assert.True(service.IsRegistered(5));
        Assert.False(service.IsRegistered(6));
    }

    [Fact]
    public void GetGuid_ReturnsStoredGuid()
    {
        var repo = new Mock<IPackageIdMapRepository>();
        repo.Setup(r => r.GetByPackageId(7))
            .Returns(new PackageIdMapDto { PackageId = 7, PackageGuid = "abc" });

        var service = new PackageIdMapService(repo.Object);
        Assert.Equal("abc", service.GetGuid(7));
    }

    [Fact]
    public void GetGuid_AbsentId_ReturnsNull()
    {
        var repo = new Mock<IPackageIdMapRepository>();
        repo.Setup(r => r.GetByPackageId(It.IsAny<int>())).Returns((PackageIdMapDto?)null);

        var service = new PackageIdMapService(repo.Object);
        Assert.Null(service.GetGuid(99));
    }

    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PackageIdMapService(null!));
    }
}
