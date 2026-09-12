using SchoolERP.Identity.DTOs;

namespace SchoolERP.Identity.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);
    Task<AuthResult> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken ct = default);
    Task LogoutAsync(Guid userId, string refreshToken, CancellationToken ct = default);
}
