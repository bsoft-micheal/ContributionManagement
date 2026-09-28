using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.PaymentModes;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages Support Data Payment Modes.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.PaymentModes.Base)]
public class PaymentModesController : ControllerBase
{
    private readonly IPaymentModeService _paymentModeService;

    public PaymentModesController(IPaymentModeService paymentModeService)
    {
        _paymentModeService = paymentModeService;
    }

    /// <summary>
    /// Retrieves all payment modes.
    /// </summary>
    [HttpGet(CommonRoutes.PaymentModes.GetAll)]
    [ActionName(nameof(GetAllPaymentModeAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PaymentModeDto>>>> GetAllPaymentModeAsync([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        var result = await _paymentModeService.GetAllPaymentModeAsync(activeOnly, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<PaymentModeDto>>.SuccessResult(result, CommonMessages.PaymentModes.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves a payment mode by ID.
    /// </summary>
    [HttpGet(CommonRoutes.PaymentModes.GetById)]
    [ActionName(nameof(GetPaymentModeAsyncById))]
    public async Task<ActionResult<ApiResponse<PaymentModeDto>>> GetPaymentModeAsyncById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _paymentModeService.GetPaymentModeAsyncById(id, cancellationToken);
        if (result == null)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<PaymentModeDto>.FailureResult(CommonMessages.General.NotFound, CommonStatusCodes.Status404NotFound));
        }
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<PaymentModeDto>.SuccessResult(result, CommonMessages.PaymentModes.GetByIdSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Creates a new payment mode (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPost(CommonRoutes.PaymentModes.Create)]
    [ActionName(nameof(SavePaymentModeAsync))]
    public async Task<ActionResult<ApiResponse<PaymentModeDto>>> SavePaymentModeAsync([FromBody] CreatePaymentModeRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _paymentModeService.SavePaymentModeAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<PaymentModeDto>.SuccessResult(result, CommonMessages.PaymentModes.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Updates an existing payment mode (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPut(CommonRoutes.PaymentModes.Update)]
    [ActionName(nameof(UpdatePaymentModeAsyncById))]
    public async Task<ActionResult<ApiResponse<PaymentModeDto>>> UpdatePaymentModeAsyncById(Guid id, [FromBody] UpdatePaymentModeRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _paymentModeService.UpdatePaymentModeAsyncById(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<PaymentModeDto>.SuccessResult(result, CommonMessages.PaymentModes.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes a payment mode (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpDelete(CommonRoutes.PaymentModes.Delete)]
    [ActionName(nameof(DeletePaymentModeAsyncById))]
    public async Task<ActionResult<ApiResponse>> DeletePaymentModeAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _paymentModeService.DeletePaymentModeAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.PaymentModes.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
