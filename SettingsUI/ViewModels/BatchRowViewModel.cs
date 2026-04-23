using System;
using System.Collections.Generic;
using System.Globalization;
using Utility.Batch;
using Utility.Resources;

namespace SettingsUI.ViewModels;

/// <summary>
/// Display-friendly projection of <see cref="BatchListDTO"/> for the batch DataGrid.
/// Pre-formats <see cref="LastRunAtDisplay"/> and localizes <see cref="LastResultDisplay"/>
/// so XAML bindings stay trivial.
/// </summary>
public sealed class BatchRowViewModel
{
    private const string EmptyValue = "-";
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

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
    }

    public int ID { get; }

    public string DisplayName { get; }
    public string Description { get; }
    public string ProcedureName { get; }
    public string LastRunAtDisplay { get; }
    public string LastResultDisplay { get; }
    public bool IsEnabled { get; }
}
