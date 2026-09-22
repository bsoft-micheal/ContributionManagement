using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    /// <returns>An AuthResponseDto containing the JWT token, or a requiresTwoFactor flag.</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Verifies the 6-digit Authenticator App (TOTP) code during the login flow.
    /// </summary>
    /// <param name="request">The email and the 6-digit OTP code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An AuthResponseDto containing the JWT token upon successful verification.</returns>
    [AllowAnonymous]
    [HttpPost("verify-2fa")]
    public async Task<ActionResult<AuthResponseDto>> VerifyTwoFactor([FromBody] VerifyTwoFactorRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.VerifyTwoFactorAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Initiates the forgot password flow by generating an OTP and sending it to the user's email.
    /// </summary>
    /// <param name="request">The user's registered email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [AllowAnonymous]
    [HttpPost("forgot-password/request")]
    public async Task<IActionResult> RequestForgotPasswordOtp([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await _authService.RequestPasswordResetOtpAsync(request, cancellationToken);
        return Ok(new { message = "If the email is registered, a password reset OTP has been sent." });
    }

    /// <summary>
    /// Verifies the OTP sent to the user's email for password reset.
    /// </summary>
    /// <param name="request">The email and the OTP received.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [AllowAnonymous]
    [HttpPost("forgot-password/verify")]
    public async Task<IActionResult> VerifyForgotPasswordOtp([FromBody] VerifyOtpRequestDto request, CancellationToken cancellationToken)
    {
        var isValid = await _authService.VerifyPasswordResetOtpAsync(request, cancellationToken);
        if (!isValid)
        {
            return BadRequest(new { message = "Invalid or expired password reset OTP." });
        }
        return Ok(new { message = "OTP verified successfully." });
    }

    /// <summary>
    /// Resets the user's password using the verified OTP.
    /// </summary>
    /// <param name="request">The email, OTP, and the new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [AllowAnonymous]
    [HttpPost("forgot-password/reset")]
    public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordWithOtpAsync(request, cancellationToken);
        return Ok(new { message = "Your password has been successfully reset. Please log in with your new credentials." });
    }
}
