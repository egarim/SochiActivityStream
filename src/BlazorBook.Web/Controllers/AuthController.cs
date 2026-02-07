using Microsoft.AspNetCore.Mvc;
using Identity.Abstractions;
using BlazorBook.Web.Services;

namespace BlazorBook.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtTokenService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        JwtTokenService jwtService,
        ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequestDto request)
    {
        try
        {
            var signUpRequest = new SignUpRequest
            {
                Email = request.Email,
                Username = request.Username,
                Password = request.Password,
                DisplayName = request.DisplayName
            };

            var result = await _authService.SignUpAsync(request.TenantId, signUpRequest);
            
            // Create session for the new user
            var signInRequest = new SignInRequest
            {
                Login = request.Username,
                Password = request.Password
            };
            
            var session = await _authService.SignInAsync(request.TenantId, signInRequest);

            var token = _jwtService.GenerateToken(
                session.SessionId,
                session.UserId,
                session.TenantId,
                session.ProfileIds.FirstOrDefault() ?? string.Empty
            );

            return Ok(new
            {
                token,
                session,
                user = result.User,
                profile = result.Profile
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during signup");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var signInRequest = new SignInRequest
            {
                Login = request.UsernameOrEmail,
                Password = request.Password
            };

            var session = await _authService.SignInAsync(request.TenantId, signInRequest);

            var token = _jwtService.GenerateToken(
                session.SessionId,
                session.UserId,
                session.TenantId,
                session.ProfileIds.FirstOrDefault() ?? string.Empty
            );

            return Ok(new
            {
                token,
                session
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request)
    {
        try
        {
            await _authService.SignOutAsync(request.SessionId);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public record SignupRequestDto(string TenantId, string Email, string Username, string Password, string? DisplayName);
public record LoginRequestDto(string TenantId, string UsernameOrEmail, string Password);
public record LogoutRequestDto(string SessionId);
