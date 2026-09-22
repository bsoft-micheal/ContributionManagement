using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [HttpGet]
    public async Task<ActionResult<SystemSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var settings = await _settingService.GetSettingsAsync(cancellationToken);
        return Ok(settings);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<SystemSettingsDto>> Update([FromBody] SystemSettingsDto settings, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Admin";
        var result = await _settingService.UpdateSettingsAsync(settings, currentUser, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("reset")]
    public async Task<ActionResult<SystemSettingsDto>> Reset(CancellationToken cancellationToken)
    {
        var result = await _settingService.ResetSettingsAsync(cancellationToken);
        return Ok(result);
    }
}
