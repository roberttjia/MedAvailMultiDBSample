namespace MedAvail.Common
{
    /// <summary>
    /// Lightweight description of a result set returned by a stored procedure.
    /// Used to validate procs that return a row set (a SQL Server result set),
    /// which the schema-migration tool converts to a PostgreSQL procedure with a
    /// refcursor OUT parameter — a calling-convention change worth testing across
    /// ADO.NET, EF Core, and EF6.
    /// </summary>
    public sealed class StoredProcResultInfo
    {
        /// <summary>True if the proc executed and a result set was returned.</summary>
        public bool ReturnedResultSet { get; set; }

        /// <summary>Number of columns in the (first) result set.</summary>
        public int FieldCount { get; set; }

        /// <summary>Number of rows read from the (first) result set.</summary>
        public int RowCount { get; set; }
    }
}
