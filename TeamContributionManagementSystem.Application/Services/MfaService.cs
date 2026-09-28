using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Application.DTOs.Mfa;
using TeamContributionManagementSystem.Domain.Entities;
using OtpNet;

namespace TeamContributionManagementSystem.Application.Services;

public class MfaService : IMfaService
{
    private readonly ILogger<MfaService> _logger;
    private readonly IUserMfaDeviceRepository _mfaDeviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public MfaService(ILogger<MfaService> logger, IUserMfaDeviceRepository mfaDeviceRepository, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _mfaDeviceRepository = mfaDeviceRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<(string SecretKey, string QrCodeUri)> GenerateMfaSetupAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var base32String = Base32Encoding.ToString(secretKey);
        
        var otpAuthUri = $"otpauth://totp/{CommonConstants.Defaults.MfaIssuer}:{userEmail}?secret={base32String}&issuer={CommonConstants.Defaults.MfaIssuer}";
        
        return Task.FromResult((base32String, otpAuthUri));
    }

    public async Task<bool> VerifyAndSaveMfaDeviceAsync(Guid userId, string secretKey, string deviceLabel, string otp, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingDevices = await _mfaDeviceRepository.GetByUserIdAsync(userId, cancellationToken);
            if (existingDevices.Any())
            {
                throw new InvalidOperationException(CommonMessages.Mfa.SingleDeviceLimit);
            }

            var base32Bytes = Base32Encoding.ToBytes(secretKey);
            var totp = new Totp(base32Bytes);

            if (totp.VerifyTotp(otp, out long _, new VerificationWindow(2, 2)))
            {
                var device = new UserMfaDevice
                {
                    UserId = userId,
                    DeviceLabel = string.IsNullOrWhiteSpace(deviceLabel) ? CommonConstants.Defaults.DefaultMfaDeviceLabel : deviceLabel,
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(VerifyAndSaveMfaDeviceAsync));
            throw;
        }
    }

    public async Task<IEnumerable<UserMfaDeviceDto>> GetUserMfaDevicesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var devices = await _mfaDeviceRepository.GetByUserIdAsync(userId, cancellationToken);
            return devices.Select(d => new UserMfaDeviceDto
            {
                Id = d.Id,
                DeviceLabel = d.DeviceLabel,
                DateAdded = d.DateAdded
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetUserMfaDevicesAsync));
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(RemoveMfaDeviceAsync));
            throw;
        }
    }
}
