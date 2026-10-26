using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.ExitClearance;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages member exit clearance queue, live dues verification, admin approval, and exit history.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.ExitClearance.Base)]
public class ExitClearanceController : ControllerBase
{
    private readonly IExitClearanceService _clearanceService;

    public ExitClearanceController(IExitClearanceService clearanceService)
    {
        _clearanceService = clearanceService;
    }

    /// <summary>
    /// Retrieves the member exit clearance queue with dynamic KPIs and filtering.
    /// </summary>
    [HttpGet(CommonRoutes.ExitClearance.GetQueue)]
    [HttpGet(CommonRoutes.ExitClearance.GetQueueNamed)]
    public async Task<ActionResult<ApiResponse<ExitClearanceQueueResponseDto>>> GetQueueAsync(
        [FromQuery] string? search,
        [FromQuery] Guid? eventId,
        [FromQuery] string? clearanceStatus,
        [FromQuery] string? paymentStatus,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _clearanceService.GetQueueAsync(search, eventId, clearanceStatus, paymentStatus, fromDate, toDate, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<ExitClearanceQueueResponseDto>.SuccessResult(result, "Exit clearance queue retrieved successfully.", CommonStatusCodes.Status200OK));
        }
        catch (Exception ex)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<ExitClearanceQueueResponseDto>.FailureResult(ex.Message, CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Retrieves live event-wise dues obligations and payment history for a specific member's exit clearance.
    /// </summary>
    [HttpGet(CommonRoutes.ExitClearance.GetDues)]
    public async Task<ActionResult<ApiResponse<ExitClearanceDuesDetailDto>>> GetDuesDetailAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _clearanceService.GetDuesDetailAsync(id, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<ExitClearanceDuesDetailDto>.SuccessResult(result, "Member dues details retrieved successfully.", CommonStatusCodes.Status200OK));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<ExitClearanceDuesDetailDto>.FailureResult(ex.Message, CommonStatusCodes.Status404NotFound));
        }
        catch (Exception ex)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<ExitClearanceDuesDetailDto>.FailureResult(ex.Message, CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Retrieves a single exit clearance record by ID.
    /// </summary>
    [HttpGet(CommonRoutes.ExitClearance.GetDetails)]
    public async Task<ActionResult<ApiResponse<ExitClearanceDuesDetailDto>>> GetByIdAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        return await GetDuesDetailAsync(id, cancellationToken);
    }

    /// <summary>
    /// Completes final exit clearance approval for a member. Restricted to Administrators.
    /// Validates zero outstanding dues and verified payments.
    /// </summary>
    [HttpPost(CommonRoutes.ExitClearance.Approve)]
    public async Task<ActionResult<ApiResponse<ExitClearanceSummaryDto>>> ApproveExitAsync(
        [FromRoute] Guid id,
        [FromBody] ApproveExitRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _clearanceService.ApproveExitAsync(id, request, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<ExitClearanceSummaryDto>.SuccessResult(result, "Member exit clearance approved successfully.", CommonStatusCodes.Status200OK));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(CommonStatusCodes.Status403Forbidden, ApiResponse<ExitClearanceSummaryDto>.FailureResult(ex.Message, CommonStatusCodes.Status403Forbidden));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<ExitClearanceSummaryDto>.FailureResult(ex.Message, CommonStatusCodes.Status404NotFound));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<ExitClearanceSummaryDto>.FailureResult(ex.Message, CommonStatusCodes.Status400BadRequest));
        }
        catch (Exception ex)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<ExitClearanceSummaryDto>.FailureResult(ex.Message, CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Retrieves permanent exit clearance history of approved member exits.
    /// </summary>
    [HttpGet(CommonRoutes.ExitClearance.GetHistory)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ExitClearanceHistoryDto>>>> GetHistoryAsync(
        [FromQuery] string? search,
        [FromQuery] Guid? eventId,
        [FromQuery] Guid? approvedBy,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _clearanceService.GetHistoryAsync(search, eventId, approvedBy, fromDate, toDate, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<ExitClearanceHistoryDto>>.SuccessResult(result, "Exit clearance history retrieved successfully.", CommonStatusCodes.Status200OK));
        }
        catch (Exception ex)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<IReadOnlyCollection<ExitClearanceHistoryDto>>.FailureResult(ex.Message, CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Retrieves the permanent snapshot and audit details of an approved exit clearance history record.
    /// </summary>
    [HttpGet(CommonRoutes.ExitClearance.GetHistoryDetail)]
    public async Task<ActionResult<ApiResponse<ExitClearanceHistoryDto>>> GetHistoryDetailAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _clearanceService.GetHistoryDetailAsync(id, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<ExitClearanceHistoryDto>.SuccessResult(result, "Exit clearance history detail retrieved successfully.", CommonStatusCodes.Status200OK));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<ExitClearanceHistoryDto>.FailureResult(ex.Message, CommonStatusCodes.Status404NotFound));
        }
        catch (Exception ex)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<ExitClearanceHistoryDto>.FailureResult(ex.Message, CommonStatusCodes.Status500InternalServerError));
        }
    }
}
