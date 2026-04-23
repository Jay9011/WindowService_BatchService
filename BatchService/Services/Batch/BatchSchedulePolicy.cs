using Utility.Batch;

namespace BatchService.Services.Batch;

public class BatchSchedulePolicy : IBatchSchedulePolicy
{
    public bool IsDue(BatchListDTO batch, DateTime now)
    {
        if (batch == null) throw new ArgumentNullException(nameof(batch));

        if (!batch.IsEnabled) return false;

        switch (batch.ScheduleType)
        {
            case EBatchScheduleType.EverySeconds:
                return IsDueForInterval(batch, now, TimeSpan.FromSeconds(1));

            case EBatchScheduleType.EveryMinutes:
                return IsDueForInterval(batch, now, TimeSpan.FromMinutes(1));

            case EBatchScheduleType.EveryHours:
                return IsDueForInterval(batch, now, TimeSpan.FromHours(1));

            case EBatchScheduleType.Daily:
                return IsDueForDaily(batch, now);

            case EBatchScheduleType.Weekly:
                return IsDueForWeekly(batch, now);

            case EBatchScheduleType.Monthly:
                return IsDueForMonthly(batch, now);

            default:
                return false;
        }
    }

    #region private methods

    /// <summary>
    /// Every N-unit policy: fires immediately on first run.
    /// </summary>
    /// <param name="batch">The batch to check.</param>
    /// <param name="now">The current local time.</param>
    /// <param name="unit">The unit of time.</param>
    /// <returns><c>true</c> if the batch is due to run, <c>false</c> otherwise.</returns>
    private static bool IsDueForInterval(BatchListDTO batch, DateTime now, TimeSpan unit)
    {
        var n = batch.IntervalValue.GetValueOrDefault();
        if (n <= 0)
        {
            return false;
        }

        if (batch.LastRunAt == null)
        {
            return true;
        }

        var interval = TimeSpan.FromTicks(unit.Ticks * n);
        return now - batch.LastRunAt.Value >= interval;
    }

    /// <summary>
    /// Daily at HH:MM - fires once per calendar day once target time is reached.
    /// </summary>
    /// <param name="batch">The batch to check.</param>
    /// <param name="now">The current local time.</param>
    /// <returns><c>true</c> if the batch is due to run, <c>false</c> otherwise.</returns>
    private static bool IsDueForDaily(BatchListDTO batch, DateTime now)
    {
        if (!TryGetTargetTimeToday(batch, now, out var targetToday))
        {
            return false;
        }

        if (now < targetToday)
        {
            return false;
        }

        return batch.LastRunAt == null || batch.LastRunAt.Value < targetToday;
    }

    /// <summary>
    /// Weekly on matching bitmask days at HH:MM.
    /// </summary>
    /// <param name="batch">The batch to check.</param>
    /// <param name="now">The current local time.</param>
    /// <returns><c>true</c> if the batch is due to run, <c>false</c> otherwise.</returns>
    private static bool IsDueForWeekly(BatchListDTO batch, DateTime now)
    {
        if (batch.WeekDays == 0)
        {
            return false;
        }

        if (!IsWeekdayEnabled(batch.WeekDays, now.DayOfWeek))
        {
            return false;
        }

        return IsDueForDaily(batch, now);
    }

    /// <summary>
    /// Every N months at HH:MM.
    /// Target = <c>LastRunAt.Date.AddMonths(N)</c> + HH:MM. (fires once today if that time has passed).
    /// </summary>
    /// <param name="batch">The batch to check.</param>
    /// <param name="now">The current local time.</param>
    /// <returns><c>true</c> if the batch is due to run, <c>false</c> otherwise.</returns>
    private static bool IsDueForMonthly(BatchListDTO batch, DateTime now)
    {
        var n = batch.IntervalValue.GetValueOrDefault();
        if (n <= 0)
        {
            return false;
        }

        if (!TryGetTimeOfDay(batch, out var hour, out var minute))
        {
            return false;
        }

        if (batch.LastRunAt == null)
        {
            var targetToday = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, DateTimeKind.Local);
            return now >= targetToday;
        }

        var lastDate = batch.LastRunAt.Value.Date;
        var nextDate = lastDate.AddMonths(n);
        var target = new DateTime(nextDate.Year, nextDate.Month, nextDate.Day, hour, minute, 0, DateTimeKind.Local);

        return now >= target;
    }

    /// <summary>
    /// Resolves today's scheduled HH:MM anchor in local time. Returns <c>false</c> if either RunHour or RunMinute is missing.
    /// </summary>
    /// <param name="batch">The batch to check.</param>
    /// <param name="now">The current local time.</param>
    /// <param name="targetToday">The target time for today.</param>
    /// <returns><c>true</c> if the target time for today is successfully resolved, <c>false</c> otherwise.</returns>
    private static bool TryGetTargetTimeToday(BatchListDTO batch, DateTime now, out DateTime targetToday)
    {
        if (!TryGetTimeOfDay(batch, out var hour, out var minute))
        {
            targetToday = default;
            return false;
        }

        targetToday = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0, DateTimeKind.Local);
        return true;
    }

    /// <summary>
    /// Tries to get the hour and minute from the batch.
    /// </summary>
    /// <param name="batch">The batch to check.</param>
    /// <param name="hour">The hour.</param>
    /// <param name="minute">The minute.</param>
    /// <returns><c>true</c> if the hour and minute are successfully resolved, <c>false</c> otherwise.</returns>
    private static bool TryGetTimeOfDay(BatchListDTO batch, out int hour, out int minute)
    {
        hour = batch.RunHour.GetValueOrDefault();
        minute = batch.RunMinute.GetValueOrDefault();

        return batch.RunHour.HasValue && batch.RunMinute.HasValue
            && hour >= 0 && hour <= 23
            && minute >= 0 && minute <= 59;
    }

    /// <summary>
    /// bit0=Sun, bit1=Mon, ..., bit6=Sat (matches schema CK_BatchList_WeekDays).
    /// </summary>
    /// <param name="mask">The mask to check.</param>
    /// <param name="day">The day of the week.</param>
    /// <returns><c>true</c> if the day is enabled, <c>false</c> otherwise.</returns>
    private static bool IsWeekdayEnabled(byte mask, DayOfWeek day) => (mask & (1 << (int)day)) != 0;

    #endregion
}
