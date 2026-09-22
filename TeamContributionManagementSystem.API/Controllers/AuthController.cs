using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Auth;
using TeamContributionManagementSystem.Application.Interfaces.Auth;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Handles user authentication, 2FA verification, and password resets.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
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
    [HttpPost("loginAsync")]
    [ActionName("LoginAsync")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> LoginAsync([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<AuthResponseDto>.SuccessResult(response, CommonMessages.Auth.LoginSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Verifies the 6-digit Authenticator App (TOTP) code during the login flow.
    /// </summary>
    /// <param name="request">The email and the 6-digit OTP code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ApiResponse containing AuthResponseDto upon successful verification.</returns>
    [AllowAnonymous]
    [HttpPost("verify-2faAsync")]
    [ActionName("VerifyTwoFactorAsync")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> VerifyTwoFactorAsync([FromBody] VerifyTwoFactorRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.VerifyTwoFactorAsync(request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<AuthResponseDto>.SuccessResult(response, CommonMessages.Auth.VerifyTwoFactorSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Initiates the forgot password flow by generating an OTP and sending it to the user's email.
    /// </summary>
    /// <param name="request">The user's registered email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [AllowAnonymous]
    [HttpPost("forgot-password/requestAsync")]
    [ActionName("RequestForgotPasswordOtpAsync")]
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
    [HttpPost("forgot-password/verifyAsync")]
    [ActionName("VerifyForgotPasswordOtpAsync")]
    public async Task<ActionResult<ApiResponse>> VerifyForgotPasswordOtpAsync([FromBody] VerifyOtpRequestDto request, CancellationToken cancellationToken)
    {
        var isValid = await _authService.VerifyPasswordResetOtpAsync(request, cancellationToken);
        if (!isValid)
        {
            return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse.FailureResult("Invalid or expired password reset OTP.", CommonStatusCodes.Status400BadRequest));
        }
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Auth.ForgotPasswordOtpVerifiedSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Resets the user's password using the verified OTP.
    /// </summary>
    /// <param name="request">The email, OTP, and the new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [AllowAnonymous]
    [HttpPost("forgot-password/resetAsync")]
    [ActionName("ResetPasswordWithOtpAsync")]
    public async Task<ActionResult<ApiResponse>> ResetPasswordWithOtpAsync([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordWithOtpAsync(request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Auth.PasswordResetSuccess, CommonStatusCodes.Status200OK));
    }
}
