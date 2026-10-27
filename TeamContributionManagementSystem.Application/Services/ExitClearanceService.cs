using System.Text.Json;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.ExitClearance;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class ExitClearanceService : IExitClearanceService
{
    private readonly IExitClearanceRepository _clearanceRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ExitClearanceService> _logger;

    public ExitClearanceService(
        IExitClearanceRepository clearanceRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<ExitClearanceService> logger)
    {
        _clearanceRepo = clearanceRepo;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ExitClearanceQueueResponseDto> GetQueueAsync(
        string? search = null,
        Guid? eventId = null,
        string? clearanceStatus = null,
        string? paymentStatus = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Ensure any newly published or existing Send-Off events have their clearances registered
        try
        {
            await SyncAllSendOffEventsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetQueueAsync: non-fatal send-off sync warning: {Message}", ex.Message);
        }

        // 2. Fetch all active clearances
        IReadOnlyCollection<ExitClearance> clearances;
        try
        {
            clearances = await _clearanceRepo.GetAllAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.Message.Contains("42P01") || ex.Message.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _logger.LogWarning(ex, "GetQueueAsync: exit_clearances table not yet created.");
            return new ExitClearanceQueueResponseDto
            {
                Items = new List<ExitClearanceSummaryDto>(),
                TotalCount = 0,
                Kpis = new ExitClearanceKpiDto()
            };
        }

        var items = new List<ExitClearanceSummaryDto>();

        foreach (var c in clearances)
        {
            var dues = await CalculateMemberDuesAsync(c.MemberId, cancellationToken);

            bool isApproved = string.Equals(c.Status, "Approved", StringComparison.OrdinalIgnoreCase);
            bool hasDues = dues.TotalOutstanding > 0;
            bool hasUnverified = dues.HasUnverifiedPayments;

            string computedStatus = isApproved ? "Approved"
                : (hasDues ? "DuesOutstanding"
                : (hasUnverified ? "PendingClearance" : "ReadyForApproval"));

            bool canApprove = !isApproved && !hasDues && !hasUnverified;
            string? blockReason = isApproved ? "Member exit has already been approved."
                : (hasDues ? $"Exit approval is not allowed. This member has pending contribution dues of ₹{dues.TotalOutstanding:N0}."
                : (hasUnverified ? "Exit approval is not allowed until all pending payments are verified." : null));

            string paymentVerificationStatus = hasUnverified ? "Pending Verification"
                : (dues.TotalVerifiedPaid > 0 ? "All Verified" : (dues.TotalObligations == 0 ? "No Dues" : "Unpaid"));

            var memberName = c.Member?.FullName ?? c.Member?.Username ?? "Unknown Member";
            var roleName = c.Member?.UserRoles?.FirstOrDefault(ur => ur.IsPrimary)?.Role?.RoleName
                ?? c.Member?.UserRoles?.FirstOrDefault()?.Role?.RoleName
                ?? "Member";

            var firstUnpaid = dues.EventDues.FirstOrDefault(e => e.OutstandingBalance > 0);
            var unpaidList = dues.EventDues.Where(e => e.OutstandingBalance > 0).ToList();

            var summary = new ExitClearanceSummaryDto
            {
                ClearanceId = c.ClearanceId,
                MemberId = c.MemberId,
                MemberName = memberName,
                MemberEmail = c.Member?.Email ?? string.Empty,
                MemberCode = c.Member?.Username,
                RoleName = roleName,
                ProfileImage = c.Member?.ProfileImage,
                JoiningDate = c.Member?.JoiningDate,
                PlannedExitDate = c.PlannedExitDate,
                EventId = c.EventId,
                EventName = c.Event?.EventName ?? "Send-Off Event",
                EventDate = c.Event?.EventDate ?? DateTime.UtcNow,
                UnpaidEventId = firstUnpaid?.EventId,
                UnpaidEventName = firstUnpaid?.EventName,
                UnpaidEvents = unpaidList,
                TotalObligations = dues.TotalObligations,
                TotalVerifiedPaid = dues.TotalVerifiedPaid,
                OutstandingAmount = dues.TotalOutstanding,
                PendingObligationsCount = unpaidList.Count,
                HasUnverifiedPayments = hasUnverified,
                PaymentVerificationStatus = paymentVerificationStatus,
                ClearanceStatus = computedStatus,
                CanApprove = canApprove,
                ApprovalBlockReason = blockReason,
                CreatedAt = c.CreatedAt,
                ApprovedAt = c.ApprovedAt,
                ApprovedByName = c.ApprovedByUser?.FullName ?? c.ApprovedByUser?.Username
            };

            items.Add(summary);
        }

        // 3. Compute dynamic KPI metrics across ALL active queue items
        var kpis = new ExitClearanceKpiDto
        {
            PendingExitRequests = items.Count(x => x.ClearanceStatus != "Approved"),
            MembersWithOutstandingDues = items.Where(x => x.ClearanceStatus != "Approved" && x.OutstandingAmount > 0)
                .Select(x => x.MemberId).Distinct().Count(),
            TotalOutstandingAmount = items.Where(x => x.ClearanceStatus != "Approved").Sum(x => x.OutstandingAmount),
            ReadyForFinalApproval = items.Count(x => x.CanApprove)
        };

        // 4. Apply Filters
        var filtered = items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLowerInvariant();
            filtered = filtered.Where(x =>
                x.MemberName.ToLowerInvariant().Contains(q) ||
                x.MemberEmail.ToLowerInvariant().Contains(q) ||
                (x.MemberCode != null && x.MemberCode.ToLowerInvariant().Contains(q)) ||
                x.EventName.ToLowerInvariant().Contains(q));
        }

        if (eventId.HasValue && eventId.Value != Guid.Empty)
        {
            filtered = filtered.Where(x => x.EventId == eventId.Value);
        }

        if (!string.IsNullOrWhiteSpace(clearanceStatus) && !string.Equals(clearanceStatus, "all", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(x => string.Equals(x.ClearanceStatus, clearanceStatus.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus) && !string.Equals(paymentStatus, "all", StringComparison.OrdinalIgnoreCase))
        {
            var ps = paymentStatus.Trim().ToLowerInvariant();
            if (ps == "cleared" || ps == "paid")
            {
                filtered = filtered.Where(x => x.OutstandingAmount == 0);
            }
            else if (ps == "dues" || ps == "pending")
            {
                filtered = filtered.Where(x => x.OutstandingAmount > 0);
            }
            else if (ps == "unverified")
            {
                filtered = filtered.Where(x => x.HasUnverifiedPayments);
            }
        }

        if (fromDate.HasValue)
        {
            filtered = filtered.Where(x => x.CreatedAt >= fromDate.Value || (x.PlannedExitDate.HasValue && x.PlannedExitDate.Value >= fromDate.Value));
        }

        if (toDate.HasValue)
        {
            filtered = filtered.Where(x => x.CreatedAt <= toDate.Value || (x.PlannedExitDate.HasValue && x.PlannedExitDate.Value <= toDate.Value));
        }

        var resultList = filtered.OrderByDescending(x => x.CreatedAt).ToList();

        return new ExitClearanceQueueResponseDto
        {
            Items = resultList,
            TotalCount = resultList.Count,
            Kpis = kpis
        };
    }

    public async Task<ExitClearanceDuesDetailDto> GetDuesDetailAsync(Guid clearanceId, CancellationToken cancellationToken = default)
    {
        var clearance = await _clearanceRepo.GetByIdAsync(clearanceId, cancellationToken)
            ?? throw new KeyNotFoundException("Exit clearance record not found.");

        var dues = await CalculateMemberDuesAsync(clearance.MemberId, cancellationToken);

        bool isApproved = string.Equals(clearance.Status, "Approved", StringComparison.OrdinalIgnoreCase);
        bool hasDues = dues.TotalOutstanding > 0;
        bool hasUnverified = dues.HasUnverifiedPayments;

        string computedStatus = isApproved ? "Approved"
            : (hasDues ? "DuesOutstanding"
            : (hasUnverified ? "PendingClearance" : "ReadyForApproval"));

        bool canApprove = !isApproved && !hasDues && !hasUnverified;
        string? blockReason = isApproved ? "Member exit has already been approved."
            : (hasDues ? $"Exit approval is not allowed. This member has pending contribution dues of ₹{dues.TotalOutstanding:N0}."
            : (hasUnverified ? "Exit approval is not allowed until all pending payments are verified." : null));

        var payments = await _clearanceRepo.GetMemberPaymentsAsync(clearance.MemberId, cancellationToken);

        var paymentHistory = payments.Select(p => new ExitPaymentHistoryItemDto
        {
            TransactionId = p.TransactionId,
            TxnNumber = p.TxnNumber,
            PaymentDate = p.PaymentDate,
            EventId = p.EventId,
            EventName = p.Event?.EventName ?? p.EventName,
            PaymentMode = p.PaymentModeItem?.PaymentModeName ?? p.PaymentMode,
            Amount = p.Amount,
            Status = p.Status,
            Utr = p.Utr,
            VerifiedBy = p.VerifiedBy,
            VerifiedOn = p.VerifiedOn,
            Notes = p.Notes
        }).ToList();

        var member = clearance.Member;
        var roleName = member?.UserRoles?.FirstOrDefault(ur => ur.IsPrimary)?.Role?.RoleName
            ?? member?.UserRoles?.FirstOrDefault()?.Role?.RoleName
            ?? "Member";

        return new ExitClearanceDuesDetailDto
        {
            ClearanceId = clearance.ClearanceId,
            MemberId = clearance.MemberId,
            MemberName = member?.FullName ?? member?.Username ?? "Member",
            MemberCode = member?.Username,
            MemberEmail = member?.Email ?? string.Empty,
            RoleName = roleName,
            JoiningDate = member?.JoiningDate,
            EventId = clearance.EventId,
            EventName = clearance.Event?.EventName ?? "Send-Off Event",
            EventDate = clearance.Event?.EventDate ?? DateTime.UtcNow,
            PlannedExitDate = clearance.PlannedExitDate,
            ClearanceStatus = computedStatus,
            TotalObligations = dues.TotalObligations,
            TotalVerifiedPaid = dues.TotalVerifiedPaid,
            TotalOutstanding = dues.TotalOutstanding,
            HasUnverifiedPayments = hasUnverified,
            CanApprove = canApprove,
            ApprovalBlockReason = blockReason,
            EventDues = dues.EventDues,
            PaymentHistory = paymentHistory
        };
    }

    public async Task<ExitClearanceSummaryDto> ApproveExitAsync(
        Guid clearanceId,
        ApproveExitRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // 1. Verify Admin Role Permission
        if (!_currentUserService.IsAdminRole)
        {
            throw new UnauthorizedAccessException("Only administrators are authorized to perform final exit clearance approval.");
        }

        var clearance = await _clearanceRepo.GetByIdAsync(clearanceId, cancellationToken)
            ?? throw new KeyNotFoundException("Exit clearance record not found.");

        if (string.Equals(clearance.Status, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Exit clearance has already been approved for this member.");
        }

        // 2. Re-calculate live dues on the backend
        var dues = await CalculateMemberDuesAsync(clearance.MemberId, cancellationToken);

        if (dues.TotalOutstanding > 0)
        {
            throw new InvalidOperationException($"Exit approval is not allowed. This member has pending contribution dues of ₹{dues.TotalOutstanding:N0}.");
        }

        if (dues.HasUnverifiedPayments)
        {
            throw new InvalidOperationException("Exit approval is not allowed until all pending payments are verified.");
        }

        var currentUserIdStr = _currentUserService.UserId;
        Guid adminUserId = Guid.TryParse(currentUserIdStr, out var parsedGuid) ? parsedGuid : clearance.MemberId;
        var adminUserName = _currentUserService.UserName ?? "System Admin";

        var adminUser = await _clearanceRepo.GetUserByIdAsync(adminUserId, cancellationToken);
        if (adminUser != null && !string.IsNullOrWhiteSpace(adminUser.FullName))
        {
            adminUserName = adminUser.FullName;
        }

        var approvalTime = DateTime.UtcNow;

        // 3. Update Exit Clearance Record
        clearance.Status = "Approved";
        clearance.ApprovedBy = adminUserId;
        clearance.ApprovedAt = approvalTime;
        clearance.ApprovalRemarks = request?.ApprovalRemarks?.Trim();
        clearance.ModifiedBy = adminUserId;
        clearance.ModifiedOn = approvalTime;
        _clearanceRepo.Update(clearance);

        // 4. Create Permanent Immutable Exit Clearance History Record
        var history = new ExitClearanceHistory
        {
            HistoryId = Guid.NewGuid(),
            ClearanceId = clearance.ClearanceId,
            MemberId = clearance.MemberId,
            EventId = clearance.EventId,
            MemberNameSnapshot = clearance.Member?.FullName ?? clearance.Member?.Username ?? "Member",
            MemberCodeSnapshot = clearance.Member?.Username,
            EventNameSnapshot = clearance.Event?.EventName ?? "Send-Off Event",
            JoiningDate = clearance.Member?.JoiningDate,
            ExitApprovalDate = approvalTime,
            ApprovedBy = adminUserId,
            ApprovedByName = adminUserName,
            TotalObligations = dues.TotalObligations,
            TotalVerifiedPaid = dues.TotalVerifiedPaid,
            TotalApprovedAdjustments = 0m,
            OutstandingAtApproval = 0m,
            PaymentVerificationSummary = $"{dues.EventDues.Count} events cleared. All verified.",
            ApprovalRemarks = request?.ApprovalRemarks?.Trim(),
            SnapshotJson = JsonSerializer.Serialize(dues),
            CreatedAt = approvalTime,
            CreatedBy = adminUserId
        };
        await _clearanceRepo.AddHistoryAsync(history, cancellationToken);

        // 5. Update Member Status
        var memberToUpdate = clearance.Member ?? await _clearanceRepo.GetUserByIdAsync(clearance.MemberId, cancellationToken);
        if (memberToUpdate != null)
        {
            memberToUpdate.IsExited = true;

            // Check if member is currently logged in (active session or current user approving their own record)
            bool isCurrentSessionUser = clearance.MemberId == adminUserId ||
                                        string.Equals(_currentUserService.UserId, clearance.MemberId.ToString(), StringComparison.OrdinalIgnoreCase);
            bool hasActiveSessions = isCurrentSessionUser || await _clearanceRepo.HasActiveLoginSessionsAsync(clearance.MemberId, cancellationToken);

            if (hasActiveSessions)
            {
                // For currently logged in users, do NOT deactivate immediately while approving Exit Clearance.
                // Keep IsActive = true so their active session continues uninterrupted; account will be deactivated once they log out.
                memberToUpdate.IsActive = true;
                _logger.LogInformation("Member {MemberId} is currently logged in. Marked IsExited = true; deactivation deferred until logout.", clearance.MemberId);
            }
            else
            {
                // If not currently logged in, deactivate immediately
                memberToUpdate.IsActive = false;
            }

            memberToUpdate.ModifiedBy = adminUserId;
            memberToUpdate.ModifiedOn = approvalTime;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Exit clearance {ClearanceId} approved for member {MemberId} by Admin {AdminId}",
            clearance.ClearanceId, clearance.MemberId, adminUserId);

        return new ExitClearanceSummaryDto
        {
            ClearanceId = clearance.ClearanceId,
            MemberId = clearance.MemberId,
            MemberName = clearance.Member?.FullName ?? clearance.Member?.Username ?? "Member",
            MemberEmail = clearance.Member?.Email ?? string.Empty,
            MemberCode = clearance.Member?.Username,
            RoleName = clearance.Member?.UserRoles?.FirstOrDefault()?.Role?.RoleName ?? "Member",
            JoiningDate = clearance.Member?.JoiningDate,
            PlannedExitDate = clearance.PlannedExitDate,
            EventId = clearance.EventId,
            EventName = clearance.Event?.EventName ?? "Send-Off Event",
            EventDate = clearance.Event?.EventDate ?? DateTime.UtcNow,
            TotalObligations = dues.TotalObligations,
            TotalVerifiedPaid = dues.TotalVerifiedPaid,
            OutstandingAmount = 0m,
            PendingObligationsCount = 0,
            HasUnverifiedPayments = false,
            PaymentVerificationStatus = "All Verified",
            ClearanceStatus = "Approved",
            CanApprove = false,
            CreatedAt = clearance.CreatedAt,
            ApprovedAt = clearance.ApprovedAt,
            ApprovedByName = adminUserName
        };
    }

    public async Task<IReadOnlyCollection<ExitClearanceHistoryDto>> GetHistoryAsync(
        string? search = null,
        Guid? eventId = null,
        Guid? approvedBy = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ExitClearanceHistory> list;
        try
        {
            list = await _clearanceRepo.GetAllHistoryAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.Message.Contains("42P01") || ex.Message.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _logger.LogWarning(ex, "GetHistoryAsync: exit_clearance_histories table not yet created.");
            return Array.Empty<ExitClearanceHistoryDto>();
        }

        var query = list.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.MemberNameSnapshot.ToLowerInvariant().Contains(q) ||
                x.EventNameSnapshot.ToLowerInvariant().Contains(q) ||
                x.ApprovedByName.ToLowerInvariant().Contains(q) ||
                (x.MemberCodeSnapshot != null && x.MemberCodeSnapshot.ToLowerInvariant().Contains(q)));
        }

        if (eventId.HasValue && eventId.Value != Guid.Empty)
        {
            query = query.Where(x => x.EventId == eventId.Value);
        }

        if (approvedBy.HasValue && approvedBy.Value != Guid.Empty)
        {
            query = query.Where(x => x.ApprovedBy == approvedBy.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.ExitApprovalDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.ExitApprovalDate <= toDate.Value);
        }

        return query.Select(x => new ExitClearanceHistoryDto
        {
            HistoryId = x.HistoryId,
            ClearanceId = x.ClearanceId,
            MemberId = x.MemberId,
            MemberNameSnapshot = x.MemberNameSnapshot,
            MemberCodeSnapshot = x.MemberCodeSnapshot,
            EventId = x.EventId,
            EventNameSnapshot = x.EventNameSnapshot,
            JoiningDate = x.JoiningDate,
            ExitApprovalDate = x.ExitApprovalDate,
            ApprovedBy = x.ApprovedBy,
            ApprovedByName = x.ApprovedByName,
            TotalObligations = x.TotalObligations,
            TotalVerifiedPaid = x.TotalVerifiedPaid,
            TotalApprovedAdjustments = x.TotalApprovedAdjustments,
            OutstandingAtApproval = x.OutstandingAtApproval,
            PaymentVerificationSummary = x.PaymentVerificationSummary,
            ApprovalRemarks = x.ApprovalRemarks,
            SnapshotJson = x.SnapshotJson,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<ExitClearanceHistoryDto> GetHistoryDetailAsync(Guid historyId, CancellationToken cancellationToken = default)
    {
        var x = await _clearanceRepo.GetHistoryByIdAsync(historyId, cancellationToken)
            ?? throw new KeyNotFoundException("Exit clearance history record not found.");

        return new ExitClearanceHistoryDto
        {
            HistoryId = x.HistoryId,
            ClearanceId = x.ClearanceId,
            MemberId = x.MemberId,
            MemberNameSnapshot = x.MemberNameSnapshot,
            MemberCodeSnapshot = x.MemberCodeSnapshot,
            EventId = x.EventId,
            EventNameSnapshot = x.EventNameSnapshot,
            JoiningDate = x.JoiningDate,
            ExitApprovalDate = x.ExitApprovalDate,
            ApprovedBy = x.ApprovedBy,
            ApprovedByName = x.ApprovedByName,
            TotalObligations = x.TotalObligations,
            TotalVerifiedPaid = x.TotalVerifiedPaid,
            TotalApprovedAdjustments = x.TotalApprovedAdjustments,
            OutstandingAtApproval = x.OutstandingAtApproval,
            PaymentVerificationSummary = x.PaymentVerificationSummary,
            ApprovalRemarks = x.ApprovalRemarks,
            SnapshotJson = x.SnapshotJson,
            CreatedAt = x.CreatedAt
        };
    }

    public async Task SyncAllSendOffEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var candidateEvents = await _clearanceRepo.GetSendOffCandidateEventsAsync(cancellationToken);

            foreach (var ev in candidateEvents)
            {
                if (IsSendOffEvent(ev))
                {
                    await ProcessSendOffEventAsync(ev, cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to complete full sync of Send-Off events");
        }
    }

    public async Task SyncSendOffBeneficiariesAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var ev = await _clearanceRepo.GetEventWithDetailsAsync(eventId, cancellationToken);

            if (ev == null) return;

            if (ev.IsDeleted)
            {
                await HandleEventDeletedAsync(eventId, cancellationToken);
                return;
            }

            if (ev.Status == EventStatus.Draft)
            {
                return;
            }

            if (!IsSendOffEvent(ev))
            {
                return;
            }

            await ProcessSendOffEventAsync(ev, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync send-off beneficiaries for event {EventId}", eventId);
        }
    }

    public async Task HandleEventDeletedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var clearances = await _clearanceRepo.GetByEventIdAsync(eventId, cancellationToken);

            foreach (var c in clearances)
            {
                if (!string.Equals(c.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    c.Status = "Revoked";
                    c.IsDeleted = true;
                    c.ModifiedOn = DateTime.UtcNow;
                    _clearanceRepo.Update(c);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle exit clearances on event deletion {EventId}", eventId);
        }
    }

    private static bool IsSendOffEvent(Event ev)
    {
        var typeName = ev.EventType?.EventTypeName?.Trim().ToLowerInvariant() ?? string.Empty;
        var eventName = ev.EventName?.Trim().ToLowerInvariant() ?? string.Empty;

        return typeName.Contains("send-off") || typeName.Contains("send off") || typeName.Contains("sendoff") ||
               typeName.Contains("relieving") || typeName.Contains("farewell") ||
               eventName.Contains("send-off") || eventName.Contains("send off") || eventName.Contains("sendoff") ||
               eventName.Contains("relieving") || eventName.Contains("farewell");
    }

    private async Task ProcessSendOffEventAsync(Event ev, CancellationToken cancellationToken)
    {
        var beneficiaries = ExtractBeneficiaries(ev);
        if (beneficiaries.Count == 0) return;

        var existingClearances = await _clearanceRepo.GetByEventIdAsync(ev.EventId, cancellationToken);
        var existingMap = existingClearances.ToDictionary(x => x.MemberId);

        foreach (var (memberId, plannedDate) in beneficiaries)
        {
            var memberExists = await _clearanceRepo.MemberExistsAsync(memberId, cancellationToken);
            if (!memberExists) continue;

            var effectiveDate = plannedDate ?? ev.EventDate;

            if (existingMap.TryGetValue(memberId, out var existing))
            {
                if (existing.IsDeleted && !string.Equals(existing.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    existing.IsDeleted = false;
                    existing.Status = "PendingClearance";
                    existing.PlannedExitDate = effectiveDate;
                    existing.ModifiedOn = DateTime.UtcNow;
                    _clearanceRepo.Update(existing);
                }
                else if (!existing.PlannedExitDate.HasValue)
                {
                    existing.PlannedExitDate = effectiveDate;
                    existing.ModifiedOn = DateTime.UtcNow;
                    _clearanceRepo.Update(existing);
                }
            }
            else
            {
                var newClearance = new ExitClearance
                {
                    ClearanceId = Guid.NewGuid(),
                    MemberId = memberId,
                    EventId = ev.EventId,
                    Status = "PendingClearance",
                    PlannedExitDate = effectiveDate,
                    CreatedBy = ev.CreatedBy,
                    CreatedAt = DateTime.UtcNow,
                    CreatedOn = DateTime.UtcNow,
                    IsDeleted = false
                };
                await _clearanceRepo.AddAsync(newClearance, cancellationToken);
            }
        }
    }

    private static Dictionary<Guid, DateTime?> ExtractBeneficiaries(Event ev)
    {
        var result = new Dictionary<Guid, DateTime?>();
        DateTime? singleSendOffDate = null;

        // 1. From EventDates JSON
        if (!string.IsNullOrWhiteSpace(ev.EventDates) && ev.EventDates.Trim().StartsWith("{"))
        {
            try
            {
                using var doc = JsonDocument.Parse(ev.EventDates);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var subProp in prop.Value.EnumerateObject())
                        {
                            if (Guid.TryParse(subProp.Name, out var mId))
                            {
                                DateTime? parsedDate = null;
                                if (subProp.Value.ValueKind == JsonValueKind.String &&
                                    DateTime.TryParse(subProp.Value.GetString(), out var dt))
                                {
                                    parsedDate = dt;
                                }
                                result[mId] = parsedDate;
                            }
                        }
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.String &&
                             DateTime.TryParse(prop.Value.GetString(), out var singleDt))
                    {
                        var key = prop.Name.ToLowerInvariant();
                        if (key.Contains("send") || key.Contains("farewell") || key.Contains("relieving") || prop.Name == ev.EventTypeId.ToString())
                        {
                            singleSendOffDate = singleDt;
                        }
                    }
                }
            }
            catch { }
        }

        // 2. From Description celebrants comment
        if (!string.IsNullOrWhiteSpace(ev.Description) && ev.Description.Contains("<!--celebrants:"))
        {
            try
            {
                var startIdx = ev.Description.IndexOf("<!--celebrants:") + 15;
                var endIdx = ev.Description.IndexOf("-->", startIdx);
                if (startIdx >= 15 && endIdx > startIdx)
                {
                    var jsonStr = ev.Description.Substring(startIdx, endIdx - startIdx).Trim();
                    using var doc = JsonDocument.Parse(jsonStr);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        var key = prop.Name.ToLowerInvariant();
                        if (key.Contains("send") || key.Contains("farewell") || key.Contains("relieving") || prop.Name == ev.EventTypeId.ToString())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var elem in prop.Value.EnumerateArray())
                                {
                                    if (elem.ValueKind == JsonValueKind.String && Guid.TryParse(elem.GetString(), out var mId))
                                    {
                                        if (!result.ContainsKey(mId))
                                        {
                                            result[mId] = singleSendOffDate ?? ev.EventDate;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // 3. From zero-amount contributions in Send-Off events
        if (ev.Contributions != null && ev.Contributions.Count > 0)
        {
            var zeroAmountMembers = ev.Contributions
                .Where(c => !c.IsDeleted && c.Amount == 0)
                .Select(c => c.UserId)
                .ToList();

            foreach (var mId in zeroAmountMembers)
            {
                if (!result.ContainsKey(mId))
                {
                    result[mId] = singleSendOffDate ?? ev.EventDate;
                }
            }
        }

        // Ensure any entry with null date defaults to singleSendOffDate or ev.EventDate
        foreach (var key in result.Keys.ToList())
        {
            if (!result[key].HasValue)
            {
                result[key] = singleSendOffDate ?? ev.EventDate;
            }
        }

        return result;
    }

    private async Task<(decimal TotalObligations, decimal TotalVerifiedPaid, decimal TotalOutstanding, bool HasUnverifiedPayments, List<EventDueItemDto> EventDues)> CalculateMemberDuesAsync(
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var contributions = await _clearanceRepo.GetMemberContributionsAsync(memberId, cancellationToken);
        var payments = await _clearanceRepo.GetMemberPaymentsAsync(memberId, cancellationToken);

        var paymentsByEvent = payments
            .Where(p => p.EventId.HasValue)
            .GroupBy(p => p.EventId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        bool hasAnyUnverifiedPayments = payments.Any(p =>
            string.Equals(p.Status, CommonConstants.PaymentStatuses.Pending, StringComparison.OrdinalIgnoreCase));

        var eventDues = new List<EventDueItemDto>();
        decimal totalObligations = 0m;
        decimal totalVerifiedPaid = 0m;
        decimal totalOutstanding = 0m;

        foreach (var c in contributions)
        {
            var ev = c.Event;
            if (ev == null) continue;

            var obligationAmount = c.Amount;
            totalObligations += obligationAmount;

            decimal verifiedPaid = 0m;
            decimal pendingPaymentAmount = 0m;
            bool eventHasPendingPayment = false;

            bool isContributionDirectlyPaid = c.PaymentDate != null ||
                (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                (c.UpiAmount.HasValue && c.UpiAmount.Value > 0) ||
                c.PaymentStatus == TeamContributionManagementSystem.Domain.Enums.PaymentStatus.Paid ||
                (c.StatusItem != null && (
                    string.Equals(c.StatusItem.StatusName, "Paid", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.StatusItem.StatusName, "Verified", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.StatusItem.StatusName, "Approved", StringComparison.OrdinalIgnoreCase)));

            if (paymentsByEvent.TryGetValue(ev.EventId, out var eventPayments))
            {
                var txnVerifiedPaid = eventPayments
                    .Where(p => string.Equals(p.Status, CommonConstants.PaymentStatuses.Verified, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(p.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(p.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                    .Sum(p => p.Amount);

                pendingPaymentAmount = eventPayments
                    .Where(p => string.Equals(p.Status, CommonConstants.PaymentStatuses.Pending, StringComparison.OrdinalIgnoreCase))
                    .Sum(p => p.Amount);

                eventHasPendingPayment = pendingPaymentAmount > 0;

                // If transaction is verified OR the contribution record itself is marked Paid, count as paid
                verifiedPaid = isContributionDirectlyPaid
                    ? Math.Max(obligationAmount, txnVerifiedPaid)
                    : txnVerifiedPaid;
            }
            else
            {
                if (isContributionDirectlyPaid)
                {
                    verifiedPaid = obligationAmount;
                }
            }

            decimal outstanding = Math.Max(0m, obligationAmount - verifiedPaid);
            totalVerifiedPaid += Math.Min(obligationAmount, verifiedPaid);
            totalOutstanding += outstanding;

            string paymentStatus = outstanding == 0 ? "Paid"
                : (verifiedPaid > 0 ? "Partially Paid" : "Unpaid");

            bool isPendingRelevant = outstanding > 0 && eventHasPendingPayment;

            eventDues.Add(new EventDueItemDto
            {
                EventId = ev.EventId,
                EventName = ev.EventName,
                EventDate = ev.EventDate,
                ContributionAmount = obligationAmount,
                VerifiedPaidAmount = verifiedPaid,
                OutstandingBalance = outstanding,
                PaymentStatus = paymentStatus,
                HasPendingPayment = isPendingRelevant,
                PendingPaymentAmount = isPendingRelevant ? pendingPaymentAmount : 0m
            });
        }

        bool hasActualUnverified = eventDues.Any(e => e.HasPendingPayment);

        return (totalObligations, totalVerifiedPaid, totalOutstanding, hasActualUnverified, eventDues);
    }
}
