using SchoolERP.Business.Identity.DTOs;

namespace SchoolERP.Business.Identity.Services.Interfaces;

public interface IAuthService
{
    Task<RegisteredUser> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResult> CompleteTwoFactorLoginAsync(TwoFactorLoginRequest request, string? ipAddress, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);
    Task<AuthResult> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<AuthResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? ipAddress, CancellationToken ct = default);
}
