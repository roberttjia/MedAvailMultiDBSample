using System.Collections.Generic;
using MedAvail.Common.Models;

namespace MedAvail.Common.Repositories
{
    /// <summary>
    /// Data-access contract for dbo.package_id_map. The implementation is bound
    /// to a specific <see cref="MedAvailDatabase"/> at construction, so the same
    /// interface can target whichever of the three databases owns the table.
    /// </summary>
    public interface IPackageIdMapRepository
    {
        /// <summary>The database this repository instance is bound to.</summary>
        MedAvailDatabase Database { get; }

        IReadOnlyList<PackageIdMapDto> GetAll();

        PackageIdMapDto? GetByPackageId(int packageId);

        int CountByPackageId(int packageId);

        /// <summary>Inserts a row. Returns rows affected.</summary>
        int Insert(PackageIdMapDto row);

        /// <summary>Updates the guid for a package_id. Returns rows affected.</summary>
        int UpdateGuid(int packageId, string? newGuid);

        /// <summary>Deletes all rows for a package_id. Returns rows affected.</summary>
        int Delete(int packageId);
    }
}
