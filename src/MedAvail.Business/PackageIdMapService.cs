using System;
using MedAvail.Common;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;

namespace MedAvail.Business
{
    /// <summary>
    /// Business-layer service over package_id_map. Adds rules on top of the
    /// repository: GUID normalization/validation, register-vs-update semantics,
    /// and duplicate guarding. The repository is injected via its interface, so
    /// the service is unit-testable with a mock (no database) and can run against
    /// any of the three databases that own package_id_map.
    /// </summary>
    public sealed class PackageIdMapService
    {
        private readonly IPackageIdMapRepository _repository;

        public PackageIdMapService(IPackageIdMapRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>The database the underlying repository targets.</summary>
        public MedAvailDatabase Database => _repository.Database;

        /// <summary>
        /// Normalizes a package GUID to canonical lowercase "D" format (no braces).
        /// Returns null for null/empty input. Throws on a non-empty invalid GUID.
        /// </summary>
        public static string? NormalizeGuid(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (!Guid.TryParse(raw, out var g))
                throw new FormatException($"'{raw}' is not a valid GUID.");
            return g.ToString("D").ToLowerInvariant();
        }

        /// <summary>
        /// Registers a new package id with a normalized guid. Business rule:
        /// a package id may only be registered once. Throws if it already exists.
        /// Returns rows affected (1 on success).
        /// </summary>
        public int Register(int packageId, string? guid)
        {
            if (packageId < 0)
                throw new ArgumentOutOfRangeException(nameof(packageId), "package id cannot be negative.");

            if (_repository.CountByPackageId(packageId) > 0)
                throw new InvalidOperationException($"package id {packageId} is already registered.");

            return _repository.Insert(new PackageIdMapDto
            {
                PackageId = packageId,
                PackageGuid = NormalizeGuid(guid)
            });
        }

        /// <summary>
        /// Upsert: updates the guid if the package id exists, otherwise inserts it.
        /// The guid is normalized first. Returns rows affected.
        /// </summary>
        public int RegisterOrUpdate(int packageId, string? guid)
        {
            if (packageId < 0)
                throw new ArgumentOutOfRangeException(nameof(packageId), "package id cannot be negative.");

            var normalized = NormalizeGuid(guid);
            if (_repository.CountByPackageId(packageId) > 0)
                return _repository.UpdateGuid(packageId, normalized);

            return _repository.Insert(new PackageIdMapDto { PackageId = packageId, PackageGuid = normalized });
        }

        /// <summary>True if a package id is registered.</summary>
        public bool IsRegistered(int packageId) => _repository.CountByPackageId(packageId) > 0;

        /// <summary>
        /// Returns the stored guid for a package id, or null if absent / no guid.
        /// </summary>
        public string? GetGuid(int packageId) => _repository.GetByPackageId(packageId)?.PackageGuid;

        /// <summary>Removes a package id registration. Returns rows affected.</summary>
        public int Unregister(int packageId) => _repository.Delete(packageId);
    }
}
