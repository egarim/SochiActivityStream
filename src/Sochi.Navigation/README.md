# Sochi.Navigation

A Prism-like MVVM navigation library for Blazor with full command pattern support.

## Overview

Sochi.Navigation brings the power of MVVM patterns to Blazor applications with ViewModel-first navigation, lifecycle hooks, command pattern support, and automatic loading states.

**NuGet:** `Sochi.Navigation`  
**Dependencies:** Blazor, Microsoft.Extensions.DependencyInjection

## Installation

```bash
dotnet add package Sochi.Navigation
```

## Key Features

- **ICommand Pattern** - Sync/async commands with CanExecute support
- **ViewModel-first Navigation** - Navigate with parameters and lifecycle hooks
- **CommandButton Component** - Automatic loading states and button management
- **Dialog Service** - Modal dialogs with parameters and results
- **MVVM Base Classes** - ViewModelBase with automatic property tracking
- **Navigation Lifecycle** - INavigationAware, IInitialize, IConfirmNavigation

## Quick Start

### 1. Setup Services

```csharp
// Program.cs
using Sochi.Navigation.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Sochi Navigation
builder.Services.AddSochiNavigation();

// Register ViewModels
builder.Services.AddViewModel<ProductListViewModel>();
builder.Services.AddViewModel<ProductDetailViewModel>();

var app = builder.Build();
// ...
```

### 2. Create a ViewModel

```csharp
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Navigation;

public class ProductListViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly IProductService _productService;
    private List<Product> _products = new();

    public ProductListViewModel(
        INavigationService navigationService,
        IProductService productService)
    {
        _navigationService = navigationService;
        _productService = productService;
        
        ViewProductCommand = new AsyncDelegateCommand<Product>(ViewProductAsync);
        RegisterCommand(ViewProductCommand);
    }

    public List<Product> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    public IAsyncCommand ViewProductCommand { get; }

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        Products = await _productService.GetAllAsync();
    }

    private async Task ViewProductAsync(Product product)
    {
        var parameters = new NavigationParameters
        {
            { "ProductId", product.Id }
        };
        await _navigationService.NavigateAsync("/product-detail", parameters);
    }
}
```

### 3. Create a Razor Component

```razor
@page "/products"
@using Sochi.Navigation.Mvvm
@using Sochi.Navigation.Components
@inherits MvvmComponentBase<ProductListViewModel>

<h3>Products</h3>

@if (ViewModel.Products.Any())
{
    <div class="product-list">
        @foreach (var product in ViewModel.Products)
        {
            <div class="product-card">
                <h4>@product.Name</h4>
                <p>@product.Price.ToString("C")</p>
                <CommandButton Command="@ViewModel.ViewProductCommand" 
                             CommandParameter="@product"
                             CssClass="btn btn-primary">
                    View Details
                </CommandButton>
            </div>
        }
    </div>
}
else
{
    <p>Loading products...</p>
}
```

## Namespaces

### Sochi.Navigation.Commands

ICommand pattern implementations for Blazor:

**Interfaces:**
- `IAsyncCommand` - Async command interface
- `IAsyncCommand<T>` - Async command with parameter

**Classes:**
- `DelegateCommand` - Synchronous command
- `DelegateCommand<T>` - Synchronous command with parameter
- `AsyncDelegateCommand` - Asynchronous command
- `AsyncDelegateCommand<T>` - Asynchronous command with parameter

**Example:**
```csharp
public class MyViewModel : ViewModelBase
{
    public MyViewModel()
    {
        SaveCommand = new AsyncDelegateCommand(SaveAsync, CanSave);
        DeleteCommand = new AsyncDelegateCommand<int>(DeleteAsync);
        
        RegisterCommand(SaveCommand);
        RegisterCommand(DeleteCommand);
    }

    public IAsyncCommand SaveCommand { get; }
    public IAsyncCommand DeleteCommand { get; }

    private bool CanSave() => !string.IsNullOrEmpty(Name);

    private async Task SaveAsync()
    {
        await _service.SaveAsync(Name);
    }

    private async Task DeleteAsync(int id)
    {
        await _service.DeleteAsync(id);
    }
}
```

### Sochi.Navigation.Navigation

ViewModel-first navigation with lifecycle hooks:

**Interfaces:**
- `INavigationService` - Navigate between pages
- `INavigationParameters` - Pass parameters during navigation
- `INavigationAware` - Lifecycle hooks (OnNavigatedFrom, OnNavigatedTo)
- `IInitialize` - Async initialization
- `IConfirmNavigation` - Confirm before leaving

**Classes:**
- `NavigationService` - Main navigation implementation
- `NavigationParameters` - Parameter dictionary
- `NavigationResult` - Result of navigation

