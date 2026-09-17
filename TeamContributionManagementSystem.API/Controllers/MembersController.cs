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
