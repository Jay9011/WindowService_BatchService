using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Serilog;
using Utility.Logging;
using Utility.Resources;
using Utility.Settings;

namespace SettingsUI;

public partial class App : Application
{
    private const string SingleInstanceMutexName = "Global\\SECUiDEA.BatchService.SettingsUI";

    private Mutex? _singleInstanceMutex;
    private bool _mutexOwned;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out _mutexOwned);

        if (!_mutexOwned)
        {
            MessageBox.Show(
                Strings.MessageDuplicateInstance,
                Strings.WindowTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Shutdown(0);    // successed shutdown the application

            return;
        }

        // ================================================
        // ==> Set the culture of the application
        // thread format culture info setting.
        // .resx files are used to store the localized strings.
        // ================================================
        var uiCulture = CultureInfo.CurrentUICulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture;
        Thread.CurrentThread.CurrentUICulture = uiCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
        // ================================================
        // <== Set the culture of the application
        // ================================================

        // set the language of the application. (XAML file)
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(uiCulture.IetfLanguageTag)));

        try
        {
            var store = new SettingsFileStore();
            store.EnsureInitializedAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                Strings.ErrorConfigDirCreate + Environment.NewLine + ex.Message,
                Strings.TitleError,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);    // abnormal shutdown the application

            return;
        }

        // ================================================
        // ==> Serilog (shared with BatchService under ProgramData\...\Logs)
        // ================================================
        try
        {
            SerilogBootstrap.Initialize("settingsui");
            Log.Information("SettingsUI started");

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }
        catch (Exception ex)
        {
            // Logging must never take the UI down; just surface the failure and continue.
            MessageBox.Show(
                ex.Message,
                Strings.TitleError,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        // ================================================
        // <== Serilog
        // ================================================

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        try
        {
            Log.Information("SettingsUI exiting (code={ExitCode})", e.ApplicationExitCode);
        }
        catch
        {
            // ignored
        }

        SerilogBootstrap.Shutdown();

        if (_singleInstanceMutex != null)
        {
            try
            {
                if (_mutexOwned)
                {
                    _singleInstanceMutex.ReleaseMutex();
                }
            }
            catch
            {
            }
            finally
            {
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }
        }

        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI exception");
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Log.Fatal(ex, "Unhandled AppDomain exception (terminating={Terminating})", e.IsTerminating);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }
}
