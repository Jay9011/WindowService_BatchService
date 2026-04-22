using System.Windows.Input;

namespace SettingsUI.Infrastructure;

/// <summary>
/// ICommand that awaits an asynchronous handler and blocks concurrent executions.
/// XAML Command Binding Syntax: Command="{Binding CommandName}"
/// Asyncchronous and blocks concurrent executions.
/// </summary>
public class AsyncRelayCommand : ICommand
{
    /// <summary>
    /// when clicked button, the action will be executed.
    /// The action will be executed asynchronously. (Task)
    /// </summary>
    private readonly Func<object?, Task> _execute;

    /// <summary>
    /// The condition to execute the command.
    /// </summary>
    private readonly Predicate<object?>? _canExecute;

    /// <summary>
    /// Whether the command is executing.
    /// </summary>
    private bool _isExecuting;
    /// <summary>
    /// Whether the command is executing.
    /// </summary>
    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            _isExecuting = value;
            RaiseCanExecuteChanged();
        }
    }

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : (Predicate<object?>)(_ => canExecute()))
    { }

    public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
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
    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="parameter">The parameter to pass to the command.</param>
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        IsExecuting = true;
        try
        {
            await _execute(parameter).ConfigureAwait(true);
        }
        finally
        {
            IsExecuting = false;
        }
    }

    /// <summary>
    /// A global request to manually re-ask CanExecute for all commands
    /// </summary>
    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}
