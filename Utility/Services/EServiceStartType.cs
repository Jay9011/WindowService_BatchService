namespace Utility.Services
{
    /// <summary>
    /// Service start type used when installing a Windows service via <c>sc.exe</c>.
    /// </summary>
    public enum EServiceStartType
    {
        /// <summary>Starts automatically at system startup.</summary>
        Auto,

        /// <summary>Starts automatically ~2 minutes after boot to reduce startup contention.</summary>
        DelayedAuto,

        /// <summary>Must be started manually (sc.exe token: <c>demand</c>).</summary>
        Manual,

        /// <summary>Service cannot be started.</summary>
        Disabled,
    }
}
