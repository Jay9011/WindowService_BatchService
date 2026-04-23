namespace BatchService.Models;

/// <summary>
/// One row returned from the <c>BATCH_GetBatchList</c> stored procedure.
/// </summary>
public class BatchListDTO
{
    public int ID { get; set; }

    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Stored procedure name to EXEC when this batch fires.</summary>
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

    #region Helper Methods

    /// <summary>
    /// Converts the DTO to a <see cref="BatchExecEntity"/>.
    /// </summary>
    /// <param name="dto">The DTO to convert.</param>
    /// <returns>A <see cref="BatchExecEntity"/> object representing the batch execution parameters.</returns>
    public static BatchExecEntity ToExecEntity(BatchListDTO dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new BatchExecEntity
        {
            Custom1 = dto.CustomValue1,
            Custom2 = dto.CustomValue2,
            Custom3 = dto.CustomValue3,
            Custom4 = dto.CustomValue4,
            Custom5 = dto.CustomValue5,
        };
    }

    #endregion
}
