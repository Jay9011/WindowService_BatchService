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
    /// Single place that builds the shared Serilog <see cref="Logger"/>
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
        /// Initializes the Serilog logger.
        /// </summary>
        /// <param name="fileNamePrefix">The prefix for the log file name.</param>
        /// <param name="configuration">The configuration for the logger.</param>
        /// <returns>The logger.</returns>
        public static Logger Initialize(string fileNamePrefix, IConfiguration? configuration = null)
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
