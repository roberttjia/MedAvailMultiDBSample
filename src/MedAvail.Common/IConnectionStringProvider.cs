namespace MedAvail.Common
{
    /// <summary>
    /// Supplies the connection string for a given MedAvail database.
    /// Implementations resolve credentials from a secure source (user-secrets,
    /// environment variables) — never hard-coded in committed source.
    /// </summary>
    public interface IConnectionStringProvider
    {
        string GetConnectionString(MedAvailDatabase database);
    }
}
