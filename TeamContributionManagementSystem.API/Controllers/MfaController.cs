using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Mfa;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages Authenticator App (TOTP) Multi-Factor Authentication setups for users.
/// </summary>
[Route(CommonRoutes.Mfa.Base)]
[ApiController]
[Authorize]
public class MfaController : ControllerBase
{
    private readonly IMfaService _mfaService;

    public MfaController(IMfaService mfaService)
    {
        _mfaService = mfaService;
    }

    /// <summary>
    /// Initiates the MFA setup process by generating a new Secret Key and QR Code URI for the user.
    /// </summary>
    [HttpGet(CommonRoutes.Mfa.Setup)]
    [ActionName(nameof(SetupMfaAsync))]
    public async Task<ActionResult<ApiResponse<object>>> SetupMfaAsync()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email)) return StatusCode(CommonStatusCodes.Status401Unauthorized, ApiResponse<object>.FailureResult(CommonMessages.General.Unauthorized, CommonStatusCodes.Status401Unauthorized));

        var result = await _mfaService.GenerateMfaSetupAsync(email);
        var data = new
        {
            SecretKey = result.SecretKey,
            QrCodeUri = result.QrCodeUri
        };
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<object>.SuccessResult(data, CommonMessages.Mfa.SetupSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Verifies the first 6-digit code from the Authenticator App and saves the Secret Key if valid.
    /// </summary>
    /// <param name="request">The Secret Key, device label, and 6-digit OTP to verify.</param>
    [HttpPost(CommonRoutes.Mfa.VerifySetup)]
    [ActionName(nameof(VerifySetupMfaAsync))]
    public async Task<ActionResult<ApiResponse>> VerifySetupMfaAsync([FromBody] MfaSetupVerifyRequestDto request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out Guid userId)) return StatusCode(CommonStatusCodes.Status401Unauthorized, ApiResponse.FailureResult(CommonMessages.General.Unauthorized, CommonStatusCodes.Status401Unauthorized));

        var success = await _mfaService.VerifyAndSaveMfaDeviceAsync(userId, request.SecretKey, request.DeviceLabel, request.Otp);
        if (success)
        {
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Mfa.VerifySetupSuccess, CommonStatusCodes.Status200OK));
        }
        return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse.FailureResult(CommonMessages.Mfa.InvalidOtpCode, CommonStatusCodes.Status400BadRequest));
    }

    /// <summary>
    /// Retrieves a list of all MFA devices currently linked to the authenticated user's account.
    /// </summary>
    [HttpGet(CommonRoutes.Mfa.GetDevices)]
    [ActionName(nameof(GetDevicesMfaAsync))]
    public async Task<ActionResult<ApiResponse<object>>> GetDevicesMfaAsync()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out Guid userId)) return StatusCode(CommonStatusCodes.Status401Unauthorized, ApiResponse<object>.FailureResult(CommonMessages.General.Unauthorized, CommonStatusCodes.Status401Unauthorized));

        var devices = await _mfaService.GetUserMfaDevicesAsync(userId);
        var result = devices.Select(d => new
        {
            d.Id,
            d.DeviceLabel,
            d.DateAdded
        });
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<object>.SuccessResult(result, CommonMessages.Mfa.GetDevicesSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes a specific MFA device from the user's account.
    /// </summary>
    /// <param name="id">The unique identifier of the MFA device.</param>
    [HttpDelete(CommonRoutes.Mfa.RemoveDevice)]
    [ActionName(nameof(RemoveDeviceMfaAsync))]
    public async Task<ActionResult<ApiResponse>> RemoveDeviceMfaAsync(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out Guid userId)) return StatusCode(CommonStatusCodes.Status401Unauthorized, ApiResponse.FailureResult(CommonMessages.General.Unauthorized, CommonStatusCodes.Status401Unauthorized));

        await _mfaService.RemoveMfaDeviceAsync(userId, id);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Mfa.RemoveDeviceSuccess, CommonStatusCodes.Status200OK));
    }
}
