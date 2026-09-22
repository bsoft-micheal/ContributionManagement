using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages system administrators and other authenticated users.
/// </summary>
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
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _userService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="request">The new user details.</param>
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = user.UserId }, user);
    }

    /// <summary>
    /// Processes a bulk import of multiple user accounts.
    /// </summary>
    /// <param name="jsonElement">A JSON array containing user details.</param>
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
            var requests = System.Text.Json.JsonSerializer.Deserialize<List<CreateUserRequestDto>>(rawJson, options);
            
            if (requests == null || requests.Count == 0)
            {
                return BadRequest($"The request body deserialized to null or empty list. Raw JSON: {rawJson}");
            }

            var created = new List<UserDto>();
            foreach (var req in requests)
            {
                if (req == null)
                {
                    return BadRequest("One of the user request items is null.");
                }
                created.Add(await _userService.CreateAsync(req, cancellationToken));
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
    /// Updates an existing user's information.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="request">The updated user details.</param>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserRequestDto request, CancellationToken cancellationToken)
        => Ok(await _userService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Updates the profile of the currently authenticated user.
    /// </summary>
    /// <param name="request">The updated profile details (name, settings, etc.).</param>
    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity is not available.");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var updatedUser = await _userService.UpdateProfileAsync(userId, request, cancellationToken);
        return Ok(updatedUser);
    }

    /// <summary>
    /// Deletes a user account from the system.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _userService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
