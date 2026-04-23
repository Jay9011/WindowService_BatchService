using System;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utility.Common;

namespace Utility.Services
{
    /// <summary>
    /// Thin wrapper over <see cref="ServiceController"/> scoped to the Batch Service.
    /// </summary>
    public class ServiceControlService
    {
        private readonly string _serviceName;
        public string ServiceName => _serviceName;

        public ServiceControlService() : this(Keys.Key_ServiceName)
        { }

        public ServiceControlService(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentException("Service name must not be empty.", nameof(serviceName));
            }

            _serviceName = serviceName;
        }

        /// <summary>
        /// Returns the current service status. Never throws; unexpected conditions map to
        /// <see cref="EServiceStatus.NotInstalled"/> or <see cref="EServiceStatus.AccessDenied"/>.
        /// </summary>
        public EServiceStatus GetStatus()
        {
            try
            {
                using (var sc = new ServiceController(_serviceName))
                {
                    sc.Refresh();
                    return MapStatus(sc.Status);
                }
            }
            catch (InvalidOperationException)
            {
                return EServiceStatus.NotInstalled;
            }
            catch (Win32Exception)
            {
                return EServiceStatus.AccessDenied;
            }
            catch
            {
                return EServiceStatus.Unknown;
            }
        }

        public Task StartAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                using (var sc = new ServiceController(_serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        return;
                    }

                    if (sc.Status != ServiceControllerStatus.StartPending)
                    {
                        sc.Start();
                    }

                    sc.WaitForStatus(ServiceControllerStatus.Running, timeout);
                }
            }, cancellationToken);
        }

        public Task StopAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                using (var sc = new ServiceController(_serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        return;
                    }

                    if (sc.CanStop && sc.Status != ServiceControllerStatus.StopPending)
                    {
                        sc.Stop();
                    }

                    sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                }
            }, cancellationToken);
        }

        public async Task RestartAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var half = TimeSpan.FromTicks(timeout.Ticks / 2);
            await StopAsync(half, cancellationToken);
            await StartAsync(half, cancellationToken);
        }

        /// <summary>
        /// Installs the Windows service via <c>sc.exe create</c>. Requires administrator privileges.
        /// </summary>
        public Task<ServiceCommandResult> InstallAsync(ServiceInstallOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(options.BinaryPath))
            {
                throw new ArgumentException("Binary path is required.", nameof(options));
            }

            return Task.Run(async () =>
            {
                var createArgs = BuildCreateArguments(options);
                var createResult = RunSc(createArgs);
                if (!createResult.IsSuccess)
                {
                    return createResult;
                }

                if (!string.IsNullOrEmpty(options.Description))
                {
                    var descArgs = "description " + Quote(_serviceName) + " " + Quote(options.Description!);
                    RunSc(descArgs);
                }

                await Task.CompletedTask;

                return createResult;
            }, cancellationToken);
        }

        /// <summary>
        /// Uninstalls the Windows service via <c>sc.exe delete</c>. Requires administrator privileges.
        /// The service must be stopped first; otherwise Windows only marks it for deletion.
        /// </summary>
        public Task<ServiceCommandResult> UninstallAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                var args = "delete " + Quote(_serviceName);
                return RunSc(args);
            }, cancellationToken);
        }

        #region Private Methods

        /// <summary>
        /// Maps a <see cref="ServiceControllerStatus"/> to an <see cref="EServiceStatus"/>.
        /// </summary>
        /// <param name="status">The <see cref="ServiceControllerStatus"/> to map.</param>
        /// <returns>The mapped <see cref="EServiceStatus"/>.</returns>
        private static EServiceStatus MapStatus(ServiceControllerStatus status)
        {
            switch (status)
            {
                case ServiceControllerStatus.Running: return EServiceStatus.Running;
                case ServiceControllerStatus.Stopped: return EServiceStatus.Stopped;
                case ServiceControllerStatus.Paused: return EServiceStatus.Paused;
                case ServiceControllerStatus.StartPending: return EServiceStatus.StartPending;
                case ServiceControllerStatus.StopPending: return EServiceStatus.StopPending;
                case ServiceControllerStatus.ContinuePending: return EServiceStatus.ContinuePending;
                case ServiceControllerStatus.PausePending: return EServiceStatus.PausePending;
                default: return EServiceStatus.Unknown;
            }
        }

        /// <summary>
        /// Build <c>sc create</c> argument string (no process invocation yet).
        /// </summary>
        private string BuildCreateArguments(ServiceInstallOptions options)
        {
            var sb = new StringBuilder();
            sb.Append("create ").Append(Quote(_serviceName));
            sb.Append(" binPath= ").Append(Quote(options.BinaryPath));
            sb.Append(" start= ").Append(ToScStart(options.StartType));
            if (!string.IsNullOrEmpty(options.DisplayName))
            {
                sb.Append(" DisplayName= ").Append(Quote(options.DisplayName!));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Runs <c>sc.exe</c> synchronously and captures stdout/stderr. Always returns a result,
        /// never throws for non-zero exit codes (caller inspects <see cref="ServiceCommandResult.IsSuccess"/>).
        /// </summary>
        private static ServiceCommandResult RunSc(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using (var process = new Process { StartInfo = psi })
            {
                process.Start();
                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                var output = string.IsNullOrEmpty(stderr)
                    ? stdout
                    : (string.IsNullOrEmpty(stdout) ? stderr : stdout + Environment.NewLine + stderr);

                return new ServiceCommandResult
                {
                    ExitCode = process.ExitCode,
                    IsSuccess = process.ExitCode == 0,
                    Output = (output ?? string.Empty).Trim(),
                };
            }
        }

        /// <summary>
        /// Wraps a value in double-quotes, escaping any internal quotes (<c>"</c> → <c>\"</c>).
        /// </summary>
        private static string Quote(string value)
        {
            if (value == null) return "\"\"";
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        /// <summary>
        /// Maps <see cref="EServiceStartType"/> to the <c>sc.exe</c> start= token.
        /// </summary>
        private static string ToScStart(EServiceStartType startType)
        {
            switch (startType)
            {
                case EServiceStartType.DelayedAuto: return "delayed-auto";
                case EServiceStartType.Manual: return "demand";
                case EServiceStartType.Disabled: return "disabled";
                case EServiceStartType.Auto:
                default: return "auto";
            }
        }

        /// <summary>
        /// Returns a localized display string for a status value (sourced from Utility.Resources.Strings).
        /// </summary>
        public static string ToDisplay(EServiceStatus status)
        {
            switch (status)
            {
                case EServiceStatus.Running: return Resources.Strings.StatusRunning;
                case EServiceStatus.Stopped: return Resources.Strings.StatusStopped;
                case EServiceStatus.Paused: return Resources.Strings.StatusPaused;
                case EServiceStatus.StartPending: return Resources.Strings.StatusStartPending;
                case EServiceStatus.StopPending: return Resources.Strings.StatusStopPending;
                case EServiceStatus.ContinuePending: return Resources.Strings.StatusStartPending;
                case EServiceStatus.PausePending: return Resources.Strings.StatusStopPending;
                case EServiceStatus.NotInstalled: return Resources.Strings.StatusNotInstalled;
                case EServiceStatus.AccessDenied: return Resources.Strings.StatusAccessDenied;
                default: return status.ToString();
            }
        }

        #endregion
    }
}
