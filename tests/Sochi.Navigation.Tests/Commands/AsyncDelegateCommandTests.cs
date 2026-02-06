using Sochi.Navigation.Commands;

namespace Sochi.Navigation.Tests.Commands;

public sealed class AsyncDelegateCommandTests
{
    [Fact]
    public async Task ExecuteAsync_invokes_action()
    {
        // Arrange
        var executed = false;
        var command = new AsyncDelegateCommand(async () =>
        {
            await Task.Delay(10);
            executed = true;
        });

        // Act
        await command.ExecuteAsync(null);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task IsExecuting_is_true_while_executing()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        var command = new AsyncDelegateCommand(() => tcs.Task);
        var isExecutingDuringRun = false;

        // Act
        var executeTask = Task.Run(async () =>
        {
            var task = command.ExecuteAsync(null);
            await Task.Delay(50); // Give time for execution to start
            isExecutingDuringRun = command.IsExecuting;
            tcs.SetResult(true);
            await task;
        });

        await executeTask;

        // Assert
        Assert.True(isExecutingDuringRun);
        Assert.False(command.IsExecuting);
    }

    [Fact]
    public void CanExecute_while_executing_returns_false()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        var command = new AsyncDelegateCommand(() => tcs.Task);

        // Act - start execution
        _ = command.ExecuteAsync(null);

        // Assert
        Assert.False(command.CanExecute(null));

        // Cleanup
        tcs.SetResult(true);
    }

    [Fact]
    public void CanExecute_without_predicate_and_not_executing_returns_true()
    {
        // Arrange
        var command = new AsyncDelegateCommand(() => Task.CompletedTask);

        // Act
        var result = command.CanExecute(null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanExecute_with_predicate_returns_predicate_result()
    {
        // Arrange
        var canExecute = false;
        var command = new AsyncDelegateCommand(() => Task.CompletedTask, () => canExecute);

        // Act & Assert
        Assert.False(command.CanExecute(null));

        canExecute = true;
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void RaiseCanExecuteChanged_raises_event()
    {
        // Arrange
        var command = new AsyncDelegateCommand(() => Task.CompletedTask);
        var eventRaised = false;
        command.CanExecuteChanged += (s, e) => eventRaised = true;

        // Act
        command.RaiseCanExecuteChanged();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void Constructor_throws_when_execute_is_null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AsyncDelegateCommand(null!));
    }
}

public sealed class AsyncDelegateCommandTTests
{
    [Fact]
    public async Task ExecuteAsync_invokes_action_with_parameter()
    {
        // Arrange
        int? receivedValue = null;
        var command = new AsyncDelegateCommand<int>(async v =>
        {
            await Task.Delay(10);
            receivedValue = v;
        });

        // Act
        await command.ExecuteAsync(42);

        // Assert
        Assert.Equal(42, receivedValue);
    }

    [Fact]
    public void CanExecute_with_predicate_receives_parameter()
    {
        // Arrange
        var command = new AsyncDelegateCommand<int>(_ => Task.CompletedTask, v => v > 10);

        // Act & Assert
        Assert.False(command.CanExecute(5));
        Assert.True(command.CanExecute(15));
    }
}
