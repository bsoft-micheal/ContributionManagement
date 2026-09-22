using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Payments;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentTransactionService _transactionService;

    public PaymentsController(IPaymentTransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet("getAllPaymentAsync")]
    [ActionName("GetAllPaymentAsync")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PaymentTransactionDto>>>> GetAllPaymentAsync(
        [FromQuery] string? eventName,
        [FromQuery] string? mode,
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var result = await _transactionService.GetAllPaymentAsync(eventName, mode, status, startDate, endDate, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<PaymentTransactionDto>>.SuccessResult(result, CommonMessages.Payments.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    [HttpGet("getPaymentAsyncById/{id:guid}")]
    [ActionName("GetPaymentAsyncById")]
    public async Task<ActionResult<ApiResponse<PaymentTransactionDto>>> GetPaymentAsyncById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _transactionService.GetPaymentAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<PaymentTransactionDto>.SuccessResult(result, CommonMessages.Payments.GetByIdSuccess, CommonStatusCodes.Status200OK));
    }

    [HttpPost("savePaymentAsync")]
    [ActionName("SavePaymentAsync")]
    public async Task<ActionResult<ApiResponse<PaymentTransactionDto>>> SavePaymentAsync([FromBody] CreatePaymentTransactionRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "User";
        var result = await _transactionService.SavePaymentAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<PaymentTransactionDto>.SuccessResult(result, CommonMessages.Payments.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("verifyPaymentAsync/{id:guid}")]
    [ActionName("VerifyPaymentAsync")]
    public async Task<ActionResult<ApiResponse<PaymentTransactionDto>>> VerifyPaymentAsync(Guid id, [FromBody] VerifyPaymentRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Admin";
        var result = await _transactionService.VerifyPaymentAsync(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<PaymentTransactionDto>.SuccessResult(result, CommonMessages.Payments.VerifySuccess, CommonStatusCodes.Status200OK));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("deletePaymentAsyncById/{id:guid}")]
    [ActionName("DeletePaymentAsyncById")]
    public async Task<ActionResult<ApiResponse>> DeletePaymentAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _transactionService.DeletePaymentAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Payments.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
