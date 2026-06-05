using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;
using MedAvail.DataAccess.Ef6.Entities;

namespace MedAvail.DataAccess.Ef6.Repositories
{
    /// <summary>
    /// EF6 implementation of IPackageDefinitionRepository against
    /// MedAvailPackageManagementDb. Maps EF6 entities to the shared DTOs.
    /// </summary>
    public sealed class Ef6PackageDefinitionRepository : IPackageDefinitionRepository
    {
        private readonly Ef6ContextFactory _factory;

        public Ef6PackageDefinitionRepository(Ef6ContextFactory factory) => _factory = factory;

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
            var e = ctx.PackageDefinitions.AsNoTracking()
                .FirstOrDefault(p => p.PackageDefinitionId == packageDefinitionId);
            return e is null ? null : ToDto(e);
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
            var e = ToEntity(definition);
            e.PackageDefinitionId = 0;
            e.AuditId = null;
            ctx.PackageDefinitions.Add(e);
            ctx.SaveChanges();
            return e.PackageDefinitionId;
        }

        public int Delete(int packageDefinitionId)
        {
            using var ctx = _factory.CreatePackageManagement();
            var e = ctx.PackageDefinitions.Find(packageDefinitionId);
            if (e is null) return 0;
            ctx.PackageDefinitions.Remove(e);
            return ctx.SaveChanges();
        }

        private static PackageDefinitionDto ToDto(PackageDefinitionEf6 e) => new()
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

        private static PackageDefinitionEf6 ToEntity(PackageDefinitionDto d) => new()
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
}
