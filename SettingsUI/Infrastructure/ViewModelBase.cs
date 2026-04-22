using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SettingsUI.Infrastructure;

/// <summary>
/// Base class that implements <see cref="INotifyPropertyChanged"/> with a small
/// helper <see cref="SetProperty"/> for concise property setters.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raise the PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Set the property value and raise the PropertyChanged event if the value has changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">The field to set.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>True if the property value has changed, false otherwise.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
