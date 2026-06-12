using Microsoft.Extensions.Configuration;
using MedAvail.Common;
using MedAvail.DataAccess.Ado;
using MedAvail.TestRunner.Configuration;
using MedAvail.TestRunner.Harness;
using MedAvail.TestRunner.Tests;

namespace MedAvail.TestRunner;

internal static class Program
{
    private static int Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        IConnectionStringProvider connections = new ConfigurationConnectionStringProvider(config);
        IDatabaseConnectivityChecker checker = new AdoDatabaseConnectivityChecker(connections);

        Console.WriteLine("MedAvail multi-database sample");
        Console.WriteLine("==============================\n");

        var harness = new TestRunnerHarness();

        // --- Connectivity tests: one per database ---
        foreach (MedAvailDatabase db in Enum.GetValues<MedAvailDatabase>())
        {
            harness.Run(
                name: $"Connect to {db}",
                category: "Connectivity",
                technology: "Ado",
                body: () =>
                {
                    var result = checker.Check(db);
                    if (!result.Succeeded)
                    {
                        // Missing/placeholder credentials => skip; anything else is a real failure.
                        var msg = result.Error ?? "connection failed";
                        if (msg.Contains("not configured", StringComparison.OrdinalIgnoreCase))
                            Assert.Skip(msg);
                        throw new TestAssertionException(msg);
                    }
                    Assert.True(
                        result.ConnectedToExpectedDatabase,
                        $"connected to '{result.ReportedDatabaseName}' but expected '{result.ExpectedDatabaseName}'");
                });
        }

        // --- ADO.NET tests: CRUD, queries, stored procedures ---
        new AdoTests(connections).RegisterAll(harness);

        // --- ADO.NET schema-conversion coverage for MedAvailDB (Core): 10 tables + 2 views ---
        new AdoCoreCoverageTests(connections).RegisterAll(harness);

        // --- EF Core tests: CRUD, queries, keyless entity, rowversion concurrency ---
        new EfCoreTests(connections).RegisterAll(harness);

        // --- EF Core schema-conversion coverage for MedAvailDB (Core): 10 tables + 2 views ---
        new EfCoreCoreCoverageTests(connections).RegisterAll(harness);

        // --- Result-set stored proc (refcursor-signature candidate) across all 3 technologies ---
        new ResultSetProcTests(connections).RegisterAll(harness);

        // --- Business-layer full-stack tests: runner -> service -> repository -> live DB ---
        new BusinessLayerTests(connections).RegisterAll(harness);
        new BusinessLayerPackageIdMapTests(connections).RegisterAll(harness);

        // --- EF6 tests (EntityFramework 6.5.x on .NET 8): CRUD, queries, concurrency ---
        new Ef6Tests(connections).RegisterAll(harness);

        // --- EF6 schema-conversion coverage for MedAvailDB (Core): 10 tables + 2 views ---
        new Ef6CoreCoverageTests(connections).RegisterAll(harness);

        // Results go to the repo root /test-results by default; override with --output <dir>.
        var outputOverride = GetOption(args, "--output");
        var outputDir = OutputLocation.Resolve(outputOverride);
        return harness.Finish(outputDir);
    }

    /// <summary>Reads a "--name value" style option from args; null if absent.</summary>
    private static string? GetOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }
}