**Example:**
```csharp
public class ProductDetailViewModel : ViewModelBase, 
    IInitialize, INavigationAware, IConfirmNavigation
{
    private int _productId;
    private Product? _product;
    private bool _isDirty;

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        if (parameters.TryGetValue("ProductId", out int productId))
        {
            _productId = productId;
            _product = await _productService.GetByIdAsync(productId);
        }
    }

    public void OnNavigatedTo(INavigationParameters parameters)
    {
        // Called when navigated to this page
    }

    public void OnNavigatedFrom(INavigationParameters parameters)
    {
        // Called when navigating away
    }

    public async Task<bool> CanNavigateAsync(INavigationParameters parameters)
    {
        if (_isDirty)
        {
            var result = await _dialogService.ShowConfirmAsync(
                "You have unsaved changes. Continue?");
            return result.IsConfirmed;
        }
        return true;
    }
}
```

### Sochi.Navigation.Mvvm

Base classes for MVVM pattern:

**Classes:**
- `BindableBase` - Property change notification
- `ViewModelBase` - Base class with command tracking
- `MvvmComponentBase<TViewModel>` - Base Razor component

**Example:**
```csharp
public class MyViewModel : ViewModelBase
{
    private string _name = "";
    private int _age;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public int Age
    {
        get => _age;
        set => SetProperty(ref _age, value);
    }
}
```

### Sochi.Navigation.Components

Blazor components for MVVM:

**Components:**
- `CommandButton` - Button with automatic loading states

**Example:**
```razor
<CommandButton Command="@ViewModel.SaveCommand"
              CssClass="btn btn-primary"
              LoadingText="Saving..."
              DisabledCssClass="btn-disabled">
    Save
</CommandButton>

<CommandButton Command="@ViewModel.DeleteCommand"
              CommandParameter="@productId"
              CssClass="btn btn-danger">
    Delete
</CommandButton>
```

**Features:**
- Automatic loading state management
- CanExecute binding
- Custom loading text
- CSS class customization
- Parameter support

### Sochi.Navigation.Dialogs

Modal dialog service:

**Interfaces:**
- `IDialogService` - Show dialogs
- `IDialogParameters` - Dialog parameters
- `IDialogResult` - Dialog result
- `IDialogAware` - Dialog lifecycle

**Classes:**
- `DialogService` - Main dialog implementation
- `DialogParameters` - Parameter dictionary
- `DialogResult` - Result of dialog

**Example:**
```csharp
// Show confirmation dialog
var result = await _dialogService.ShowConfirmAsync("Delete this item?");
if (result.IsConfirmed)
{
    await DeleteAsync();
}

// Show custom dialog
var parameters = new DialogParameters
{
    { "ProductId", productId }
};
var result = await _dialogService.ShowDialogAsync("EditProductDialog", parameters);
if (result.IsSuccess)
{
    var updatedProduct = result.GetValue<Product>("Product");
}
```

## Complete Example

### ViewModel

```csharp
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Navigation;
using Sochi.Navigation.Dialogs;

public class CustomerListViewModel : ViewModelBase, IInitialize
{
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ICustomerService _customerService;
    private List<Customer> _customers = new();
    private Customer? _selectedCustomer;

    public CustomerListViewModel(
        INavigationService navigationService,
        IDialogService dialogService,
        ICustomerService customerService)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;
        _customerService = customerService;

        AddCommand = new AsyncDelegateCommand(AddCustomerAsync);
        EditCommand = new AsyncDelegateCommand<Customer>(EditCustomerAsync);
        DeleteCommand = new AsyncDelegateCommand<Customer>(DeleteCustomerAsync);
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync);

        RegisterCommand(AddCommand);
        RegisterCommand(EditCommand);
        RegisterCommand(DeleteCommand);
        RegisterCommand(RefreshCommand);
    }

    public List<Customer> Customers
    {
        get => _customers;
        set => SetProperty(ref _customers, value);
    }

    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    public IAsyncCommand AddCommand { get; }
    public IAsyncCommand EditCommand { get; }
    public IAsyncCommand DeleteCommand { get; }
    public IAsyncCommand RefreshCommand { get; }

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        await LoadCustomersAsync();
    }

    private async Task AddCustomerAsync()
    {
        await _navigationService.NavigateAsync("/customer-detail");
    }

    private async Task EditCustomerAsync(Customer customer)
    {
        var parameters = new NavigationParameters
        {
            { "CustomerId", customer.Id }
        };
        await _navigationService.NavigateAsync("/customer-detail", parameters);
    }

    private async Task DeleteCustomerAsync(Customer customer)
    {
        var result = await _dialogService.ShowConfirmAsync(
            $"Delete customer '{customer.Name}'?");
        
        if (result.IsConfirmed)
        {
            await _customerService.DeleteAsync(customer.Id);
            await LoadCustomersAsync();
        }
    }

    private async Task RefreshAsync()
    {
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        Customers = await _customerService.GetAllAsync();
    }
}
```

### Razor Component

