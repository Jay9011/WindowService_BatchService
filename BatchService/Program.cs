using System.Globalization;
using BatchService;
using Serilog;
using Utility.Common;
using Utility.Logging;
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

SerilogBootstrap.Initialize("batchservice", builder.Configuration);
builder.Services.AddSerilog(Log.Logger, dispose: true);

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

try
{
    var host = builder.Build();
    Log.Information("BatchService starting (env={Environment}, configDir={ConfigDir})",
        ConfigPaths.IsDevelopment ? "Development" : "Production", configDirectory);
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BatchService terminated unexpectedly");
    throw;
}
finally
{
    SerilogBootstrap.Shutdown();
}
