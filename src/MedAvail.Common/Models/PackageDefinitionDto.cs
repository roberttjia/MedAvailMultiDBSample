using System;

namespace MedAvail.Common.Models
{
    /// <summary>
    /// Database-agnostic representation of dbo.package_definition
    /// (MedAvailPackageManagementDb). Shared by all data-access implementations
    /// so the business layer never depends on a specific ORM's entity type.
    ///
    /// package_definition_id is IDENTITY (DB-generated).
    /// AuditId is the SQL Server rowversion column AuditID_MA (DB-managed).
    /// </summary>
    public sealed class PackageDefinitionDto
    {
        public int PackageDefinitionId { get; set; }            // package_definition_id (IDENTITY PK)
        public string Description { get; set; } = string.Empty; // description
        public string PackageCode { get; set; } = string.Empty; // package_code
        public int PackageCodeAbsLocationId { get; set; }       // package_code_abs_location_id
        public decimal PackageHeight { get; set; }              // package_height
        public decimal PackageWidth { get; set; }               // package_width
        public decimal PackageLength { get; set; }              // package_length
        public int Shape { get; set; }                          // shape
        public bool Cap { get; set; }                           // cap
        public decimal CapDiameter { get; set; }                // cap_diameter
        public decimal CapLength { get; set; }                  // cap_length
        public decimal Weight { get; set; }                     // weight
        public int FragileScale { get; set; }                   // fragile_scale
        public int LotCodeAbsLocation { get; set; }             // lot_code_abs_location
        public int LotCodeRelLocation { get; set; }             // lot_code_rel_location
        public int ExpiryAbsLocation { get; set; }              // expiry_abs_location
        public int ExpiryRelLocation { get; set; }              // expiry_rel_location
        public bool Valid { get; set; }                         // valid
        public int DefinitionStateId { get; set; }              // definition_state_id
        public string? DefinitionRejectReason { get; set; }     // definition_reject_reason (NULL)
        public int ProductCategoryId { get; set; }              // product_category_id
        public string ProductName { get; set; } = string.Empty; // product_name
        public int ProductCodeTypeId { get; set; }              // product_code_type_id
        public string ProductCode { get; set; } = string.Empty; // product_code
        public string ProductManufacturer { get; set; } = string.Empty; // product_manufacturer
        public decimal PackageSize { get; set; }                // package_size
        public int PackageSizeUomId { get; set; }               // package_size_uom_id
        public string? DrugSchedule { get; set; }               // drug_schedule (NULL)
        public DateTime ChangedDate { get; set; }               // changed_date
        public string ChangedBy { get; set; } = string.Empty;   // changed_by
        public int? ExpirationMethod { get; set; }              // expiration_method (NULL)
        public int? DaysToAdvisedExpiration { get; set; }       // days_to_advised_expiration (NULL)
        public int? DrugScheduleId { get; set; }                // drug_schedule_id (NULL)
        public bool ControlledSubstance { get; set; }           // controlled_substance
        public string CreatedBy { get; set; } = string.Empty;   // created_by
        public DateTime CreatedOn { get; set; }                 // created_on
        public int LotCodeSource { get; set; }                  // lot_code_source
        public byte[]? AuditId { get; set; }                    // AuditID_MA (rowversion, DB-managed)
        public string? Notes { get; set; }                      // notes (NULL)
        public int PackageDefinitionTypeId { get; set; }        // package_definition_type_id
        public bool IsDemoPackage { get; set; }                 // is_demo_package
    }
}
