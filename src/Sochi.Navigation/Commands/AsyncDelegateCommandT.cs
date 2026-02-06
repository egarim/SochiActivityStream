namespace Sochi.Navigation.Commands;

/// <summary>
/// An asynchronous command that invokes a <see cref="Func{T, Task}"/> delegate with a typed parameter.
/// </summary>
/// <typeparam name="T">The type of the command parameter.</typeparam>
public sealed class AsyncDelegateCommand<T> : IAsyncCommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private bool _isExecuting;

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncDelegateCommand{T}"/>.
    /// </summary>
    /// <param name="execute">The async function to execute.</param>
    /// <param name="canExecute">Optional predicate determining if the command can execute.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="execute"/> is null.</exception>
    public AsyncDelegateCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (_isExecuting != value)
            {
                _isExecuting = value;
                RaiseCanExecuteChanged();
            }
        }
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        if (IsExecuting)
            return false;

        if (parameter == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
            return _canExecute == null;

        return _canExecute?.Invoke((T?)parameter) ?? true;
    }

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter);
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        IsExecuting = true;
        try
        {
            await _execute((T?)parameter);
        }
        finally
        {
            IsExecuting = false;
        }
    }

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
