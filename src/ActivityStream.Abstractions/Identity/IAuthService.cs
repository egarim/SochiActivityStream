namespace ActivityStream.Abstractions.Identity;

public interface IAuthService
{
    Task<SignUpResult> SignUpAsync(SignUpRequest request, CancellationToken ct = default);
    Task<SessionDto> SignInAsync(SignInRequest request, CancellationToken ct = default);
}
