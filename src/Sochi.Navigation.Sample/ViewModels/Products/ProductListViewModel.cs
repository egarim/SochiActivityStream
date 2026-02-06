using System.Collections.ObjectModel;
using System.Windows.Input;
using Sochi.Navigation.Commands;
using Sochi.Navigation.Dialogs;
using Sochi.Navigation.Mvvm;
using Sochi.Navigation.Navigation;
using Sochi.Navigation.Sample.Models;
using Sochi.Navigation.Sample.Services;

namespace Sochi.Navigation.Sample.ViewModels.Products;

/// <summary>
/// ViewModel for the product list page.
/// </summary>
public sealed class ProductListViewModel : ViewModelBase, IInitialize
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
        RefreshCommand = new AsyncDelegateCommand(LoadProductsAsync, () => !IsBusy);
        NavigateToDetailCommand = new AsyncDelegateCommand<Product>(NavigateToDetailAsync, p => !IsBusy && p != null);
        DeleteProductCommand = new AsyncDelegateCommand<Product>(DeleteProductAsync, p => !IsBusy && p != null);

        // Register commands for automatic CanExecuteChanged notifications
        RegisterCommand(RefreshCommand);
        RegisterCommand(NavigateToDetailCommand);
        RegisterCommand(DeleteProductCommand);
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
                RaiseCanExecuteChangedForAllCommands();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    #endregion

    #region Commands

    public IAsyncCommand LoadProductsCommand { get; }
    public IAsyncCommand RefreshCommand { get; }
    public IAsyncCommand NavigateToDetailCommand { get; }
    public IAsyncCommand DeleteProductCommand { get; }

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

    private async Task NavigateToDetailAsync(Product? product)
    {
        if (product == null) return;

        var parameters = new NavigationParameters();
        parameters.Add("ProductId", product.Id);

        await _navigationService.NavigateAsync("/product-detail", parameters);
    }

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

    #endregion
}
