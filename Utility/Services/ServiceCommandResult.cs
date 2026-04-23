namespace Utility.Services
{
    /// <summary>
    /// Outcome of a <c>sc.exe</c> invocation. <see cref="IsSuccess"/> mirrors <c>ExitCode == 0</c>
    /// and <see cref="Output"/> contains a merged stdout/stderr suitable for direct display.
    /// </summary>
    public class ServiceCommandResult
    {
        public bool IsSuccess { get; set; }

        public int ExitCode { get; set; }

        public string Output { get; set; } = string.Empty;
    }
}
