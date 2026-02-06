# Sochi.Navigation - Blazor MVVM Navigation Library
## Complete Implementation Plan for Copilot LLM Coding Agent

---

## 📋 Project Overview

Create a complete MVVM navigation library for Blazor that provides Prism-like navigation patterns, including:
- ViewModel-first navigation
- Navigation parameters and lifecycle
- ICommand pattern support with async commands
- Dialog service
- Region navigation
- Sample application demonstrating all features

---

## 🏗️ Solution Structure

```
Sochi.Navigation/
├── src/
│   ├── Sochi.Navigation/                    # Core library
│   │   ├── Sochi.Navigation.csproj
│   │   ├── Commands/
│   │   │   ├── IAsyncCommand.cs
│   │   │   ├── DelegateCommand.cs
│   │   │   ├── DelegateCommand{T}.cs
│   │   │   ├── AsyncDelegateCommand.cs
│   │   │   └── AsyncDelegateCommand{T}.cs
│   │   ├── Navigation/
│   │   │   ├── INavigationService.cs
│   │   │   ├── NavigationService.cs
│   │   │   ├── INavigationParameters.cs
│   │   │   ├── NavigationParameters.cs
│   │   │   ├── INavigationAware.cs
│   │   │   ├── IInitialize.cs
│   │   │   ├── IConfirmNavigation.cs
│   │   │   ├── NavigationResult.cs
│   │   │   └── NavigationException.cs
│   │   ├── Regions/
│   │   │   ├── IRegionManager.cs
│   │   │   ├── RegionManager.cs
│   │   │   ├── IRegion.cs
│   │   │   └── Region.cs
│   │   ├── Dialogs/
│   │   │   ├── IDialogService.cs
│   │   │   ├── DialogService.cs
│   │   │   ├── IDialogAware.cs
│   │   │   ├── DialogParameters.cs
│   │   │   └── DialogResult.cs
│   │   ├── Mvvm/
│   │   │   ├── ViewModelBase.cs
│   │   │   ├── BindableBase.cs
│   │   │   └── MvvmComponentBase{T}.cs
│   │   ├── Components/
│   │   │   ├── CommandButton.razor
│   │   │   ├── RegionHost.razor
│   │   │   └── DialogHost.razor
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   └── Sochi.Navigation.Sample/             # Sample Blazor App
│       ├── Sochi.Navigation.Sample.csproj
│       ├── Program.cs
│       ├── App.razor
│       ├── _Imports.razor
│       ├── Pages/
│       │   ├── Index.razor
│       │   ├── Products/
│       │   │   ├── ProductList.razor
│       │   │   └── ProductDetail.razor
│       │   └── Customers/
│       │       ├── CustomerList.razor
│       │       └── CustomerDetail.razor
│       ├── ViewModels/
│       │   ├── Products/
│       │   │   ├── ProductListViewModel.cs
│       │   │   └── ProductDetailViewModel.cs
│       │   └── Customers/
│       │       ├── CustomerListViewModel.cs
│       │       └── CustomerDetailViewModel.cs
│       ├── Dialogs/
│       │   ├── ConfirmDialog.razor
│       │   └── ConfirmDialogViewModel.cs
│       ├── Services/
│       │   ├── IProductService.cs
│       │   ├── ProductService.cs
│       │   ├── ICustomerService.cs
│       │   └── CustomerService.cs
│       └── Models/
│           ├── Product.cs
│           └── Customer.cs
├── tests/
│   └── Sochi.Navigation.Tests/
│       ├── Sochi.Navigation.Tests.csproj
│       ├── Commands/
│       │   └── DelegateCommandTests.cs
│       └── Navigation/
│           └── NavigationServiceTests.cs
├── samples/
│   └── AdvancedSample/                      # Advanced scenarios
├── Sochi.Navigation.sln
└── README.md
```

---

## 📦 Step 1: Create Solution and Projects

### 1.1 Create Solution
```bash
dotnet new sln -n Sochi.Navigation
```

### 1.2 Create Class Library Project
```bash
dotnet new classlib -n Sochi.Navigation -f net8.0 -o src/Sochi.Navigation
dotnet sln add src/Sochi.Navigation/Sochi.Navigation.csproj
```

**Update Sochi.Navigation.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>Sochi.Navigation</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Company>Your Company</Company>
    <Description>MVVM Navigation library for Blazor with Prism-like patterns</Description>
    <PackageTags>blazor;mvvm;navigation;prism;commands</PackageTags>
    <RepositoryUrl>https://github.com/yourusername/Sochi.Navigation</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### 1.3 Create Sample Blazor Server App
```bash
dotnet new blazor -n Sochi.Navigation.Sample -o src/Sochi.Navigation.Sample
dotnet sln add src/Sochi.Navigation.Sample/Sochi.Navigation.Sample.csproj
dotnet add src/Sochi.Navigation.Sample/Sochi.Navigation.Sample.csproj reference src/Sochi.Navigation/Sochi.Navigation.csproj
```

### 1.4 Create Test Project
```bash
dotnet new xunit -n Sochi.Navigation.Tests -o tests/Sochi.Navigation.Tests
dotnet sln add tests/Sochi.Navigation.Tests/Sochi.Navigation.Tests.csproj
dotnet add tests/Sochi.Navigation.Tests/Sochi.Navigation.Tests.csproj reference src/Sochi.Navigation/Sochi.Navigation.csproj
```

**Add test packages:**
```bash
dotnet add tests/Sochi.Navigation.Tests package FluentAssertions
dotnet add tests/Sochi.Navigation.Tests package Moq
dotnet add tests/Sochi.Navigation.Tests package bunit
```

---

## 📝 Step 2: Implement Core Command Infrastructure

### 2.1 Create IAsyncCommand.cs
**Path:** `src/Sochi.Navigation/Commands/IAsyncCommand.cs`

```csharp
using System.Windows.Input;

namespace Sochi.Navigation.Commands;

/// <summary>
/// Extends ICommand to support asynchronous execution
/// </summary>
public interface IAsyncCommand : ICommand
{
    /// <summary>
    /// Executes the command asynchronously
    /// </summary>
    Task ExecuteAsync(object? parameter);

    /// <summary>
    /// Indicates whether the command is currently executing
    /// </summary>
    bool IsExecuting { get; }
}
```

### 2.2 Create DelegateCommand.cs
**Path:** `src/Sochi.Navigation/Commands/DelegateCommand.cs`

```csharp
using System.Windows.Input;

namespace Sochi.Navigation.Commands;

/// <summary>
/// Synchronous command without parameter
/// </summary>
public class DelegateCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public event EventHandler? CanExecuteChanged;

    public DelegateCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

### 2.3 Create DelegateCommand{T}.cs
**Path:** `src/Sochi.Navigation/Commands/DelegateCommand{T}.cs`

```csharp
using System.Windows.Input;

namespace Sochi.Navigation.Commands;

