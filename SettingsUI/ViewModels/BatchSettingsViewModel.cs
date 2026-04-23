using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using SettingsUI.Infrastructure;
using Utility.Abstractions.Services;
using Utility.Batch;
using Utility.Common;
using Utility.DataAccess;
using Utility.Resources;
using Utility.Settings;

namespace SettingsUI.ViewModels;

/// <summary>
/// Edits BatchJob: stored procedure name, polling interval and max concurrency.
/// PollingInterval is exposed as a "hh:mm:ss" string to play nicely with TextBox binding.
/// MaxConcurrency is exposed as a string (with a validity flag) for the same reason.
/// Also exposes a "load" command that runs the configured procedure against the current DB settings so the user can preview how many batches are defined.
/// </summary>
public class BatchSettingsViewModel : ViewModelBase
{
    private const int DefaultMaxConcurrency = 4;
    private const int MinMaxConcurrency = 1;
    private const int MaxMaxConcurrency = 64;

    private readonly IDbConnectionService _connectionService;
    private readonly Func<DbSettingsDTO>? _getDbSettings;

    public BatchSettingsViewModel() : this(new DbConnectionService(), null)
    { }

    public BatchSettingsViewModel(IDbConnectionService connectionService, Func<DbSettingsDTO>? getDbSettings)
    {
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        _getDbSettings = getDbSettings;

        Batches = new ObservableCollection<BatchRowViewModel>();

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

    private string _maxConcurrencyText = DefaultMaxConcurrency.ToString(CultureInfo.InvariantCulture);
    /// <summary>
    /// Max concurrency as a string so the text box can show invalid input
    /// while we flip <see cref="MaxConcurrencyValid"/> to false and
    /// refuse to save.
    /// </summary>
    public string MaxConcurrencyText
    {
        get => _maxConcurrencyText;
        set
        {
            if (SetProperty(ref _maxConcurrencyText, value ?? string.Empty))
            {
                MaxConcurrencyValid = TryParseMaxConcurrency(_maxConcurrencyText, out _);
            }
        }
    }

    private bool _maxConcurrencyValid = true;
    public bool MaxConcurrencyValid
    {
        get => _maxConcurrencyValid;
        private set => SetProperty(ref _maxConcurrencyValid, value);
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
    /// Rows returned by the last "load batch list" execution, projected into
    /// display-friendly <see cref="BatchRowViewModel"/> wrappers.
    /// </summary>
    public ObservableCollection<BatchRowViewModel> Batches { get; }

    /// <summary>
    /// Executes <see cref="ProcedureName"/> on the current DB settings and fills
    /// <see cref="Batches"/> with the returned rows.
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

        var effectiveMax = options.MaxConcurrency <= 0 ? DefaultMaxConcurrency : options.MaxConcurrency;
        MaxConcurrencyText = effectiveMax.ToString(CultureInfo.InvariantCulture);

        LoadResult = string.Empty;
        LoadSucceeded = false;
        Batches.Clear();
    }

    /// <summary>
    /// Convert the batch job options to the file.
    /// </summary>
    /// <returns>The batch job options to convert.</returns>
    public BatchJobOptions ToOptions()
    {
        var interval = TryParseInterval(_pollingIntervalText, out var parsed) ? parsed : TimeSpan.FromSeconds(10);
        var maxConcurrency = TryParseMaxConcurrency(_maxConcurrencyText, out var mc) ? mc : DefaultMaxConcurrency;

        return new BatchJobOptions
        {
            ProcedureName = ProcedureName,
            PollingInterval = interval,
            MaxConcurrency = maxConcurrency,
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
    /// Try to parse the max concurrency text as an int within the allowed range.
    /// </summary>
    private static bool TryParseMaxConcurrency(string text, out int value)
    {
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
            && value >= MinMaxConcurrency
            && value <= MaxMaxConcurrency)
        {
            return true;
        }

        value = DefaultMaxConcurrency;
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
        Batches.Clear();

        if (_getDbSettings == null)
        {
            LoadResult = Strings.TestFailed + ": DB settings provider is not wired up.";
            return;
        }

        try
        {
            var settings = _getDbSettings();
            var result = await _connectionService
                .TryGetBatchListAsync(settings, _procedureName);

            LoadSucceeded = result.IsSuccess;

            if (result.IsSuccess)
            {
                foreach (var row in result.Rows)
                {
                    Batches.Add(new BatchRowViewModel(row));
                }

                LoadResult = string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.BatchListLoadedFormat,
                    result.Count);
            }
            else
            {
                LoadResult = Strings.TestFailed + ": " + result.Message;
            }
        }
        catch (Exception ex)
        {
            LoadSucceeded = false;
            LoadResult = Strings.TestFailed + ": " + ex.Message;
        }
    }
}
