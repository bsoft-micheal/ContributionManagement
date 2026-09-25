using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages user member profiles and their assigned roles within the organization.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route(CommonRoutes.Members.Base)]
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
    [HttpGet(CommonRoutes.Members.GetAll)]
    [ActionName(nameof(GetAllMemberAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MemberDto>>>> GetAllMemberAsync(CancellationToken cancellationToken)
    {
        var result = await _memberService.GetAllMemberAsync(cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<MemberDto>>.SuccessResult(result, CommonMessages.Members.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Creates a single new member record (Admin only).
    /// </summary>
    /// <param name="request">The member details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPost(CommonRoutes.Members.Create)]
    [ActionName(nameof(SaveMemberAsync))]
    public async Task<ActionResult<ApiResponse<MemberDto>>> SaveMemberAsync([FromBody] CreateMemberRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var member = await _memberService.SaveMemberAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<MemberDto>.SuccessResult(member, CommonMessages.Members.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Processes a bulk import of multiple members at once (Admin only).
    /// </summary>
    /// <param name="jsonElement">A JSON array containing member details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPost(CommonRoutes.Members.SaveBulk)]
    [ActionName(nameof(SaveBulkMemberAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MemberDto>>>> SaveBulkMemberAsync([FromBody] System.Text.Json.JsonElement jsonElement, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
            var rawJson = jsonElement.GetRawText();
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var requests = System.Text.Json.JsonSerializer.Deserialize<List<CreateMemberRequestDto>>(rawJson, options);
            
            if (requests == null || requests.Count == 0)
            {
                return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<MemberDto>>.FailureResult(CommonMessages.General.NullOrEmptyRequestList, CommonStatusCodes.Status400BadRequest));
            }

            var created = new List<MemberDto>();
            foreach (var req in requests)
            {
                if (req == null)
                {
                    return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<MemberDto>>.FailureResult(CommonMessages.General.NullRequestItem, CommonStatusCodes.Status400BadRequest));
                }
                created.Add(await _memberService.SaveMemberAsync(req, currentUser, cancellationToken));
            }
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<MemberDto>>.SuccessResult(created, CommonMessages.Members.SaveBulkSuccess, CommonStatusCodes.Status200OK));
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<MemberDto>>.FailureResult($"{CommonMessages.General.DeserializationFailed}: {jsonEx.Message}", CommonStatusCodes.Status400BadRequest));
        }
        catch (Exception ex)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<IReadOnlyCollection<MemberDto>>.FailureResult(ex.Message, CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Updates an existing member's information (Admin only).
    /// </summary>
    /// <param name="id">The unique identifier of the member.</param>
    /// <param name="request">The updated member details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPut(CommonRoutes.Members.Update)]
    [ActionName(nameof(UpdateMemberAsyncById))]
    public async Task<ActionResult<ApiResponse<MemberDto>>> UpdateMemberAsyncById(Guid id, [FromBody] UpdateMemberRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _memberService.UpdateMemberAsyncById(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<MemberDto>.SuccessResult(result, CommonMessages.Members.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes a member profile (Admin only).
    /// </summary>
    /// <param name="id">The unique identifier of the member to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpDelete(CommonRoutes.Members.Delete)]
    [ActionName(nameof(DeleteMemberAsyncById))]
    public async Task<ActionResult<ApiResponse>> DeleteMemberAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _memberService.DeleteMemberAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Members.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
