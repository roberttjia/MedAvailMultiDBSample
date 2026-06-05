namespace MedAvail.Common
{
    /// <summary>
    /// The four MedAvail databases that live on a single SQL Server instance.
    /// Each has its own connection string; EF contexts are one-per-database.
    /// </summary>
    public enum MedAvailDatabase
    {
        Core,                // MedAvailDB
        Auditing,            // MedAvailAuditingDb
        DataAcquisition,     // MedAvailDataAcquisitionDb
        PackageManagement    // MedAvailPackageManagementDb
    }
}
