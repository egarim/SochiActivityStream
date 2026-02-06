using Sochi.Navigation.Commands;

namespace Sochi.Navigation.Tests.Commands;

public sealed class DelegateCommandTests
{
    [Fact]
    public void Execute_invokes_action()
    {
        // Arrange
        var executed = false;
        var command = new DelegateCommand(() => executed = true);

        // Act
        command.Execute(null);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void CanExecute_without_predicate_returns_true()
    {
        // Arrange
        var command = new DelegateCommand(() => { });

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
        var command = new DelegateCommand(() => { }, () => canExecute);

        // Act & Assert
        Assert.False(command.CanExecute(null));

        canExecute = true;
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void RaiseCanExecuteChanged_raises_event()
    {
        // Arrange
        var command = new DelegateCommand(() => { });
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
        Assert.Throws<ArgumentNullException>(() => new DelegateCommand(null!));
    }
}

public sealed class DelegateCommandTTests
{
    [Fact]
    public void Execute_invokes_action_with_parameter()
    {
        // Arrange
        int? receivedValue = null;
        var command = new DelegateCommand<int>(v => receivedValue = v);

        // Act
        command.Execute(42);

        // Assert
        Assert.Equal(42, receivedValue);
    }

    [Fact]
    public void CanExecute_with_predicate_receives_parameter()
    {
        // Arrange
        var command = new DelegateCommand<int>(_ => { }, v => v > 10);

        // Act & Assert
        Assert.False(command.CanExecute(5));
        Assert.True(command.CanExecute(15));
    }

    [Fact]
    public void CanExecute_with_null_and_value_type_returns_true_when_no_predicate()
    {
        // Arrange
        var command = new DelegateCommand<int>(_ => { });

        // Act
        var result = command.CanExecute(null);

        // Assert
        Assert.True(result);
    }
}
