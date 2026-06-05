using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedAvail.DataAccess.Ef6.Entities
{
    /// <summary>
    /// EF6 entity for dbo.package_definition (MedAvailPackageManagementDb).
    /// AuditID_MA is the SQL Server rowversion, mapped as an EF6 concurrency token.
    /// </summary>
    [Table("package_definition")]
    public class PackageDefinitionEf6
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("package_definition_id")]
        public int PackageDefinitionId { get; set; }

        [Column("description")] public string Description { get; set; } = string.Empty;
        [Column("package_code")] public string PackageCode { get; set; } = string.Empty;
        [Column("package_code_abs_location_id")] public int PackageCodeAbsLocationId { get; set; }
        [Column("package_height")] public decimal PackageHeight { get; set; }
        [Column("package_width")] public decimal PackageWidth { get; set; }
        [Column("package_length")] public decimal PackageLength { get; set; }
        [Column("shape")] public int Shape { get; set; }
        [Column("cap")] public bool Cap { get; set; }
        [Column("cap_diameter")] public decimal CapDiameter { get; set; }
        [Column("cap_length")] public decimal CapLength { get; set; }
        [Column("weight")] public decimal Weight { get; set; }
        [Column("fragile_scale")] public int FragileScale { get; set; }
        [Column("lot_code_abs_location")] public int LotCodeAbsLocation { get; set; }
        [Column("lot_code_rel_location")] public int LotCodeRelLocation { get; set; }
        [Column("expiry_abs_location")] public int ExpiryAbsLocation { get; set; }
        [Column("expiry_rel_location")] public int ExpiryRelLocation { get; set; }
        [Column("valid")] public bool Valid { get; set; }
        [Column("definition_state_id")] public int DefinitionStateId { get; set; }
        [Column("definition_reject_reason")] public string? DefinitionRejectReason { get; set; }
        [Column("product_category_id")] public int ProductCategoryId { get; set; }
        [Column("product_name")] public string ProductName { get; set; } = string.Empty;
        [Column("product_code_type_id")] public int ProductCodeTypeId { get; set; }
        [Column("product_code")] public string ProductCode { get; set; } = string.Empty;
        [Column("product_manufacturer")] public string ProductManufacturer { get; set; } = string.Empty;
        [Column("package_size")] public decimal PackageSize { get; set; }
        [Column("package_size_uom_id")] public int PackageSizeUomId { get; set; }
        [Column("drug_schedule")] public string? DrugSchedule { get; set; }
        [Column("changed_date")] public DateTime ChangedDate { get; set; }
        [Column("changed_by")] public string ChangedBy { get; set; } = string.Empty;
        [Column("expiration_method")] public int? ExpirationMethod { get; set; }
        [Column("days_to_advised_expiration")] public int? DaysToAdvisedExpiration { get; set; }
        [Column("drug_schedule_id")] public int? DrugScheduleId { get; set; }
        [Column("controlled_substance")] public bool ControlledSubstance { get; set; }
        [Column("created_by")] public string CreatedBy { get; set; } = string.Empty;
        [Column("created_on")] public DateTime CreatedOn { get; set; }
        [Column("lot_code_source")] public int LotCodeSource { get; set; }

        [Timestamp]
        [Column("AuditID_MA")]
        public byte[]? AuditId { get; set; }

        [Column("notes")] public string? Notes { get; set; }
        [Column("package_definition_type_id")] public int PackageDefinitionTypeId { get; set; }
        [Column("is_demo_package")] public bool IsDemoPackage { get; set; }
    }
}
