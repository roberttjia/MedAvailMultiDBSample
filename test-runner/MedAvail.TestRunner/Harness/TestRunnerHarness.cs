using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace MedAvail.TestRunner.Harness;

/// <summary>
/// Runs tests, prints one parseable line per test, and writes a timestamped
/// JSON report at the end.
///
/// Parseable console format (stable, easy to grep/split):
///   TEST | &lt;STATUS&gt; | &lt;Category&gt; | &lt;Technology&gt; | &lt;ElapsedMs&gt;ms | &lt;Name&gt; | &lt;Message&gt;
/// e.g.
///   TEST | PASS | Connectivity | Ado | 251ms | Connect PackageManagement |
///   TEST | FAIL | Connectivity | Ado | 16ms  | Connect Core | login failed
///
/// Summary line:
///   SUMMARY | total=4 passed=4 failed=0 skipped=0 | results=test-results-....json
/// </summary>
public sealed class TestRunnerHarness
{
    private readonly TestRun _run;
    private readonly List<string> _logLines = new();

    public TestRunnerHarness()
    {
        var now = DateTime.UtcNow;
        _run = new TestRun
        {
            RunId = now.ToString("yyyyMMdd-HHmmss"),
            StartedAtUtc = now.ToString("o")
        };
    }

    /// <summary>
    /// Run one test. The body throws TestAssertionException to fail,
    /// TestSkippedException to skip, any other exception is an error (fail).
    /// </summary>
    public void Run(string name, string category, string technology, Action body)
    {
        var startedUtc = DateTime.UtcNow;
        var sw = Stopwatch.StartNew();
        TestStatus status;
        string? message = null;

        try
        {
            body();
            status = TestStatus.Pass;
        }
        catch (TestSkippedException ex)
        {
            status = TestStatus.Skip;
            message = ex.Message;
        }
        catch (TestAssertionException ex)
        {
            status = TestStatus.Fail;
            message = ex.Message;
        }
        catch (Exception ex)
        {
            status = TestStatus.Fail;
            message = $"{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            sw.Stop();
        }

        var result = new TestResult
        {
            Name = name,
            Category = category,
            Technology = technology,
            Status = status,
            ElapsedMs = sw.ElapsedMilliseconds,
            Message = message,
            StartedAtUtc = startedUtc.ToString("o")
        };
        _run.Results.Add(result);
        WriteLine(result);
    }

    /// <summary>Pre-recorded result (for probes that already ran their own timing).</summary>
    public void Record(TestResult result)
    {
        _run.Results.Add(result);
        WriteLine(result);
    }

    private void WriteLine(TestResult r)
    {
        var statusText = r.Status switch
        {
            TestStatus.Pass => "PASS",
            TestStatus.Fail => "FAIL",
            _ => "SKIP"
        };
        var line =
            $"TEST | {statusText} | {r.Category} | {r.Technology} | {r.ElapsedMs}ms | {r.Name} | {r.Message}";
        _logLines.Add(line);
        Console.WriteLine(line);
    }

    /// <summary>
    /// Finalizes counts, writes the JSON report, prints the summary line,
    /// and returns a process exit code (0 = no failures).
    /// </summary>
    public int Finish(string outputDirectory)
    {
        _run.FinishedAtUtc = DateTime.UtcNow.ToString("o");
        _run.Recount();

        Directory.CreateDirectory(outputDirectory);

        // 1. JSON report
        var jsonName = $"test-results-{_run.RunId}.json";
        var jsonPath = Path.Combine(outputDirectory, jsonName);
        var json = JsonSerializer.Serialize(_run, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonPath, json);

        // 2. Newline-delimited log (parseable lines + summary)
        var summary =
            $"SUMMARY | total={_run.Total} passed={_run.Passed} failed={_run.Failed} " +
            $"skipped={_run.Skipped} | results={jsonName}";
        var logName = $"test-results-{_run.RunId}.log";
        var logPath = Path.Combine(outputDirectory, logName);
        var logBody = string.Join(Environment.NewLine, _logLines) +
                      Environment.NewLine + summary + Environment.NewLine;
        File.WriteAllText(logPath, logBody);

        Console.WriteLine();
        Console.WriteLine(summary);
        Console.WriteLine($"JSON report: {jsonPath}");
        Console.WriteLine($"Log file   : {logPath}");

        return _run.Failed == 0 ? 0 : 1;
    }
}
