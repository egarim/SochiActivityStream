using System.Windows.Input;

namespace Sochi.Navigation.Commands;

/// <summary>
/// Extends <see cref="ICommand"/> to support asynchronous execution.
/// </summary>
public interface IAsyncCommand : ICommand
{
    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <param name="parameter">Data used by the command.</param>
    Task ExecuteAsync(object? parameter);

    /// <summary>
    /// Gets a value indicating whether the command is currently executing.
    /// </summary>
    bool IsExecuting { get; }
}
