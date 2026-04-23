using System.Windows.Input;
using SettingsUI.Infrastructure;
using Utility.Abstractions.Services;
using Utility.DataAccess;
using Utility.Settings;

namespace SettingsUI.ViewModels;

/// <summary>
/// Edits the DB connection fields and can test connectivity.
/// Backed by an in-memory <see cref="DbSettingsDTO"/> that the main VM persists on save.
/// </summary>
public class DatabaseSettingsViewModel : ViewModelBase
{
    private readonly IDbConnectionService _connectionTester;

    private string _server = string.Empty;
    private int? _port = 1433;
    private string _database = string.Empty;
    private string _userId = string.Empty;
    private string _password = string.Empty;
    private bool _integratedSecurity;

    private string _testResult = string.Empty;
    private bool _testSucceeded;

    public DatabaseSettingsViewModel()
        : this(new DbConnectionService())
    {
    }

    public DatabaseSettingsViewModel(IDbConnectionService connectionTester)
    {
        _connectionTester = connectionTester ?? throw new ArgumentNullException(nameof(connectionTester));
        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync, CanTestConnection);
    }

    public string Server
    {
        get => _server;
        set => SetProperty(ref _server, value ?? string.Empty);
    }

    public int? Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public string Database
    {
        get => _database;
        set => SetProperty(ref _database, value ?? string.Empty);
    }

    public string UserId
    {
        get => _userId;
        set => SetProperty(ref _userId, value ?? string.Empty);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value ?? string.Empty);
    }

    public bool IntegratedSecurity
    {
        get => _integratedSecurity;
        set => SetProperty(ref _integratedSecurity, value);
    }

    public string TestResult
    {
        get => _testResult;
        private set => SetProperty(ref _testResult, value);
    }

    public bool TestSucceeded
    {
        get => _testSucceeded;
        private set => SetProperty(ref _testSucceeded, value);
    }

    public ICommand TestConnectionCommand { get; }

    public void Load(DbSettingsDTO dto)
    {
        if (dto == null) return;

        Server = dto.Server;
        Port = dto.Port;
        Database = dto.Database;
        UserId = dto.UserId;
        Password = dto.Password;
        IntegratedSecurity = dto.IntegratedSecurity;
        TestResult = string.Empty;
        TestSucceeded = false;
    }

    public DbSettingsDTO ToDto() => new()
    {
        Server = Server,
        Port = Port,
        Database = Database,
        UserId = UserId,
        Password = Password,
        IntegratedSecurity = IntegratedSecurity,
    };

    /// <summary>
    /// Check if the connection can be tested.
    /// </summary>
    /// <returns>True if the connection can be tested, false otherwise.</returns>
    private bool CanTestConnection()
    {
        if (string.IsNullOrWhiteSpace(Server) || string.IsNullOrWhiteSpace(Database))
        {
            return false;
        }

        if (!IntegratedSecurity && string.IsNullOrWhiteSpace(UserId))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Test the connection to the database.
    /// </summary>
    private async Task TestConnectionAsync()
    {
        TestResult = string.Empty;
        TestSucceeded = false;

        try
        {
            var result = await _connectionTester.TestAsync(ToDto());

            TestSucceeded = result.IsSuccess;
            TestResult = result.IsSuccess
                ? (string.IsNullOrWhiteSpace(result.Message)
                    ? Utility.Resources.Strings.TestSuccess
                    : Utility.Resources.Strings.TestSuccess + ": " + result.Message)
                : Utility.Resources.Strings.TestFailed + ": " + result.Message;
        }
        catch (Exception ex)
        {
            TestSucceeded = false;
            TestResult = Utility.Resources.Strings.TestFailed + ": " + ex.Message;
        }
    }
}
