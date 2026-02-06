# Sochi.Navigation.Sample

Comprehensive sample application demonstrating all features of Sochi.Navigation.

## Overview

This sample application showcases the complete capabilities of the Sochi.Navigation MVVM library including:
- ViewModel-first navigation
- Command pattern with loading states
- Dialog service
- Navigation lifecycle hooks
- Parameter passing
- Navigation guards

## Running the Sample

```bash
cd src/Sochi.Navigation.Sample
dotnet run
```

Navigate to `https://localhost:5001` in your browser.

## Project Structure

```
Sochi.Navigation.Sample/
├── Components/          # Reusable Blazor components
├── Models/             # Data models
├── Services/           # Business services
├── ViewModels/         # MVVM ViewModels
├── Pages/              # Blazor pages/views
└── Program.cs          # Service configuration
```

## Features Demonstrated

### 1. Basic Navigation

**File:** `ViewModels/HomeViewModel.cs`

Demonstrates:
- Simple navigation
- Command pattern
- ViewModel registration

```csharp
public class HomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public HomeViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        NavigateCommand = new AsyncDelegateCommand<string>(NavigateAsync);
        RegisterCommand(NavigateCommand);
    }

    public IAsyncCommand NavigateCommand { get; }

    private async Task NavigateAsync(string route)
    {
        await _navigationService.NavigateAsync(route);
    }
}
```

### 2. Parameters and Initialization

**File:** `ViewModels/ProductDetailViewModel.cs`

Demonstrates:
- Receiving navigation parameters
- IInitialize interface
- Async initialization
- Data loading

```csharp
public class ProductDetailViewModel : ViewModelBase, IInitialize
{
    private readonly IProductService _productService;
    private Product? _product;

    public Product? Product
    {
        get => _product;
        set => SetProperty(ref _product, value);
    }

    public async Task InitializeAsync(INavigationParameters parameters)
    {
        if (parameters.TryGetValue("ProductId", out int productId))
        {
            Product = await _productService.GetByIdAsync(productId);
        }
    }
}
```

### 3. CommandButton with Loading States

**File:** `Pages/Products.razor`

Demonstrates:
- CommandButton component
- Automatic loading states
- Parameter binding
- CSS customization

```razor
<CommandButton Command="@ViewModel.SaveCommand"
              CssClass="btn btn-primary"
              LoadingText="Saving..."
              DisabledCssClass="btn-disabled">
    Save Product
</CommandButton>

<CommandButton Command="@ViewModel.DeleteCommand"
              CommandParameter="@product.Id"
              CssClass="btn btn-danger">
    Delete
</CommandButton>
```

### 4. Dialog Service

**File:** `ViewModels/CustomerListViewModel.cs`

Demonstrates:
- Confirmation dialogs
- Custom dialogs
- Dialog parameters
- Dialog results

```csharp
private async Task DeleteAsync(Customer customer)
{
    var result = await _dialogService.ShowConfirmAsync(
        $"Delete customer '{customer.Name}'?");
    
    if (result.IsConfirmed)
    {
        await _customerService.DeleteAsync(customer.Id);
    }
}

private async Task EditAsync(Customer customer)
{
    var parameters = new DialogParameters
    {
        { "CustomerId", customer.Id }
    };
    
    var result = await _dialogService.ShowDialogAsync(
        "EditCustomerDialog", parameters);
    
    if (result.IsSuccess)
    {
        await RefreshAsync();
    }
}
```

### 5. Navigation Lifecycle

**File:** `ViewModels/EditViewModel.cs`

Demonstrates:
- INavigationAware interface
- OnNavigatedTo / OnNavigatedFrom
- State management
- Cleanup

```csharp
public class EditViewModel : ViewModelBase, INavigationAware
{
    public void OnNavigatedTo(INavigationParameters parameters)
    {
        // Initialize UI state
        StartTimer();
    }

    public void OnNavigatedFrom(INavigationParameters parameters)
    {
        // Cleanup
        StopTimer();
        SaveDraft();
    }
}
```

### 6. Navigation Guards

**File:** `ViewModels/FormViewModel.cs`

Demonstrates:
- IConfirmNavigation interface
- Preventing navigation
- Unsaved changes warning
- User confirmation

