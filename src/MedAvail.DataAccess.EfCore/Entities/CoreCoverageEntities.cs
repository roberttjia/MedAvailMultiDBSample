using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MedAvail.DataAccess.EfCore.Entities;

// Minimal database-first EF Core entities for MedAvailDB (Core) schema-conversion
// coverage. Each maps its real primary key plus a few representative typed columns
// (bit / datetime / int / bigint / varbinary / string) to exercise the type
// conversions that matter for migration. Query-only. Tables use [Key]; the two
// views are mapped [Keyless].

[Table("medcenter")]
public class MedcenterCore
{
    [Key, Column("medcenter_id")] public int MedcenterId { get; set; }
    [Column("medcenter_mode")] public int MedcenterMode { get; set; }
    [Column("name")] public string? Name { get; set; }
    [Column("serial_number")] public string? SerialNumber { get; set; }
}

[Table("ResourceBinary")]
public class ResourceBinaryCore
{
    [Key, Column("resourceInfo_id")] public int ResourceInfoId { get; set; }
    [Column("node_id")] public int NodeId { get; set; }
    [Column("enabled")] public bool Enabled { get; set; }
    [Column("datacontent")] public byte[]? DataContent { get; set; }
    [Column("changed_on")] public DateTime ChangedOn { get; set; }
}

[Table("workflow_profile")]
public class WorkflowProfileCore
{
    [Key, Column("workflow_profile_id")] public string WorkflowProfileId { get; set; } = string.Empty;
    [Column("workflow_type")] public string WorkflowType { get; set; } = string.Empty;
    [Column("is_container")] public bool IsContainer { get; set; }
    [Column("workflow_definition")] public byte[] WorkflowDefinition { get; set; } = Array.Empty<byte>();
}

[Table("user")]
public class UserCore
{
    [Key, Column("user_id")] public int UserId { get; set; }
    [Column("username")] public string Username { get; set; } = string.Empty;
    [Column("active")] public bool Active { get; set; }
    [Column("changed_date")] public DateTime ChangedDate { get; set; }
}

[Table("kiosk")]
public class KioskCore
{
    [Key, Column("kiosk_id")] public string KioskId { get; set; } = string.Empty;
    [Column("name")] public string? Name { get; set; }
    [Column("created_on")] public DateTime CreatedOn { get; set; }
}

[Table("pacard")]
public class PacardCore
{
    [Key, Column("pacard_id")] public int PacardId { get; set; }
    [Column("card_number")] public long CardNumber { get; set; }
    [Column("active")] public bool Active { get; set; }
    [Column("changed_on")] public DateTime ChangedOn { get; set; }
}

[Table("legal_document")]
public class LegalDocumentCore
{
    [Key, Column("legal_document_id")] public string LegalDocumentId { get; set; } = string.Empty;
    [Column("language_id")] public int LanguageId { get; set; }
    [Column("data")] public byte[] Data { get; set; } = Array.Empty<byte>();
}

[Table("language")]
public class LanguageCore
{
    [Key, Column("language_id")] public int LanguageId { get; set; }
    [Column("name")] public string Name { get; set; } = string.Empty;
    [Column("changed_on")] public DateTime ChangedOn { get; set; }
}

[Table("part")]
public class PartCore
{
    [Key, Column("part_id")] public int PartId { get; set; }
    [Column("part_type_id")] public int PartTypeId { get; set; }
    [Column("node_id")] public int NodeId { get; set; }
    [Column("is_enabled")] public bool IsEnabled { get; set; }
    [Column("changed_on")] public DateTime ChangedOn { get; set; }
}

[Table("PatientProfile")]
public class PatientProfileCore
{
    [Key, Column("PatientProfileId")] public string PatientProfileId { get; set; } = string.Empty;
    [Column("ExternalPatientId")] public string ExternalPatientId { get; set; } = string.Empty;
    [Column("AccountLocked")] public bool AccountLocked { get; set; }
    [Column("CreatedOn")] public DateTime CreatedOn { get; set; }
}

[Keyless]
[Table("AllDuplicateParts")]
public class AllDuplicatePartsCore
{
    [Column("part_id")] public int PartId { get; set; }
    [Column("part_type_id")] public int PartTypeId { get; set; }
    [Column("node_id")] public int NodeId { get; set; }
}

[Keyless]
[Table("bin_stats")]
public class BinStatsCore
{
    [Column("bin_definition_id")] public int BinDefinitionId { get; set; }
    [Column("slot_count")] public int SlotCount { get; set; }
}
