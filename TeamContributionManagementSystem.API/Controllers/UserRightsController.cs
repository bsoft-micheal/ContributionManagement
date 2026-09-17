using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/user-rights")]
public class UserRightsController : ControllerBase
{
    private readonly IRoleRightsService _roleRightsService;

    public UserRightsController(IRoleRightsService roleRightsService)
    {
        _roleRightsService = roleRightsService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RoleRightDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _roleRightsService.GetAllAsync(cancellationToken));

    [HttpGet("{roleName}")]
    public async Task<ActionResult<IReadOnlyCollection<RoleRightDto>>> GetByRole(string roleName, CancellationToken cancellationToken)
        => Ok(await _roleRightsService.GetByRoleAsync(roleName, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] UpdateRoleRightsRequestDto request, CancellationToken cancellationToken)
    {
        await _roleRightsService.SaveRoleRightsAsync(request, cancellationToken);
        return NoContent();
    }
}
