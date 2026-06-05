using System;

namespace MedAvail.Common.Models
{
    /// <summary>
    /// Represents a dbo.container row in MedAvailPackageManagementDb. Created and
    /// modified through the CreateContainer / ModifyContainer stored procedures,
    /// which return the affected row(s).
    /// </summary>
    public sealed class ContainerDto
    {
        public string ContainerId { get; set; } = string.Empty;   // container_id (nvarchar(255) PK, a GUID)
        public string? Description { get; set; }                  // description
        public int Shape { get; set; }                            // shape (FK -> lookup_package_shape)
        public decimal Length { get; set; }                       // length
        public decimal Width { get; set; }                        // width
        public decimal Height { get; set; }                       // height
        public int PackageDefinitionTypeId { get; set; }          // package_definition_type_id (proc's @container_type)
        public string? ChangedBy { get; set; }                    // changed_by
        public DateTime ChangedOn { get; set; }                   // changed_on
    }
}
