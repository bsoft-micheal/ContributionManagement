using TeamContributionManagementSystem.Application.DTOs.Auth;

namespace TeamContributionManagementSystem.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> VerifyTwoFactorAsync(VerifyTwoFactorRequestDto request, CancellationToken cancellationToken = default);
    Task RequestPasswordResetOtpAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> VerifyPasswordResetOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default);
    Task ResetPasswordWithOtpAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
}
