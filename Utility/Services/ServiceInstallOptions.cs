namespace Utility.Services
{
    /// <summary>
    /// Parameters passed to <see cref="ServiceControlService.InstallAsync"/> to register a new Windows service.
    /// </summary>
    public class ServiceInstallOptions
    {
        /// <summary>
        /// Full path to the service executable (e.g. <c>C:\Program Files\...\BatchService.exe</c>).
        /// Required.
        /// </summary>
        public string BinaryPath { get; set; } = string.Empty;

        /// <summary>
        /// Friendly display name shown in services.msc. Optional.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Free-form description shown in services.msc. Optional.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// How Windows should start the service. Defaults to <see cref="EServiceStartType.Auto"/>.
        /// </summary>
        public EServiceStartType StartType { get; set; } = EServiceStartType.Auto;
    }
}
