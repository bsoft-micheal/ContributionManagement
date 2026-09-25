using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Auth;
using TeamContributionManagementSystem.Application.Interfaces.Auth;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRoleRightsService _roleRightsService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeviceSessionRepository _deviceSessionRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;
    private readonly ISystemSettingService _systemSettingService;

    public AuthService(ILogger<AuthService> logger, 
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRoleRightsService roleRightsService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IDeviceSessionRepository deviceSessionRepository,
        IMemberRepository memberRepository,
        IMemoryCache memoryCache,
        IConfiguration configuration,
        ISystemSettingService systemSettingService)
    {
        _logger = logger;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _roleRightsService = roleRightsService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _deviceSessionRepository = deviceSessionRepository;
        _memberRepository = memberRepository;
        _memoryCache = memoryCache;
        _configuration = configuration;
        _systemSettingService = systemSettingService;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(CommonLogMessages.Auth.LoginAttempt, request.Email);
            var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning(CommonLogMessages.Auth.LoginFailed, request.Email, CommonMessages.Auth.InvalidCredentials);
                throw new InvalidOperationException(CommonMessages.Auth.InvalidCredentials);
            }

            if (!user.IsActive)
            {
                throw new InvalidOperationException(CommonMessages.Auth.AccountDeactivated);
            }

            if (user.MfaDevices != null && user.MfaDevices.Any())
            {
                return new AuthResponseDto
                {
                    RequiresTwoFactor = true,
                    Email = user.Email
                };
            }

            _logger.LogInformation(CommonLogMessages.Auth.LoginSuccess, user.Email);
            return await GenerateAuthResponseAndLogSessionAsync(user, request.DeviceInfo, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(LoginAsync));
            throw;
        }
    }

    public async Task<AuthResponseDto> VerifyTwoFactorAsync(VerifyTwoFactorRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (user is null)
            {
                throw new InvalidOperationException(CommonMessages.Auth.UserNotFound);
            }

            if (!user.IsActive)
            {
                throw new InvalidOperationException(CommonMessages.Auth.AccountDeactivated);
            }

            if (user.MfaDevices == null || !user.MfaDevices.Any())
            {
                throw new InvalidOperationException(CommonMessages.Mfa.NotEnabled);
            }

            int maxFailedAttempts = int.TryParse(_configuration["MfaSecurity:MaxFailedAttempts"], out var maxAttempts) ? maxAttempts : 3;
            int lockoutMinutes = int.TryParse(_configuration["MfaSecurity:LockoutMinutes"], out var lockMins) ? lockMins : 15;

            var lockoutKey = $"{CommonConstants.CacheKeys.MfaLockoutPrefix}{user.UserId}";
            var attemptsKey = $"{CommonConstants.CacheKeys.MfaAttemptsPrefix}{user.UserId}";

            // 1. Check if user is currently locked out
            if (_memoryCache.TryGetValue(lockoutKey, out DateTime lockoutUntil))
            {
                if (DateTime.UtcNow < lockoutUntil)
                {
                    var remainingMinutes = Math.Max(1, (int)Math.Ceiling((lockoutUntil - DateTime.UtcNow).TotalMinutes));
                    throw new InvalidOperationException(string.Format(CommonMessages.Mfa.LockoutFormat, remainingMinutes));
                }
                else
                {
                    _memoryCache.Remove(lockoutKey);
                    _memoryCache.Remove(attemptsKey);
                }
            }

            // 2. Validate OTP
            bool isValid = false;
            foreach (var mfaDevice in user.MfaDevices)
            {
                var base32Bytes = OtpNet.Base32Encoding.ToBytes(mfaDevice.SecretKey);
                var totp = new OtpNet.Totp(base32Bytes);
                if (totp.VerifyTotp(request.Otp.Trim(), out long _, new OtpNet.VerificationWindow(2, 2)))
                {
                    isValid = true;
                    break;
                }
            }

            if (!isValid)
            {
                int currentAttempts = _memoryCache.GetOrCreate(attemptsKey, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(lockoutMinutes);
                    return 0;
                });

                currentAttempts++;
                _memoryCache.Set(attemptsKey, currentAttempts, TimeSpan.FromMinutes(lockoutMinutes));

                if (currentAttempts >= maxFailedAttempts)
                {
                    var lockoutUntilTime = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                    _memoryCache.Set(lockoutKey, lockoutUntilTime, TimeSpan.FromMinutes(lockoutMinutes));
                    _memoryCache.Remove(attemptsKey);

                    throw new InvalidOperationException(string.Format(CommonMessages.Mfa.MaxAttemptsExceededFormat, lockoutMinutes));
                }
                else
                {
                    int remaining = maxFailedAttempts - currentAttempts;
                    throw new InvalidOperationException(string.Format(CommonMessages.Mfa.InvalidOtpRemainingFormat, remaining, remaining > 1 ? "s" : string.Empty));
                }
            }

            // 3. OTP is valid: Reset failed attempts & lockout
            _memoryCache.Remove(attemptsKey);
            _memoryCache.Remove(lockoutKey);

            _logger.LogInformation(CommonLogMessages.Auth.MfaVerified, user.Email);
            return await GenerateAuthResponseAndLogSessionAsync(user, request.DeviceInfo, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(VerifyTwoFactorAsync));
            throw;
        }
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAndLogSessionAsync(AppUser user, DeviceDetailPayloadDto? deviceInfo, CancellationToken cancellationToken)
    {
        Guid? sessionId = null;

        if (deviceInfo != null)
        {
            TeamContributionManagementSystem.Domain.Entities.DeviceDetail? device = null;

            // Mobile Login (DeviceType == 2)
            if (deviceInfo.DeviceType == 2)
            {
                // Validate required mobile fields
                if (string.IsNullOrWhiteSpace(deviceInfo.DeviceId) ||
                    string.IsNullOrWhiteSpace(deviceInfo.DeviceName) ||
                    string.IsNullOrWhiteSpace(deviceInfo.Brand) ||
                    string.IsNullOrWhiteSpace(deviceInfo.Model) ||
                    string.IsNullOrWhiteSpace(deviceInfo.Os) ||
                    string.IsNullOrWhiteSpace(deviceInfo.OsVersion) ||
                    string.IsNullOrWhiteSpace(deviceInfo.SystemName) ||
                    string.IsNullOrWhiteSpace(deviceInfo.SystemVersion) ||
                    string.IsNullOrWhiteSpace(deviceInfo.AppVersion) ||
                    !deviceInfo.TotalMemory.HasValue || deviceInfo.TotalMemory <= 0)
                {
                    throw new InvalidOperationException("Mobile device information is incomplete. Required fields: DeviceId, DeviceName, Brand, Model, OS, OsVersion, SystemName, SystemVersion, AppVersion, TotalMemory.");
                }

                device = await _deviceSessionRepository.GetDeviceByDeviceIdAsync(user.UserId, deviceInfo.DeviceId, cancellationToken);
                if (device == null)
                {
                    device = new TeamContributionManagementSystem.Domain.Entities.DeviceDetail
                    {
                        UserId = user.UserId,
                        DeviceId = deviceInfo.DeviceId,
                        DeviceName = deviceInfo.DeviceName,
                        Brand = deviceInfo.Brand,
                        Model = deviceInfo.Model,
                        Os = deviceInfo.Os,
                        OsVersion = deviceInfo.OsVersion,
                        SystemName = deviceInfo.SystemName,
                        SystemVersion = deviceInfo.SystemVersion,
                        DeviceType = deviceInfo.DeviceType,
                        AppVersion = deviceInfo.AppVersion,
                        TotalMemory = deviceInfo.TotalMemory,
                        Browser = deviceInfo.Browser,
                        BrowserVersion = deviceInfo.BrowserVersion,
                        IsActive = true,
                        LastSeenAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _deviceSessionRepository.Add(device);
                }
                else
                {
                    device.DeviceName = deviceInfo.DeviceName;
                    device.Brand = deviceInfo.Brand;
                    device.Model = deviceInfo.Model;
                    device.Os = deviceInfo.Os;
                    device.OsVersion = deviceInfo.OsVersion;
                    device.SystemName = deviceInfo.SystemName;
                    device.SystemVersion = deviceInfo.SystemVersion;
                    device.AppVersion = deviceInfo.AppVersion;
                    device.TotalMemory = deviceInfo.TotalMemory;
                    device.LastSeenAt = DateTime.UtcNow;
                    device.UpdatedAt = DateTime.UtcNow;
                    device.IsActive = true;
                    _deviceSessionRepository.Update(device);
                }
            }

            // Always create DeviceLoginHistory session tracking for both Web and Mobile
            var loginHistory = new TeamContributionManagementSystem.Domain.Entities.DeviceLoginHistory
            {
                UserId = user.UserId,
                DeviceDetail = device,
                DeviceDetailId = device?.Id,
                LoginTime = DateTime.UtcNow,
                IsActive = true
            };
            _deviceSessionRepository.AddLoginHistory(loginHistory);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            sessionId = loginHistory.Id;
        }
        else
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken); // To save OTP clear if applicable
        }

        var response = _jwtTokenGenerator.GenerateToken(user, sessionId);
        response.Rights = await _roleRightsService.GetRoleRightAsyncByRole(user.Role.ToString(), cancellationToken);
        response.RequiresTwoFactor = false;

        var member = await _memberRepository.GetByEmailAsync(user.Email, cancellationToken);
        if (member != null)
        {
            response.Phone = member.Phone;
            response.DateOfBirth = member.DateOfBirth;
            response.JoiningDate = member.JoiningDate;
            response.Gender = member.Gender;
            response.MemberType = member.MemberType;
            if (member.Role != null && !string.IsNullOrWhiteSpace(member.Role.RoleName))
            {
                response.Role = member.Role.RoleName;
            }
        }

        return response;
    }

    public async Task RequestPasswordResetOtpAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (user is null)
            {
                throw new InvalidOperationException(CommonMessages.Auth.UserNotFound);
            }

            var settings = await _systemSettingService.GetSettingsAsync(cancellationToken);
            int expiryMinutes = 10;
            if (!string.IsNullOrWhiteSpace(settings.OtpExpiry) && int.TryParse(settings.OtpExpiry, out var parsedExpiry) && parsedExpiry > 0)
            {
                expiryMinutes = parsedExpiry;
            }

            var otp = new Random().Next(100000, 999999).ToString();
            user.PasswordResetOtp = otp;
            user.PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var cacheKey = $"{CommonConstants.CacheKeys.PwdResetAttemptsPrefix}{request.Email.Trim().ToLowerInvariant()}";
            _memoryCache.Remove(cacheKey);

            var subject = CommonConstants.EmailTemplates.PasswordResetSubject;
            var emailBody = $@"
