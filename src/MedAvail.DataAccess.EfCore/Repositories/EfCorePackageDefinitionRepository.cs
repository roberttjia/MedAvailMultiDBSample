using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;
using MedAvail.DataAccess.EfCore.Entities;

namespace MedAvail.DataAccess.EfCore.Repositories;

/// <summary>
/// EF Core implementation of IPackageDefinitionRepository against
/// MedAvailPackageManagementDb. Maps between the EF entity and the shared DTO so
/// the business layer never sees an EF type.
/// </summary>
public sealed class EfCorePackageDefinitionRepository : IPackageDefinitionRepository
{
    private readonly EfCoreContextFactory _factory;

    public EfCorePackageDefinitionRepository(EfCoreContextFactory factory) => _factory = factory;

    public IReadOnlyList<PackageDefinitionDto> GetRecent(int maxRows = 100)
    {
        using var ctx = _factory.CreatePackageManagement();
        return ctx.PackageDefinitions
            .AsNoTracking()
            .OrderByDescending(p => p.PackageDefinitionId)
            .Take(maxRows)
            .ToList()
            .Select(ToDto)
            .ToList();
    }

    public PackageDefinitionDto? GetById(int packageDefinitionId)
    {
        using var ctx = _factory.CreatePackageManagement();
        var entity = ctx.PackageDefinitions
            .AsNoTracking()
            .FirstOrDefault(p => p.PackageDefinitionId == packageDefinitionId);
        return entity is null ? null : ToDto(entity);
    }

    public int GetMaxId()
    {
        using var ctx = _factory.CreatePackageManagement();
        return ctx.PackageDefinitions.Any()
            ? ctx.PackageDefinitions.Max(p => p.PackageDefinitionId)
            : 0;
    }

    public int Insert(PackageDefinitionDto definition)
    {
        using var ctx = _factory.CreatePackageManagement();
        var entity = ToEntity(definition);
        entity.PackageDefinitionId = 0; // identity, DB-assigned
        entity.AuditId = null;          // rowversion, DB-assigned
        ctx.PackageDefinitions.Add(entity);
        ctx.SaveChanges();
        return entity.PackageDefinitionId;
    }

    public int Delete(int packageDefinitionId)
    {
        using var ctx = _factory.CreatePackageManagement();
        var entity = ctx.PackageDefinitions.FirstOrDefault(p => p.PackageDefinitionId == packageDefinitionId);
        if (entity is null) return 0;
        ctx.PackageDefinitions.Remove(entity);
        return ctx.SaveChanges();
    }

    private static PackageDefinitionDto ToDto(PackageDefinitionEntity e) => new()
    {
        PackageDefinitionId = e.PackageDefinitionId,
        Description = e.Description,
        PackageCode = e.PackageCode,
        PackageCodeAbsLocationId = e.PackageCodeAbsLocationId,
        PackageHeight = e.PackageHeight,
        PackageWidth = e.PackageWidth,
        PackageLength = e.PackageLength,
        Shape = e.Shape,
        Cap = e.Cap,
        CapDiameter = e.CapDiameter,
        CapLength = e.CapLength,
        Weight = e.Weight,
        FragileScale = e.FragileScale,
        LotCodeAbsLocation = e.LotCodeAbsLocation,
        LotCodeRelLocation = e.LotCodeRelLocation,
        ExpiryAbsLocation = e.ExpiryAbsLocation,
        ExpiryRelLocation = e.ExpiryRelLocation,
        Valid = e.Valid,
        DefinitionStateId = e.DefinitionStateId,
        DefinitionRejectReason = e.DefinitionRejectReason,
        ProductCategoryId = e.ProductCategoryId,
        ProductName = e.ProductName,
        ProductCodeTypeId = e.ProductCodeTypeId,
        ProductCode = e.ProductCode,
        ProductManufacturer = e.ProductManufacturer,
        PackageSize = e.PackageSize,
        PackageSizeUomId = e.PackageSizeUomId,
        DrugSchedule = e.DrugSchedule,
        ChangedDate = e.ChangedDate,
        ChangedBy = e.ChangedBy,
        ExpirationMethod = e.ExpirationMethod,
        DaysToAdvisedExpiration = e.DaysToAdvisedExpiration,
        DrugScheduleId = e.DrugScheduleId,
        ControlledSubstance = e.ControlledSubstance,
        CreatedBy = e.CreatedBy,
        CreatedOn = e.CreatedOn,
        LotCodeSource = e.LotCodeSource,
        AuditId = e.AuditId,
        Notes = e.Notes,
        PackageDefinitionTypeId = e.PackageDefinitionTypeId,
        IsDemoPackage = e.IsDemoPackage
    };

    private static PackageDefinitionEntity ToEntity(PackageDefinitionDto d) => new()
    {
        PackageDefinitionId = d.PackageDefinitionId,
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
        DefinitionRejectReason = d.DefinitionRejectReason,
        ProductCategoryId = d.ProductCategoryId,
        ProductName = d.ProductName,
        ProductCodeTypeId = d.ProductCodeTypeId,
        ProductCode = d.ProductCode,
        ProductManufacturer = d.ProductManufacturer,
        PackageSize = d.PackageSize,
        PackageSizeUomId = d.PackageSizeUomId,
        DrugSchedule = d.DrugSchedule,
        ChangedDate = d.ChangedDate,
        ChangedBy = d.ChangedBy,
        ExpirationMethod = d.ExpirationMethod,
        DaysToAdvisedExpiration = d.DaysToAdvisedExpiration,
        DrugScheduleId = d.DrugScheduleId,
        ControlledSubstance = d.ControlledSubstance,
        CreatedBy = d.CreatedBy,
        CreatedOn = d.CreatedOn,
        LotCodeSource = d.LotCodeSource,
        Notes = d.Notes,
        PackageDefinitionTypeId = d.PackageDefinitionTypeId,
        IsDemoPackage = d.IsDemoPackage
    };
}
