using Microsoft.Extensions.Options;
using Utility.Settings;

namespace BatchService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IOptionsMonitor<BatchServiceOptions> _options;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Option Change Subscription Monitor Listener Object
    /// </summary>
    private IDisposable? _changeSubscription;

    public Worker(ILogger<Worker> logger, IOptionsMonitor<BatchServiceOptions> options, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _options = options;
        _scopeFactory = scopeFactory;
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
            var currentOptions = _options.CurrentValue;
            using var scope = _scopeFactory.CreateScope();
            // var repository = scope.ServiceProvider.GetRequiredService<IBatchSettingsRepository>();

            try
            {
                // var settings = await repository.GetBatchSettingsAsync(currentOptions.Database, currentOptions.BatchJob.ProcedureName, stoppingToken);


            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            await Task.Delay(currentOptions.BatchJob.PollingInterval, stoppingToken);
        }
    }

    #region Private Methods

    /// <summary>
    /// Options Changed Event Handler
    /// </summary>
    /// <param name="options">Batch Service Options</param>
    private void OnOptionsChanged(BatchServiceOptions options)
    {
        var server = options.Database?.Server ?? string.Empty;
        var database = options.Database?.Database ?? string.Empty;
        var userId = options.Database?.UserId ?? string.Empty;
        var integrated = options.Database?.IntegratedSecurity ?? false;
        var procedure = options.BatchJob?.ProcedureName ?? string.Empty;
        var polling = options.BatchJob?.PollingInterval ?? TimeSpan.Zero;

        _logger.LogInformation(
            "Options changed: Server={Server}, Database={Database}, UserId={UserId}, IntegratedSecurity={IntegratedSecurity}, Procedure={Procedure}, PollingInterval={PollingInterval}",
            server,
            database,
            MaskUserId(userId),
            integrated,
            procedure,
            polling);
    }

    /// <summary>
    /// Masking the UserId
    /// </summary>
    /// <param name="userId">The UserId to mask.</param>
    /// <returns>The masked UserId.</returns>
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
