using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Expenses;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/expenses")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpGet("getAllExpenseAsync")]
    [ActionName("GetAllExpenseAsync")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ExpenseDto>>>> GetAllExpenseAsync(
        [FromQuery] string? eventName,
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetAllExpenseAsync(eventName, category, status, startDate, endDate, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<ExpenseDto>>.SuccessResult(result, CommonMessages.Expenses.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    [HttpGet("getExpenseAsyncById/{id:guid}")]
    [ActionName("GetExpenseAsyncById")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> GetExpenseAsyncById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _expenseService.GetExpenseAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<ExpenseDto>.SuccessResult(result, CommonMessages.Expenses.GetByIdSuccess, CommonStatusCodes.Status200OK));
    }

    [HttpPost("saveExpenseAsync")]
    [ActionName("SaveExpenseAsync")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> SaveExpenseAsync([FromBody] CreateExpenseRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "User";
        var result = await _expenseService.SaveExpenseAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<ExpenseDto>.SuccessResult(result, CommonMessages.Expenses.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    [HttpPut("updateExpenseAsyncById/{id:guid}")]
    [ActionName("UpdateExpenseAsyncById")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> UpdateExpenseAsyncById(Guid id, [FromBody] UpdateExpenseRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "User";
        var result = await _expenseService.UpdateExpenseAsyncById(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<ExpenseDto>.SuccessResult(result, CommonMessages.Expenses.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    [HttpDelete("deleteExpenseAsyncById/{id:guid}")]
    [ActionName("DeleteExpenseAsyncById")]
    public async Task<ActionResult<ApiResponse>> DeleteExpenseAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _expenseService.DeleteExpenseAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Expenses.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
