using System;

namespace Utility.Settings
{
    /// <summary>
    /// Batch Service Job Options
    /// </summary>
    public class BatchJobOptions
    {
        public string ProcedureName { get; set; } = string.Empty;

        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);
    }
}
