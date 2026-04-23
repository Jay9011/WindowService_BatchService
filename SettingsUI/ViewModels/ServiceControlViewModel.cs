using System.Windows.Input;
using System.Windows.Threading;
using SettingsUI.Infrastructure;
using Utility.Services;

namespace SettingsUI.ViewModels;

/// <summary>
/// Polls the Windows service status periodically and exposes Start/Stop/Restart commands.
/// </summary>
public class ServiceControlViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(30);

    private readonly ServiceControlService _controller;
    public string ServiceName => _controller.ServiceName;

    private readonly DispatcherTimer _timer;

    private string _statusDisplay = string.Empty;

    public string StatusDisplay
    {
        get => _statusDisplay;
        private set => SetProperty(ref _statusDisplay, value);
    }

    private EServiceStatus _status = EServiceStatus.Unknown;

    public EServiceStatus Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                StatusDisplay = ServiceControlService.ToDisplay(value);
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    private string _lastError = string.Empty;
    public string LastError
    {
        get => _lastError;
        private set => SetProperty(ref _lastError, value);
    }

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand RestartCommand { get; }
    public ICommand RefreshCommand { get; }

    public ServiceControlViewModel() : this(new ServiceControlService())
    {
    }

    public ServiceControlViewModel(ServiceControlService controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));

        // ================================================
        // ==> Setting Commands
        // ================================================
        StartCommand = new AsyncRelayCommand(() => RunAsync(_controller.StartAsync), CanStart);
        StopCommand = new AsyncRelayCommand(() => RunAsync(_controller.StopAsync), CanStop);
        RestartCommand = new AsyncRelayCommand(() => RunAsync(_controller.RestartAsync), CanRestart);
        RefreshCommand = new RelayCommand((Action)Refresh);
        // ================================================
        // <== Setting Commands
        // ================================================

        _timer = new DispatcherTimer { Interval = PollingInterval };
        _timer.Tick += (_, _) => Refresh();
    }

    /// <summary>
    /// Start polling the service status.
    /// </summary>
    public void StartPolling()
    {
        Refresh();
        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }
    }

    /// <summary>
    /// Stop polling the service status.
    /// </summary>
    public void StopPolling()
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
        }
    }

    /// <summary>
    /// Refresh the service status.
    /// </summary>
    public void Refresh()
    {
        Status = _controller.GetStatus();
    }

    #region Private Methods

    /// <summary>
    /// Check if the service can be started.
    /// </summary>
    /// <returns>True if the service can be started, false otherwise.</returns>
    private bool CanStart() => _status == EServiceStatus.Stopped || _status == EServiceStatus.Paused;

    /// <summary>
    /// Check if the service can be stopped.
    /// </summary>
    /// <returns>True if the service can be stopped, false otherwise.</returns>
    private bool CanStop() => _status == EServiceStatus.Running || _status == EServiceStatus.Paused;

    /// <summary>
    /// Check if the service can be restarted.
    /// </summary>
    /// <returns>True if the service can be restarted, false otherwise.</returns>
    private bool CanRestart() => CanStop();

    /// <summary>
    /// Run the asynchronous action.
    /// </summary>
    /// <param name="action">The asynchronous action to run.</param>
    /// <returns>The task.</returns>
    private async Task RunAsync(Func<TimeSpan, CancellationToken, Task> action)
    {
        LastError = string.Empty;
        try
        {
            await action(OperationTimeout, CancellationToken.None);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        finally
        {
            Refresh();
        }
    }

    #endregion

    public void Dispose()
    {
        StopPolling();
    }

}
