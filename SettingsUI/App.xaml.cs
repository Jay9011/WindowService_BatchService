using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using Utility.Settings;
using UiStrings = SettingsUI.Resources.Strings;
using CommonStrings = Utility.Resources.Strings;

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
                UiStrings.MessageDuplicateInstance,
                UiStrings.WindowTitle,
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
                CommonStrings.ErrorConfigDirCreate + Environment.NewLine + ex.Message,
                UiStrings.TitleError,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);    // abnormal shutdown the application

            return;
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
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
}
