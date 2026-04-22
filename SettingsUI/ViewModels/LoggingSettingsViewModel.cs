using System.Collections.ObjectModel;
using SettingsUI.Infrastructure;
using Utility.Common;

namespace SettingsUI.ViewModels;

/// <summary>
/// Edits the Logging:LogLevel dictionary. Only the two well-known categories are exposed.
/// Other keys already present in the file are preserved by <see cref="SettingsFileStore.SaveLogLevelsAsync"/>.
/// </summary>
public class LoggingSettingsViewModel : ViewModelBase
{
    private string _default = Keys.Value_LogLevel_Information;
    private string _hostingLifetime = Keys.Value_LogLevel_Information;

    public LoggingSettingsViewModel()
    {
        LogLevelOptions = new ReadOnlyCollection<string>(new[]
        {
            Keys.Value_LogLevel_Trace,
            Keys.Value_LogLevel_Debug,
            Keys.Value_LogLevel_Information,
            Keys.Value_LogLevel_Warning,
            Keys.Value_LogLevel_Error,
            Keys.Value_LogLevel_Critical,
            Keys.Value_LogLevel_None
        });
    }

    public ReadOnlyCollection<string> LogLevelOptions { get; }

    public string Default
    {
        get => _default;
        set => SetProperty(ref _default, value ?? string.Empty);
    }

    public string HostingLifetime
    {
        get => _hostingLifetime;
        set => SetProperty(ref _hostingLifetime, value ?? string.Empty);
    }

    /// <summary>
    /// Load the logging levels from the file.
    /// </summary>
    /// <param name="levels">The logging levels to load.</param>
    public void Load(IDictionary<string, string> levels)
    {
        if (levels == null) return;

        if (levels.TryGetValue(Keys.Key_LogLevel_Default, out var def) && !string.IsNullOrWhiteSpace(def))
        {
            Default = def;
        }

        if (levels.TryGetValue(Keys.Key_LogLevel_HostingLifetime, out var hl) && !string.IsNullOrWhiteSpace(hl))
        {
            HostingLifetime = hl;
        }
    }

    /// <summary>
    /// Returns the edited values; the store merges these onto existing entries.
    /// </summary>
    public Dictionary<string, string> ToLevels()
    {
        return new Dictionary<string, string>
        {
            [Keys.Key_LogLevel_Default] = Default,
            [Keys.Key_LogLevel_HostingLifetime] = HostingLifetime,
        };
    }
}
