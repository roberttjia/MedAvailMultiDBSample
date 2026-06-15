using System.Data.Entity;
using Npgsql;

namespace MedAvail.DataAccess.Ef6
{
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
