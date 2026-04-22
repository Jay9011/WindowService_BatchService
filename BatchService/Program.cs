using System.Globalization;
using BatchService;
using Utility.Common;
using Utility.Security;
using Utility.Settings;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentUICulture;

var store = new SettingsFileStore();
await store.EnsureInitializedAsync();

var builder = Host.CreateApplicationBuilder(args);

var configDirectory = ConfigPaths.GetConfigDirectory();

// ================================================
// ==> Strrings AppSettings File
// ================================================
builder.Configuration.Sources.Clear();
builder.Configuration.SetBasePath(configDirectory);
builder.Configuration.AddJsonFile(Keys.MainConfigFileName, optional: false, reloadOnChange: true);

if (ConfigPaths.IsDevelopment)
{
    builder.Configuration.AddJsonFile(Keys.DevelopmentConfigFileName, optional: true, reloadOnChange: true);
}
// ================================================
// <== Settings AppSettings File
// ================================================

builder.Configuration.AddEnvironmentVariables();
if (args is { Length: > 0 })
{
    builder.Configuration.AddCommandLine(args);
}

// ================================================
// ==> Add Windows Service
// ================================================
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = Keys.Key_ServiceName;
});
// ================================================
// <== Add Windows Service
// ================================================

// ================================================
// ==> Configure Batch Service Options
// ================================================
builder.Services.Configure<BatchServiceOptions>(
    builder.Configuration.GetSection(Keys.Key_Batch)
);
builder.Services.PostConfigure<BatchServiceOptions>(options =>
{
    if (options.Database != null && !string.IsNullOrEmpty(options.Database.Password))
    {
        options.Database.Password = DpapiProtector.Decrypt(options.Database.Password);
    }
});
// ================================================
// <== Configure Batch Service Options
// ================================================

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
