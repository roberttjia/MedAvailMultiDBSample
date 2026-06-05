using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;

namespace MedAvail.DataAccess.Ef6
{
    /// <summary>
    /// EF6 on modern .NET (net8) has no app.config-based provider registration,
    /// so the SQL Server provider and the ADO.NET provider factory must be
    /// registered in code. Applied to every context in this assembly via the
    /// [DbConfigurationType] attribute.
    /// </summary>
    public sealed class Ef6SqlServerConfiguration : DbConfiguration
    {
        public Ef6SqlServerConfiguration()
        {
            // EF provider services for SQL Server.
            SetProviderServices(
                SqlProviderServices.ProviderInvariantName,
                SqlProviderServices.Instance);

            // ADO.NET DbProviderFactory (System.Data.SqlClient) used by EF6.
            SetProviderFactory(
                SqlProviderServices.ProviderInvariantName,
                SqlClientFactory.Instance);

            // No default connection factory that probes LocalDB — connections are
            // always supplied explicitly via the context constructor.
            SetDefaultConnectionFactory(new SqlConnectionFactory());
        }
    }
}
