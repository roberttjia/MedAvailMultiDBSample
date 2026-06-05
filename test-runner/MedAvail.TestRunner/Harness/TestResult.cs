using System.Text.Json.Serialization;

namespace MedAvail.TestRunner.Harness;

/// <summary>
/// Outcome of a single test. Serialized into the test-results JSON file and
/// also rendered as a single parseable console line by <see cref="TestRunnerHarness"/>.
/// </summary>
public sealed class TestResult
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional grouping (e.g. "Connectivity", "PackageDefinition").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Access technology exercised: Ado, EfCore, Ef6, or None.</summary>
    public string Technology { get; set; } = "None";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TestStatus Status { get; set; }

    public long ElapsedMs { get; set; }

    /// <summary>Human-readable detail; failure reason when Status == Fail.</summary>
    public string? Message { get; set; }

    public string StartedAtUtc { get; set; } = string.Empty;
}
