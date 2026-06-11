using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedAvail.DataAccess.Ef6.Entities
{
    // Minimal database-first EF6 entities for MedAvailDB (Core) schema-conversion
    // coverage. Each maps its real primary key plus a few representative typed
    // columns (bit / datetime / int / varbinary / string) so the mapping exercises
    // the type conversions that matter for migration. Query-only.

    [Table("medcenter")]
    public class MedcenterEf6
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("medcenter_id")] public int MedcenterId { get; set; }
        [Column("medcenter_mode")] public int MedcenterMode { get; set; }
        [Column("name")] public string? Name { get; set; }
        [Column("serial_number")] public string? SerialNumber { get; set; }
    }

    [Table("ResourceBinary")]
    public class ResourceBinaryEf6
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("resourceInfo_id")] public int ResourceInfoId { get; set; }
        [Column("node_id")] public int NodeId { get; set; }
        [Column("enabled")] public bool Enabled { get; set; }
        [Column("datacontent")] public byte[]? DataContent { get; set; }
        [Column("changed_on")] public DateTime ChangedOn { get; set; }
    }

    [Table("workflow_profile")]
    public class WorkflowProfileEf6
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("workflow_profile_id")] public string WorkflowProfileId { get; set; } = string.Empty;
        [Column("workflow_type")] public string WorkflowType { get; set; } = string.Empty;
        [Column("is_container")] public bool IsContainer { get; set; }
        [Column("workflow_definition")] public byte[] WorkflowDefinition { get; set; } = Array.Empty<byte>();
    }

    [Table("user")]
    public class UserEf6
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("user_id")] public int UserId { get; set; }
        [Column("username")] public string Username { get; set; } = string.Empty;
        [Column("active")] public bool Active { get; set; }
        [Column("changed_date")] public DateTime ChangedDate { get; set; }
    }

    [Table("kiosk")]
    public class KioskEf6
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("kiosk_id")] public string KioskId { get; set; } = string.Empty;
        [Column("name")] public string? Name { get; set; }
        [Column("created_on")] public DateTime CreatedOn { get; set; }
    }

    [Table("pacard")]
    public class PacardEf6
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("pacard_id")] public int PacardId { get; set; }
        [Column("card_number")] public long CardNumber { get; set; }
        [Column("active")] public bool Active { get; set; }
        [Column("changed_on")] public DateTime ChangedOn { get; set; }
    }

    [Table("legal_document")]
    public class LegalDocumentEf6
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("legal_document_id")] public string LegalDocumentId { get; set; } = string.Empty;
        [Column("language_id")] public int LanguageId { get; set; }
        [Column("data")] public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    [Table("language")]
    public class LanguageEf6
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("language_id")] public int LanguageId { get; set; }
        [Column("name")] public string Name { get; set; } = string.Empty;
        [Column("changed_on")] public DateTime ChangedOn { get; set; }
    }

    [Table("part")]
    public class PartEf6
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("part_id")] public int PartId { get; set; }
        [Column("part_type_id")] public int PartTypeId { get; set; }
        [Column("node_id")] public int NodeId { get; set; }
        [Column("is_enabled")] public bool IsEnabled { get; set; }
        [Column("changed_on")] public DateTime ChangedOn { get; set; }
    }

    [Table("PatientProfile")]
    public class PatientProfileEf6
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("PatientProfileId")] public string PatientProfileId { get; set; } = string.Empty;
        [Column("ExternalPatientId")] public string ExternalPatientId { get; set; } = string.Empty;
        [Column("AccountLocked")] public bool AccountLocked { get; set; }
        [Column("CreatedOn")] public DateTime CreatedOn { get; set; }
    }
}
