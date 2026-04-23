using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Utility.Batch;
using Utility.Resources;

namespace SettingsUI.ViewModels;

/// <summary>
/// Display-friendly projection of <see cref="BatchListDTO"/> for the batch DataGrid.
/// Pre-formats <see cref="LastRunAtDisplay"/>, <see cref="LastResultDisplay"/> and <see cref="ScheduleDisplay"/> so XAML bindings stay trivial.
/// </summary>
public sealed class BatchRowViewModel
{
    private const string EmptyValue = "-";
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    private const string TimeOfDayFormat = "HH:mm";

    private static readonly Dictionary<EBatchResult, Func<string>> ResultLookup = new Dictionary<EBatchResult, Func<string>>
    {
        [EBatchResult.Success] = () => Strings.ResultSuccess,
        [EBatchResult.Failure] = () => Strings.ResultFailure,
    };

    public BatchRowViewModel(BatchListDTO dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        ID = dto.ID;
        DisplayName = dto.DisplayName ?? string.Empty;
        Description = dto.Description ?? string.Empty;
        ProcedureName = dto.ProcedureName ?? string.Empty;
        IsEnabled = dto.IsEnabled;

        LastRunAtDisplay = dto.LastRunAt.HasValue
            ? dto.LastRunAt.Value.ToString(DateTimeFormat, CultureInfo.CurrentCulture)
            : EmptyValue;

        LastResultDisplay = ResultLookup.TryGetValue(dto.LastResult, out var factory)
            ? factory()
            : EmptyValue;

        ScheduleDisplay = FormatSchedule(dto);
    }

    public int ID { get; }

    public string DisplayName { get; }
    public string Description { get; }
    public string ProcedureName { get; }
    public string LastRunAtDisplay { get; }
    public string LastResultDisplay { get; }
    public string ScheduleDisplay { get; }
    public bool IsEnabled { get; }

    /// <summary>
    /// Build a human-readable schedule string (localized) from a DTO.
    /// Falls back to <see cref="Strings.ScheduleUnknown"/> when values look inconsistent.
    /// </summary>
    private static string FormatSchedule(BatchListDTO dto)
    {
        var culture = CultureInfo.CurrentCulture;

        switch (dto.ScheduleType)
        {
            case EBatchScheduleType.EverySeconds:
                return dto.IntervalValue.HasValue
                    ? string.Format(culture, Strings.ScheduleEverySecondsFormat, dto.IntervalValue.Value)
                    : Strings.ScheduleUnknown;

            case EBatchScheduleType.EveryMinutes:
                return dto.IntervalValue.HasValue
                    ? string.Format(culture, Strings.ScheduleEveryMinutesFormat, dto.IntervalValue.Value)
                    : Strings.ScheduleUnknown;

            case EBatchScheduleType.EveryHours:
                return dto.IntervalValue.HasValue
                    ? string.Format(culture, Strings.ScheduleEveryHoursFormat, dto.IntervalValue.Value)
                    : Strings.ScheduleUnknown;

            case EBatchScheduleType.Daily:
                return string.Format(
                    culture,
                    Strings.ScheduleDailyFormat,
                    FormatTimeOfDay(dto.RunHour, dto.RunMinute));

            case EBatchScheduleType.Weekly:
                return string.Format(
                    culture,
                    Strings.ScheduleWeeklyFormat,
                    FormatWeekDays(dto.WeekDays, culture),
                    FormatTimeOfDay(dto.RunHour, dto.RunMinute));

            case EBatchScheduleType.Monthly:
                return dto.IntervalValue.HasValue
                    ? string.Format(
                        culture,
                        Strings.ScheduleMonthlyFormat,
                        dto.IntervalValue.Value,
                        FormatTimeOfDay(dto.RunHour, dto.RunMinute))
                    : Strings.ScheduleUnknown;

            default:
                return Strings.ScheduleUnknown;
        }
    }

    /// <summary>
    /// Formats the time of day.
    /// </summary>
    /// <param name="hour">The hour.</param>
    /// <param name="minute">The minute.</param>
    /// <returns>The formatted time of day.</returns>
    private static string FormatTimeOfDay(byte? hour, byte? minute)
    {
        var h = hour.GetValueOrDefault();
        var m = minute.GetValueOrDefault();
        return new TimeSpan(h, m, 0).ToString(@"hh\:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats the week days.
    /// </summary>
    /// <param name="bitmask">The bitmask.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The formatted week days.</returns>
    private static string FormatWeekDays(byte bitmask, CultureInfo culture)
    {
        if (bitmask == 0) return EmptyValue;

        var names = culture.DateTimeFormat.AbbreviatedDayNames; // bit0=Sun .. bit6=Sat matches DayOfWeek
        var parts = new List<string>(7);
        for (int bit = 0; bit < 7; bit++)
        {
            if ((bitmask & (1 << bit)) != 0)
            {
                parts.Add(names[bit]);
            }
        }

        return string.Join(",", parts);
    }
}
