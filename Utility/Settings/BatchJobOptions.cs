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

        /// <summary>
        /// Maximum number of batches that may run in parallel within a single polling tick.
        /// Values &lt;= 0 fall back to the default (4).
        /// </summary>
        public int MaxConcurrency { get; set; } = 4;
    }
}
