using System;
using System.ComponentModel;
using System.ServiceProcess;
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
            await StopAsync(half, cancellationToken).ConfigureAwait(false);
            await StartAsync(half, cancellationToken).ConfigureAwait(false);
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
