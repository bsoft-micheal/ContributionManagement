using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _userService.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = user.UserId }, user);
    }

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

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserRequestDto request, CancellationToken cancellationToken)
        => Ok(await _userService.UpdateAsync(id, request, cancellationToken));

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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _userService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
