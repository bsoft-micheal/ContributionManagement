using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Roles;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages system roles (e.g., Admin, User) that dictate user permissions.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    /// Retrieves a list of all defined roles in the system.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RoleDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _roleService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <param name="request">The details of the role to create.</param>
    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleRequestDto request, CancellationToken cancellationToken)
        => Ok(await _roleService.CreateAsync(request, cancellationToken));

    /// <summary>
    /// Updates the name or description of an existing role.
    /// </summary>
    /// <param name="id">The unique identifier of the role.</param>
    /// <param name="request">The updated role details.</param>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoleDto>> Update(Guid id, [FromBody] UpdateRoleRequestDto request, CancellationToken cancellationToken)
        => Ok(await _roleService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Deletes a role from the system.
    /// </summary>
    /// <param name="id">The unique identifier of the role to delete.</param>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _roleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
