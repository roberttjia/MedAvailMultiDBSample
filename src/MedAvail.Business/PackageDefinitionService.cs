using System;
using System.Collections.Generic;
using System.Linq;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;

namespace MedAvail.Business
{
    /// <summary>
    /// Business-layer service over package definitions. Holds pure business rules
    /// (validation, defaulting, derived values) and orchestrates the repository.
    /// The repository is injected via its interface, so this class is fully
    /// unit-testable without a database.
    /// </summary>
    public sealed class PackageDefinitionService
    {
        private readonly IPackageDefinitionRepository _repository;

        public PackageDefinitionService(IPackageDefinitionRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Validates a package definition against business rules. Pure logic,
        /// no I/O.
        /// </summary>
        public PackageValidationResult Validate(PackageDefinitionDto definition)
        {
            if (definition is null) throw new ArgumentNullException(nameof(definition));

            var result = new PackageValidationResult();

            if (string.IsNullOrWhiteSpace(definition.ProductName))
                result.AddError("Product name is required.");

            if (string.IsNullOrWhiteSpace(definition.PackageCode))
                result.AddError("Package code is required.");

            if (definition.PackageHeight <= 0 || definition.PackageWidth <= 0 || definition.PackageLength <= 0)
                result.AddError("Package dimensions must be positive.");

            if (definition.Weight < 0)
                result.AddError("Weight cannot be negative.");

            if (definition.Cap && (definition.CapDiameter <= 0 || definition.CapLength <= 0))
                result.AddError("Capped packages must have positive cap dimensions.");

            return result;
        }

        /// <summary>Volume derived from the package dimensions (business calc).</summary>
        public decimal CalculateVolume(PackageDefinitionDto definition)
        {
            if (definition is null) throw new ArgumentNullException(nameof(definition));
            return definition.PackageHeight * definition.PackageWidth * definition.PackageLength;
        }

        /// <summary>
        /// Applies default values for a new definition prior to persistence
        /// (e.g. timestamps, audit fields). Pure transformation of the input.
        /// </summary>
        public PackageDefinitionDto ApplyCreationDefaults(PackageDefinitionDto definition, string createdBy, DateTime utcNow)
        {
            if (definition is null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(createdBy)) throw new ArgumentException("createdBy is required.", nameof(createdBy));

            definition.CreatedBy = createdBy;
            definition.ChangedBy = createdBy;
            definition.CreatedOn = utcNow;
            definition.ChangedDate = utcNow;
            if (!definition.Valid)
                definition.Valid = true;   // new definitions default to valid
            return definition;
        }

        /// <summary>
        /// Validates and, if valid, inserts via the repository. Returns the new id
        /// or throws if validation fails. Exercises both business logic and the
        /// repository boundary (mockable).
        /// </summary>
        public int CreateValidated(PackageDefinitionDto definition, string createdBy, DateTime utcNow)
        {
            var prepared = ApplyCreationDefaults(definition, createdBy, utcNow);
            var validation = Validate(prepared);
            if (!validation.IsValid)
                throw new InvalidOperationException($"Invalid package definition: {validation.Summary}");
            return _repository.Insert(prepared);
        }

        /// <summary>Returns the most recent definitions, capped to a sane page size.</summary>
        public IReadOnlyList<PackageDefinitionDto> GetRecent(int requested)
        {
            // Business rule: clamp page size to [1, 500].
            var capped = Math.Max(1, Math.Min(requested, 500));
            return _repository.GetRecent(capped);
        }
    }
}
