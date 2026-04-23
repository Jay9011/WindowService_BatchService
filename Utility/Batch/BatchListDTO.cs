using System;

namespace Utility.Batch
{
    /// <summary>
    /// One row returned from the <c>BATCH_GetBatchList</c> stored procedure.
    /// </summary>
    public class BatchListDTO
    {
        public int ID { get; set; }

        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ProcedureName { get; set; } = string.Empty;

        // ============================================
        // --------------- schedule ------------------
        public EBatchScheduleType ScheduleType { get; set; }
        public int? IntervalValue { get; set; }
        public byte? RunHour { get; set; }
        public byte? RunMinute { get; set; }

        /// <summary>
        /// Weekly-only bitmask. bit0=Sun, bit1=Mon, ..., bit6=Sat.
        /// </summary>
        public byte WeekDays { get; set; }
        // ============================================

        // ============================================
        // ---------- custom key/value pairs ----------
        public string? CustomKey1 { get; set; }
        public string? CustomValue1 { get; set; }
        public string? CustomKey2 { get; set; }
        public string? CustomValue2 { get; set; }
        public string? CustomKey3 { get; set; }
        public string? CustomValue3 { get; set; }
        public string? CustomKey4 { get; set; }
        public string? CustomValue4 { get; set; }
        public string? CustomKey5 { get; set; }
        public string? CustomValue5 { get; set; }
        // ============================================

        // ============================================
        // --------- denormalized latest run ----------
        public DateTime? LastRunAt { get; set; }
        public EBatchResult LastResult { get; set; }

        public bool IsEnabled { get; set; }

        public DateTime UpdatedAt { get; set; }
        // ============================================
    }
}
