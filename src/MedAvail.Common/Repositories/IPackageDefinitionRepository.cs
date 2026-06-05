using System.Collections.Generic;
using MedAvail.Common.Models;

namespace MedAvail.Common.Repositories
{
    /// <summary>
    /// Data-access contract for dbo.package_definition. Implemented separately by
    /// the ADO.NET, EF Core, and EF6 layers; the business layer depends only on
    /// this interface so the access technology is swappable.
    /// </summary>
    public interface IPackageDefinitionRepository
    {
        /// <summary>Returns the most recent definitions, newest id first.</summary>
        IReadOnlyList<PackageDefinitionDto> GetRecent(int maxRows = 100);

        /// <summary>Loads a single definition by its identity key, or null.</summary>
        PackageDefinitionDto? GetById(int packageDefinitionId);

        /// <summary>
        /// Inserts a new definition and returns the DB-generated identity id.
        /// </summary>
        int Insert(PackageDefinitionDto definition);

        /// <summary>Deletes by identity key. Returns rows affected.</summary>
        int Delete(int packageDefinitionId);

        /// <summary>Current maximum identity value, or 0 when the table is empty.</summary>
        int GetMaxId();
    }
}
