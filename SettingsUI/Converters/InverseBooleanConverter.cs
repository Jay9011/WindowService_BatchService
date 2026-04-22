using System;
using System.Globalization;
using System.Windows.Data;

namespace SettingsUI.Converters;

/// <summary>
/// Inverts a boolean for bindings like IsEnabled="{Binding Flag, Converter=...}".
/// Non-boolean input falls back to <c>false</c>.
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : false;
}
