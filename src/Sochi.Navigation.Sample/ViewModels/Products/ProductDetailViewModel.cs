using Sochi.Navigation.Commands;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using Sochi.Navigation.Sample.Models;
using Sochi.Navigation.Sample.Services;

namespace Sochi.Navigation.Sample.ViewModels.Products;

/// <summary>
/// ViewModel for the product detail page.
/// </summary>
public sealed class ProductDetailViewModel : ViewModelBase, IInitialize, IConfirmNavigation
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
        SaveCommand = new AsyncDelegateCommand(SaveAsync, () => !IsBusy && HasUnsavedChanges);

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
                Title = _product != null ? $"Product: {_product.Name}" : "Product Details";
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
            {
                RaiseCanExecuteChangedForAllCommands();
            }
        }
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

    public Task<bool> CanNavigateAsync(INavigationParameters parameters)
    {
        // In a real app, you might show a dialog here
        return Task.FromResult(!HasUnsavedChanges);
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

    #endregion
}