/// <summary>
/// Synchronous command with typed parameter
/// </summary>
public class DelegateCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public event EventHandler? CanExecuteChanged;

    public DelegateCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        if (parameter == null && typeof(T).IsValueType)
            return _canExecute == null;

        return _canExecute?.Invoke((T?)parameter) ?? true;
    }

    public void Execute(object? parameter) => _execute((T?)parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

### 2.4 Create AsyncDelegateCommand.cs
**Path:** `src/Sochi.Navigation/Commands/AsyncDelegateCommand.cs`

```csharp
namespace Sochi.Navigation.Commands;

/// <summary>
/// Asynchronous command without parameter
/// </summary>
public class AsyncDelegateCommand : IAsyncCommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public AsyncDelegateCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

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

    public bool CanExecute(object? parameter)
    {
        if (IsExecuting)
            return false;

        return _canExecute?.Invoke() ?? true;
    }

    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter);
    }

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

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

### 2.5 Create AsyncDelegateCommand{T}.cs
**Path:** `src/Sochi.Navigation/Commands/AsyncDelegateCommand{T}.cs`

```csharp
namespace Sochi.Navigation.Commands;

/// <summary>
/// Asynchronous command with typed parameter
/// </summary>
public class AsyncDelegateCommand<T> : IAsyncCommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public AsyncDelegateCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

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

    public bool CanExecute(object? parameter)
    {
        if (IsExecuting)
            return false;

        if (parameter == null && typeof(T).IsValueType)
            return _canExecute == null;

        return _canExecute?.Invoke((T?)parameter) ?? true;
    }

    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter);
    }

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

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

---

## 📝 Step 3: Implement Navigation Infrastructure

### 3.1 Create Navigation Interfaces

**Path:** `src/Sochi.Navigation/Navigation/INavigationParameters.cs`
```csharp
namespace Sochi.Navigation.Navigation;

/// <summary>
/// Represents parameters passed during navigation
/// </summary>
public interface INavigationParameters : IEnumerable<KeyValuePair<string, object>>
{
    void Add(string key, object value);
    bool ContainsKey(string key);
    T? GetValue<T>(string key);
    bool TryGetValue<T>(string key, out T? value);
    int Count { get; }
}
```

**Path:** `src/Sochi.Navigation/Navigation/INavigationAware.cs`
```csharp
namespace Sochi.Navigation.Navigation;

/// <summary>
/// Interface for ViewModels that need to be notified of navigation events
/// </summary>
public interface INavigationAware
{
    /// <summary>
    /// Called when navigating TO this ViewModel
    /// </summary>
    void OnNavigatedTo(INavigationParameters parameters);

    /// <summary>
    /// Called when navigating AWAY from this ViewModel
    /// </summary>
    void OnNavigatedFrom(INavigationParameters parameters);
}
```

**Path:** `src/Sochi.Navigation/Navigation/IInitialize.cs`
```csharp
namespace Sochi.Navigation.Navigation;

/// <summary>
/// Interface for ViewModels that need asynchronous initialization
/// </summary>
public interface IInitialize
{
    /// <summary>
    /// Initialize the ViewModel asynchronously with navigation parameters
    /// </summary>
    Task InitializeAsync(INavigationParameters parameters);
}
```

**Path:** `src/Sochi.Navigation/Navigation/IConfirmNavigation.cs`
```csharp
namespace Sochi.Navigation.Navigation;

/// <summary>
/// Interface for ViewModels that need to confirm before navigating away
/// </summary>
public interface IConfirmNavigation
{
    /// <summary>
    /// Determine if navigation can proceed
    /// </summary>
    Task<bool> CanNavigateAsync(INavigationParameters parameters);
}
```

**Path:** `src/Sochi.Navigation/Navigation/INavigationService.cs`
```csharp
namespace Sochi.Navigation.Navigation;

/// <summary>
/// Service for handling navigation between pages
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigate to the specified URI
    /// </summary>
    Task NavigateAsync(string uri);

    /// <summary>
    /// Navigate to the specified URI with parameters
    /// </summary>
    Task NavigateAsync(string uri, INavigationParameters parameters);

    /// <summary>
    /// Navigate to the specified URI with parameters and options
    /// </summary>
    Task<NavigationResult> NavigateAsync(string uri, INavigationParameters parameters, bool forceLoad);

    /// <summary>
    /// Navigate back to the previous page
    /// </summary>
    Task GoBackAsync();

    /// <summary>
    /// Gets the current route
    /// </summary>
    string CurrentRoute { get; }

    /// <summary>
    /// Gets a value indicating whether the navigation service can go back
    /// </summary>
    bool CanGoBack { get; }
}
```

### 3.2 Create NavigationParameters.cs
**Path:** `src/Sochi.Navigation/Navigation/NavigationParameters.cs`

```csharp
using Microsoft.AspNetCore.WebUtilities;
using System.Collections;

namespace Sochi.Navigation.Navigation;

/// <summary>
/// Implementation of navigation parameters
/// </summary>
public class NavigationParameters : INavigationParameters
{
    private readonly Dictionary<string, object> _parameters = new();

    public NavigationParameters() { }

    public NavigationParameters(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
            return;

        // Remove leading '?' if present
        if (queryString.StartsWith("?"))
            queryString = queryString.Substring(1);

        var parsed = QueryHelpers.ParseQuery(queryString);
        foreach (var kvp in parsed)
        {
            _parameters[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
        }
    }

    public void Add(string key, object value)
    {
        _parameters[key] = value;
    }

    public bool ContainsKey(string key)
    {
        return _parameters.ContainsKey(key);
    }

    public T? GetValue<T>(string key)
    {
        if (_parameters.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    public bool TryGetValue<T>(string key, out T? value)
    {
        if (_parameters.TryGetValue(key, out var objValue))
        {
            if (objValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            try
            {
                value = (T)Convert.ChangeType(objValue, typeof(T));
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    public int Count => _parameters.Count;

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return _parameters.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public string ToQueryString()
    {
        if (_parameters.Count == 0)
            return string.Empty;

        var queryParams = _parameters.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value?.ToString() ?? "")}");

        return "?" + string.Join("&", queryParams);
    }
}
```

### 3.3 Create NavigationResult.cs
**Path:** `src/Sochi.Navigation/Navigation/NavigationResult.cs`

```csharp
namespace Sochi.Navigation.Navigation;

/// <summary>
/// Represents the result of a navigation operation
/// </summary>
public class NavigationResult
{
    public bool Success { get; init; }
    public Exception? Exception { get; init; }

    public static NavigationResult CreateSuccess() => new() { Success = true };
    
    public static NavigationResult CreateFailure(Exception exception) => 
        new() { Success = false, Exception = exception };
}
```

### 3.4 Create NavigationException.cs
**Path:** `src/Sochi.Navigation/Navigation/NavigationException.cs`

```csharp
namespace Sochi.Navigation.Navigation;

/// <summary>
/// Exception thrown during navigation operations
/// </summary>
public class NavigationException : Exception
{
    public NavigationException(string message) : base(message) { }
    
    public NavigationException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

### 3.5 Create NavigationService.cs
**Path:** `src/Sochi.Navigation/Navigation/NavigationService.cs`

```csharp
using Microsoft.AspNetCore.Components;

namespace Sochi.Navigation.Navigation;

/// <summary>
/// Implementation of the navigation service
/// </summary>
public class NavigationService : INavigationService
{
    private readonly NavigationManager _navigationManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<string> _navigationStack = new();
    private object? _currentViewModel;

    public NavigationService(
        NavigationManager navigationManager,
        IServiceProvider serviceProvider)
    {
        _navigationManager = navigationManager;
        _serviceProvider = serviceProvider;
    }

