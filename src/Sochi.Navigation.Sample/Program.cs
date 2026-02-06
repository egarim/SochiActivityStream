using Sochi.Navigation.Sample.Components;
using Sochi.Navigation.Extensions;
using Sochi.Navigation.Sample.Services;
using Sochi.Navigation.Sample.ViewModels;
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
builder.Services.AddViewModel<HomeViewModel>();
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
