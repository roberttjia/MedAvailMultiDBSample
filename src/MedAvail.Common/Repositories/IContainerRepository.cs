using System.Collections.Generic;
using MedAvail.Common.Models;

namespace MedAvail.Common.Repositories
{
    /// <summary>
    /// Stored-procedure-based access to dbo.container in MedAvailPackageManagementDb.
    /// CreateContainer / ModifyContainer are invoked as CommandType.StoredProcedure.
    /// </summary>
    public interface IContainerRepository
    {
        /// <summary>
        /// Calls dbo.CreateContainer. The proc generates the container_id (GUID)
        /// and returns the created row. Returns the new ContainerDto.
        /// </summary>
        ContainerDto CreateContainer(int shape, decimal length, decimal width, decimal height,
            string changedBy, int? containerType = null, string? description = null);

        /// <summary>
        /// Calls dbo.ModifyContainer for an existing container_id and returns the
        /// updated row.
        /// </summary>
        ContainerDto ModifyContainer(string containerId, int shape, decimal length, decimal width,
            decimal height, string changedBy, int? containerType = null, string? description = null);

        /// <summary>Reads a container directly by id (query, not a proc), or null.</summary>
        ContainerDto? GetById(string containerId);

        int Delete(string containerId);

        /// <summary>Returns a valid shape_id from lookup_package_shape (for test inputs), or null.</summary>
        int? GetAnyShapeId();
    }
}
