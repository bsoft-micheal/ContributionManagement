using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Settings;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISystemSettingService _settingService;

    public SettingsController(ISystemSettingService settingService)
    {
        _settingService = settingService;
    }

    [HttpGet("getSettingAsync")]
    [ActionName("GetSettingAsync")]
    public async Task<ActionResult<ApiResponse<SystemSettingsDto>>> GetSettingAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingService.GetSettingAsync(cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SystemSettingsDto>.SuccessResult(settings, CommonMessages.Settings.GetSuccess, CommonStatusCodes.Status200OK));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("updateSettingAsync")]
    [ActionName("UpdateSettingAsync")]
    public async Task<ActionResult<ApiResponse<SystemSettingsDto>>> UpdateSettingAsync([FromBody] SystemSettingsDto settings, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Admin";
        var result = await _settingService.UpdateSettingAsync(settings, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SystemSettingsDto>.SuccessResult(result, CommonMessages.Settings.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("resetSettingAsync")]
    [ActionName("ResetSettingAsync")]
    public async Task<ActionResult<ApiResponse<SystemSettingsDto>>> ResetSettingAsync(CancellationToken cancellationToken)
    {
        var result = await _settingService.ResetSettingAsync(cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SystemSettingsDto>.SuccessResult(result, CommonMessages.Settings.ResetSuccess, CommonStatusCodes.Status200OK));
    }
}
