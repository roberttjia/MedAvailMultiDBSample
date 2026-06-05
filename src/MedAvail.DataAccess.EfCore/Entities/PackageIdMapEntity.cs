using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MedAvail.DataAccess.EfCore.Entities;

/// <summary>
/// EF Core entity for dbo.package_id_map. The table has no primary key, so it is
/// mapped as a keyless entity (configured with HasNoKey in the context). The same
/// table name exists in three databases; each context maps its own copy.
/// </summary>
[Keyless]
[Table("package_id_map")]
public class PackageIdMapEntity
{
    [Column("package_id")] public int PackageId { get; set; }
    [Column("package_guid")] public string? PackageGuid { get; set; }
}
