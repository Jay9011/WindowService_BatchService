using System;
using System.IO;
using Utility.Common;

namespace Utility.Settings
{
    /// <summary>
    /// Resolves configuration file locations depending on the runtime environment.
    ///
    /// <list type="bullet">
    ///     <item>Development: application base directory (output folder)</item>
    ///     <item>Production:  %ProgramData%\SECUiDEA\BatchService</item>
    /// </list>
    /// </summary>
    public static class ConfigPaths
    {
#if DEBUG
        public static bool IsDevelopment = true;
#else
        public static bool IsDevelopment = false;
#endif

        /// <summary>
        /// Returns the configuration directory for the current environment.
        /// Does NOT create the directory; call <see cref="EnsureDirectoryExists"/> when needed.
        /// </summary>
        public static string GetConfigDirectory()
        {
            if (IsDevelopment)
            {
                return AppContext.BaseDirectory;
            }

            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(programData, Keys.Key_Company, Keys.Key_ProductFolder);
        }

        /// <summary>
        /// Full path of the main configuration file (appsettings.json).
        /// </summary>
        public static string GetMainConfigFilePath()
        {
            return Path.Combine(GetConfigDirectory(), Keys.MainConfigFileName);
        }

        /// <summary>
        /// Full path of the development configuration file (appsettings.Development.json).
        /// The UI must never write to this file; Worker only loads it in Development mode.
        /// </summary>
        public static string GetDevelopmentConfigFilePath()
        {
            return Path.Combine(GetConfigDirectory(), Keys.DevelopmentConfigFileName);
        }

        /// <summary>
        /// Ensures the configuration directory exists. Throws if creation fails.
        /// </summary>
        /// <returns>The absolute path of the configuration directory.</returns>
        /// <exception cref="IOException">If directory creation fails (e.g. missing admin rights).</exception>
        public static string EnsureDirectoryExists()
        {
            var directory = GetConfigDirectory();

            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                throw new IOException(Resources.Strings.ErrorConfigDirCreate + ": " + directory, ex);
            }

            return directory;
        }

        /// <summary>
        /// Returns the log directory co-located with the configuration directory.
        /// Both BatchService and SettingsUI use the same ProgramData root so log files
        /// inherit the same ACLs as appsettings.json.
        /// </summary>
        public static string GetLogsDirectory()
        {
            return Path.Combine(GetConfigDirectory(), Keys.Key_LogsFolder);
        }

        /// <summary>
        /// Ensures the log directory exists. Call this before initializing Serilog
        /// so the first write does not fail when ProgramData subfolder has not been created yet.
        /// </summary>
        public static string EnsureLogsDirectoryExists()
        {
            var directory = GetLogsDirectory();

            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                throw new IOException(Resources.Strings.ErrorConfigDirCreate + ": " + directory, ex);
            }

            return directory;
        }
    }
}
