using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages Authenticator App (TOTP) Multi-Factor Authentication setups for users.
/// </summary>
[Route("api/v1/[controller]")]
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
    [HttpGet("setup")]
    public async Task<IActionResult> SetupMfa()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var result = await _mfaService.GenerateMfaSetupAsync(email);
        return Ok(new
        {
            SecretKey = result.SecretKey,
            QrCodeUri = result.QrCodeUri
        });
    }

    /// <summary>
    /// Verifies the first 6-digit code from the Authenticator App and saves the Secret Key if valid.
    /// </summary>
    /// <param name="request">The Secret Key, device label, and 6-digit OTP to verify.</param>
    [HttpPost("verify-setup")]
    public async Task<IActionResult> VerifySetup([FromBody] MfaSetupVerifyRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

        var success = await _mfaService.VerifyAndSaveMfaDeviceAsync(userId, request.SecretKey, request.DeviceLabel, request.Otp);
        if (success)
        {
            return Ok(new { Message = "MFA Device added successfully" });
        }
        return BadRequest(new { Message = "Invalid OTP code" });
    }

    /// <summary>
    /// Retrieves a list of all MFA devices currently linked to the authenticated user's account.
    /// </summary>
    [HttpGet("devices")]
    public async Task<IActionResult> GetDevices()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

        var devices = await _mfaService.GetUserMfaDevicesAsync(userId);
        var result = devices.Select(d => new
        {
            d.Id,
            d.DeviceLabel,
            d.DateAdded
        });
        return Ok(result);
    }

    /// <summary>
    /// Deletes a specific MFA device from the user's account.
    /// </summary>
    /// <param name="id">The unique identifier of the MFA device.</param>
    [HttpDelete("devices/{id}")]
    public async Task<IActionResult> RemoveDevice(Guid id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

        await _mfaService.RemoveMfaDeviceAsync(userId, id);
        return NoContent();
    }
}

public class MfaSetupVerifyRequest
{
    public string SecretKey { get; set; }
    public string DeviceLabel { get; set; }
    public string Otp { get; set; }
}
