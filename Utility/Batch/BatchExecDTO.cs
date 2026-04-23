using System;

namespace Utility.Batch
{
    /// <summary>
    /// Outcome of a single batch execution inside one polling tick.
    /// Produced by the batch execution service and surfaced for logging / aggregation.
    /// </summary>
    public class BatchExecDTO
    {
        public int BatchID { get; }
        public string DisplayName { get; }
        public string ProcedureName { get; }

        public DateTime StartedAt { get; }
        public DateTime EndedAt { get; }
        public TimeSpan Elapsed { get; }

        public bool IsSuccess { get; }
        public string? Message { get; }

        public BatchExecDTO(int batchId, string displayName, string procedureName, DateTime startedAt, DateTime endedAt, TimeSpan elapsed, bool isSuccess, string? message)
        {
            BatchID = batchId;
            DisplayName = displayName ?? string.Empty;
            ProcedureName = procedureName ?? string.Empty;
            StartedAt = startedAt;
            EndedAt = endedAt;
            Elapsed = elapsed;
            IsSuccess = isSuccess;
            Message = message;
        }
    }
}
