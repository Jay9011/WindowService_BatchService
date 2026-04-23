using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using SettingsUI.Infrastructure;
using Utility.Abstractions.Services;
using Utility.Common;
using Utility.DataAccess;
using Utility.Settings;
using UiStrings = SettingsUI.Resources.Strings;

namespace SettingsUI.ViewModels;

/// <summary>
/// Edits BatchJob: stored procedure name and polling interval.
/// PollingInterval is exposed as a "hh:mm:ss" string to play nicely with TextBox binding.
/// Also exposes a "load" command that runs the configured procedure against the
/// current DB settings so the user can verify how many rows it returns.
/// </summary>
public class BatchSettingsViewModel : ViewModelBase
{
    private readonly IDbConnectionService _connectionService;
    private readonly Func<DbSettingsDTO>? _getDbSettings;

    public BatchSettingsViewModel() : this(new DbConnectionService(), null)
    { }

    public BatchSettingsViewModel(IDbConnectionService connectionService, Func<DbSettingsDTO>? getDbSettings)
    {
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        _getDbSettings = getDbSettings;

        LoadBatchListCommand = new AsyncRelayCommand(LoadBatchListAsync, CanLoadBatchList);
    }

    private string _procedureName = string.Empty;
    public string ProcedureName
    {
        get => _procedureName;
        set
        {
            if (SetProperty(ref _procedureName, value ?? string.Empty))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    private string _pollingIntervalText = Keys.Value_PollingInterval;
    /// <summary>
    /// Polling interval as "hh:mm:ss" (or "d.hh:mm:ss"). Invalid formats are kept but
    /// <see cref="PollingIntervalValid"/> switches to false and <see cref="ToOptions"/>
    /// returns a safe default.
    /// </summary>
    public string PollingIntervalText
    {
        get => _pollingIntervalText;
        set
        {
            if (SetProperty(ref _pollingIntervalText, value ?? string.Empty))
            {
                PollingIntervalValid = TryParseInterval(_pollingIntervalText, out _);
            }
        }
    }

    private bool _pollingIntervalValid = true;
    public bool PollingIntervalValid
    {
        get => _pollingIntervalValid;
        private set => SetProperty(ref _pollingIntervalValid, value);
    }

    private string _loadResult = string.Empty;
    /// <summary>
    /// Human-readable result of the last "load batch list" execution.
    /// Either "{count} row(s) returned" or a failure message.
    /// </summary>
    public string LoadResult
    {
        get => _loadResult;
        private set => SetProperty(ref _loadResult, value);
    }

    private bool _loadSucceeded;
    public bool LoadSucceeded
    {
        get => _loadSucceeded;
        private set => SetProperty(ref _loadSucceeded, value);
    }

    /// <summary>
    /// Executes <see cref="ProcedureName"/> on the current DB settings and reports
    /// how many rows came back.
    /// </summary>
    public ICommand LoadBatchListCommand { get; }

    /// <summary>
    /// Load the batch job options from the file.
    /// </summary>
    /// <param name="options">The batch job options to load.</param>
    public void Load(BatchJobOptions options)
    {
        if (options == null) return;

        ProcedureName = options.ProcedureName;
        PollingIntervalText = options.PollingInterval.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

        LoadResult = string.Empty;
        LoadSucceeded = false;
    }

    /// <summary>
    /// Convert the batch job options to the file.
    /// </summary>
    /// <returns>The batch job options to convert.</returns>
    public BatchJobOptions ToOptions()
    {
        var interval = TryParseInterval(_pollingIntervalText, out var parsed) ? parsed : TimeSpan.FromSeconds(10);

        return new BatchJobOptions
        {
            ProcedureName = ProcedureName,
            PollingInterval = interval,
        };
    }

    /// <summary>
    /// Try to parse the polling interval text to a TimeSpan.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="value">The parsed TimeSpan.</param>
    /// <returns>True if the text is a valid polling interval, false otherwise.</returns>
    private static bool TryParseInterval(string text, out TimeSpan value)
    {
        if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out value) && value > TimeSpan.Zero)
        {
            return true;
        }

        value = TimeSpan.Zero;
        return false;
    }

    /// <summary>
    /// Check if the batch list can be loaded.
    /// </summary>
    /// <returns>True if the batch list can be loaded, false otherwise.</returns>
    private bool CanLoadBatchList()
    {
        return !string.IsNullOrWhiteSpace(_procedureName) && _getDbSettings != null;
    }

    /// <summary>
    /// Load the batch list.
    /// </summary>
    /// <returns>The task.</returns>
    private async Task LoadBatchListAsync()
    {
        LoadResult = string.Empty;
        LoadSucceeded = false;

        if (_getDbSettings == null)
        {
            LoadResult = Utility.Resources.Strings.TestFailed + ": DB settings provider is not wired up.";
            return;
        }

        try
        {
            var settings = _getDbSettings();
            var result = await _connectionService
                .TryGetBatchListCountAsync(settings, _procedureName);

            LoadSucceeded = result.IsSuccess;
            LoadResult = result.IsSuccess
                ? string.Format(CultureInfo.CurrentCulture, UiStrings.BatchListLoadedFormat, result.Count)
                : Utility.Resources.Strings.TestFailed + ": " + result.Message;
        }
        catch (Exception ex)
        {
            LoadSucceeded = false;
            LoadResult = Utility.Resources.Strings.TestFailed + ": " + ex.Message;
        }
    }
}
