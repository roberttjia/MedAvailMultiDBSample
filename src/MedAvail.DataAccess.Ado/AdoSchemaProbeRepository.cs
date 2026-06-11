using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using MedAvail.Common;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>One column's schema as reported by the live database.</summary>
    public sealed class ProbedColumn
    {
        public string Name { get; set; } = string.Empty;
        public string DataTypeName { get; set; } = string.Empty;  // SQL Server type name
        public Type ClrType { get; set; } = typeof(object);       // mapped .NET type
        public bool AllowDbNull { get; set; }
    }

    /// <summary>Result of probing a table or view: its column schema + sample row count.</summary>
    public sealed class SchemaProbeResult
    {
        public string ObjectName { get; set; } = string.Empty;
        public IReadOnlyList<ProbedColumn> Columns { get; set; } = Array.Empty<ProbedColumn>();
        public int SampleRowCount { get; set; }
        public long TotalRowCount { get; set; }
    }

    /// <summary>
    /// Generic ADO.NET schema/structure probe for any table or view in a database,
    /// bound to a specific MedAvailDatabase (default Core / MedAvailDB).
    ///
    /// Used for schema-conversion coverage: a probe SELECT validates that an
    /// object exists, is queryable, and exposes the expected columns/types — which
    /// is exactly what must survive a SQL Server -> PostgreSQL migration. Read-only,
    /// so it needs no seeding and works whether or not the object has data.
    /// </summary>
    public sealed class AdoSchemaProbeRepository : AdoRepositoryBase
    {
        public AdoSchemaProbeRepository(IConnectionStringProvider connections,
            MedAvailDatabase database = MedAvailDatabase.Core)
            : base(connections, database) { }

        /// <summary>
        /// Reads column schema for a table/view via a TOP(sampleSize) probe and a
        /// COUNT. The object name is validated against a strict identifier pattern
        /// (no user input here, but defensive since it is concatenated).
        /// </summary>
        public SchemaProbeResult Probe(string objectName, int sampleSize = 5)
        {
            ValidateIdentifier(objectName);

            using var conn = OpenConnection();

            var columns = new List<ProbedColumn>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT TOP ({sampleSize}) * FROM dbo.[{objectName}];";
                using var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);

                var schema = reader.GetColumnSchema();
                foreach (var col in schema)
                {
                    columns.Add(new ProbedColumn
                    {
                        Name = col.ColumnName,
                        DataTypeName = col.DataTypeName ?? "unknown",
                        ClrType = col.DataType ?? typeof(object),
                        AllowDbNull = col.AllowDBNull ?? true
                    });
                }

                var sampled = 0;
                while (reader.Read()) sampled++;

                return new SchemaProbeResult
                {
                    ObjectName = objectName,
                    Columns = columns,
                    SampleRowCount = sampled,
                    TotalRowCount = 0 // filled below
                };
            }
        }

        /// <summary>Full probe including a total row count.</summary>
        public SchemaProbeResult ProbeWithCount(string objectName, int sampleSize = 5)
        {
            var result = Probe(objectName, sampleSize);
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT_BIG(*) FROM dbo.[{objectName}];";
            result.TotalRowCount = Convert.ToInt64(cmd.ExecuteScalar());
            return result;
        }

        private static void ValidateIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                !System.Text.RegularExpressions.Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$"))
            {
                throw new ArgumentException($"Invalid object identifier: '{name}'.", nameof(name));
            }
        }
    }
}
