using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Payments;
using TeamContributionManagementSystem.Application.Exceptions;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages payment transactions, verification, and audit logs.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.Payments.Base)]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentTransactionService _transactionService;

    public PaymentsController(IPaymentTransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    /// <summary>
    /// Retrieves a list of all payment transactions.
    /// </summary>
    [HttpGet(CommonRoutes.Payments.GetAll)]
    [ActionName(nameof(GetAllPaymentAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PaymentTransactionDto>>>> GetAllPaymentAsync(
        [FromQuery] string? eventName,
        [FromQuery] string? mode,
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _transactionService.GetAllPaymentAsync(eventName, mode, status, startDate, endDate, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<PaymentTransactionDto>>.SuccessResult(result, CommonMessages.Payments.GetAllSuccess, CommonStatusCodes.Status200OK));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<IReadOnlyCollection<PaymentTransactionDto>>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Retrieves a payment transaction by ID.
    /// </summary>
    [HttpGet(CommonRoutes.Payments.GetById)]
    [ActionName(nameof(GetPaymentAsyncById))]
    public async Task<ActionResult<ApiResponse<PaymentTransactionDto>>> GetPaymentAsyncById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _transactionService.GetPaymentAsyncById(id, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<PaymentTransactionDto>.SuccessResult(result, CommonMessages.Payments.GetByIdSuccess, CommonStatusCodes.Status200OK));
        }
        catch (RecordDeletedException ex)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<PaymentTransactionDto>.FailureResult(ex.Message, CommonStatusCodes.Status404NotFound));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<PaymentTransactionDto>.FailureResult("This record no longer exists. Please refresh the page.", CommonStatusCodes.Status404NotFound));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<PaymentTransactionDto>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Records a new payment transaction.
    /// </summary>
    [HttpPost(CommonRoutes.Payments.Create)]
    [ActionName(nameof(SavePaymentAsync))]
    public async Task<ActionResult<ApiResponse<PaymentTransactionDto>>> SavePaymentAsync([FromBody] CreatePaymentTransactionRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
            var result = await _transactionService.SavePaymentAsync(request, currentUser, cancellationToken);
            return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<PaymentTransactionDto>.SuccessResult(result, CommonMessages.Payments.SaveSuccess, CommonStatusCodes.Status201Created));
        }
        catch (AlreadyProcessedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse<PaymentTransactionDto>.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<PaymentTransactionDto>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Allows a member to submit payment proof (UTR / Ref no) with optional screenshot (e.g. from email payment link).
    /// </summary>
    [AllowAnonymous]
    [HttpPost(CommonRoutes.Payments.SubmitProof)]
    [ActionName(nameof(SubmitPaymentProofAsync))]
    public async Task<ActionResult<ApiResponse<PaymentTransactionDto>>> SubmitPaymentProofAsync([FromBody] SubmitPaymentProofDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _transactionService.SubmitProofAsync(request, cancellationToken);
            return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<PaymentTransactionDto>.SuccessResult(result, CommonMessages.Payments.SubmitProofSuccess, CommonStatusCodes.Status201Created));
        }
        catch (AlreadyProcessedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse<PaymentTransactionDto>.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<PaymentTransactionDto>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Loads event &amp; member context for the payment confirmation page.
    /// </summary>
    [AllowAnonymous]
    [HttpGet(CommonRoutes.Payments.GetPaymentContext)]
    [ActionName(nameof(GetPaymentContextDetailsAsync))]
    public async Task<ActionResult<ApiResponse<PaymentContextDto>>> GetPaymentContextDetailsAsync(
        [FromQuery] Guid? eventId,
        [FromQuery] Guid? memberId,
        [FromQuery] string? eventName,
        [FromQuery] string? memberName,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _transactionService.GetPaymentContextAsync(eventId, memberId, eventName, memberName, cancellationToken);
            if (result == null)
            {
                return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<PaymentContextDto>.FailureResult(CommonMessages.General.NotFound, CommonStatusCodes.Status404NotFound));
            }
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<PaymentContextDto>.SuccessResult(result, CommonMessages.Payments.GetContextSuccess, CommonStatusCodes.Status200OK));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<PaymentContextDto>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Verifies or approves a pending payment transaction (Admin or Organizer).
    /// </summary>
    [Authorize(Roles = CommonRoles.AdminOrOrganizer)]
    [HttpPut(CommonRoutes.Payments.Verify)]
    [ActionName(nameof(VerifyPaymentAsync))]
    public async Task<ActionResult<ApiResponse<PaymentTransactionDto>>> VerifyPaymentAsync(Guid id, [FromBody] VerifyPaymentRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
            var result = await _transactionService.VerifyPaymentAsync(id, request, currentUser, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<PaymentTransactionDto>.SuccessResult(result, CommonMessages.Payments.VerifySuccess, CommonStatusCodes.Status200OK));
        }
        catch (RecordDeletedException ex)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<PaymentTransactionDto>.FailureResult(ex.Message, CommonStatusCodes.Status404NotFound));
        }
        catch (RecordModifiedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse<PaymentTransactionDto>.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (AlreadyProcessedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse<PaymentTransactionDto>.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<PaymentTransactionDto>.FailureResult("This record no longer exists. Please refresh the page.", CommonStatusCodes.Status404NotFound));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<PaymentTransactionDto>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Deletes a payment transaction (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpDelete(CommonRoutes.Payments.Delete)]
    [ActionName(nameof(DeletePaymentAsyncById))]
    public async Task<ActionResult<ApiResponse>> DeletePaymentAsyncById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _transactionService.DeletePaymentAsyncById(id, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Payments.DeleteSuccess, CommonStatusCodes.Status200OK));
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
