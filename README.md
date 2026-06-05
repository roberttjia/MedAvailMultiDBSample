# MedAvail Multi-Database Sample

A console test runner that exercises the four MedAvail databases through three
data-access technologies — **ADO.NET**, **EF Core 8**, and **EF6** — to verify
that each code-side entity is correctly associated with its owning database.

## The four databases

| Logical name (`MedAvailDatabase`) | SQL Server database     |
|-----------------------------------|-------------------------|
| `Core`                            | `MedAvailDB`            |
| `Auditing`                        | `MedAvailAuditingDb`    |
| `DataAcquisition`                 | `MedAvailDataAcquisitionDb` |
| `PackageManagement`               | `MedAvailPackageManagementDb` |

All four live on a single SQL Server instance (port 1433).

## Projects

```
src/
  MedAvail.Common            (netstandard2.0)  abstractions, DTOs, repo interfaces
  MedAvail.Business          (netstandard2.0)  business services over the repos
  MedAvail.DataAccess.Ado    (netstandard2.0)  ADO.NET implementation
  MedAvail.DataAccess.EfCore (net8.0)          EF Core 8 implementation
  MedAvail.DataAccess.Ef6    (net48)           EF6 implementation
test-runner/
  MedAvail.TestRunner        (net8.0)          console host + scenarios
```

The runner depends on the business layer and the data-access implementations;
it never calls a database directly.

## Configuring credentials (no secrets in source)

`appsettings.json` ships with **placeholders** (`__SQL_SERVER__`, etc.). Supply
real values at runtime via user-secrets (preferred for dev) or environment
variables. Nothing real is committed.

```bash
cd test-runner/MedAvail.TestRunner
dotnet user-secrets set "Sql:Server"   "your-sql-host"
dotnet user-secrets set "Sql:UserId"   "your-sql-login"
dotnet user-secrets set "Sql:Password" "your-sql-password"
```

Equivalent environment variables (note the double underscore):

```
Sql__Server=your-sql-host
Sql__UserId=your-sql-login
Sql__Password=your-sql-password
```

Database names and port already have working defaults in `appsettings.json`.

## Running

```bash
dotnet run --project test-runner/MedAvail.TestRunner
```

By default the runner writes its reports to a `test-results/` folder at the
**repository root**. Override the location with `--output`:

```bash
dotnet run --project test-runner/MedAvail.TestRunner -- --output C:\reports
```

(The `--` separates `dotnet run` arguments from the app's own arguments.)

## Test output and reports

Each test prints a single, parseable line to the console:

```
TEST | <STATUS> | <Category> | <Technology> | <ElapsedMs>ms | <Name> | <Message>
```

`<STATUS>` is one of `PASS`, `FAIL`, or `SKIP`. The fields are separated by
` | ` (space-pipe-space) so they are easy to split or grep. Example:

```
TEST | PASS | Connectivity | Ado | 245ms | Connect to DataAcquisition |
TEST | FAIL | Connectivity | Ado | 16ms  | Connect to Core | login failed for user '...'
```

A summary line follows the tests:

```
SUMMARY | total=4 passed=4 failed=0 skipped=0 | results=test-results-20260602-030217.json
```

The process exit code is **0** when there are no failures, **1** otherwise
(skips do not fail the run), so it slots into CI directly.

### Report files

Every run writes two timestamped files to the output folder
(`test-results-<yyyyMMdd-HHmmss>.*`):

| File   | Contents |
|--------|----------|
| `.json` | Structured report: run metadata, rollup counts, and a `Results[]` array with each test's `Name`, `Category`, `Technology`, `Status`, `ElapsedMs`, `Message`, and `StartedAtUtc`. |
| `.log`  | The newline-delimited `TEST | ...` lines plus the `SUMMARY` line — the same text shown on the console. |

Example `.json`:

```json
{
  "RunId": "20260602-030217",
  "StartedAtUtc": "2026-06-02T03:02:17.0000000Z",
  "FinishedAtUtc": "2026-06-02T03:02:18.2000000Z",
  "Machine": "EXAMPLE-HOST",
  "Total": 4,
  "Passed": 4,
  "Failed": 0,
  "Skipped": 0,
  "Results": [
    {
      "Name": "Connect to Core",
      "Category": "Connectivity",
      "Technology": "Ado",
      "Status": "Pass",
      "ElapsedMs": 515,
      "Message": null,
      "StartedAtUtc": "2026-06-02T03:02:17.0200000Z"
    }
  ]
}
```

### Parsing examples

Grep just the failures from a log:

```bash
grep "^TEST | FAIL" test-results/test-results-*.log
```

PowerShell — show failed test names from the latest JSON report:

```powershell
$latest = Get-ChildItem test-results\test-results-*.json |
          Sort-Object LastWriteTime | Select-Object -Last 1
(Get-Content $latest -Raw | ConvertFrom-Json).Results |
    Where-Object Status -eq 'Fail' |
    Select-Object Name, Technology, Message
```

> Reports are git-ignored (`test-results/`), so they never get committed.

