using System.Threading.Tasks;
using Utility.Batch;
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
        /// Executes the configured BatchList stored procedure and projects the first result set into lightweight <see cref="BatchListDTO"/> rows for the UI to display.
        /// </summary>
        /// <param name="settings">Database settings to connect with.</param>
        /// <param name="procedureName">Stored procedure to execute (no parameters).</param>
        /// <returns>Result containing the projected rows or an error message.</returns>
        Task<BatchListQueryResult> TryGetBatchListAsync(DbSettingsDTO settings, string procedureName);
    }
}
