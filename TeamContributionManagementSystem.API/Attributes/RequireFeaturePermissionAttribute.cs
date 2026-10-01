using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.API.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequireFeaturePermissionAttribute : TypeFilterAttribute
{
    public RequireFeaturePermissionAttribute(int featureId, AccessType requiredAccessType = AccessType.ReadOnly)
        : base(typeof(FeaturePermissionFilter))
    {
        Arguments = [featureId, requiredAccessType];
    }
}

public class FeaturePermissionFilter : IAsyncActionFilter
{
    private readonly int _featureId;
    private readonly AccessType _requiredAccessType;
    private readonly IRoleRightRepository _roleRightRepository;

    public FeaturePermissionFilter(int featureId, AccessType requiredAccessType, IRoleRightRepository roleRightRepository)
    {
        _featureId = featureId;
        _requiredAccessType = requiredAccessType;
        _roleRightRepository = roleRightRepository;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var roleClaim = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
                     ?? user.FindFirst("role")?.Value;

        if (string.IsNullOrWhiteSpace(roleClaim))
        {
            await next();
            return;
        }

        var rights = await _roleRightRepository.GetByRoleNameAsync(roleClaim, context.HttpContext.RequestAborted);
        if (rights != null && rights.Count > 0)
        {
            var matchedRight = rights.FirstOrDefault(r => r.FeatureID == _featureId);

            // Fallback to parent page featureId if specific action featureId isn't explicitly matched
            if (matchedRight == null)
            {
                int? parentFeatureId = GetParentFeatureId(_featureId);
                if (parentFeatureId.HasValue)
                {
                    matchedRight = rights.FirstOrDefault(r => r.FeatureID == parentFeatureId.Value);
                }
            }

            if (matchedRight != null)
            {
                var userAccessType = (AccessType)matchedRight.AccessType;

                // 1. If user accessType is Deny (3), block all requests
                if (userAccessType == AccessType.Deny)
                {
                    context.Result = new ObjectResult(ApiResponse<object>.FailureResult(
                        "Forbidden: You do not have permission to access this feature.",
                        CommonStatusCodes.Status403Forbidden))
                    {
                        StatusCode = CommonStatusCodes.Status403Forbidden
                    };
                    return;
                }

                // 2. If endpoint requires ReadWrite (2) and user has ReadOnly (1), block write request
                if (_requiredAccessType == AccessType.ReadWrite && userAccessType == AccessType.ReadOnly)
                {
                    context.Result = new ObjectResult(ApiResponse<object>.FailureResult(
                        "Forbidden: You have Read Only permission and cannot perform write operations.",
                        CommonStatusCodes.Status403Forbidden))
                    {
                        StatusCode = CommonStatusCodes.Status403Forbidden
                    };
                    return;
                }
            }
        }

        await next();
    }

    private static int? GetParentFeatureId(int actionFeatureId)
    {
        if (actionFeatureId >= 25 && actionFeatureId <= 29) return 2;   // Users
        if (actionFeatureId >= 30 && actionFeatureId <= 34) return 4;   // Event
        if (actionFeatureId >= 35 && actionFeatureId <= 39) return 6;   // Gallery
        if (actionFeatureId >= 40 && actionFeatureId <= 41) return 8;   // Contribution
        if (actionFeatureId >= 42 && actionFeatureId <= 43) return 9;   // Payment History
        if (actionFeatureId >= 44 && actionFeatureId <= 45) return 10;  // Calculation
        if (actionFeatureId >= 46 && actionFeatureId <= 51) return 11;  // Expense
        if (actionFeatureId >= 52 && actionFeatureId <= 54) return 24;  // Payment Submission
        if (actionFeatureId >= 55 && actionFeatureId <= 60) return 12;  // Support Ticket
        if (actionFeatureId >= 61 && actionFeatureId <= 65) return 18;  // Budget Calculation
        if (actionFeatureId >= 66 && actionFeatureId <= 70) return 19;  // Types
        if (actionFeatureId >= 71 && actionFeatureId <= 75) return 20;  // Status
        if (actionFeatureId >= 76 && actionFeatureId <= 78) return 21;  // Exit Process
        if (actionFeatureId >= 79 && actionFeatureId <= 80) return 22;  // Settings
        if (actionFeatureId >= 81 && actionFeatureId <= 82) return 23;  // Reports
        if (actionFeatureId >= 83 && actionFeatureId <= 89) return 2;   // Users
        return null;
    }
}