```razor
@page "/customers"
@using Sochi.Navigation.Mvvm
@using Sochi.Navigation.Components
@inherits MvvmComponentBase<CustomerListViewModel>

<div class="customer-list-page">
    <div class="header">
        <h3>Customers</h3>
        <div class="actions">
            <CommandButton Command="@ViewModel.AddCommand" 
                         CssClass="btn btn-primary">
                Add Customer
            </CommandButton>
            <CommandButton Command="@ViewModel.RefreshCommand"
                         CssClass="btn btn-secondary">
                Refresh
            </CommandButton>
        </div>
    </div>

    @if (ViewModel.Customers.Any())
    {
        <table class="table">
            <thead>
                <tr>
                    <th>Name</th>
                    <th>Email</th>
                    <th>Phone</th>
                    <th>Actions</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var customer in ViewModel.Customers)
                {
                    <tr>
                        <td>@customer.Name</td>
                        <td>@customer.Email</td>
                        <td>@customer.Phone</td>
                        <td>
                            <CommandButton Command="@ViewModel.EditCommand"
                                         CommandParameter="@customer"
                                         CssClass="btn btn-sm btn-primary">
                                Edit
                            </CommandButton>
                            <CommandButton Command="@ViewModel.DeleteCommand"
                                         CommandParameter="@customer"
                                         CssClass="btn btn-sm btn-danger">
                                Delete
                            </CommandButton>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    }
    else
    {
        <p>No customers found.</p>
    }
</div>

@code {
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }
}
```

## Advanced Features

### Navigation Parameters

```csharp
// Navigate with parameters
var parameters = new NavigationParameters
{
    { "Id", 123 },
    { "Mode", "Edit" },
    { "Data", complexObject }
};
await _navigationService.NavigateAsync("/page", parameters);

// Receive parameters
public async Task InitializeAsync(INavigationParameters parameters)
{
    var id = parameters.GetValue<int>("Id");
    var mode = parameters.GetValue<string>("Mode");
    var data = parameters.GetValue<MyType>("Data");
}
```

### Confirm Navigation

```csharp
public class EditViewModel : ViewModelBase, IConfirmNavigation
{
    private bool _hasUnsavedChanges;

    public async Task<bool> CanNavigateAsync(INavigationParameters parameters)
    {
        if (_hasUnsavedChanges)
        {
            var result = await _dialogService.ShowConfirmAsync(
                "You have unsaved changes. Discard them?");
            return result.IsConfirmed;
        }
        return true;
    }
}
```

### Custom Commands

```csharp
public class SaveCommand : AsyncDelegateCommand
{
    private readonly IDataService _dataService;

    public SaveCommand(IDataService dataService)
        : base(() => ExecuteAsync(dataService), () => CanExecute(dataService))
    {
        _dataService = dataService;
    }

    private static async Task ExecuteAsync(IDataService dataService)
    {
        await dataService.SaveAsync();
    }

    private static bool CanExecute(IDataService dataService)
    {
        return dataService.HasChanges;
    }
}
```

## Best Practices

1. **Use ViewModelBase** - Inherit from ViewModelBase for automatic property change notification
2. **Register Commands** - Always call RegisterCommand() for commands to update UI automatically
3. **Use IInitialize** - Prefer IInitialize over OnInitializedAsync for async initialization
4. **CommandButton** - Use CommandButton instead of regular buttons for automatic loading states
5. **Navigation Parameters** - Use strongly-typed parameters with GetValue<T>
6. **Lifecycle Hooks** - Implement INavigationAware for cleanup and state management
7. **Confirm Navigation** - Implement IConfirmNavigation to prevent accidental data loss

## Testing

```csharp
public class CustomerListViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsCustomers()
    {
        // Arrange
        var mockNavigation = new Mock<INavigationService>();
        var mockDialog = new Mock<IDialogService>();
        var mockCustomerService = new Mock<ICustomerService>();
        mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(new List<Customer> 
            { 
                new Customer { Id = 1, Name = "Alice" } 
            });

        var viewModel = new CustomerListViewModel(
            mockNavigation.Object,
            mockDialog.Object,
            mockCustomerService.Object);

        // Act
        await viewModel.InitializeAsync(new NavigationParameters());

        // Assert
        Assert.Single(viewModel.Customers);
        Assert.Equal("Alice", viewModel.Customers[0].Name);
    }

    [Fact]
    public async Task DeleteCommand_ShowsConfirmation()
    {
        // Arrange
        var mockNavigation = new Mock<INavigationService>();
        var mockDialog = new Mock<IDialogService>();
        mockDialog
            .Setup(d => d.ShowConfirmAsync(It.IsAny<string>()))
            .ReturnsAsync(DialogResult.Confirmed());
        
        var mockCustomerService = new Mock<ICustomerService>();
        var viewModel = new CustomerListViewModel(
            mockNavigation.Object,
            mockDialog.Object,
            mockCustomerService.Object);

        var customer = new Customer { Id = 1, Name = "Alice" };

        // Act
        await viewModel.DeleteCommand.ExecuteAsync(customer);

        // Assert
        mockDialog.Verify(d => d.ShowConfirmAsync(
            It.Is<string>(s => s.Contains("Alice"))), Times.Once);
        mockCustomerService.Verify(s => s.DeleteAsync(1), Times.Once);
    }
}
```

## See Also

- [Sochi.Navigation.Sample](../Sochi.Navigation.Sample/README.md) - Complete sample application
- [Main README](../../README.md) - All libraries overview
