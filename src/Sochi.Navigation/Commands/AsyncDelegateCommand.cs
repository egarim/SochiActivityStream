namespace Sochi.Navigation.Commands;

/// <summary>
/// An asynchronous command that invokes a <see cref="Func{Task}"/> delegate.
/// </summary>
public sealed class AsyncDelegateCommand : IAsyncCommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncDelegateCommand"/>.
    /// </summary>
    /// <param name="execute">The async function to execute.</param>
    /// <param name="canExecute">Optional predicate determining if the command can execute.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="execute"/> is null.</exception>
    public AsyncDelegateCommand(Func<Task> execute, Func<bool>? canExecute = null)
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

        return _canExecute?.Invoke() ?? true;
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
            await _execute();
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
