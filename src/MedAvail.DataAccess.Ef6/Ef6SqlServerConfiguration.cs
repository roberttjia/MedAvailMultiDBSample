using System.Data.Entity;
using Npgsql;

namespace MedAvail.DataAccess.Ef6
{
    /// <summary>
    /// EF6 on modern .NET (net8) has no app.config-based provider registration,
    /// so the Npgsql provider and the ADO.NET provider factory must be registered
    /// in code. Applied to every context in this assembly via the
    /// [DbConfigurationType] attribute.
    /// </summary>
    public sealed class Ef6NpgsqlConfiguration : DbConfiguration
    {
        public Ef6NpgsqlConfiguration()
        {
            SetProviderServices("Npgsql", NpgsqlServices.Instance);
            SetProviderFactory("Npgsql", NpgsqlFactory.Instance);
            SetDefaultConnectionFactory(new NpgsqlConnectionFactory());
        }
    }
}
