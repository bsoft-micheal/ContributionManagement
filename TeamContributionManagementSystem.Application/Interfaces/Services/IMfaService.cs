using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IMfaService
{
    Task<(string SecretKey, string QrCodeUri)> GenerateMfaSetupAsync(string userEmail, CancellationToken cancellationToken = default);
    Task<bool> VerifyAndSaveMfaDeviceAsync(Guid userId, string secretKey, string deviceLabel, string otp, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserMfaDevice>> GetUserMfaDevicesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RemoveMfaDeviceAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default);
}
