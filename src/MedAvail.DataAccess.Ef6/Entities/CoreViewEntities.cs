using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedAvail.DataAccess.Ef6.Entities
{
    // EF6 entities for the two MedAvailDB views. EF6 requires a key even for a
    // view, so a non-null column is declared as the key (query-only mapping).

    [Table("AllDuplicateParts")]
    public class AllDuplicatePartsEf6
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("part_id")] public int PartId { get; set; }
        [Column("part_type_id")] public int PartTypeId { get; set; }
        [Column("node_id")] public int NodeId { get; set; }
    }

    [Table("bin_stats")]
    public class BinStatsEf6
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("bin_definition_id")] public int BinDefinitionId { get; set; }
        [Column("slot_count")] public int SlotCount { get; set; }
    }
}
