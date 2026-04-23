namespace Utility.Batch
{
    /// <summary>
    /// Schedule kind for a batch entry. Values match the TINYINT stored in <c>BatchList.ScheduleType</c>and the <c>CK_BatchList_ScheduleShape</c> constraint in the database.
    /// </summary>
    public enum EBatchScheduleType
    {
        /// <summary>Fires every <c>IntervalValue</c> seconds.</summary>
        EverySeconds = 1,

        /// <summary>Fires every <c>IntervalValue</c> minutes.</summary>
        EveryMinutes = 2,

        /// <summary>Fires every <c>IntervalValue</c> hours.</summary>
        EveryHours = 3,

        /// <summary>Fires once a day at <c>RunHour</c>:<c>RunMinute</c>.</summary>
        Daily = 4,

        /// <summary>Fires on selected <c>WeekDays</c> at <c>RunHour</c>:<c>RunMinute</c>.</summary>
        Weekly = 5,

        /// <summary>Fires every <c>IntervalValue</c> months at <c>RunHour</c>:<c>RunMinute</c>.</summary>
        Monthly = 6,
    }
}
