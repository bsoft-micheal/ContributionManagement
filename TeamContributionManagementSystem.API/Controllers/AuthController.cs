using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Auth;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Auth;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Handles user authentication, 2FA verification, and password resets.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route(CommonRoutes.Auth.Base)]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="request">The login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ApiResponse containing AuthResponseDto.</returns>
    [AllowAnonymous]
    [HttpPost(CommonRoutes.Auth.Login)]
    [ActionName(nameof(LoginAsync))]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> LoginAsync([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<AuthResponseDto>.SuccessResult(response, CommonMessages.Auth.LoginSuccess, CommonStatusCodes.Status200OK));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<AuthResponseDto>.FailureResult(ex.Message, CommonStatusCodes.Status400BadRequest));
        }
    }

    /// <summary>
    /// Verifies the 6-digit Authenticator App (TOTP) code during the login flow.
    /// </summary>
    /// <param name="request">The email and the 6-digit OTP code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ApiResponse containing AuthResponseDto upon successful verification.</returns>
    [AllowAnonymous]
    [HttpPost(CommonRoutes.Auth.Verify2Fa)]
    [ActionName(nameof(VerifyTwoFactorAsync))]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> VerifyTwoFactorAsync([FromBody] VerifyTwoFactorRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.VerifyTwoFactorAsync(request, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<AuthResponseDto>.SuccessResult(response, CommonMessages.Auth.VerifyTwoFactorSuccess, CommonStatusCodes.Status200OK));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<AuthResponseDto>.FailureResult(ex.Message, CommonStatusCodes.Status400BadRequest));
        }
    }

    /// <summary>
    /// Switches the authenticated user's active role.
    /// </summary>
    /// <param name="request">The target role identifier or name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ApiResponse containing updated AuthResponseDto.</returns>
    [Authorize]
    [HttpPost(CommonRoutes.Auth.SwitchRole)]
    [ActionName(nameof(SwitchRoleAsync))]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> SwitchRoleAsync([FromBody] SwitchRoleRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) 
                ?? User.FindFirstValue(ClaimTypes.Name) 
                ?? User.Identity?.Name 
                ?? string.Empty;

            var userAgent = Request.Headers["User-Agent"].ToString();
            var deviceHeader = Request.Headers["X-Device-Type"].ToString();
            var isMobileHeader = Request.Headers["X-Is-Mobile"].ToString();
            var clientTypeHeader = Request.Headers["X-Client-Type"].ToString();

            bool isMobileClient = request.IsFromMobile ||
                                 (request.DeviceInfo != null && request.DeviceInfo.DeviceType == 2) ||
                                 deviceHeader == "2" ||
                                 string.Equals(isMobileHeader, "true", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(clientTypeHeader, "mobile", StringComparison.OrdinalIgnoreCase) ||
                                 (!string.IsNullOrWhiteSpace(userAgent) && (
                                     userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
                                     userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
                                     userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
                                     userAgent.Contains("CFNetwork", StringComparison.OrdinalIgnoreCase) ||
                                     userAgent.Contains("Expo", StringComparison.OrdinalIgnoreCase) ||
                                     userAgent.Contains("ReactNative", StringComparison.OrdinalIgnoreCase) ||
                                     userAgent.Contains("okhttp", StringComparison.OrdinalIgnoreCase)
                                 ));

            if (isMobileClient)
            {
                request.IsFromMobile = true;
            }

            var response = await _authService.SwitchRoleAsync(request, userEmail, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<AuthResponseDto>.SuccessResult(response, "Role switched successfully", CommonStatusCodes.Status200OK));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<AuthResponseDto>.FailureResult(ex.Message, CommonStatusCodes.Status400BadRequest));
        }
    }

    /// <summary>
    /// Initiates the forgot password flow by generating an OTP and sending it to the user's email.
    /// </summary>
    /// <param name="request">The user's registered email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [AllowAnonymous]
    [HttpPost(CommonRoutes.Auth.ForgotPasswordRequest)]
    [ActionName(nameof(RequestForgotPasswordOtpAsync))]
    public async Task<ActionResult<ApiResponse>> RequestForgotPasswordOtpAsync([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await _authService.RequestPasswordResetOtpAsync(request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Auth.ForgotPasswordOtpSentSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Verifies the OTP sent to the user's email for password reset.
    /// </summary>
    /// <param name="request">The email and the OTP received.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [AllowAnonymous]
    [HttpPost(CommonRoutes.Auth.ForgotPasswordVerify)]
    [ActionName(nameof(VerifyForgotPasswordOtpAsync))]
    public async Task<ActionResult<ApiResponse>> VerifyForgotPasswordOtpAsync([FromBody] VerifyOtpRequestDto request, CancellationToken cancellationToken)
    {
        var isValid = await _authService.VerifyPasswordResetOtpAsync(request, cancellationToken);
        if (!isValid)
        {
            return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse.FailureResult(CommonMessages.Auth.InvalidOrExpiredOtp, CommonStatusCodes.Status400BadRequest));
        }
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Auth.ForgotPasswordOtpVerifiedSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Resets the user's password using the verified OTP.
    /// </summary>
    /// <param name="request">The email, OTP, and the new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [AllowAnonymous]
    [HttpPost(CommonRoutes.Auth.ForgotPasswordReset)]
    [ActionName(nameof(ResetPasswordWithOtpAsync))]
    public async Task<ActionResult<ApiResponse>> ResetPasswordWithOtpAsync([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordWithOtpAsync(request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Auth.PasswordResetSuccess, CommonStatusCodes.Status200OK));
    }
}
