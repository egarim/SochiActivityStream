using System.Windows.Input;

namespace Sochi.Navigation.Commands;

/// <summary>
/// A synchronous command that invokes an <see cref="Action{T}"/> delegate with a typed parameter.
/// </summary>
/// <typeparam name="T">The type of the command parameter.</typeparam>
public sealed class DelegateCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Initializes a new instance of <see cref="DelegateCommand{T}"/>.
    /// </summary>
    /// <param name="execute">The action to execute.</param>
    /// <param name="canExecute">Optional predicate determining if the command can execute.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="execute"/> is null.</exception>
    public DelegateCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        if (parameter == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
            return _canExecute == null;

        return _canExecute?.Invoke((T?)parameter) ?? true;
    }

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute((T?)parameter);

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
