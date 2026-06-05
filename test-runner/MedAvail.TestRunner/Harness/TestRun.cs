using System;
using System.Collections.Generic;
using System.Linq;

namespace MedAvail.TestRunner.Harness;

/// <summary>
/// Aggregate of a whole test run; this is the root object serialized to
/// test-results-&lt;timestamp&gt;.json.
/// </summary>
public sealed class TestRun
{
    public string RunId { get; set; } = string.Empty;
    public string StartedAtUtc { get; set; } = string.Empty;
    public string FinishedAtUtc { get; set; } = string.Empty;
    public string Machine { get; set; } = Environment.MachineName;

    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }

    public List<TestResult> Results { get; set; } = new();

    public void Recount()
    {
        Total = Results.Count;
        Passed = Results.Count(r => r.Status == TestStatus.Pass);
        Failed = Results.Count(r => r.Status == TestStatus.Fail);
        Skipped = Results.Count(r => r.Status == TestStatus.Skip);
    }
}
