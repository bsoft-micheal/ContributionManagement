using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/members")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;

    public MembersController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MemberDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _memberService.GetAllAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<MemberDto>> Create([FromBody] CreateMemberRequestDto request, CancellationToken cancellationToken)
    {
        var member = await _memberService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = member.MemberId }, member);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MemberDto>> Update(Guid id, [FromBody] UpdateMemberRequestDto request, CancellationToken cancellationToken)
        => Ok(await _memberService.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _memberService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
