using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreDAL.Configuration;
using Utility.Abstractions.Services;
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

        public async Task<BatchListQueryResult> TryGetBatchListCountAsync(DbSettingsDTO settings, string procedureName)
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
                        (Dictionary<string, object>)null,
                        false))
                {
                    if (!result.IsSuccess)
                    {
                        return BatchListQueryResult.Failure(result.Message ?? string.Empty);
                    }

                    var table = result.DataSet != null && result.DataSet.Tables.Count > 0
                        ? result.DataSet.Tables[0]
                        : null;

                    var count = table?.Rows.Count ?? 0;
                    return BatchListQueryResult.Success(count, result.Message ?? string.Empty);
                }
            }
            catch (Exception ex)
            {
                return BatchListQueryResult.Failure(ex.Message);
            }
        }
    }
}
