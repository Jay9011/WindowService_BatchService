using BatchService.Models;

namespace BatchService.Services;

/// <summary>
/// Orchestrates one polling tick of the batch scheduler
/// </summary>
public interface IBatchExecutionService
{
    /// <summary>
    /// Runs every batch that is due at the current time (UTC).
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A list of <see cref="BatchExecDTO"/> objects representing the results of the execution.</returns>
    Task<IReadOnlyList<BatchExecDTO>> RunDueAsync(CancellationToken cancellationToken = default);
}
