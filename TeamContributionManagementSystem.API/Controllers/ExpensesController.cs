using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Expenses;
using TeamContributionManagementSystem.Application.Exceptions;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages event expenses and vendor payment tracking.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.Expenses.Base)]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Retrieves a list of all recorded expenses.
    /// </summary>
    [HttpGet(CommonRoutes.Expenses.GetAll)]
    [ActionName(nameof(GetAllExpenseAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ExpenseDto>>>> GetAllExpenseAsync(
        [FromQuery] string? eventName,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _expenseService.GetAllExpenseAsync(eventName, category, status, startDate, endDate, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<ExpenseDto>>.SuccessResult(result, CommonMessages.Expenses.GetAllSuccess, CommonStatusCodes.Status200OK));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<IReadOnlyCollection<ExpenseDto>>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Retrieves an expense by ID.
    /// </summary>
    [HttpGet(CommonRoutes.Expenses.GetById)]
    [ActionName(nameof(GetExpenseAsyncById))]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> GetExpenseAsyncById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _expenseService.GetExpenseAsyncById(id, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<ExpenseDto>.SuccessResult(result, CommonMessages.Expenses.GetByIdSuccess, CommonStatusCodes.Status200OK));
        }
        catch (RecordDeletedException ex)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<ExpenseDto>.FailureResult(ex.Message, CommonStatusCodes.Status404NotFound));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<ExpenseDto>.FailureResult("This record no longer exists. Please refresh the page.", CommonStatusCodes.Status404NotFound));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<ExpenseDto>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Creates a new expense record.
    /// </summary>
    [HttpPost(CommonRoutes.Expenses.Create)]
    [ActionName(nameof(SaveExpenseAsync))]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> SaveExpenseAsync([FromBody] CreateExpenseRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
            var result = await _expenseService.SaveExpenseAsync(request, currentUser, cancellationToken);
            return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<ExpenseDto>.SuccessResult(result, CommonMessages.Expenses.SaveSuccess, CommonStatusCodes.Status201Created));
        }
        catch (AlreadyProcessedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse<ExpenseDto>.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<ExpenseDto>.FailureResult(ex.Message, CommonStatusCodes.Status400BadRequest));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<ExpenseDto>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Updates an existing expense record.
    /// </summary>
    [HttpPut(CommonRoutes.Expenses.Update)]
    [ActionName(nameof(UpdateExpenseAsyncById))]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> UpdateExpenseAsyncById(Guid id, [FromBody] UpdateExpenseRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
            var result = await _expenseService.UpdateExpenseAsyncById(id, request, currentUser, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<ExpenseDto>.SuccessResult(result, CommonMessages.Expenses.UpdateSuccess, CommonStatusCodes.Status200OK));
        }
        catch (RecordDeletedException ex)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<ExpenseDto>.FailureResult(ex.Message, CommonStatusCodes.Status404NotFound));
        }
        catch (RecordModifiedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse<ExpenseDto>.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (AlreadyProcessedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse<ExpenseDto>.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<ExpenseDto>.FailureResult("This record no longer exists. Please refresh the page.", CommonStatusCodes.Status404NotFound));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<ExpenseDto>.FailureResult(ex.Message, CommonStatusCodes.Status400BadRequest));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<ExpenseDto>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Deletes an expense record.
    /// </summary>
    [HttpDelete(CommonRoutes.Expenses.Delete)]
    [ActionName(nameof(DeleteExpenseAsyncById))]
    public async Task<ActionResult<ApiResponse>> DeleteExpenseAsyncById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _expenseService.DeleteExpenseAsyncById(id, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Expenses.DeleteSuccess, CommonStatusCodes.Status200OK));
        }
        catch (RecordDeletedException ex)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse.FailureResult(ex.Message, CommonStatusCodes.Status404NotFound));
        }
        catch (RecordModifiedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (AlreadyProcessedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse.FailureResult("This record no longer exists. Please refresh the page.", CommonStatusCodes.Status404NotFound));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }
}
