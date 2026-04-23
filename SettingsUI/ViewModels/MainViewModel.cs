using System.Windows;
using System.Windows.Input;
using SettingsUI.Infrastructure;
using Utility.DataAccess;
using Utility.Resources;
using Utility.Settings;

namespace SettingsUI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly SettingsFileStore _store;

    public DatabaseSettingsViewModel Database { get; }
    public BatchSettingsViewModel Batch { get; }
    public LoggingSettingsViewModel Logging { get; }
    public ServiceControlViewModel Service { get; }

    public ICommand SaveCommand { get; }
    public ICommand ReloadCommand { get; }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public MainViewModel() : this(new SettingsFileStore())
    { }

    public MainViewModel(SettingsFileStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));

        var connectionService = new DbConnectionService();

        Database = new DatabaseSettingsViewModel(connectionService);
        Batch = new BatchSettingsViewModel(connectionService, () => Database.ToDto());
        Logging = new LoggingSettingsViewModel();
        Service = new ServiceControlViewModel();

        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        ReloadCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
    }

    /// <summary>
    /// Loads all sections from disk into the child ViewModels.
    /// </summary>
    public async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            await _store.EnsureInitializedAsync();

            var options = await _store.LoadBatchOptionsAsync();
            Database.Load(options.Database);
            Batch.Load(options.BatchJob);

            var levels = await _store.LoadLogLevelsAsync();
            Logging.Load(levels);

            Service.Refresh();

            StatusMessage = Strings.MessageReloadDone;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            MessageBox.Show(ex.Message, Strings.TitleError, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    #region Private Methods

    /// <summary>
    /// Check if the save command can be executed.
    /// </summary>
    /// <returns>True if the save command can be executed, false otherwise.</returns>
    private bool CanSave() => !IsBusy && Batch.PollingIntervalValid && Batch.MaxConcurrencyValid;

    /// <summary>
    /// Save the settings to the file.
    /// </summary>
    /// <returns>The task.</returns>
    private async Task SaveAsync()
    {
        if (IsBusy) return;

        if (!Batch.PollingIntervalValid)
        {
            MessageBox.Show(
                Strings.ValidationPollingIntervalFormat,
                Strings.TitleError,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!Batch.MaxConcurrencyValid)
        {
            MessageBox.Show(
                Strings.ValidationMaxConcurrencyRange,
                Strings.TitleError,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var options = new BatchServiceOptions
            {
                Database = Database.ToDto(),
                BatchJob = Batch.ToOptions(),
            };

            await _store.SaveBatchOptionsAsync(options);
            await _store.SaveLogLevelsAsync(Logging.ToLevels());

            StatusMessage = Strings.MessageSaveSuccess + " / " + Strings.MessageWorkerWillReload;

            MessageBox.Show(
                StatusMessage,
                Strings.TitleInfo,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = Strings.MessageSaveFailed + ": " + ex.Message;
            MessageBox.Show(
                StatusMessage,
                Strings.TitleError,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion
}
