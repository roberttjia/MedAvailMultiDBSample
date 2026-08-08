using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using MedAvail.Common;
using MedAvail.Common.Models;
using MedAvail.Common.Repositories;

namespace MedAvail.DataAccess.Ado
{
    /// <summary>
    /// ADO.NET access to package_definition (MedAvailPackageManagementDb).
    /// Demonstrates query + CRUD. package_definition has many FK dependencies on
    /// lookup tables, so Insert copies FK values from an existing row to remain
    /// valid without seeding lookups.
    /// </summary>
    public sealed class AdoPackageDefinitionRepository : AdoRepositoryBase, IPackageDefinitionRepository
    {
        private const string SelectColumns =
            "package_definition_id, description, package_code, package_code_abs_location_id, " +
            "package_height, package_width, package_length, shape, cap, cap_diameter, cap_length, " +
            "weight, fragile_scale, lot_code_abs_location, lot_code_rel_location, expiry_abs_location, " +
            "expiry_rel_location, valid, definition_state_id, definition_reject_reason, product_category_id, " +
            "product_name, product_code_type_id, product_code, product_manufacturer, package_size, " +
            "package_size_uom_id, drug_schedule, changed_date, changed_by, expiration_method, " +
            "days_to_advised_expiration, drug_schedule_id, controlled_substance, created_by, created_on, " +
            "lot_code_source, AuditID_MA, notes, package_definition_type_id, is_demo_package";

        public AdoPackageDefinitionRepository(IConnectionStringProvider connections)
            : base(connections, MedAvailDatabase.PackageManagement) { }

        public IReadOnlyList<PackageDefinitionDto> GetRecent(int maxRows = 100)
        {
            var list = new List<PackageDefinitionDto>();
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                $"SELECT {SelectColumns} FROM package_definition ORDER BY package_definition_id DESC LIMIT @max;";
            cmd.Parameters.Add(Param("@max", NpgsqlDbType.Integer, maxRows));
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(Map(reader));
            return list;
        }

