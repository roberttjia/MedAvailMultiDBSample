using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MedAvail.Common;

namespace MedAvail.TestRunner.Configuration;

/// <summary>
/// Builds per-database connection strings from configuration.
///
/// Values resolve through the standard configuration chain set up in Program.cs:
/// appsettings.json (placeholders) -> user-secrets -> environment variables.
/// Real credentials are NEVER stored in committed files; set them with:
///   dotnet user-secrets set "Sql:Server"   "myhost"
///   dotnet user-secrets set "Sql:UserId"   "myuser"
///   dotnet user-secrets set "Sql:Password" "mypassword"
/// or via environment variables (e.g. Sql__Server, Sql__Password).
/// </summary>
public sealed class ConfigurationConnectionStringProvider : IConnectionStringProvider
{
    private readonly IConfiguration _config;

    public ConfigurationConnectionStringProvider(IConfiguration config) => _config = config;

    public string GetConnectionString(MedAvailDatabase database)
    {
        var sql = _config.GetSection("Sql");

        var server = Require(sql, "Server");
        var userId = Require(sql, "UserId");
        var password = Require(sql, "Password");
        var port = sql.GetValue<int?>("Port") ?? 1433;
        var trustCert = sql.GetValue<bool?>("TrustServerCertificate") ?? true;

        var databaseName = sql.GetSection("Databases")[database.ToString()]
            ?? throw new InvalidOperationException(
                $"No database name configured for Sql:Databases:{database}.");

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{server},{port}",
            InitialCatalog = databaseName,
            UserID = userId,
            Password = password,
            TrustServerCertificate = trustCert,
            // keeps the sample responsive when the placeholder server is unreachable
            ConnectTimeout = 15
        };

        return builder.ConnectionString;
    }

    private static string Require(IConfigurationSection section, string key)
    {
        var value = section[key];
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith("__", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Sql:{key} is not configured. Set it via user-secrets or environment " +
                $"variable (placeholder value '{value}' is not usable).");
        }
        return value;
    }
}
