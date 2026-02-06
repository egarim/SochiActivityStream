using System.Windows.Input;
using Sochi.Navigation.Commands;

namespace Sochi.Navigation.Mvvm;

/// <summary>
/// Base class for ViewModels with common functionality.
/// </summary>
public abstract class ViewModelBase : BindableBase
{
    private bool _isBusy;
    private string _title = string.Empty;
    private readonly List<ICommand> _commands = new();

    /// <summary>
    /// Gets or sets a value indicating whether the ViewModel is busy.
    /// When changed, automatically raises <see cref="ICommand.CanExecuteChanged"/> for all registered commands.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCanExecuteChangedForAllCommands();
            }
        }
    }

    /// <summary>
    /// Gets or sets the title of the ViewModel.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Registers a command to be tracked for automatic <see cref="ICommand.CanExecuteChanged"/> notifications.
    /// </summary>
    /// <param name="command">The command to register.</param>
    protected void RegisterCommand(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!_commands.Contains(command))
        {
            _commands.Add(command);
        }
    }

    /// <summary>
    /// Raises <see cref="ICommand.CanExecuteChanged"/> for all registered commands.
    /// </summary>
    protected void RaiseCanExecuteChangedForAllCommands()
    {
        foreach (var command in _commands)
        {
            RaiseCanExecuteChanged(command);
        }
    }

    /// <summary>
    /// Raises <see cref="ICommand.CanExecuteChanged"/> for a specific command.
    /// </summary>
    /// <param name="command">The command to raise the event for.</param>
    protected static void RaiseCanExecuteChanged(ICommand command)
    {
        switch (command)
        {
            case DelegateCommand delegateCommand:
                delegateCommand.RaiseCanExecuteChanged();
                break;
            case AsyncDelegateCommand asyncCommand:
                asyncCommand.RaiseCanExecuteChanged();
                break;
        }

        // Handle generic versions via reflection on the RaiseCanExecuteChanged method
        var type = command.GetType();
        if (type.IsGenericType)
        {
            var method = type.GetMethod("RaiseCanExecuteChanged");
            method?.Invoke(command, null);
        }
    }
}
