using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Settings;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages application-wide settings and system configurations.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.Settings.Base)]
public class SettingsController : ControllerBase
{
    private readonly ISystemSettingService _settingService;

    public SettingsController(ISystemSettingService settingService)
    {
        _settingService = settingService;
    }

    /// <summary>
    /// Retrieves current system settings.
    /// </summary>
    [HttpGet(CommonRoutes.Settings.Get)]
    [ActionName(nameof(GetSettingAsync))]
    public async Task<ActionResult<ApiResponse<SystemSettingsDto>>> GetSettingAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingService.GetSettingAsync(cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SystemSettingsDto>.SuccessResult(settings, CommonMessages.Settings.GetSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Updates system settings (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPost(CommonRoutes.Settings.Update)]
    [ActionName(nameof(UpdateSettingAsync))]
    public async Task<ActionResult<ApiResponse<SystemSettingsDto>>> UpdateSettingAsync([FromBody] SystemSettingsDto settings, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _settingService.UpdateSettingAsync(settings, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SystemSettingsDto>.SuccessResult(result, CommonMessages.Settings.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Resets system settings back to default values (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPost(CommonRoutes.Settings.Reset)]
    [ActionName(nameof(ResetSettingAsync))]
    public async Task<ActionResult<ApiResponse<SystemSettingsDto>>> ResetSettingAsync(CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _settingService.ResetSettingAsync(currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SystemSettingsDto>.SuccessResult(result, CommonMessages.Settings.ResetSuccess, CommonStatusCodes.Status200OK));
    }
}