```csharp
public class FormViewModel : ViewModelBase, IConfirmNavigation
{
    private bool _isDirty;

    public async Task<bool> CanNavigateAsync(INavigationParameters parameters)
    {
        if (_isDirty)
        {
            var result = await _dialogService.ShowConfirmAsync(
                "You have unsaved changes. Discard them?");
            return result.IsConfirmed;
        }
        return true;
    }
}
```

### 7. Master-Detail Navigation

**File:** `ViewModels/CustomerListViewModel.cs` + `CustomerDetailViewModel.cs`

Demonstrates:
- List-to-detail navigation
- Passing complex objects
- Back navigation
- Parameter validation

```csharp
// In CustomerListViewModel
private async Task ViewCustomerAsync(Customer customer)
{
    var parameters = new NavigationParameters
    {
        { "CustomerId", customer.Id },
        { "Mode", "View" }
    };
    await _navigationService.NavigateAsync("/customer-detail", parameters);
}

// In CustomerDetailViewModel
public async Task InitializeAsync(INavigationParameters parameters)
{
    var id = parameters.GetValue<int>("CustomerId");
    var mode = parameters.GetValue<string>("Mode");
    
    _customer = await _customerService.GetByIdAsync(id);
    _isReadOnly = mode == "View";
}
```

### 8. Complex Commands

**File:** `ViewModels/OrderViewModel.cs`

Demonstrates:
- Commands with CanExecute
- Dynamic command enablement
- Composite operations
- Error handling

```csharp
public class OrderViewModel : ViewModelBase
{
    private decimal _total;
    private bool _isProcessing;

    public OrderViewModel()
    {
        SubmitCommand = new AsyncDelegateCommand(
            SubmitAsync, 
            CanSubmit);
        RegisterCommand(SubmitCommand);
    }

    public decimal Total
    {
        get => _total;
        set
        {
            if (SetProperty(ref _total, value))
            {
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public IAsyncCommand SubmitCommand { get; }

    private bool CanSubmit() => Total > 0 && !_isProcessing;

    private async Task SubmitAsync()
    {
        _isProcessing = true;
        SubmitCommand.RaiseCanExecuteChanged();

        try
        {
            await _orderService.SubmitOrderAsync(Total);
            await _navigationService.NavigateAsync("/order-confirmation");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync(ex.Message);
        }
        finally
        {
            _isProcessing = false;
            SubmitCommand.RaiseCanExecuteChanged();
        }
    }
}
```

## Sample Pages

### Home Page
- Landing page with navigation links
- Demonstrates basic navigation

### Product List
- Display list of products
- Navigate to product details
- Add/Edit/Delete operations

### Product Detail
- Display product details
- Edit mode with form validation
- Save/Cancel with confirmation

### Customer Management
- Master-detail navigation
- CRUD operations
- Dialog integration

### Settings
- Form with unsaved changes guard
- Navigation confirmation
- State persistence

## Models

### Product
```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}
```

### Customer
```csharp
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
}
```

## Services

### IProductService
```csharp
public interface IProductService
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
}
```

### ICustomerService
```csharp
public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer> CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
}
```

## Configuration

### Program.cs

```csharp
using Sochi.Navigation.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Sochi Navigation
builder.Services.AddSochiNavigation();

// Register ViewModels
builder.Services.AddViewModel<HomeViewModel>();
builder.Services.AddViewModel<ProductListViewModel>();
builder.Services.AddViewModel<ProductDetailViewModel>();
builder.Services.AddViewModel<CustomerListViewModel>();
builder.Services.AddViewModel<CustomerDetailViewModel>();

// Register Services
builder.Services.AddSingleton<IProductService, ProductService>();
builder.Services.AddSingleton<ICustomerService, CustomerService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

## Key Takeaways

1. **ViewModel-First** - ViewModels drive navigation, not URLs
2. **Commands** - Use ICommand pattern for all user actions
3. **Lifecycle** - Leverage IInitialize and INavigationAware
4. **Parameters** - Pass strongly-typed parameters
5. **Guards** - Implement IConfirmNavigation to prevent data loss
6. **Dialogs** - Use dialog service for confirmations and forms
7. **Loading States** - CommandButton handles loading automatically

## Next Steps

1. Explore each ViewModel to see patterns
2. Run the application and navigate between pages
3. Try editing forms and canceling to see navigation guards
4. Check the browser console for lifecycle events
5. Modify the sample to experiment with features

## See Also

- [Sochi.Navigation](../Sochi.Navigation/README.md) - Main library documentation
- [Main README](../../README.md) - All libraries overview
