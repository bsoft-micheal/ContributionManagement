using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Roles;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<RoleDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _roleService.GetAllAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleRequestDto request, CancellationToken cancellationToken)
        => Ok(await _roleService.CreateAsync(request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoleDto>> Update(Guid id, [FromBody] UpdateRoleRequestDto request, CancellationToken cancellationToken)
        => Ok(await _roleService.UpdateAsync(id, request, cancellationToken));
}