    public string CurrentRoute => _navigationManager.Uri;

    public bool CanGoBack => _navigationStack.Count > 0;

    public async Task NavigateAsync(string uri)
    {
        await NavigateAsync(uri, new NavigationParameters());
    }

    public async Task NavigateAsync(string uri, INavigationParameters parameters)
    {
        await NavigateAsync(uri, parameters, false);
    }

    public async Task<NavigationResult> NavigateAsync(
        string uri, 
        INavigationParameters parameters, 
        bool forceLoad)
    {
        try
        {
            // Handle OnNavigatedFrom for current ViewModel
            if (_currentViewModel is INavigationAware currentNavAware)
            {
                currentNavAware.OnNavigatedFrom(parameters);
            }

            // Check if navigation can proceed
            if (_currentViewModel is IConfirmNavigation confirmNavigation)
            {
                var canNavigate = await confirmNavigation.CanNavigateAsync(parameters);
                if (!canNavigate)
                {
                    return NavigationResult.CreateFailure(
                        new NavigationException("Navigation cancelled by current ViewModel"));
                }
            }

            // Build URI with query string
            var fullUri = uri;
            if (parameters?.Count > 0 && parameters is NavigationParameters navParams)
            {
                fullUri += navParams.ToQueryString();
            }

            // Store current URI for back navigation
            if (!string.IsNullOrEmpty(_navigationManager.Uri))
            {
                _navigationStack.Push(_navigationManager.Uri);
            }

            // Perform navigation
            _navigationManager.NavigateTo(fullUri, forceLoad);

            return NavigationResult.CreateSuccess();
        }
        catch (Exception ex)
        {
            return NavigationResult.CreateFailure(ex);
        }
    }

    public Task GoBackAsync()
    {
        if (_navigationStack.Count > 0)
        {
            var previousUri = _navigationStack.Pop();
            _navigationManager.NavigateTo(previousUri);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Internal method to set the current ViewModel (called by MvvmComponentBase)
    /// </summary>
    internal void SetCurrentViewModel(object? viewModel)
    {
        _currentViewModel = viewModel;
    }
}
```

---

## 📝 Step 4: Implement MVVM Base Classes

### 4.1 Create BindableBase.cs
**Path:** `src/Sochi.Navigation/Mvvm/BindableBase.cs`

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Sochi.Navigation.Mvvm;

/// <summary>
/// Base class for bindable objects with property change notification
/// </summary>
public abstract class BindableBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual bool SetProperty<T>(
        ref T storage, 
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

### 4.2 Create ViewModelBase.cs
**Path:** `src/Sochi.Navigation/Mvvm/ViewModelBase.cs`

```csharp
using System.Windows.Input;
using Sochi.Navigation.Commands;

namespace Sochi.Navigation.Mvvm;

/// <summary>
/// Base class for ViewModels with common functionality
/// </summary>
public abstract class ViewModelBase : BindableBase
{
    private bool _isBusy;
    private string _title = string.Empty;
    private readonly List<ICommand> _commands = new();

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

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Register a command to be tracked for automatic CanExecuteChanged notifications
    /// </summary>
    protected void RegisterCommand(ICommand command)
    {
        if (command != null && !_commands.Contains(command))
        {
            _commands.Add(command);
        }
    }

    /// <summary>
    /// Raise CanExecuteChanged for all registered commands
    /// </summary>
    protected void RaiseCanExecuteChangedForAllCommands()
    {
        foreach (var command in _commands)
        {
            RaiseCanExecuteChanged(command);
        }
    }

    /// <summary>
    /// Raise CanExecuteChanged for a specific command
    /// </summary>
    protected void RaiseCanExecuteChanged(ICommand command)
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
    }
}
```

### 4.3 Create MvvmComponentBase{T}.razor
**Path:** `src/Sochi.Navigation/Mvvm/MvvmComponentBase{T}.razor`

```razor
@using System.ComponentModel
@using Sochi.Navigation.Navigation
@typeparam TViewModel where TViewModel : class, INotifyPropertyChanged
@inject TViewModel ViewModel
@inject NavigationManager NavigationManager
@inject INavigationService NavigationService
@implements IDisposable

@code {
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Subscribe to ViewModel property changes
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        // Set current ViewModel in NavigationService
        if (NavigationService is NavigationService navService)
        {
            navService.SetCurrentViewModel(ViewModel);
        }

        // Handle navigation awareness
        if (ViewModel is INavigationAware navigationAware)
        {
            var uri = new Uri(NavigationManager.Uri);
            var parameters = new NavigationParameters(uri.Query);
            navigationAware.OnNavigatedTo(parameters);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        // Handle async initialization
        if (ViewModel is IInitialize initialize)
        {
            var uri = new Uri(NavigationManager.Uri);
            var parameters = new NavigationParameters(uri.Query);
            await initialize.InitializeAsync(parameters);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    public virtual void Dispose()
    {
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }
}
```

---

## 📝 Step 5: Implement Blazor Components

### 5.1 Create CommandButton.razor
**Path:** `src/Sochi.Navigation/Components/CommandButton.razor`

```razor
@using System.Windows.Input
@using Sochi.Navigation.Commands
@implements IDisposable

<button type="button"
        class="@CssClass"
        disabled="@(!CanExecute)"
        @onclick="OnClickAsync"
        @attributes="AdditionalAttributes">
    @if (IsExecuting)
    {
        @if (LoadingContent != null)
        {
            @LoadingContent
        }
        else
        {
            <span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
            @LoadingText
        }
    }
    else
    {
        @ChildContent
    }
</button>

@code {
    [Parameter] public ICommand? Command { get; set; }
    [Parameter] public object? CommandParameter { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? LoadingContent { get; set; }
    [Parameter] public string LoadingText { get; set; } = "Processing...";
    [Parameter] public string CssClass { get; set; } = "btn btn-primary";
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private bool CanExecute => Command?.CanExecute(CommandParameter) ?? false;
    private bool IsExecuting => Command is IAsyncCommand asyncCmd && asyncCmd.IsExecuting;

    protected override void OnInitialized()
    {
        if (Command != null)
        {
            Command.CanExecuteChanged += OnCanExecuteChanged;
        }
    }

    private async Task OnClickAsync()
    {
        if (Command == null)
            return;

        if (Command is IAsyncCommand asyncCommand)
        {
            await asyncCommand.ExecuteAsync(CommandParameter);
        }
        else
        {
            Command.Execute(CommandParameter);
        }
    }

    private void OnCanExecuteChanged(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (Command != null)
        {
            Command.CanExecuteChanged -= OnCanExecuteChanged;
        }
    }
}
```

---

## 📝 Step 6: Implement Dialog Service

### 6.1 Create Dialog Interfaces

**Path:** `src/Sochi.Navigation/Dialogs/IDialogService.cs`
```csharp
namespace Sochi.Navigation.Dialogs;

/// <summary>
/// Service for displaying dialogs
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Show a dialog
    /// </summary>
    Task<IDialogResult> ShowDialogAsync(string dialogName, IDialogParameters? parameters = null);

    /// <summary>
    /// Show a confirmation dialog
    /// </summary>
    Task<bool> ShowConfirmationAsync(string title, string message);
}
```

**Path:** `src/Sochi.Navigation/Dialogs/IDialogAware.cs`
```csharp
namespace Sochi.Navigation.Dialogs;

/// <summary>
/// Interface for dialog ViewModels
/// </summary>
public interface IDialogAware
{
    /// <summary>
    /// Called when the dialog is opened
    /// </summary>
    void OnDialogOpened(IDialogParameters parameters);

    /// <summary>
    /// Determines if the dialog can be closed
    /// </summary>
    bool CanCloseDialog();

    /// <summary>
    /// Called when the dialog is closing
    /// </summary>
    void OnDialogClosed();

    /// <summary>
    /// Event raised to request dialog closure
    /// </summary>
    event Action<IDialogResult> RequestClose;
}
```

**Path:** `src/Sochi.Navigation/Dialogs/IDialogParameters.cs`
```csharp
namespace Sochi.Navigation.Dialogs;

/// <summary>
/// Parameters passed to dialogs
/// </summary>
public interface IDialogParameters
{
    void Add(string key, object value);
    T? GetValue<T>(string key);
    bool TryGetValue<T>(string key, out T? value);
}
```

**Path:** `src/Sochi.Navigation/Dialogs/IDialogResult.cs`
```csharp
namespace Sochi.Navigation.Dialogs;

/// <summary>
/// Result returned from a dialog
/// </summary>
public interface IDialogResult
{
    IDialogParameters Parameters { get; }
    bool? Result { get; }
}
```

### 6.2 Create Dialog Implementations

**Path:** `src/Sochi.Navigation/Dialogs/DialogParameters.cs`
```csharp
namespace Sochi.Navigation.Dialogs;

public class DialogParameters : IDialogParameters
{
    private readonly Dictionary<string, object> _parameters = new();

    public void Add(string key, object value)
    {
        _parameters[key] = value;
    }

    public T? GetValue<T>(string key)
    {
        if (_parameters.TryGetValue(key, out var value))
        {
            return (T?)Convert.ChangeType(value, typeof(T));
        }
        return default;
    }

    public bool TryGetValue<T>(string key, out T? value)
    {
        if (_parameters.TryGetValue(key, out var objValue))
        {
            value = (T?)Convert.ChangeType(objValue, typeof(T));
            return true;
        }
        value = default;
        return false;
    }
}
```

**Path:** `src/Sochi.Navigation/Dialogs/DialogResult.cs`
```csharp
namespace Sochi.Navigation.Dialogs;

public class DialogResult : IDialogResult
{
    public IDialogParameters Parameters { get; init; } = new DialogParameters();
    public bool? Result { get; init; }
}
```

**Path:** `src/Sochi.Navigation/Dialogs/DialogService.cs`
```csharp
namespace Sochi.Navigation.Dialogs;

public class DialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private TaskCompletionSource<IDialogResult>? _dialogTaskCompletionSource;
    
    public event Action<string, IDialogParameters?>? ShowDialogRequested;
    public event Action? HideDialogRequested;

    public DialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<IDialogResult> ShowDialogAsync(string dialogName, IDialogParameters? parameters = null)
    {
        _dialogTaskCompletionSource = new TaskCompletionSource<IDialogResult>();
        ShowDialogRequested?.Invoke(dialogName, parameters);
        return await _dialogTaskCompletionSource.Task;
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var parameters = new DialogParameters();
        parameters.Add("Title", title);
        parameters.Add("Message", message);

        var result = await ShowDialogAsync("ConfirmDialog", parameters);
        return result.Result ?? false;
    }

    internal void CloseDialog(IDialogResult result)
    {
        _dialogTaskCompletionSource?.SetResult(result);
        HideDialogRequested?.Invoke();
    }
}
```

---

## 📝 Step 7: Service Registration

### 7.1 Create ServiceCollectionExtensions.cs
**Path:** `src/Sochi.Navigation/Extensions/ServiceCollectionExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sochi.Navigation.Navigation;
using Sochi.Navigation.Dialogs;

namespace Sochi.Navigation.Extensions;

/// <summary>
/// Extension methods for registering Sochi.Navigation services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Sochi.Navigation services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddSochiNavigation(this IServiceCollection services)
    {
        services.AddScoped<INavigationService, NavigationService>();
        services.AddScoped<IDialogService, DialogService>();

        return services;
    }

    /// <summary>
    /// Register a ViewModel with scoped lifetime
    /// </summary>
    public static IServiceCollection AddViewModel<TViewModel>(this IServiceCollection services)
        where TViewModel : class
    {
        services.AddScoped<TViewModel>();
        return services;
    }

    /// <summary>
    /// Register multiple ViewModels at once
    /// </summary>
    public static IServiceCollection AddViewModels(
        this IServiceCollection services,
        params Type[] viewModelTypes)
    {
        foreach (var type in viewModelTypes)
        {
            services.AddScoped(type);
        }
        return services;
    }
}
```

---

## 📝 Step 8: Sample Application - Models

### 8.1 Create Product.cs
**Path:** `src/Sochi.Navigation.Sample/Models/Product.cs`

```csharp
namespace Sochi.Navigation.Sample.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
```

### 8.2 Create Customer.cs
**Path:** `src/Sochi.Navigation.Sample/Models/Customer.cs`

```csharp
namespace Sochi.Navigation.Sample.Models;

public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    
    public string FullName => $"{FirstName} {LastName}";
}
```

---

## 📝 Step 9: Sample Application - Services

### 9.1 Create IProductService.cs
**Path:** `src/Sochi.Navigation.Sample/Services/IProductService.cs`

```csharp
using Sochi.Navigation.Sample.Models;

