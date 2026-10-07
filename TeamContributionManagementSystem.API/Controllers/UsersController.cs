using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Services;

using TeamContributionManagementSystem.Application.DTOs.Auth;
using TeamContributionManagementSystem.Application.Interfaces.Auth;

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
    private readonly IAuthService _authService;

    public UsersController(IUserManagementService userService, IAuthService authService)
    {
        _userService = userService;
        _authService = authService;
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
            for (int i = 0; i < requests.Count; i++)
            {
                var req = requests[i];
                if (req == null)
                {
                    return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<UserDto>>.FailureResult($"Row {i + 1}: {CommonMessages.General.NullRequestItem}", CommonStatusCodes.Status400BadRequest));
                }

                try
                {
                    created.Add(await _userService.SaveUserAsync(req, currentUser, cancellationToken));
                }
                catch (InvalidOperationException ex)
                {
                    return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<UserDto>>.FailureResult($"Row {i + 1} ({req.FullName ?? req.Email}): {ex.Message}", CommonStatusCodes.Status400BadRequest));
                }
                catch (Exception ex)
                {
                    var msg = ex.InnerException?.Message ?? ex.Message;
                    if (msg.Contains("users_email_key", StringComparison.OrdinalIgnoreCase) || (msg.Contains("email", StringComparison.OrdinalIgnoreCase) && msg.Contains("unique", StringComparison.OrdinalIgnoreCase)))
                    {
                        return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<UserDto>>.FailureResult($"Row {i + 1}: Email '{req.Email}' already exists in the database.", CommonStatusCodes.Status400BadRequest));
                    }
                    if (msg.Contains("users_username_key", StringComparison.OrdinalIgnoreCase) || (msg.Contains("username", StringComparison.OrdinalIgnoreCase) && msg.Contains("unique", StringComparison.OrdinalIgnoreCase)))
                    {
                        return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<UserDto>>.FailureResult($"Row {i + 1}: Username '{req.Username ?? req.Email.Split('@')[0]}' already exists in the database.", CommonStatusCodes.Status400BadRequest));
                    }
                    if (msg.Contains("users_phone_key", StringComparison.OrdinalIgnoreCase) || (msg.Contains("phone", StringComparison.OrdinalIgnoreCase) && msg.Contains("unique", StringComparison.OrdinalIgnoreCase)))
                    {
                        return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<UserDto>>.FailureResult($"Row {i + 1}: Phone number '{req.Phone}' already exists in the database.", CommonStatusCodes.Status400BadRequest));
                    }
                    return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<IReadOnlyCollection<UserDto>>.FailureResult($"Row {i + 1} ({req.FullName ?? req.Email}): {msg}", CommonStatusCodes.Status400BadRequest));
                }
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

    /// <summary>
    /// Switches the authenticated user's active role.
    /// </summary>
    /// <param name="request">The target role identifier or name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ApiResponse containing updated AuthResponseDto.</returns>
    [HttpPost(CommonRoutes.Users.SwitchRole)]
    [ActionName(nameof(SwitchRoleAsync))]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> SwitchRoleAsync([FromBody] SwitchRoleRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) 
                ?? User.FindFirstValue(ClaimTypes.Name) 
                ?? User.Identity?.Name 
                ?? string.Empty;

            var response = await _authService.SwitchRoleAsync(request, userEmail, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<AuthResponseDto>.SuccessResult(response, "Role switched successfully", CommonStatusCodes.Status200OK));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<AuthResponseDto>.FailureResult(ex.Message, CommonStatusCodes.Status400BadRequest));
        }
    }
}
