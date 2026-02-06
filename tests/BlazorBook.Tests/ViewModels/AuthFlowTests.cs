using NUnit.Framework;
using Identity.Abstractions;
using SocialKit.Components.ViewModels;

namespace BlazorBook.Tests.ViewModels;

[TestFixture]
public class AuthFlowTests
{
    private TestFixture _fixture = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new TestFixture();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SIGN UP TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void SignUpViewModel_Title_ShouldBeCreateAccount()
    {
        var vm = _fixture.GetViewModel<SignUpViewModel>();
        Assert.That(vm.Title, Is.EqualTo("Create Account"));
    }

    [Test]
    public void SignUpCommand_WhenFieldsEmpty_CannotExecute()
    {
        var vm = _fixture.GetViewModel<SignUpViewModel>();
        
        Assert.That(vm.SignUpCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void SignUpCommand_WhenPasswordTooShort_CannotExecute()
    {
        var vm = _fixture.GetViewModel<SignUpViewModel>();
        vm.DisplayName = "Test User";
        vm.Handle = "testuser";
        vm.Email = "test@example.com";
        vm.Password = "12345"; // Only 5 chars, needs 6
        
        Assert.That(vm.SignUpCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void SignUpCommand_WhenAllFieldsValid_CanExecute()
    {
        var vm = _fixture.GetViewModel<SignUpViewModel>();
        vm.DisplayName = "Test User";
        vm.Handle = "testuser";
        vm.Email = "test@example.com";
        vm.Password = "password123";
        
        Assert.That(vm.SignUpCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task SignUpCommand_ShouldCreateUserAndSignIn()
    {
        var vm = _fixture.GetViewModel<SignUpViewModel>();
        vm.DisplayName = "Alice Smith";
        vm.Handle = "alice";
        vm.Email = "alice@example.com";
        vm.Password = "password123";
        
        await vm.SignUpCommand.ExecuteAsync(null);
        
        // User should be signed in
        Assert.That(_fixture.CurrentUser.IsAuthenticated, Is.True);
        Assert.That(_fixture.CurrentUser.DisplayName, Is.EqualTo("Alice Smith"));
        Assert.That(_fixture.CurrentUser.Handle, Is.EqualTo("alice"));
        
        // Should navigate to home
        Assert.That(_fixture.Navigation.LastNavigatedUri, Is.EqualTo("/"));
    }

    [Test]
    public void DisplayName_WhenSet_ShouldAutoGenerateHandle()
    {
        var vm = _fixture.GetViewModel<SignUpViewModel>();
        
        vm.DisplayName = "John Doe";
        
        Assert.That(vm.Handle, Is.EqualTo("johndoe"));
    }

    [Test]
    public async Task SignUpCommand_WhenDuplicateEmail_ShouldShowError()
    {
        // First registration
        var vm1 = _fixture.GetViewModel<SignUpViewModel>();
        vm1.DisplayName = "User One";
        vm1.Handle = "userone";
        vm1.Email = "duplicate@example.com";
        vm1.Password = "password123";
        await vm1.SignUpCommand.ExecuteAsync(null);
        
        // Second registration with same email
        await _fixture.CurrentUser.SignOutAsync();
        var vm2 = _fixture.GetViewModel<SignUpViewModel>();
        vm2.DisplayName = "User Two";
        vm2.Handle = "usertwo";
        vm2.Email = "duplicate@example.com";
        vm2.Password = "password456";
        await vm2.SignUpCommand.ExecuteAsync(null);
        
        Assert.That(vm2.ErrorMessage, Is.Not.Null.Or.Empty);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LOGIN TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public void LoginViewModel_Title_ShouldBeLogIn()
    {
        var vm = _fixture.GetViewModel<LoginViewModel>();
        Assert.That(vm.Title, Is.EqualTo("Log In"));
    }

    [Test]
    public void LoginCommand_WhenFieldsEmpty_CannotExecute()
    {
        var vm = _fixture.GetViewModel<LoginViewModel>();
        
        Assert.That(vm.LoginCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void LoginCommand_WhenFieldsFilled_CanExecute()
    {
        var vm = _fixture.GetViewModel<LoginViewModel>();
        vm.Email = "test@example.com";
        vm.Password = "password";
        
        Assert.That(vm.LoginCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task FullAuthFlow_SignUpThenLogout_ThenLogin()
    {
        // Step 1: Sign up
        var signUpVm = _fixture.GetViewModel<SignUpViewModel>();
        signUpVm.DisplayName = "Bob Wilson";
        signUpVm.Handle = "bob";
        signUpVm.Email = "bob@example.com";
        signUpVm.Password = "secretpass";
        await signUpVm.SignUpCommand.ExecuteAsync(null);
        
        Assert.That(_fixture.CurrentUser.IsAuthenticated, Is.True);
        Assert.That(_fixture.CurrentUser.DisplayName, Is.EqualTo("Bob Wilson"));
        
        // Step 2: Log out
        await _fixture.CurrentUser.SignOutAsync();
        Assert.That(_fixture.CurrentUser.IsAuthenticated, Is.False);
        
        // Step 3: Log in with same credentials
        var loginVm = _fixture.GetViewModel<LoginViewModel>();
        loginVm.Email = "bob@example.com";
        loginVm.Password = "secretpass";
        await loginVm.LoginCommand.ExecuteAsync(null);
        
        // Should be signed in again
        Assert.That(_fixture.CurrentUser.IsAuthenticated, Is.True);
        Assert.That(_fixture.CurrentUser.DisplayName, Is.EqualTo("Bob Wilson"));
        Assert.That(_fixture.Navigation.LastNavigatedUri, Is.EqualTo("/"));
    }

    [Test]
    public async Task LoginCommand_WhenInvalidCredentials_ShouldShowError()
    {
        var vm = _fixture.GetViewModel<LoginViewModel>();
        vm.Email = "nonexistent@example.com";
        vm.Password = "wrongpassword";
        await vm.LoginCommand.ExecuteAsync(null);
        
        Assert.That(vm.ErrorMessage, Is.Not.Null.Or.Empty);
        Assert.That(_fixture.CurrentUser.IsAuthenticated, Is.False);
    }

    [Test]
    public async Task NavigateToSignUpCommand_ShouldNavigateToSignUpPage()
    {
        var vm = _fixture.GetViewModel<LoginViewModel>();
        await vm.NavigateToSignUpCommand.ExecuteAsync(null);
        
        Assert.That(_fixture.Navigation.LastNavigatedUri, Is.EqualTo("/signup"));
    }

    [Test]
    public async Task NavigateToLoginCommand_ShouldNavigateToLoginPage()
    {
        var vm = _fixture.GetViewModel<SignUpViewModel>();
        await vm.NavigateToLoginCommand.ExecuteAsync(null);
        
        Assert.That(_fixture.Navigation.LastNavigatedUri, Is.EqualTo("/login"));
    }
}