namespace Sochi.Navigation.Sample.Services;

public interface IProductService
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task DeleteAsync(int id);
}
```

### 9.2 Create ProductService.cs
**Path:** `src/Sochi.Navigation.Sample/Services/ProductService.cs`

```csharp
using Sochi.Navigation.Sample.Models;

namespace Sochi.Navigation.Sample.Services;

public class ProductService : IProductService
{
    private readonly List<Product> _products = new()
    {
        new Product { Id = 1, Name = "Laptop", Category = "Electronics", Price = 999.99m, Stock = 10, Description = "High-performance laptop" },
        new Product { Id = 2, Name = "Mouse", Category = "Electronics", Price = 29.99m, Stock = 50, Description = "Wireless mouse" },
        new Product { Id = 3, Name = "Keyboard", Category = "Electronics", Price = 79.99m, Stock = 30, Description = "Mechanical keyboard" },
        new Product { Id = 4, Name = "Monitor", Category = "Electronics", Price = 299.99m, Stock = 15, Description = "27-inch 4K monitor" },
        new Product { Id = 5, Name = "Headphones", Category = "Electronics", Price = 149.99m, Stock = 25, Description = "Noise-cancelling headphones" },
    };

    public async Task<List<Product>> GetAllAsync()
    {
        await Task.Delay(500); // Simulate API call
        return _products.ToList();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        await Task.Delay(300);
        return _products.FirstOrDefault(p => p.Id == id);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        await Task.Delay(300);
        product.Id = _products.Max(p => p.Id) + 1;
        _products.Add(product);
        return product;
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        await Task.Delay(300);
        var existing = _products.FirstOrDefault(p => p.Id == product.Id);
        if (existing != null)
        {
            _products.Remove(existing);
            _products.Add(product);
        }
        return product;
    }

