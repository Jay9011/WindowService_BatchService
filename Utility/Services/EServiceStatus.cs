namespace Utility.Services
{
    /// <summary>
    /// Canonical status values used by the UI. Maps to <see cref="ServiceControllerStatus"/> plus
    /// two additional non-controller states.
    /// </summary>
    public enum EServiceStatus
    {
        Unknown = 0,
        Running,
        Stopped,
        Paused,
        StartPending,
        StopPending,
        ContinuePending,
        PausePending,
        NotInstalled,
        AccessDenied
    }
}
