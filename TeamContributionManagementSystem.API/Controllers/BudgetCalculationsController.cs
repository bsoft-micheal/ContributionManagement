using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.BudgetCalculations;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages support data items and unit rates used for event budget calculations.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/budget-calculations")]
public class BudgetCalculationsController : ControllerBase
{
    private readonly IBudgetCalculationService _budgetCalculationService;

    public BudgetCalculationsController(IBudgetCalculationService budgetCalculationService)
    {
        _budgetCalculationService = budgetCalculationService;
    }

    /// <summary>
    /// Retrieves all budget calculation items and rates.
    /// </summary>
    [HttpGet("getAllBudgetCalculationAsync")]
    [ActionName("GetAllBudgetCalculationAsync")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<BudgetCalculationDto>>>> GetAllBudgetCalculationAsync(CancellationToken cancellationToken)
    {
        var result = await _budgetCalculationService.GetAllBudgetCalculationAsync(cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<BudgetCalculationDto>>.SuccessResult(result, CommonMessages.BudgetCalculations.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves a budget calculation item by ID.
    /// </summary>
    [HttpGet("getBudgetCalculationAsyncById/{id:guid}")]
    [ActionName("GetBudgetCalculationAsyncById")]
    public async Task<ActionResult<ApiResponse<BudgetCalculationDto>>> GetBudgetCalculationAsyncById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _budgetCalculationService.GetBudgetCalculationAsyncById(id, cancellationToken);
        if (result == null)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<BudgetCalculationDto>.FailureResult(CommonMessages.General.NotFound, CommonStatusCodes.Status404NotFound));
        }
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<BudgetCalculationDto>.SuccessResult(result, CommonMessages.BudgetCalculations.GetByIdSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Creates a new budget calculation item (Admin only).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("saveBudgetCalculationAsync")]
    [ActionName("SaveBudgetCalculationAsync")]
    public async Task<ActionResult<ApiResponse<BudgetCalculationDto>>> SaveBudgetCalculationAsync([FromBody] CreateBudgetCalculationRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _budgetCalculationService.SaveBudgetCalculationAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<BudgetCalculationDto>.SuccessResult(result, CommonMessages.BudgetCalculations.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Updates an existing budget calculation item (Admin only).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("updateBudgetCalculationAsyncById/{id:guid}")]
    [ActionName("UpdateBudgetCalculationAsyncById")]
    public async Task<ActionResult<ApiResponse<BudgetCalculationDto>>> UpdateBudgetCalculationAsyncById(Guid id, [FromBody] UpdateBudgetCalculationRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _budgetCalculationService.UpdateBudgetCalculationAsyncById(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<BudgetCalculationDto>.SuccessResult(result, CommonMessages.BudgetCalculations.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes a budget calculation item (Admin only).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("deleteBudgetCalculationAsyncById/{id:guid}")]
    [ActionName("DeleteBudgetCalculationAsyncById")]
    public async Task<ActionResult<ApiResponse>> DeleteBudgetCalculationAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _budgetCalculationService.DeleteBudgetCalculationAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.BudgetCalculations.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