    public async Task DeleteAsync(int id)
    {
        await Task.Delay(300);
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product != null)
        {
            _products.Remove(product);
        }
    }
}
```

### 9.3 Create ICustomerService.cs and CustomerService.cs
**Path:** `src/Sochi.Navigation.Sample/Services/ICustomerService.cs`

```csharp
using Sochi.Navigation.Sample.Models;

namespace Sochi.Navigation.Sample.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
}
```

**Path:** `src/Sochi.Navigation.Sample/Services/CustomerService.cs`

```csharp
using Sochi.Navigation.Sample.Models;

namespace Sochi.Navigation.Sample.Services;

public class CustomerService : ICustomerService
{
    private readonly List<Customer> _customers = new()
    {
        new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com", Phone = "555-0001", Address = "123 Main St", City = "Phoenix" },
        new Customer { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", Phone = "555-0002", Address = "456 Oak Ave", City = "Tucson" },
        new Customer { Id = 3, FirstName = "Bob", LastName = "Johnson", Email = "bob@example.com", Phone = "555-0003", Address = "789 Pine Rd", City = "Mesa" },
    };

    public async Task<List<Customer>> GetAllAsync()
    {
        await Task.Delay(500);
        return _customers.ToList();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        await Task.Delay(300);
        return _customers.FirstOrDefault(c => c.Id == id);
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        await Task.Delay(300);
        customer.Id = _customers.Max(c => c.Id) + 1;
        _customers.Add(customer);
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer)
    {
        await Task.Delay(300);
        var existing = _customers.FirstOrDefault(c => c.Id == customer.Id);
        if (existing != null)
        {
            _customers.Remove(existing);
            _customers.Add(customer);
        }
        return customer;
    }

    public async Task DeleteAsync(int id)
    {
        await Task.Delay(300);
        var customer = _customers.FirstOrDefault(c => c.Id == id);
        if (customer != null)
        {
            _customers.Remove(customer);
        }
    }
}
```

---

## 📝 Step 10: Sample Application - ViewModels

### 10.1 Create ProductListViewModel.cs
**Path:** `src/Sochi.Navigation.Sample/ViewModels/Products/ProductListViewModel.cs`

```csharp
using System.Collections.ObjectModel;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using Sochi.Navigation.Dialogs;
using Sochi.Navigation.Sample.Models;
using Sochi.Navigation.Sample.Services;

namespace Sochi.Navigation.Sample.ViewModels.Products;

public class ProductListViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly IProductService _productService;
    private readonly IDialogService _dialogService;
    private ObservableCollection<Product> _products = new();
    private Product? _selectedProduct;
    private string _searchText = string.Empty;

    public ProductListViewModel(
        INavigationService navigationService,
        IProductService productService,
        IDialogService dialogService)
    {
        _navigationService = navigationService;
        _productService = productService;
        _dialogService = dialogService;

        Title = "Products";

        // Initialize commands
        LoadProductsCommand = new AsyncDelegateCommand(LoadProductsAsync);
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync, CanRefresh);
        NavigateToDetailCommand = new AsyncDelegateCommand<Product>(NavigateToDetailAsync, CanNavigateToDetail);
        DeleteProductCommand = new AsyncDelegateCommand<Product>(DeleteProductAsync, CanDeleteProduct);
        SearchCommand = new DelegateCommand(ExecuteSearch, CanSearch);

