using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages granular access rights assigned to specific roles.
/// </summary>
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

    /// <summary>
    /// Retrieves a list of all rights mapped to roles.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RoleRightDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _roleRightsService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Retrieves the access rights assigned to a specific role.
    /// </summary>
    /// <param name="roleName">The name of the role (e.g., 'Admin').</param>
    [HttpGet("{roleName}")]
    public async Task<ActionResult<IReadOnlyCollection<RoleRightDto>>> GetByRole(string roleName, CancellationToken cancellationToken)
        => Ok(await _roleRightsService.GetByRoleAsync(roleName, cancellationToken));

    /// <summary>
    /// Saves or updates the granular access rights for a specific role.
    /// </summary>
    /// <param name="request">The role and its new rights configuration.</param>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] UpdateRoleRightsRequestDto request, CancellationToken cancellationToken)
    {
        await _roleRightsService.SaveRoleRightsAsync(request, cancellationToken);
        return NoContent();
    }
}