        public PackageDefinitionDto? GetById(int packageDefinitionId)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                $"SELECT {SelectColumns} FROM package_definition WHERE package_definition_id = @id;";
            cmd.Parameters.Add(Param("@id", NpgsqlDbType.Integer, packageDefinitionId));
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        public int GetMaxId()
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(MAX(package_definition_id), 0) FROM package_definition;";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int Insert(PackageDefinitionDto definition)
        {
            using var conn = OpenConnection();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO package_definition
(description, package_code, package_code_abs_location_id, package_height, package_width,
 package_length, shape, cap, cap_diameter, cap_length, weight, fragile_scale,
 lot_code_abs_location, lot_code_rel_location, expiry_abs_location, expiry_rel_location,
 valid, definition_state_id, product_category_id, product_name, product_code_type_id,
 product_code, product_manufacturer, package_size, package_size_uom_id, changed_date,
 changed_by, controlled_substance, created_by, created_on, lot_code_source,
 package_definition_type_id, is_demo_package)
VALUES
(@description, @package_code, @pcal, @height, @width, @length, @shape, @cap, @capd, @capl,
 @weight, @fragile, @lcal, @lcrl, @eal, @erl, @valid, @defstate, @prodcat, @product_name,
 @prodcodetype, @product_code, @manufacturer, @size, @sizeuom, @changed_date, @changed_by,
 @controlled, @created_by, @created_on, @lotsource, @deftype, @isdemo)
RETURNING package_definition_id;";

            cmd.Parameters.Add(Param("@description", NpgsqlDbType.Text, definition.Description));
            cmd.Parameters.Add(Param("@package_code", NpgsqlDbType.Text, definition.PackageCode));
            cmd.Parameters.Add(Param("@pcal", NpgsqlDbType.Integer, definition.PackageCodeAbsLocationId));
            cmd.Parameters.Add(Param("@height", NpgsqlDbType.Numeric, definition.PackageHeight));
            cmd.Parameters.Add(Param("@width", NpgsqlDbType.Numeric, definition.PackageWidth));
            cmd.Parameters.Add(Param("@length", NpgsqlDbType.Numeric, definition.PackageLength));
            cmd.Parameters.Add(Param("@shape", NpgsqlDbType.Integer, definition.Shape));
            cmd.Parameters.Add(Param("@cap", NpgsqlDbType.Numeric, definition.Cap ? 1 : 0));
            cmd.Parameters.Add(Param("@capd", NpgsqlDbType.Numeric, definition.CapDiameter));
            cmd.Parameters.Add(Param("@capl", NpgsqlDbType.Numeric, definition.CapLength));
            cmd.Parameters.Add(Param("@weight", NpgsqlDbType.Numeric, definition.Weight));
            cmd.Parameters.Add(Param("@fragile", NpgsqlDbType.Integer, definition.FragileScale));
            cmd.Parameters.Add(Param("@lcal", NpgsqlDbType.Integer, definition.LotCodeAbsLocation));
            cmd.Parameters.Add(Param("@lcrl", NpgsqlDbType.Integer, definition.LotCodeRelLocation));
            cmd.Parameters.Add(Param("@eal", NpgsqlDbType.Integer, definition.ExpiryAbsLocation));
            cmd.Parameters.Add(Param("@erl", NpgsqlDbType.Integer, definition.ExpiryRelLocation));
            cmd.Parameters.Add(Param("@valid", NpgsqlDbType.Numeric, definition.Valid ? 1 : 0));
            cmd.Parameters.Add(Param("@defstate", NpgsqlDbType.Integer, definition.DefinitionStateId));
            cmd.Parameters.Add(Param("@prodcat", NpgsqlDbType.Integer, definition.ProductCategoryId));
            cmd.Parameters.Add(Param("@product_name", NpgsqlDbType.Text, definition.ProductName));
            cmd.Parameters.Add(Param("@prodcodetype", NpgsqlDbType.Integer, definition.ProductCodeTypeId));
            cmd.Parameters.Add(Param("@product_code", NpgsqlDbType.Text, definition.ProductCode));
            cmd.Parameters.Add(Param("@manufacturer", NpgsqlDbType.Text, definition.ProductManufacturer));
            cmd.Parameters.Add(Param("@size", NpgsqlDbType.Numeric, definition.PackageSize));
            cmd.Parameters.Add(Param("@sizeuom", NpgsqlDbType.Integer, definition.PackageSizeUomId));
            cmd.Parameters.Add(Param("@changed_date", NpgsqlDbType.Timestamp, definition.ChangedDate));
            cmd.Parameters.Add(Param("@changed_by", NpgsqlDbType.Text, definition.ChangedBy));
            cmd.Parameters.Add(Param("@controlled", NpgsqlDbType.Numeric, definition.ControlledSubstance ? 1 : 0));
            cmd.Parameters.Add(Param("@created_by", NpgsqlDbType.Text, definition.CreatedBy));
            cmd.Parameters.Add(Param("@created_on", NpgsqlDbType.Timestamp, definition.CreatedOn));
            cmd.Parameters.Add(Param("@lotsource", NpgsqlDbType.Integer, definition.LotCodeSource));
            cmd.Parameters.Add(Param("@deftype", NpgsqlDbType.Integer, definition.PackageDefinitionTypeId));
            cmd.Parameters.Add(Param("@isdemo", NpgsqlDbType.Numeric, definition.IsDemoPackage ? 1 : 0));

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int Delete(int packageDefinitionId)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM package_definition WHERE package_definition_id = @id;";
            cmd.Parameters.Add(Param("@id", NpgsqlDbType.Integer, packageDefinitionId));
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Reads the AuditID_MA (bytea) bytes for a row, or null if absent.
        /// </summary>
        public byte[]? GetRowVersion(int packageDefinitionId)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT AuditID_MA FROM package_definition WHERE package_definition_id = @id;";
            cmd.Parameters.Add(Param("@id", NpgsqlDbType.Integer, packageDefinitionId));
            var result = cmd.ExecuteScalar();
            return result is null or DBNull ? null : (byte[])result;
        }

