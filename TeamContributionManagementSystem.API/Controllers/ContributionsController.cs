using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/contributions")]
public class ContributionsController : ControllerBase
{
    private readonly IContributionService _contributionService;

    public ContributionsController(IContributionService contributionService)
    {
        _contributionService = contributionService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ContributionDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _contributionService.GetAllAsync(cancellationToken));

    [HttpGet("event/{eventId:guid}")]
    public async Task<ActionResult<IReadOnlyCollection<ContributionDto>>> GetByEvent(Guid eventId, CancellationToken cancellationToken)
        => Ok(await _contributionService.GetByEventIdAsync(eventId, cancellationToken));

    [HttpPost("pay")]
    public async Task<ActionResult<ContributionDto>> Pay([FromBody] PayContributionRequestDto request, CancellationToken cancellationToken)
        => Ok(await _contributionService.PayAsync(request, cancellationToken));
}
