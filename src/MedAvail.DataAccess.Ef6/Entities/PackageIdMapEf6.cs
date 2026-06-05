using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedAvail.DataAccess.Ef6.Entities
{
    /// <summary>
    /// EF6 entity for dbo.package_id_map. The table has no primary key. EF6 has no
    /// first-class keyless support, so package_id is declared as the entity key for
    /// querying only; all writes are done via parameterized raw SQL.
    /// </summary>
    [Table("package_id_map")]
    public class PackageIdMapEf6
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("package_id")]
        public int PackageId { get; set; }

        [Column("package_guid")]
        public string? PackageGuid { get; set; }
    }
}
