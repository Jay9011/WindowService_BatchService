using System.Threading.Tasks;
using Utility.DataAccess;
using Utility.Settings;

namespace Utility.Abstractions.Services
{
    /// <summary>
    /// Thin abstraction over CoreDAL's connection test so UI / other layers do
    /// not need to reference <c>Microsoft.Data.SqlClient</c> or CoreDAL directly.
    /// </summary>
    public interface IDbConnectionService
    {
        /// <summary>
        /// Tests connectivity using the supplied DB settings.
        /// </summary>
        /// <param name="settings">Database settings to validate and try.</param>
        /// <returns>Result indicating success/failure and a human readable message.</returns>
        Task<DbConnectionTestResult> TestAsync(DbSettingsDTO settings);

        /// <summary>
        /// Executes the given stored procedure and returns the row count of the
        /// first result set. Used by SettingsUI to verify that the configured
        /// BatchList procedure is wired up correctly.
        /// </summary>
        /// <param name="settings">Database settings to connect with.</param>
        /// <param name="procedureName">Stored procedure to execute (no parameters).</param>
        /// <returns>Result containing row count or an error message.</returns>
        Task<BatchListQueryResult> TryGetBatchListCountAsync(DbSettingsDTO settings, string procedureName);
    }
}
