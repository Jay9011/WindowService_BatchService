using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CoreDAL.Configuration;
using Utility.Abstractions.Services;
using Utility.Batch;
using Utility.Settings;

namespace Utility.DataAccess
{
    /// <summary>
    /// Default implementation of <see cref="IDbConnectionService"/>.
    /// Uses the same CoreDAL path the runtime services use so that SettingsUI
    /// and BatchService validate connections identically.
    /// </summary>
    public class DbConnectionService : IDbConnectionService
    {
        private readonly DatabaseType _databaseType;

        public DbConnectionService() : this(DatabaseType.MSSQL)
        {
        }

        public DbConnectionService(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        public async Task<DbConnectionTestResult> TestAsync(DbSettingsDTO settings)
        {
            if (settings == null)
            {
                return DbConnectionTestResult.Failure("Database settings are required.");
            }

            var connectionInfo = settings.ToConnectionInfo();
            if (!connectionInfo.Validate(out string validationMessage))
            {
                return DbConnectionTestResult.Failure(validationMessage);
            }

            try
            {
                var result = await DbDALFactory
                    .CreateCoreDal(_databaseType)
                    .TestConnectionAsync(connectionInfo.ToConnectionString());

                return result.IsSuccess
                    ? DbConnectionTestResult.Success(result.Message ?? string.Empty)
                    : DbConnectionTestResult.Failure(result.Message ?? string.Empty);
            }
            catch (Exception ex)
            {
                return DbConnectionTestResult.Failure(ex.Message);
            }
        }

        public async Task<BatchListQueryResult> TryGetBatchListAsync(DbSettingsDTO settings, string procedureName)
        {
            if (settings == null)
            {
                return BatchListQueryResult.Failure("Database settings are required.");
            }

            if (string.IsNullOrWhiteSpace(procedureName))
            {
                return BatchListQueryResult.Failure("Procedure name is required.");
            }

            var connectionInfo = settings.ToConnectionInfo();
            if (!connectionInfo.Validate(out string validationMessage))
            {
                return BatchListQueryResult.Failure(validationMessage);
            }

            try
            {
                using (var result = await DbDALFactory
                    .CreateCoreDal(_databaseType)
                    .ExecuteProcedureAsync(
                        connectionInfo.ToConnectionString(),
                        procedureName,
                        (Dictionary<string, object>?)null,
                        false))
                {
                    if (!result.IsSuccess)
                    {
                        return BatchListQueryResult.Failure(result.Message ?? string.Empty);
                    }

                    var table = result.DataSet != null && result.DataSet.Tables.Count > 0
                        ? result.DataSet.Tables[0]
                        : null;

                    var rows = MapRows(table);
                    return BatchListQueryResult.Success(rows, result.Message ?? string.Empty);
                }
            }
            catch (Exception ex)
            {
                return BatchListQueryResult.Failure(ex.Message);
            }
        }

        #region Private Methods

        /// <summary>
        /// Projects the first result set of the BatchList SP onto <see cref="BatchListDTO"/> using case-insensitive column lookups.
        /// </summary>
        private static IReadOnlyList<BatchListDTO> MapRows(DataTable? table)
        {
            if (table == null || table.Rows.Count == 0)
            {
                return Array.Empty<BatchListDTO>();
            }

            var list = new List<BatchListDTO>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                list.Add(new BatchListDTO
                {
                    ID = GetInt32(row, "ID"),
                    DisplayName = GetString(row, "DisplayName"),
                    Description = GetNullableString(row, "Description"),
                    ProcedureName = GetString(row, "ProcedureName"),

                    ScheduleType = (EBatchScheduleType)GetInt32(row, "ScheduleType"),
                    IntervalValue = GetNullableInt32(row, "IntervalValue"),
                    RunHour = GetNullableByte(row, "RunHour"),
                    RunMinute = GetNullableByte(row, "RunMinute"),
                    WeekDays = GetByte(row, "WeekDays"),

                    CustomKey1 = GetNullableString(row, "CustomKey1"),
                    CustomValue1 = GetNullableString(row, "CustomValue1"),
                    CustomKey2 = GetNullableString(row, "CustomKey2"),
                    CustomValue2 = GetNullableString(row, "CustomValue2"),
                    CustomKey3 = GetNullableString(row, "CustomKey3"),
                    CustomValue3 = GetNullableString(row, "CustomValue3"),
                    CustomKey4 = GetNullableString(row, "CustomKey4"),
                    CustomValue4 = GetNullableString(row, "CustomValue4"),
                    CustomKey5 = GetNullableString(row, "CustomKey5"),
                    CustomValue5 = GetNullableString(row, "CustomValue5"),

                    LastRunAt = GetNullableDateTime(row, "LastRunAt"),
                    LastResult = (EBatchResult)GetInt32(row, "LastResult"),
                    IsEnabled = GetBoolean(row, "IsEnabled"),
                    UpdatedAt = GetNullableDateTime(row, "UpdatedAt") ?? default,
                });
            }

            return list;
        }

        private static bool HasColumn(DataRow row, string columnName)
            => row.Table.Columns.Contains(columnName);

        private static string GetString(DataRow row, string columnName)
        {
            if (!HasColumn(row, columnName)) return string.Empty;
            var value = row[columnName];
            return value == null || value == DBNull.Value ? string.Empty : value.ToString() ?? string.Empty;
        }

        private static string? GetNullableString(DataRow row, string columnName)
        {
            if (!HasColumn(row, columnName)) return null;
            var value = row[columnName];
            return value == null || value == DBNull.Value ? null : value.ToString();
        }

        private static int GetInt32(DataRow row, string columnName)
        {
            if (!HasColumn(row, columnName)) return 0;
            var value = row[columnName];
            if (value == null || value == DBNull.Value) return 0;
            try { return Convert.ToInt32(value); }
            catch { return 0; }
        }

        private static int? GetNullableInt32(DataRow row, string columnName)
        {
            if (!HasColumn(row, columnName)) return null;
            var value = row[columnName];
            if (value == null || value == DBNull.Value) return null;
            try { return Convert.ToInt32(value); }
            catch { return null; }
        }

        private static byte GetByte(DataRow row, string columnName)
        {
            if (!HasColumn(row, columnName)) return 0;
            var value = row[columnName];
            if (value == null || value == DBNull.Value) return 0;
            try { return Convert.ToByte(value); }
            catch { return 0; }
        }

        private static byte? GetNullableByte(DataRow row, string columnName)
        {
            if (!HasColumn(row, columnName)) return null;
            var value = row[columnName];
            if (value == null || value == DBNull.Value) return null;
            try { return Convert.ToByte(value); }
            catch { return null; }
        }

        private static bool GetBoolean(DataRow row, string columnName)
        {
            if (!HasColumn(row, columnName)) return false;
            var value = row[columnName];
            if (value == null || value == DBNull.Value) return false;
            try { return Convert.ToBoolean(value); }
            catch { return false; }
        }

        private static DateTime? GetNullableDateTime(DataRow row, string columnName)
        {
            if (!HasColumn(row, columnName)) return null;
            var value = row[columnName];
            if (value == null || value == DBNull.Value) return null;
            if (value is DateTime dt) return dt;
            if (DateTime.TryParse(value.ToString(), out var parsed)) return parsed;
            return null;
        }

        #endregion
    }
}