        // Register commands for automatic CanExecuteChanged
        RegisterCommand(RefreshCommand);
        RegisterCommand(NavigateToDetailCommand);
        RegisterCommand(DeleteProductCommand);
        RegisterCommand(SearchCommand);
    }

    #region Properties

    public ObservableCollection<Product> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                RaiseCanExecuteChanged(NavigateToDetailCommand);
                RaiseCanExecuteChanged(DeleteProductCommand);
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                RaiseCanExecuteChanged(SearchCommand);
            }
        }
    }

    #endregion

    #region Commands

    public IAsyncCommand LoadProductsCommand { get; }
    public IAsyncCommand RefreshCommand { get; }
    public IAsyncCommand NavigateToDetailCommand { get; }
    public IAsyncCommand DeleteProductCommand { get; }
    public ICommand SearchCommand { get; }

    #endregion

    #region IInitialize

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        await LoadProductsAsync();
    }

    #endregion

    #region Command Implementations

    private async Task LoadProductsAsync()
    {
        IsBusy = true;
        try
        {
            var products = await _productService.GetAllAsync();
            Products = new ObservableCollection<Product>(products);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        await LoadProductsAsync();
    }

    private bool CanRefresh() => !IsBusy;

    private async Task NavigateToDetailAsync(Product? product)
    {
        if (product == null) return;

        var parameters = new NavigationParameters
        {
            { "ProductId", product.Id }
        };

        await _navigationService.NavigateAsync("/product-detail", parameters);
    }

    private bool CanNavigateToDetail(Product? product) => !IsBusy && product != null;

    private async Task DeleteProductAsync(Product? product)
    {
        if (product == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Confirm Delete",
            $"Are you sure you want to delete {product.Name}?");

        if (!confirmed)
            return;

        IsBusy = true;
        try
        {
            await _productService.DeleteAsync(product.Id);
            Products.Remove(product);

            if (SelectedProduct == product)
                SelectedProduct = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanDeleteProduct(Product? product) => !IsBusy && product != null;

    private void ExecuteSearch()
    {
        // In a real app, this would filter the products
        LoadProductsCommand.ExecuteAsync(null);
    }

    private bool CanSearch() => !string.IsNullOrWhiteSpace(SearchText);

    #endregion
}
```

### 10.2 Create ProductDetailViewModel.cs
**Path:** `src/Sochi.Navigation.Sample/ViewModels/Products/ProductDetailViewModel.cs`

```csharp
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using Sochi.Navigation.Sample.Models;
using Sochi.Navigation.Sample.Services;

namespace Sochi.Navigation.Sample.ViewModels.Products;

public class ProductDetailViewModel : ViewModelBase, IInitialize, IConfirmNavigation
{
    private readonly INavigationService _navigationService;
    private readonly IProductService _productService;
    private Product? _product;
    private bool _hasUnsavedChanges;

    public ProductDetailViewModel(
        INavigationService navigationService,
        IProductService productService)
    {
        _navigationService = navigationService;
        _productService = productService;

        Title = "Product Details";

        GoBackCommand = new AsyncDelegateCommand(GoBackAsync);
        SaveCommand = new AsyncDelegateCommand(SaveAsync, CanSave);

        RegisterCommand(SaveCommand);
    }

    #region Properties

    public Product? Product
    {
        get => _product;
        set
        {
            if (SetProperty(ref _product, value))
            {
                Title = $"Product: {_product?.Name}";
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => SetProperty(ref _hasUnsavedChanges, value);
    }

    #endregion

    #region Commands

    public IAsyncCommand GoBackCommand { get; }
    public IAsyncCommand SaveCommand { get; }

    #endregion

    #region IInitialize

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        if (parameters.TryGetValue<int>("ProductId", out var productId))
        {
            IsBusy = true;
            try
            {
                Product = await _productService.GetByIdAsync(productId);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    #endregion

    #region IConfirmNavigation

    public async Task<bool> CanNavigateAsync(INavigationParameters parameters)
    {
        if (HasUnsavedChanges)
        {
            // In a real app, show a dialog here
            // For now, just return false to prevent navigation
            return false;
        }
        return true;
    }

    #endregion

    #region Command Implementations

    private async Task GoBackAsync()
    {
        await _navigationService.GoBackAsync();
    }

    private async Task SaveAsync()
    {
        if (Product == null) return;

        IsBusy = true;
        try
        {
            await _productService.UpdateAsync(Product);
            HasUnsavedChanges = false;
            await _navigationService.GoBackAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() => !IsBusy && Product != null && HasUnsavedChanges;

    #endregion
}
```

### 10.3 Create CustomerListViewModel.cs
**Path:** `src/Sochi.Navigation.Sample/ViewModels/Customers/CustomerListViewModel.cs`

```csharp
using System.Collections.ObjectModel;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using Sochi.Navigation.Sample.Models;
using Sochi.Navigation.Sample.Services;

namespace Sochi.Navigation.Sample.ViewModels.Customers;

public class CustomerListViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly ICustomerService _customerService;
    private ObservableCollection<Customer> _customers = new();

    public CustomerListViewModel(
        INavigationService navigationService,
        ICustomerService customerService)
    {
        _navigationService = navigationService;
        _customerService = customerService;

        Title = "Customers";

        LoadCustomersCommand = new AsyncDelegateCommand(LoadCustomersAsync);
        
        RegisterCommand(LoadCustomersCommand);
    }

    public ObservableCollection<Customer> Customers
    {
        get => _customers;
        set => SetProperty(ref _customers, value);
    }

    public IAsyncCommand LoadCustomersCommand { get; }

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        IsBusy = true;
        try
        {
            var customers = await _customerService.GetAllAsync();
            Customers = new ObservableCollection<Customer>(customers);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 10.4 Create CustomerDetailViewModel.cs
**Path:** `src/Sochi.Navigation.Sample/ViewModels/Customers/CustomerDetailViewModel.cs`

```csharp
using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using Sochi.Navigation.Sample.Models;
using Sochi.Navigation.Sample.Services;

namespace Sochi.Navigation.Sample.ViewModels.Customers;

public class CustomerDetailViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly ICustomerService _customerService;
    private Customer? _customer;

    public CustomerDetailViewModel(
        INavigationService navigationService,
        ICustomerService customerService)
    {
        _navigationService = navigationService;
        _customerService = customerService;

        Title = "Customer Details";

        GoBackCommand = new AsyncDelegateCommand(GoBackAsync);
        
        RegisterCommand(GoBackCommand);
    }

    public Customer? Customer
    {
        get => _customer;
        set
        {
            if (SetProperty(ref _customer, value))
            {
                Title = $"Customer: {_customer?.FullName}";
            }
        }
    }

    public IAsyncCommand GoBackCommand { get; }

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        if (parameters.TryGetValue<int>("CustomerId", out var customerId))
        {
            IsBusy = true;
            try
            {
                Customer = await _customerService.GetByIdAsync(customerId);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private async Task GoBackAsync()
    {
        await _navigationService.GoBackAsync();
    }
}
```

---

## 📝 Step 11: Sample Application - Pages

### 11.1 Create ProductList.razor
**Path:** `src/Sochi.Navigation.Sample/Pages/Products/ProductList.razor`

```razor
@page "/products"
@using Sochi.Navigation.Mvvm
@using Sochi.Navigation.Sample.ViewModels.Products
@using Sochi.Navigation.Components
@inherits MvvmComponentBase<ProductListViewModel>

<PageTitle>@ViewModel.Title</PageTitle>

<div class="container-fluid py-4">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h3>@ViewModel.Title</h3>
        <CommandButton Command="@ViewModel.RefreshCommand" CssClass="btn btn-secondary">
            <i class="bi bi-arrow-clockwise"></i> Refresh
        </CommandButton>
    </div>

    <div class="row mb-3">
        <div class="col-md-6">
            <div class="input-group">
                <input type="text" 
                       class="form-control" 
                       placeholder="Search products..."
                       @bind="ViewModel.SearchText"
                       @bind:event="oninput" />
                <CommandButton Command="@ViewModel.SearchCommand" CssClass="btn btn-primary">
                    <i class="bi bi-search"></i> Search
                </CommandButton>
            </div>
        </div>
    </div>

    @if (ViewModel.IsBusy)
    {
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="mt-2">Loading products...</p>
        </div>
    }
    else if (ViewModel.Products?.Any() == true)
    {
        <div class="table-responsive">
            <table class="table table-hover">
                <thead class="table-light">
                    <tr>
                        <th>Name</th>
                        <th>Category</th>
                        <th>Price</th>
                        <th>Stock</th>
                        <th>Status</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var product in ViewModel.Products)
                    {
                        <tr class="@(ViewModel.SelectedProduct == product ? "table-active" : "")"
                            @onclick="() => ViewModel.SelectedProduct = product"
                            style="cursor: pointer;">
                            <td>
                                <strong>@product.Name</strong>
                                <br />
                                <small class="text-muted">@product.Description</small>
                            </td>
                            <td>@product.Category</td>
                            <td>@product.Price.ToString("C")</td>
                            <td>
                                <span class="badge @(product.Stock > 20 ? "bg-success" : product.Stock > 5 ? "bg-warning" : "bg-danger")">
                                    @product.Stock units
                                </span>
                            </td>
                            <td>
                                @if (product.IsActive)
                                {
                                    <span class="badge bg-success">Active</span>
                                }
                                else
                                {
                                    <span class="badge bg-secondary">Inactive</span>
                                }
                            </td>
                            <td>
                                <div class="btn-group" role="group">
                                    <CommandButton Command="@ViewModel.NavigateToDetailCommand"
                                                  CommandParameter="@product"
                                                  CssClass="btn btn-sm btn-primary">
                                        <i class="bi bi-eye"></i> View
                                    </CommandButton>
                                    <CommandButton Command="@ViewModel.DeleteProductCommand"
                                                  CommandParameter="@product"
                                                  CssClass="btn btn-sm btn-danger"
                                                  LoadingText="Deleting...">
                                        <i class="bi bi-trash"></i> Delete
                                    </CommandButton>
                                </div>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
    else
    {
        <div class="alert alert-info">
            <i class="bi bi-info-circle"></i> No products found.
        </div>
    }
</div>
```

### 11.2 Create ProductDetail.razor
**Path:** `src/Sochi.Navigation.Sample/Pages/Products/ProductDetail.razor`

```razor
@page "/product-detail"
@using Sochi.Navigation.Mvvm
@using Sochi.Navigation.Sample.ViewModels.Products
@using Sochi.Navigation.Components
@inherits MvvmComponentBase<ProductDetailViewModel>

<PageTitle>@ViewModel.Title</PageTitle>

<div class="container py-4">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h3>@ViewModel.Title</h3>
        <CommandButton Command="@ViewModel.GoBackCommand" CssClass="btn btn-secondary">
            <i class="bi bi-arrow-left"></i> Back to List
        </CommandButton>
    </div>

    @if (ViewModel.IsBusy)
    {
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        </div>
    }
    else if (ViewModel.Product != null)
    {
        <div class="card">
            <div class="card-header">
                <h5>Product Information</h5>
            </div>
            <div class="card-body">
                <div class="row mb-3">
                    <div class="col-md-6">
                        <label class="form-label">Name</label>
                        <input type="text" class="form-control" @bind="ViewModel.Product.Name" 
                               @oninput="() => ViewModel.HasUnsavedChanges = true" />
                    </div>
                    <div class="col-md-6">
                        <label class="form-label">Category</label>
                        <input type="text" class="form-control" @bind="ViewModel.Product.Category"
                               @oninput="() => ViewModel.HasUnsavedChanges = true" />
                    </div>
                </div>

                <div class="row mb-3">
                    <div class="col-md-12">
                        <label class="form-label">Description</label>
                        <textarea class="form-control" rows="3" @bind="ViewModel.Product.Description"
                                  @oninput="() => ViewModel.HasUnsavedChanges = true"></textarea>
                    </div>
                </div>

                <div class="row mb-3">
                    <div class="col-md-4">
                        <label class="form-label">Price</label>
                        <input type="number" class="form-control" @bind="ViewModel.Product.Price"
                               @oninput="() => ViewModel.HasUnsavedChanges = true" step="0.01" />
                    </div>
                    <div class="col-md-4">
                        <label class="form-label">Stock</label>
                        <input type="number" class="form-control" @bind="ViewModel.Product.Stock"
                               @oninput="() => ViewModel.HasUnsavedChanges = true" />
                    </div>
                    <div class="col-md-4">
                        <div class="form-check mt-4">
                            <input class="form-check-input" type="checkbox" @bind="ViewModel.Product.IsActive"
                                   @onchange="() => ViewModel.HasUnsavedChanges = true" />
                            <label class="form-check-label">
                                Active
                            </label>
                        </div>
                    </div>
                </div>

                @if (ViewModel.HasUnsavedChanges)
                {
                    <div class="alert alert-warning">
                        <i class="bi bi-exclamation-triangle"></i> You have unsaved changes
                    </div>
                }

                <div class="d-flex gap-2">
                    <CommandButton Command="@ViewModel.SaveCommand" CssClass="btn btn-success">
                        <i class="bi bi-save"></i> Save Changes
                    </CommandButton>
                    <CommandButton Command="@ViewModel.GoBackCommand" CssClass="btn btn-secondary">
                        Cancel
                    </CommandButton>
                </div>
            </div>
            <div class="card-footer text-muted">
                Created: @ViewModel.Product.CreatedDate.ToString("g")
            </div>
        </div>
    }
    else
    {
        <div class="alert alert-danger">
            <i class="bi bi-exclamation-triangle"></i> Product not found
        </div>
    }
</div>
```

### 11.3 Create CustomerList.razor
**Path:** `src/Sochi.Navigation.Sample/Pages/Customers/CustomerList.razor`

```razor
@page "/customers"
@using Sochi.Navigation.Mvvm
@using Sochi.Navigation.Sample.ViewModels.Customers
@inherits MvvmComponentBase<CustomerListViewModel>

<PageTitle>@ViewModel.Title</PageTitle>

<div class="container-fluid py-4">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h3>@ViewModel.Title</h3>
    </div>

    @if (ViewModel.IsBusy)
    {
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="mt-2">Loading customers...</p>
        </div>
    }
    else if (ViewModel.Customers?.Any() == true)
    {
        <div class="table-responsive">
            <table class="table table-hover">
                <thead class="table-light">
                    <tr>
                        <th>Name</th>
                        <th>Email</th>
                        <th>Phone</th>
                        <th>City</th>
                        <th>Created</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var customer in ViewModel.Customers)
                    {
                        <tr>
                            <td><strong>@customer.FullName</strong></td>
                            <td>@customer.Email</td>
                            <td>@customer.Phone</td>
                            <td>@customer.City</td>
                            <td>@customer.CreatedDate.ToString("d")</td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
    else
    {
        <div class="alert alert-info">
            <i class="bi bi-info-circle"></i> No customers found.
        </div>
    }
</div>
```

---

## 📝 Step 12: Configure Sample Application

### 12.1 Update Program.cs
**Path:** `src/Sochi.Navigation.Sample/Program.cs`

```csharp
using Sochi.Navigation.Extensions;
using Sochi.Navigation.Sample.Services;
using Sochi.Navigation.Sample.ViewModels.Products;
using Sochi.Navigation.Sample.ViewModels.Customers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Sochi.Navigation
builder.Services.AddSochiNavigation();

// Add application services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

// Add ViewModels
builder.Services.AddViewModel<ProductListViewModel>();
builder.Services.AddViewModel<ProductDetailViewModel>();
builder.Services.AddViewModel<CustomerListViewModel>();
builder.Services.AddViewModel<CustomerDetailViewModel>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### 12.2 Update _Imports.razor
**Path:** `src/Sochi.Navigation.Sample/_Imports.razor`

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using Sochi.Navigation.Sample
@using Sochi.Navigation.Commands
@using Sochi.Navigation.Navigation
@using Sochi.Navigation.Mvvm
@using Sochi.Navigation.Components
```

### 12.3 Update Index.razor
**Path:** `src/Sochi.Navigation.Sample/Pages/Index.razor`

```razor
@page "/"

<PageTitle>Home</PageTitle>

<div class="container py-5">
    <div class="text-center mb-5">
        <h1 class="display-4">Welcome to Sochi.Navigation</h1>
        <p class="lead">A complete MVVM navigation library for Blazor</p>
    </div>

    <div class="row">
        <div class="col-md-6 mb-4">
            <div class="card h-100">
                <div class="card-body">
                    <h5 class="card-title">
                        <i class="bi bi-box"></i> Products
                    </h5>
                    <p class="card-text">
                        Explore our product catalog with full MVVM navigation patterns.
                    </p>
                    <a href="/products" class="btn btn-primary">
                        View Products <i class="bi bi-arrow-right"></i>
                    </a>
                </div>
            </div>
        </div>

        <div class="col-md-6 mb-4">
            <div class="card h-100">
                <div class="card-body">
                    <h5 class="card-title">
                        <i class="bi bi-people"></i> Customers
                    </h5>
                    <p class="card-text">
                        Manage customer information with navigation parameters and lifecycle.
                    </p>
                    <a href="/customers" class="btn btn-primary">
                        View Customers <i class="bi bi-arrow-right"></i>
                    </a>
                </div>
            </div>
        </div>
    </div>

    <div class="card mt-4">
        <div class="card-header">
            <h5>Features Demonstrated</h5>
        </div>
        <div class="card-body">
            <ul>
                <li><strong>ICommand Pattern:</strong> Sync and Async commands with CanExecute</li>
                <li><strong>Navigation Service:</strong> ViewModel-first navigation with parameters</li>
                <li><strong>Navigation Lifecycle:</strong> INavigationAware, IInitialize, IConfirmNavigation</li>
                <li><strong>CommandButton Component:</strong> Automatic loading states and button management</li>
                <li><strong>MVVM Base Classes:</strong> ViewModelBase with property tracking</li>
                <li><strong>Dialog Service:</strong> Modal dialogs with results</li>
            </ul>
        </div>
    </div>
</div>
```

---

## 📝 Step 13: Unit Tests

### 13.1 Create DelegateCommandTests.cs
**Path:** `tests/Sochi.Navigation.Tests/Commands/DelegateCommandTests.cs`

```csharp
using FluentAssertions;
using Sochi.Navigation.Commands;
using Xunit;

namespace Sochi.Navigation.Tests.Commands;

public class DelegateCommandTests
{
    [Fact]
    public void Execute_ShouldInvokeAction()
    {
        // Arrange
        var executed = false;
        var command = new DelegateCommand(() => executed = true);

        // Act
        command.Execute(null);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void CanExecute_WithoutPredicate_ShouldReturnTrue()
    {
        // Arrange
        var command = new DelegateCommand(() => { });

        // Act
        var result = command.CanExecute(null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanExecute_WithPredicate_ShouldReturnPredicateResult()
    {
        // Arrange
        var canExecute = false;
        var command = new DelegateCommand(() => { }, () => canExecute);

        // Act & Assert
        command.CanExecute(null).Should().BeFalse();

        canExecute = true;
        command.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RaiseCanExecuteChanged_ShouldRaiseEvent()
    {
        // Arrange
        var command = new DelegateCommand(() => { });
        var eventRaised = false;
        command.CanExecuteChanged += (s, e) => eventRaised = true;

        // Act
        command.RaiseCanExecuteChanged();

        // Assert
        eventRaised.Should().BeTrue();
    }
}

public class AsyncDelegateCommandTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldInvokeAction()
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
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task IsExecuting_ShouldBeTrue_WhileExecuting()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        var command = new AsyncDelegateCommand(async () => await tcs.Task);

        // Act
        var executeTask = command.ExecuteAsync(null);
        
        // Assert
        command.IsExecuting.Should().BeTrue();

        tcs.SetResult(true);
        await executeTask;

        command.IsExecuting.Should().BeFalse();
    }

    [Fact]
    public void CanExecute_WhileExecuting_ShouldReturnFalse()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        var command = new AsyncDelegateCommand(async () => await tcs.Task);

        // Act
        var executeTask = command.ExecuteAsync(null);

        // Assert
        command.CanExecute(null).Should().BeFalse();

        tcs.SetResult(true);
    }
}
```

### 13.2 Create NavigationServiceTests.cs
**Path:** `tests/Sochi.Navigation.Tests/Navigation/NavigationServiceTests.cs`

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sochi.Navigation.Navigation;
using Xunit;

namespace Sochi.Navigation.Tests.Navigation;

public class NavigationServiceTests
{
    [Fact]
    public async Task NavigateAsync_WithUri_ShouldNavigate()
    {
        // Arrange
        var services = new ServiceCollection();
        var navManagerMock = new Mock<NavigationManager>();
        services.AddSingleton(navManagerMock.Object);
        services.AddScoped<INavigationService, NavigationService>();

        var provider = services.BuildServiceProvider();
        var navigationService = provider.GetRequiredService<INavigationService>();

        // Act
        await navigationService.NavigateAsync("/test");

        // Assert
        navManagerMock.Verify(x => x.NavigateTo("/test", false), Times.Once);
    }

    [Fact]
    public async Task NavigateAsync_WithParameters_ShouldIncludeQueryString()
    {
        // Arrange
        var services = new ServiceCollection();
        var navManagerMock = new Mock<NavigationManager>();
        services.AddSingleton(navManagerMock.Object);
        services.AddScoped<INavigationService, NavigationService>();

        var provider = services.BuildServiceProvider();
        var navigationService = provider.GetRequiredService<INavigationService>();

        var parameters = new NavigationParameters();
        parameters.Add("id", 123);

        // Act
        await navigationService.NavigateAsync("/test", parameters);

        // Assert
        navManagerMock.Verify(
            x => x.NavigateTo(It.Is<string>(s => s.Contains("id=123")), false),
            Times.Once);
    }
}
```

---

## 📝 Step 14: Documentation

### 14.1 Create README.md
**Path:** `README.md`

```markdown
# Sochi.Navigation

A complete MVVM navigation library for Blazor with Prism-like patterns.

## Features

- ✅ **ICommand Pattern** - DelegateCommand and AsyncDelegateCommand with full CanExecute support
- ✅ **Navigation Service** - ViewModel-first navigation with parameters
- ✅ **Navigation Lifecycle** - INavigationAware, IInitialize, IConfirmNavigation interfaces
- ✅ **CommandButton Component** - Automatic loading states and disabled state management
- ✅ **MVVM Base Classes** - ViewModelBase with automatic command tracking
- ✅ **Dialog Service** - Modal dialogs with parameters and results
- ✅ **Back Navigation** - Built-in navigation stack support

## Installation

```bash
dotnet add package Sochi.Navigation
```

## Quick Start

### 1. Register Services

```csharp
// Program.cs
builder.Services.AddSochiNavigation();

// Register your ViewModels
builder.Services.AddViewModel<ProductListViewModel>();
builder.Services.AddViewModel<ProductDetailViewModel>();
```

### 2. Create a ViewModel

```csharp
public class ProductListViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    
    public ProductListViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        NavigateToDetailCommand = new AsyncDelegateCommand<Product>(NavigateToDetailAsync);
        RegisterCommand(NavigateToDetailCommand);
    }
    
    public IAsyncCommand NavigateToDetailCommand { get; }
    
    private async Task NavigateToDetailAsync(Product product)
    {
        var parameters = new NavigationParameters
        {
            { "ProductId", product.Id }
        };
        await _navigationService.NavigateAsync("/product-detail", parameters);
    }
    
    public async Task InitializeAsync(INavigationParameters parameters)
    {
        // Load data
    }
}
```

### 3. Create a Razor Component

```razor
@page "/products"
@inherits MvvmComponentBase<ProductListViewModel>

<CommandButton Command="@ViewModel.NavigateToDetailCommand" 
              CommandParameter="@product">
    View Details
</CommandButton>
```

## Documentation

See the [samples](src/Sochi.Navigation.Sample) for complete examples.

## License

MIT
```

---

## 📋 Step 15: Build and Test Checklist

### Build Steps
1. ✅ Build solution: `dotnet build`
2. ✅ Run tests: `dotnet test`
3. ✅ Run sample app: `dotnet run --project src/Sochi.Navigation.Sample`
4. ✅ Test navigation flow: Home → Products → Product Detail → Back
5. ✅ Test commands: Refresh, Delete, Save
6. ✅ Test loading states on all async commands
7. ✅ Pack NuGet: `dotnet pack src/Sochi.Navigation`

### Validation
- [ ] All commands execute properly
- [ ] CanExecute updates button states
- [ ] Navigation parameters pass correctly
- [ ] IInitialize loads data on navigation
- [ ] IConfirmNavigation blocks when hasUnsavedChanges
- [ ] Loading states show on async commands
- [ ] Back navigation works
- [ ] All tests pass

---

## 🎯 Summary

This plan creates a complete, production-ready MVVM navigation library for Blazor with:
- **Commands**: Full ICommand support (sync/async, typed/untyped)
- **Navigation**: Service-based navigation with parameters and lifecycle
- **Components**: CommandButton with automatic state management
- **Sample App**: Full working example demonstrating all features
- **Tests**: Unit tests for core functionality
- **Documentation**: README and inline XML docs

Total estimated files: **~45 files**
Total estimated lines of code: **~3,500 lines**

The library provides a familiar, Prism-like experience for Blazor developers while staying true to Blazor's component model.
