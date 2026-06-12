using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Auth;
using TeamContributionManagementSystem.Application.Interfaces.Auth;

namespace TeamContributionManagementSystem.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password/request")]
    public async Task<IActionResult> RequestForgotPasswordOtp([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await _authService.RequestPasswordResetOtpAsync(request, cancellationToken);
        return Ok(new { message = "If the email is registered, a password reset OTP has been sent." });
    }

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

    [AllowAnonymous]
    [HttpPost("forgot-password/reset")]
    public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordWithOtpAsync(request, cancellationToken);
        return Ok(new { message = "Your password has been successfully reset. Please log in with your new credentials." });
    }
}
