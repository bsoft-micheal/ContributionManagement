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
[Route(CommonRoutes.Users.Base)]
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
    [HttpGet(CommonRoutes.Users.GetAll)]
    [ActionName(nameof(GetAllUserAsync))]
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
    [HttpPost(CommonRoutes.Users.Create)]
    [ActionName(nameof(SaveUserAsync))]
    public async Task<ActionResult<ApiResponse<UserDto>>> SaveUserAsync([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var user = await _userService.SaveUserAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<UserDto>.SuccessResult(user, CommonMessages.Users.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Processes a bulk import of multiple user accounts.
    /// </summary>
    /// <param name="jsonElement">A JSON array containing user details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost(CommonRoutes.Users.SaveBulk)]
    [ActionName(nameof(SaveBulkUserAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UserDto>>>> SaveBulkUserAsync([FromBody] System.Text.Json.JsonElement jsonElement, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
            var rawJson = jsonElement.GetRawText();
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var requests = System.Text.Json.JsonSerializer.Deserialize<List<CreateUserRequestDto>>(rawJson, options);
            
            if (requests == null || requests.Count == 0)
            {
                return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<UserDto>>.FailureResult(CommonMessages.General.NullOrEmptyRequestList, CommonStatusCodes.Status400BadRequest));
            }

            var created = new List<UserDto>();
            foreach (var req in requests)
            {
                if (req == null)
                {
                    return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<UserDto>>.FailureResult(CommonMessages.General.NullRequestItem, CommonStatusCodes.Status400BadRequest));
                }
                created.Add(await _userService.SaveUserAsync(req, currentUser, cancellationToken));
            }
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<UserDto>>.SuccessResult(created, CommonMessages.Users.SaveBulkSuccess, CommonStatusCodes.Status200OK));
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<UserDto>>.FailureResult($"{CommonMessages.General.DeserializationFailed}: {jsonEx.Message}", CommonStatusCodes.Status400BadRequest));
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
    [HttpPut(CommonRoutes.Users.Update)]
    [ActionName(nameof(UpdateUserAsyncById))]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUserAsyncById(Guid id, [FromBody] UpdateUserRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _userService.UpdateUserAsyncById(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<UserDto>.SuccessResult(result, CommonMessages.Users.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves the profile of the currently authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(CommonRoutes.Users.GetProfile)]
    [ActionName(nameof(GetProfileAsync))]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetProfileAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException(CommonMessages.General.UserIdentityNotAvailable);

        if (!Guid.TryParse(userIdClaim, out var userId))
            return StatusCode(CommonStatusCodes.Status401Unauthorized, ApiResponse<UserDto>.FailureResult(CommonMessages.General.Unauthorized, CommonStatusCodes.Status401Unauthorized));

        var user = await _userService.GetProfileAsync(userId, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<UserDto>.SuccessResult(user, CommonMessages.Users.GetProfileSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Updates the profile of the currently authenticated user.
    /// </summary>
    /// <param name="request">The updated profile details (name, settings, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut(CommonRoutes.Users.UpdateProfile)]
    [ActionName(nameof(UpdateProfileAsync))]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfileAsync([FromBody] UpdateProfileRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException(CommonMessages.General.UserIdentityNotAvailable);

        if (!Guid.TryParse(userIdClaim, out var userId))
            return StatusCode(CommonStatusCodes.Status401Unauthorized, ApiResponse<UserDto>.FailureResult(CommonMessages.General.Unauthorized, CommonStatusCodes.Status401Unauthorized));

        var updatedUser = await _userService.UpdateProfileAsync(userId, request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<UserDto>.SuccessResult(updatedUser, CommonMessages.Users.UpdateProfileSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes a user account from the system.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete(CommonRoutes.Users.Delete)]
    [ActionName(nameof(DeleteUserAsyncById))]
    public async Task<ActionResult<ApiResponse>> DeleteUserAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _userService.DeleteUserAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Users.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
