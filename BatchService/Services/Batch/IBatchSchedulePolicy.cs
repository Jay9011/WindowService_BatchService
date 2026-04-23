using Utility.Batch;

namespace BatchService.Services.Batch;

/// <summary>
/// Decides whether a batch is due to run at a given point in time,
/// given its schedule configuration and last-run timestamp.
/// </summary>
public interface IBatchSchedulePolicy
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="batch"/> should fire at <paramref name="now"/>.
    /// Caller passes a local-time clock (matches DB defaults which use <c>SYSDATETIME()</c>).
    /// </summary>
    /// <param name="batch">The batch to check.</param>
    /// <param name="now">The current local time.</param>
    /// <returns><c>true</c> if the batch is due to run, <c>false</c> otherwise.</returns>
    bool IsDue(BatchListDTO batch, DateTime now);
}
