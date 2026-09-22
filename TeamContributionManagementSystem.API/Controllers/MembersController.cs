using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages user member profiles and their assigned roles within the organization.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/members")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;

    public MembersController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    /// <summary>
    /// Retrieves a list of all active members.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MemberDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _memberService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Creates a single new member record (Admin only).
    /// </summary>
    /// <param name="request">The member details.</param>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<MemberDto>> Create([FromBody] CreateMemberRequestDto request, CancellationToken cancellationToken)
    {
        var member = await _memberService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = member.MemberId }, member);
    }

    /// <summary>
    /// Processes a bulk import of multiple members at once (Admin only).
    /// </summary>
    /// <param name="jsonElement">A JSON array containing member details.</param>
    [Authorize(Roles = "Admin")]
    [HttpPost("bulk")]
    public async Task<ActionResult> CreateBulk([FromBody] System.Text.Json.JsonElement jsonElement, CancellationToken cancellationToken)
    {
        try
        {
            var rawJson = jsonElement.GetRawText();
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var requests = System.Text.Json.JsonSerializer.Deserialize<List<CreateMemberRequestDto>>(rawJson, options);
            
            if (requests == null || requests.Count == 0)
            {
                return BadRequest($"The request body deserialized to null or empty list. Raw JSON: {rawJson}");
            }

            var created = new List<MemberDto>();
            foreach (var req in requests)
            {
                if (req == null)
                {
                    return BadRequest("One of the member request items is null.");
                }
                created.Add(await _memberService.CreateAsync(req, cancellationToken));
            }
            return Ok(created);
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            return BadRequest($"JSON deserialization failed: {jsonEx.Message}. Raw JSON received: {jsonElement.GetRawText()}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal bulk error: {ex.Message}. StackTrace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Updates an existing member's information (Admin only).
    /// </summary>
    /// <param name="id">The unique identifier of the member.</param>
    /// <param name="request">The updated member details.</param>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MemberDto>> Update(Guid id, [FromBody] UpdateMemberRequestDto request, CancellationToken cancellationToken)
        => Ok(await _memberService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Deletes a member profile (Admin only).
    /// </summary>
    /// <param name="id">The unique identifier of the member to delete.</param>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _memberService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
