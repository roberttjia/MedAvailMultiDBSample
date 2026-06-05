namespace MedAvail.Common.Models
{
    /// <summary>
    /// Represents a row of dbo.package_id_map. This table exists (by the same
    /// name) in three of the four databases — Core, DataAcquisition and
    /// PackageManagement — which makes it the focal point for the
    /// entity-to-database association tests.
    ///
    /// The table has no primary key; package_id is treated as the logical key.
    /// </summary>
    public sealed class PackageIdMapDto
    {
        public int PackageId { get; set; }        // package_id  (int, NOT NULL)
        public string? PackageGuid { get; set; }  // package_guid (nvarchar(40), NULL)
    }
}
