using System;
using System.Windows.Input;

namespace SettingsUI.Infrastructure;

/// <summary>
/// Synchronous ICommand. Use <see cref="AsyncRelayCommand"/> for awaitable handlers.
/// XAML Command Binding Syntax: Command="{Binding CommandName}"
/// </summary>
public class RelayCommand : ICommand
{
    /// <summary>
    /// when clicked button, the action will be executed.
    /// </summary>
    private readonly Action<object?> _execute;

    /// <summary>
    /// inject the condition to execute the command.
    /// </summary>
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : (Predicate<object?>)(_ => canExecute()))
    { }

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Event handler for the CanExecuteChanged event.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Returns true if the command can execute, false otherwise.
    /// </summary>
    /// <param name="parameter">The parameter to pass to the command.</param>
    /// <returns>True if the command can execute, false otherwise.</returns>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="parameter">The parameter to pass to the command.</param>
    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>
    /// A global request to manually re-ask CanExecute for all commands
    /// </summary>
    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}
