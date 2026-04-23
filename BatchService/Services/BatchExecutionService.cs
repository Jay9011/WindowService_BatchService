using System.Collections.Concurrent;
using System.Diagnostics;
using BatchService.Models;
using BatchService.Repository.Abstraction.Batch;
using Microsoft.Extensions.Options;
using Utility.Settings;

namespace BatchService.Services;

public class BatchExecutionService : IBatchExecutionService
{
    private const int DefaultMaxConcurrency = 4;

    private readonly IBatchRepository _repository;
    private readonly IBatchSchedulePolicy _schedulePolicy;
    private readonly IOptionsMonitor<BatchServiceOptions> _options;
    private readonly ILogger<BatchExecutionService> _logger;

    public BatchExecutionService(IBatchRepository repository, IBatchSchedulePolicy schedulePolicy, IOptionsMonitor<BatchServiceOptions> options, ILogger<BatchExecutionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _schedulePolicy = schedulePolicy ?? throw new ArgumentNullException(nameof(schedulePolicy));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<BatchExecDTO>> RunDueAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<BatchListDTO> allBatches;
        try
        {
            allBatches = await _repository.GetBatchListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load batch list; skipping tick");
            return Array.Empty<BatchExecDTO>();
        }

        // Snapshot now once so every batch in this tick is judged against the same clock.
        // Local time (matches DB SYSDATETIME() defaults).
        var now = DateTime.Now;

        var dueBatches = allBatches
            .Where(b => _schedulePolicy.IsDue(b, now))
            .ToList();

        if (dueBatches.Count == 0)
        {
            return Array.Empty<BatchExecDTO>();
        }

        var parallelism = ResolveMaxConcurrency();

        _logger.LogInformation("Running {DueCount}/{TotalCount} batch(es) with MaxConcurrency={Parallelism}", dueBatches.Count, allBatches.Count, parallelism);

        var results = new ConcurrentBag<BatchExecDTO>();
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = parallelism, // Maximum number of batches that may run in parallel within a single polling tick.
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(dueBatches, options, async (batch, cancelToken) =>
        {
            var result = await RunOneAsync(batch, cancelToken);
            results.Add(result);
        });

        return results.ToArray();
    }

    #region private methods

    /// <summary>
    /// Runs a single batch.
    /// </summary>
    /// <param name="batch">The batch to run.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="BatchExecDTO"/> object representing the result of the execution.</returns>
    private async Task<BatchExecDTO> RunOneAsync(BatchListDTO batch, CancellationToken cancellationToken)
    {
        var startedAt = DateTime.Now;
        var stopwatch = Stopwatch.StartNew();

        bool isSuccess;
        string? message;

        try
        {
            using var sqlResult = await _repository.RunBatchAsync(batch, cancellationToken);
            isSuccess = sqlResult.IsSuccess;
            message = sqlResult.Message;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return BuildCancelledResult(batch, startedAt, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            isSuccess = false;
            message = ex.Message;
            _logger.LogError(ex, "Batch {Name} (ID={Id}) threw while executing {Proc}", batch.DisplayName, batch.ID, batch.ProcedureName);
        }

        stopwatch.Stop();
        var endedAt = startedAt + stopwatch.Elapsed;

        var result = new BatchExecDTO(batch.ID, batch.DisplayName, batch.ProcedureName, startedAt, endedAt, stopwatch.Elapsed, isSuccess, message);

        await TryWriteLogAsync(result, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Batch {Name} (ID={Id}) finished in {Elapsed} ms", batch.DisplayName, batch.ID, (long)result.Elapsed.TotalMilliseconds);
        }
        else
        {
            _logger.LogWarning("Batch {Name} (ID={Id}) failed in {Elapsed} ms: {Message}", batch.DisplayName, batch.ID, (long)result.Elapsed.TotalMilliseconds, result.Message);
        }

        return result;
    }

    /// <summary>
    /// Tries to write the log for the batch execution.
    /// </summary>
    /// <param name="result">The result of the batch execution.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    private async Task TryWriteLogAsync(BatchExecDTO result, CancellationToken cancellationToken)
    {
        var entry = new BatchLogEntity
        {
            BatchID = result.BatchID,
            StartedAt = result.StartedAt,
            EndedAt = result.EndedAt,
            IsSuccess = result.IsSuccess,
            Message = result.Message,
        };

        try
        {
            using var _ = await _repository.WriteLogAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist BatchLog for BatchID={Id}", result.BatchID);
        }
    }

    /// <summary>
    /// Builds a cancelled result for the batch execution.
    /// </summary>
    /// <param name="batch">The batch to run.</param>
    /// <param name="startedAt">The start time of the batch execution.</param>
    /// <param name="elapsed">The elapsed time of the batch execution.</param>
    /// <returns>A <see cref="BatchExecDTO"/> object representing the result of the execution.</returns>
    private static BatchExecDTO BuildCancelledResult(BatchListDTO batch, DateTime startedAt, TimeSpan elapsed)
    {
        return new BatchExecDTO(batch.ID, batch.DisplayName, batch.ProcedureName, startedAt, startedAt + elapsed, elapsed,
            isSuccess: false, message: "Cancelled");
    }

    /// <summary>
    /// Resolves the maximum concurrency for the batch execution.
    /// </summary>
    /// <returns>The maximum concurrency for the batch execution.</returns>
    private int ResolveMaxConcurrency()
    {
        var configured = _options.CurrentValue?.BatchJob?.MaxConcurrency ?? DefaultMaxConcurrency;
        return configured > 0 ? configured : DefaultMaxConcurrency;
    }

    #endregion
}
