using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;
using OtpNet;

namespace TeamContributionManagementSystem.Application.Services;

public class MfaService : IMfaService
{
    private readonly Microsoft.Extensions.Logging.ILogger<MfaService> _logger;
    private readonly IUserMfaDeviceRepository _mfaDeviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public MfaService(Microsoft.Extensions.Logging.ILogger<MfaService> logger, IUserMfaDeviceRepository mfaDeviceRepository, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _mfaDeviceRepository = mfaDeviceRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<(string SecretKey, string QrCodeUri)> GenerateMfaSetupAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var base32String = Base32Encoding.ToString(secretKey);
        
        var otpAuthUri = $"otpauth://totp/TeamContributionApp:{userEmail}?secret={base32String}&issuer=TeamContributionApp";
        
        return Task.FromResult((base32String, otpAuthUri));
    }

    public async Task<bool> VerifyAndSaveMfaDeviceAsync(Guid userId, string secretKey, string deviceLabel, string otp, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingDevices = await _mfaDeviceRepository.GetByUserIdAsync(userId, cancellationToken);
            if (existingDevices.Any())
            {
                throw new InvalidOperationException("A user can only configure one MFA device. Please remove the existing device first.");
            }

            var base32Bytes = Base32Encoding.ToBytes(secretKey);
        var totp = new Totp(base32Bytes);

        if (totp.VerifyTotp(otp, out long timeStepMatched, new VerificationWindow(2, 2)))
        {
            var device = new UserMfaDevice
            {
                UserId = userId,
                DeviceLabel = string.IsNullOrWhiteSpace(deviceLabel) ? "New Device" : deviceLabel,
                SecretKey = secretKey,
                DateAdded = DateTime.UtcNow
            };

            await _mfaDeviceRepository.AddAsync(device, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }

        return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in VerifyAndSaveMfaDeviceAsync");
            throw;
        }
    }

    public async Task<IEnumerable<UserMfaDevice>> GetUserMfaDevicesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _mfaDeviceRepository.GetByUserIdAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUserMfaDevicesAsync");
            throw;
        }
    }

    public async Task RemoveMfaDeviceAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var device = await _mfaDeviceRepository.GetByIdAsync(userId, deviceId, cancellationToken);
        if (device != null)
        {
            await _mfaDeviceRepository.RemoveAsync(device, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RemoveMfaDeviceAsync");
            throw;
        }
    }
}
