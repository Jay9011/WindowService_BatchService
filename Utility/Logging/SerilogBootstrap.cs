using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Utility.Settings;

namespace Utility.Logging
{
    /// <summary>
    /// Single place that builds the shared Serilog <see cref="Logger"/> used by both
    /// <c>BatchService</c> (Windows Service) and <c>SettingsUI</c> (WPF).
    ///
    /// Responsibilities:
    /// <list type="bullet">
    ///     <item>Reads <c>Serilog</c> and <c>Logging</c> sections from <see cref="IConfiguration"/> when provided.</item>
    ///     <item>Writes a rolling file (daily) under <see cref="ConfigPaths.GetLogsDirectory"/>.</item>
    ///     <item>Writes a console sink as well (useful when running from CLI / VS).</item>
    ///     <item>Keeps 30 recent files, splits on 20 MB.</item>
    /// </list>
    /// The Windows Event Log is handled by the default <c>Microsoft.Extensions.Logging</c>
    /// pipeline (which the Windows Service host registers automatically) and therefore is
    /// NOT configured here to avoid duplicate entries.
    /// </summary>
    public static class SerilogBootstrap
    {
        /// <summary>
        /// Default file size that triggers a rollover, in addition to the daily rollover.
        /// </summary>
        private const long DefaultFileSizeLimitBytes = 20L * 1024L * 1024L;

        /// <summary>
        /// How many historical files are retained. Older files are deleted.
        /// </summary>
        private const int DefaultRetainedFileCountLimit = 30;

        /// <summary>
        /// Output format. Uses IETF-style timestamp and right-aligned 3-char level.
        /// </summary>
        private const string DefaultOutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Creates and assigns <see cref="Log.Logger"/> for the current process.
        /// Safe to call multiple times; the previously assigned logger is replaced
        /// but NOT disposed automatically (the host pipeline takes care of that when
        /// registered via <c>AddSerilog(logger, dispose: true)</c>).
        /// </summary>
        /// <param name="fileNamePrefix">Per-application prefix, e.g. <c>"batchservice"</c> or <c>"settingsui"</c>.
        /// Final file name becomes <c>{prefix}-YYYYMMDD.log</c>.</param>
        /// <param name="configuration">Optional configuration. When supplied, the
        /// <c>Serilog</c> / <c>Logging</c> sections are respected; otherwise sensible defaults are used.</param>
        /// <returns>The created logger (also assigned to <see cref="Log.Logger"/>).</returns>
        public static Logger Initialize(string fileNamePrefix, IConfiguration configuration = null)
        {
            if (string.IsNullOrWhiteSpace(fileNamePrefix))
            {
                throw new ArgumentException("File name prefix is required.", nameof(fileNamePrefix));
            }

            var logsDirectory = ConfigPaths.EnsureLogsDirectoryExists();
            var filePath = Path.Combine(logsDirectory, fileNamePrefix + "-.log");

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Verbose)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: DefaultOutputTemplate)
                .WriteTo.File(
                    path: filePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: DefaultRetainedFileCountLimit,
                    fileSizeLimitBytes: DefaultFileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    outputTemplate: DefaultOutputTemplate);

            if (configuration != null)
            {
                // Levels/overrides from "Serilog" section win when specified; otherwise the
                // Verbose floor above allows Microsoft.Extensions.Logging filters (under
                // "Logging:LogLevel") to do the final gating upstream.
                loggerConfig = loggerConfig.ReadFrom.Configuration(configuration);
            }

            var logger = loggerConfig.CreateLogger();
            Log.Logger = logger;
            return logger;
        }

        /// <summary>
        /// Flushes any buffered events to their sinks. Call this just before the
        /// process exits (e.g. in <c>finally</c> around <c>host.Run()</c> or in WPF's
        /// <c>OnExit</c>) so that logs captured during shutdown actually reach disk.
        /// </summary>
        public static void Shutdown()
        {
            Log.CloseAndFlush();
        }
    }
}
