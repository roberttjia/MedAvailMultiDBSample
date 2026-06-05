namespace MedAvail.Common
{
    /// <summary>
    /// Outcome of a connectivity probe against a single MedAvail database.
    /// </summary>
    public sealed class ConnectivityResult
    {
        public MedAvailDatabase Database { get; }

        /// <summary>The database name the server reported (DB_NAME()).</summary>
        public string? ReportedDatabaseName { get; }

        /// <summary>The database name we expected to be connected to.</summary>
        public string ExpectedDatabaseName { get; }

        public bool Succeeded { get; }

        /// <summary>Server product version (@@VERSION-derived), when reachable.</summary>
        public string? ServerVersion { get; }

        /// <summary>Round-trip time for opening + probing, in milliseconds.</summary>
        public long ElapsedMs { get; }

        /// <summary>Error message when the probe failed; null on success.</summary>
        public string? Error { get; }

        /// <summary>True when connected, AND the server reports the expected database.</summary>
        public bool ConnectedToExpectedDatabase =>
            Succeeded &&
            string.Equals(ReportedDatabaseName, ExpectedDatabaseName, System.StringComparison.OrdinalIgnoreCase);

        private ConnectivityResult(
            MedAvailDatabase database, string expectedDatabaseName, bool succeeded,
            string? reportedDatabaseName, string? serverVersion, long elapsedMs, string? error)
        {
            Database = database;
            ExpectedDatabaseName = expectedDatabaseName;
            Succeeded = succeeded;
            ReportedDatabaseName = reportedDatabaseName;
            ServerVersion = serverVersion;
            ElapsedMs = elapsedMs;
            Error = error;
        }

        public static ConnectivityResult Success(
            MedAvailDatabase database, string expectedDatabaseName,
            string reportedDatabaseName, string? serverVersion, long elapsedMs) =>
            new(database, expectedDatabaseName, true, reportedDatabaseName, serverVersion, elapsedMs, null);

        public static ConnectivityResult Failure(
            MedAvailDatabase database, string expectedDatabaseName, string error, long elapsedMs) =>
            new(database, expectedDatabaseName, false, null, null, elapsedMs, error);
    }
}
