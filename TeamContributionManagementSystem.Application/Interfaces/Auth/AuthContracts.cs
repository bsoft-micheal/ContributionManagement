using TeamContributionManagementSystem.Application.DTOs.Auth;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task RequestPasswordResetOtpAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> VerifyPasswordResetOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default);
    Task ResetPasswordWithOtpAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
}

public interface IJwtTokenGenerator
{
    AuthResponseDto GenerateToken(AppUser user);
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
