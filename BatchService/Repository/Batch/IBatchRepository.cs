using BatchService.Models;
using CoreDAL.ORM;
using Utility.Batch;

namespace BatchService.Repository.Batch;

/// <summary>
/// Data access surface used by <see cref="Worker"/> to drive batch execution.
/// </summary>
public interface IBatchRepository
{
    /// <summary>
    /// Loads every row from <c>BATCH_GetBatchList</c>.
    /// </summary>
    /// <param name="includeDisabled"> When <c>true</c> disabled batches are returned as well. The default matches the SP default and only returns <c>IsEnabled = 1</c> entries.</param>
    /// <returns>A list of <see cref="BatchListDTO"/> objects representing the batch definitions.</returns>
    Task<List<BatchListDTO>> GetBatchListAsync(bool includeDisabled = false);

    /// <summary>
    /// Executes the stored procedure referenced by <paramref name="dto"/>
    /// </summary>
    /// <param name="dto">The batch DTO to execute.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="SQLResult"/> object representing the result of the operation.</returns>
    Task<SQLResult> RunBatchAsync(BatchListDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes one <c>BatchLog</c> row and updates <c>BatchList.LastRunAt</c> / <c>LastResult</c> atomically via <c>BATCH_WriteLog</c>.
    /// </summary>
    /// <param name="entity">Log payload (BatchID / timings / outcome).</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="SQLResult"/> object representing the result of the operation.</returns>
    Task<SQLResult> WriteLogAsync(BatchLogEntity entity, CancellationToken cancellationToken = default);
}
