using TeamContributionManagementSystem.Application.DTOs.Auth;
using TeamContributionManagementSystem.Application.Interfaces.Auth;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRoleRightsService _roleRightsService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeviceSessionRepository _deviceSessionRepository;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRoleRightsService roleRightsService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IDeviceSessionRepository deviceSessionRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _roleRightsService = roleRightsService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _deviceSessionRepository = deviceSessionRepository;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null || !user.IsActive || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid email or password");
        }

        if (user.MfaDevices != null && user.MfaDevices.Any())
        {
            return new AuthResponseDto
            {
                RequiresTwoFactor = true,
                Email = user.Email
            };
        }

        return await GenerateAuthResponseAndLogSessionAsync(user, request.DeviceInfo, cancellationToken);
    }

    public async Task<AuthResponseDto> VerifyTwoFactorAsync(VerifyTwoFactorRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException("Invalid user.");
        }

        if (user.MfaDevices == null || !user.MfaDevices.Any())
        {
            throw new InvalidOperationException("MFA is not enabled for this user.");
        }

        bool isValid = false;
        foreach (var mfaDevice in user.MfaDevices)
        {
            var base32Bytes = OtpNet.Base32Encoding.ToBytes(mfaDevice.SecretKey);
            var totp = new OtpNet.Totp(base32Bytes);
            if (totp.VerifyTotp(request.Otp.Trim(), out long timeStepMatched, new OtpNet.VerificationWindow(2, 2)))
            {
                isValid = true;
                break;
            }
        }

        if (!isValid)
        {
            throw new InvalidOperationException("Invalid OTP code.");
        }

        // Save changes will be called inside GenerateAuthResponseAndLogSessionAsync via _unitOfWork


        return await GenerateAuthResponseAndLogSessionAsync(user, request.DeviceInfo, cancellationToken);
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAndLogSessionAsync(TeamContributionManagementSystem.Domain.Entities.AppUser user, DeviceDetailPayloadDto? deviceInfo, CancellationToken cancellationToken)
    {
        Guid? sessionId = null;

        if (deviceInfo != null)
        {
            var device = await _deviceSessionRepository.GetDeviceByDeviceIdAsync(user.UserId, deviceInfo.DeviceId, cancellationToken);
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
                device.LastSeenAt = DateTime.UtcNow;
                device.UpdatedAt = DateTime.UtcNow;
                device.IsActive = true;
                _deviceSessionRepository.Update(device);
            }

            var loginHistory = new TeamContributionManagementSystem.Domain.Entities.DeviceLoginHistory
            {
                UserId = user.UserId,
                DeviceDetail = device,
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
        response.Rights = await _roleRightsService.GetByRoleAsync(user.Role.ToString(), cancellationToken);
        response.RequiresTwoFactor = false;
        return response;
    }

    public async Task RequestPasswordResetOtpAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("A user with this email address was not found.");
        }

        var otp = new Random().Next(100000, 999999).ToString();
        user.PasswordResetOtp = otp;
        user.PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(15);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var subject = "Password Reset OTP - Team Contribution Management System";
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
        <p style=""color: #ef4444; font-size: 13px; font-weight: 600; margin-bottom: 20px;"">This OTP will expire in 15 minutes.</p>
        <hr style=""border: 0; border-top: 1px solid rgba(74, 63, 107, 0.08); margin: 20px 0;"" />
        <p style=""color: #a78bfa; font-size: 12px; line-height: 1.5; margin: 0;"">If you did not request a password reset, you can safely ignore this email.</p>
    </div>
    <div style=""text-align: center; margin-top: 25px; color: #9d96bd; font-size: 12px;"">
        &copy; 2026 Team Contribution Management System. All rights reserved.
    </div>
</div>";

        await _emailService.SendEmailAsync(user.Email, subject, emailBody, cancellationToken: cancellationToken);
    }

    public async Task<bool> VerifyPasswordResetOtpAsync(VerifyOtpRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("A user with this email address was not found.");
        }

        if (string.IsNullOrEmpty(user.PasswordResetOtp) || user.PasswordResetOtp != request.Otp.Trim())
        {
            return false;
        }

        if (user.PasswordResetOtpExpiry < DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    public async Task ResetPasswordWithOtpAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("A user with this email address was not found.");
        }

        if (string.IsNullOrEmpty(user.PasswordResetOtp) || user.PasswordResetOtp != request.Otp.Trim())
        {
            throw new InvalidOperationException("Invalid or missing password reset OTP.");
        }

        if (user.PasswordResetOtpExpiry < DateTime.UtcNow)
        {
            throw new InvalidOperationException("The password reset OTP has expired. Please request a new one.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.PasswordResetOtp = null;
        user.PasswordResetOtpExpiry = null;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
