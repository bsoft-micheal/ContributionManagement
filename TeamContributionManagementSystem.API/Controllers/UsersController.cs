using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages system administrators and other authenticated users.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userService;

    public UsersController(IUserManagementService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Retrieves a list of all registered users.
    /// </summary>
    [HttpGet("getAllUserAsync")]
    [ActionName("GetAllUserAsync")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UserDto>>>> GetAllUserAsync(CancellationToken cancellationToken)
    {
        var result = await _userService.GetAllUserAsync(cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<UserDto>>.SuccessResult(result, CommonMessages.Users.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="request">The new user details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("saveUserAsync")]
    [ActionName("SaveUserAsync")]
    public async Task<ActionResult<ApiResponse<UserDto>>> SaveUserAsync([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userService.SaveUserAsync(request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<UserDto>.SuccessResult(user, CommonMessages.Users.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Processes a bulk import of multiple user accounts.
    /// </summary>
    /// <param name="jsonElement">A JSON array containing user details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("saveBulkUserAsync")]
    [ActionName("SaveBulkUserAsync")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UserDto>>>> SaveBulkUserAsync([FromBody] System.Text.Json.JsonElement jsonElement, CancellationToken cancellationToken)
    {
        try
        {
            var rawJson = jsonElement.GetRawText();
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var requests = System.Text.Json.JsonSerializer.Deserialize<List<CreateUserRequestDto>>(rawJson, options);
            
            if (requests == null || requests.Count == 0)
            {
                return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<UserDto>>.FailureResult("The request body deserialized to null or empty list.", CommonStatusCodes.Status400BadRequest));
            }

            var created = new List<UserDto>();
            foreach (var req in requests)
            {
                if (req == null)
                {
                    return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<UserDto>>.FailureResult("One of the user request items is null.", CommonStatusCodes.Status400BadRequest));
                }
                created.Add(await _userService.SaveUserAsync(req, cancellationToken));
            }
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<UserDto>>.SuccessResult(created, CommonMessages.Users.SaveBulkSuccess, CommonStatusCodes.Status200OK));
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<UserDto>>.FailureResult($"JSON deserialization failed: {jsonEx.Message}", CommonStatusCodes.Status400BadRequest));
        }
        catch (Exception ex)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<IReadOnlyCollection<UserDto>>.FailureResult(ex.Message, CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Updates an existing user's information.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="request">The updated user details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("updateUserAsyncById/{id:guid}")]
    [ActionName("UpdateUserAsyncById")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUserAsyncById(Guid id, [FromBody] UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserAsyncById(id, request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<UserDto>.SuccessResult(result, CommonMessages.Users.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves the profile of the currently authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("getProfileAsync")]
    [ActionName("GetProfileAsync")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetProfileAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity is not available.");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return StatusCode(CommonStatusCodes.Status401Unauthorized, ApiResponse<UserDto>.FailureResult("Unauthorized", CommonStatusCodes.Status401Unauthorized));

        var user = await _userService.GetProfileAsync(userId, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<UserDto>.SuccessResult(user, CommonMessages.Users.GetProfileSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Updates the profile of the currently authenticated user.
    /// </summary>
    /// <param name="request">The updated profile details (name, settings, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("updateProfileAsync")]
    [ActionName("UpdateProfileAsync")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfileAsync([FromBody] UpdateProfileRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity is not available.");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return StatusCode(CommonStatusCodes.Status401Unauthorized, ApiResponse<UserDto>.FailureResult("Unauthorized", CommonStatusCodes.Status401Unauthorized));

        var updatedUser = await _userService.UpdateProfileAsync(userId, request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<UserDto>.SuccessResult(updatedUser, CommonMessages.Users.UpdateProfileSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes a user account from the system.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("deleteUserAsyncById/{id:guid}")]
    [ActionName("DeleteUserAsyncById")]
    public async Task<ActionResult<ApiResponse>> DeleteUserAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _userService.DeleteUserAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Users.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
