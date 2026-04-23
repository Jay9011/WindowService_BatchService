using System.Diagnostics;
using BatchService.Services;
using Microsoft.Extensions.Options;
using Utility.Settings;

namespace BatchService;

public class Worker : BackgroundService
{
    private static readonly TimeSpan MinPollingInterval = TimeSpan.FromMilliseconds(250);

    private readonly ILogger<Worker> _logger;
    private readonly IOptionsMonitor<BatchServiceOptions> _options;
    private readonly IBatchExecutionService _executionService;

    private IDisposable? _changeSubscription;

    public Worker(ILogger<Worker> logger, IOptionsMonitor<BatchServiceOptions> options, IBatchExecutionService executionService)
    {
        _logger = logger;
        _options = options;
        _executionService = executionService;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _changeSubscription = _options.OnChange(OnOptionsChanged);
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _changeSubscription?.Dispose();
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var tickStart = Stopwatch.GetTimestamp();

            try
            {
                await _executionService.RunDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch tick failed unexpectedly");
            }

            // ================================================
            // ==> Polling Interval
            // ================================================
            var interval = ClampPollingInterval(_options.CurrentValue.BatchJob.PollingInterval);
            var elapsed = Stopwatch.GetElapsedTime(tickStart);
            var remaining = interval - elapsed;

            if (remaining <= TimeSpan.Zero)
            {
                await Task.Yield();
                continue;
            }

            try
            {
                await Task.Delay(remaining, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            // ================================================
            // <== Polling Interval
            // ================================================
        }
    }

    #region Private Methods

    /// <summary>
    /// Clamps the polling interval.
    /// </summary>
    /// <param name="configured">The configured polling interval.</param>
    /// <returns>The clamped polling interval.</returns>
    private static TimeSpan ClampPollingInterval(TimeSpan configured)
    {
        return configured < MinPollingInterval ? MinPollingInterval : configured;
    }

    /// <summary>
    /// Options Changed Event Handler
    /// </summary>
    /// <param name="options">Batch Service Options</param>
    private void OnOptionsChanged(BatchServiceOptions options)
    {
        _logger.LogInformation(
            "Options changed: Server={Server}, Database={Database}, UserId={UserId}, IntegratedSecurity={IntegratedSecurity}, Procedure={Procedure}, PollingInterval={PollingInterval}, MaxConcurrency={MaxConcurrency}",
            options.Database?.Server ?? string.Empty,
            options.Database?.Database ?? string.Empty,
            MaskUserId(options.Database?.UserId ?? string.Empty),
            options.Database?.IntegratedSecurity ?? false,
            options.BatchJob?.ProcedureName ?? string.Empty,
            options.BatchJob?.PollingInterval ?? TimeSpan.Zero,
            options.BatchJob?.MaxConcurrency ?? 0);
    }

    /// <summary>
    /// Masking the UserId
    /// </summary>
    private static string MaskUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return string.Empty;
        }

        if (userId.Length <= 2)
        {
            return new string('*', userId.Length);
        }

        return userId.Substring(0, 1) + new string('*', userId.Length - 2) + userId.Substring(userId.Length - 1, 1);
    }

    #endregion
}