        /// <summary>
        /// Sets the bit columns (cap, valid, controlled_substance, is_demo_package)
        /// for a row, returning rows affected. Used to verify bit round-tripping.
        /// </summary>
        public int UpdateBitFlags(int packageDefinitionId, bool cap, bool valid,
            bool controlledSubstance, bool isDemo)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "UPDATE package_definition SET cap = @cap, valid = @valid, " +
                "controlled_substance = @cs, is_demo_package = @demo WHERE package_definition_id = @id;";
            cmd.Parameters.Add(Param("@cap", NpgsqlDbType.Numeric, cap ? 1 : 0));
            cmd.Parameters.Add(Param("@valid", NpgsqlDbType.Numeric, valid ? 1 : 0));
            cmd.Parameters.Add(Param("@cs", NpgsqlDbType.Numeric, controlledSubstance ? 1 : 0));
            cmd.Parameters.Add(Param("@demo", NpgsqlDbType.Numeric, isDemo ? 1 : 0));
            cmd.Parameters.Add(Param("@id", NpgsqlDbType.Integer, packageDefinitionId));
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Updates a row's description using an optimistic-concurrency guard on the
        /// AuditID_MA bytea column. Returns rows affected (0 means the token was stale).
        /// </summary>
        public int UpdateDescriptionIfVersionMatches(int packageDefinitionId, string description, byte[] expectedRowVersion)
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "UPDATE package_definition SET description = @desc " +
                "WHERE package_definition_id = @id AND AuditID_MA = @rv;";
            cmd.Parameters.Add(Param("@desc", NpgsqlDbType.Text, description));
            cmd.Parameters.Add(Param("@id", NpgsqlDbType.Integer, packageDefinitionId));
            cmd.Parameters.Add(Param("@rv", NpgsqlDbType.Bytea, expectedRowVersion));
            return cmd.ExecuteNonQuery();
        }

        private static PackageDefinitionDto Map(NpgsqlDataReader r)
        {
            return new PackageDefinitionDto
            {
                PackageDefinitionId = r.GetInt32(0),
                Description = r.GetString(1),
                PackageCode = r.GetString(2),
                PackageCodeAbsLocationId = r.GetInt32(3),
                PackageHeight = r.GetDecimal(4),
                PackageWidth = r.GetDecimal(5),
                PackageLength = r.GetDecimal(6),
                Shape = r.GetInt32(7),
                Cap = r.GetBoolean(8),
                CapDiameter = r.GetDecimal(9),
                CapLength = r.GetDecimal(10),
                Weight = r.GetDecimal(11),
                FragileScale = r.GetInt32(12),
                LotCodeAbsLocation = r.GetInt32(13),
                LotCodeRelLocation = r.GetInt32(14),
                ExpiryAbsLocation = r.GetInt32(15),
                ExpiryRelLocation = r.GetInt32(16),
                Valid = r.GetBoolean(17),
                DefinitionStateId = r.GetInt32(18),
                DefinitionRejectReason = GetNullableString(r, 19),
                ProductCategoryId = r.GetInt32(20),
                ProductName = r.GetString(21),
                ProductCodeTypeId = r.GetInt32(22),
                ProductCode = r.GetString(23),
                ProductManufacturer = r.GetString(24),
                PackageSize = r.GetDecimal(25),
                PackageSizeUomId = r.GetInt32(26),
                DrugSchedule = GetNullableString(r, 27),
                ChangedDate = r.GetDateTime(28),
                ChangedBy = r.GetString(29),
                ExpirationMethod = GetNullable<int>(r, 30),
                DaysToAdvisedExpiration = GetNullable<int>(r, 31),
                DrugScheduleId = GetNullable<int>(r, 32),
                ControlledSubstance = r.GetBoolean(33),
                CreatedBy = r.GetString(34),
                CreatedOn = r.GetDateTime(35),
                LotCodeSource = r.GetInt32(36),
                AuditId = r.IsDBNull(37) ? null : (byte[])r[37],
                Notes = GetNullableString(r, 38),
                PackageDefinitionTypeId = r.GetInt32(39),
                IsDemoPackage = r.GetBoolean(40)
            };
        }
    }
}