<div style=""font-family: 'Outfit', 'Inter', sans-serif; background-color: #f7f6fb; padding: 40px; border-radius: 16px; max-width: 600px; margin: 0 auto; color: #1e1a2e; border: 1px solid rgba(74, 63, 107, 0.08);"">
    <div style=""text-align: center; margin-bottom: 30px;"">
        <h2 style=""margin: 0; color: #7c3aed; font-weight: 900; letter-spacing: 0.05em;"">TEAM CONTRIBUTION</h2>
        <span style=""font-size: 12px; color: #5b5280; font-weight: 700; text-transform: uppercase;"">Management System</span>
    </div>
    <div style=""background-color: #ffffff; border-radius: 12px; padding: 30px; box-shadow: 0 10px 30px rgba(30, 26, 46, 0.03);"">
        <h3 style=""margin-top: 0; color: #1e1a2e; font-weight: 800;"">Password Reset Request</h3>
        <p style=""color: #5b5280; font-size: 14px; line-height: 1.6;"">Hello {user.FullName},</p>
        <p style=""color: #5b5280; font-size: 14px; line-height: 1.6;"">We received a request to reset the password for your account. Please use the following One-Time Password (OTP) to complete the verification process:</p>
        <div style=""background: linear-gradient(135deg, #7c3aed 0%, #4f46e5 100%); color: #ffffff; text-align: center; font-size: 32px; font-weight: 900; letter-spacing: 6px; padding: 15px; border-radius: 8px; margin: 25px 0; font-family: monospace;"">
            {otp}
        </div>
        <p style=""color: #ef4444; font-size: 13px; font-weight: 600; margin-bottom: 20px;"">This OTP will expire in {expiryMinutes} minutes.</p>
        <hr style=""border: 0; border-top: 1px solid rgba(74, 63, 107, 0.08); margin: 20px 0;"" />
        <p style=""color: #a78bfa; font-size: 12px; line-height: 1.5; margin: 0;"">If you did not request a password reset, you can safely ignore this email.</p>
    </div>
    <div style=""text-align: center; margin-top: 25px; color: #9d96bd; font-size: 12px;"">
        &copy; 2026 Team Contribution Management System. All rights reserved.
    </div>
</div>";

            await _emailService.SendEmailAsync(user.Email, subject, emailBody, cancellationToken: cancellationToken);
            _logger.LogInformation(CommonLogMessages.Auth.OtpSent, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(RequestPasswordResetOtpAsync));
            throw;
        }
    }

    public async Task<bool> VerifyPasswordResetOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (user is null)
            {
                throw new InvalidOperationException(CommonMessages.Auth.UserNotFound);
            }

            var settings = await _systemSettingService.GetSettingsAsync(cancellationToken);
            int maxRetry = 3;
            if (!string.IsNullOrWhiteSpace(settings.MaxRetry) && int.TryParse(settings.MaxRetry, out var parsedMax) && parsedMax > 0)
            {
                maxRetry = parsedMax;
            }

            var cacheKey = $"{CommonConstants.CacheKeys.PwdResetAttemptsPrefix}{request.Email.Trim().ToLowerInvariant()}";
            int currentAttempts = _memoryCache.TryGetValue(cacheKey, out int val) ? val : 0;

            if (currentAttempts >= maxRetry)
            {
                user.PasswordResetOtp = null;
                user.PasswordResetOtpExpiry = null;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException(CommonMessages.Auth.MaxOtpAttemptsExceeded);
            }

            if (string.IsNullOrEmpty(user.PasswordResetOtp) || user.PasswordResetOtpExpiry < DateTime.UtcNow)
            {
                throw new InvalidOperationException(CommonMessages.Auth.OtpExpired);
            }

            if (user.PasswordResetOtp != request.Otp.Trim())
            {
                currentAttempts++;
                _memoryCache.Set(cacheKey, currentAttempts, TimeSpan.FromMinutes(30));

                if (currentAttempts >= maxRetry)
                {
                    user.PasswordResetOtp = null;
                    user.PasswordResetOtpExpiry = null;
                    _userRepository.Update(user);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    throw new InvalidOperationException(CommonMessages.Auth.MaxOtpAttemptsExceeded);
                }

                int remaining = maxRetry - currentAttempts;
                throw new InvalidOperationException(string.Format(CommonMessages.Auth.InvalidOtpRemainingFormat, remaining, remaining == 1 ? string.Empty : "s"));
            }

            _memoryCache.Remove(cacheKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(VerifyPasswordResetOtpAsync));
            throw;
        }
    }

    public async Task ResetPasswordWithOtpAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (user is null)
            {
                throw new InvalidOperationException(CommonMessages.Auth.UserNotFound);
            }

            if (string.IsNullOrEmpty(user.PasswordResetOtp) || user.PasswordResetOtp != request.Otp.Trim())
            {
                throw new InvalidOperationException(CommonMessages.Auth.InvalidOrExpiredOtp);
            }

            if (user.PasswordResetOtpExpiry < DateTime.UtcNow)
            {
                throw new InvalidOperationException(CommonMessages.Auth.OtpExpired);
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.PasswordResetOtp = null;
            user.PasswordResetOtpExpiry = null;

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _memoryCache.Remove($"{CommonConstants.CacheKeys.PwdResetAttemptsPrefix}{request.Email.Trim().ToLowerInvariant()}");
            _logger.LogInformation(CommonLogMessages.Auth.PasswordResetSuccess, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(ResetPasswordWithOtpAsync));
            throw;
        }
    }
}
