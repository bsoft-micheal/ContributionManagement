using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.DTOs.Reports;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class ReportService : IReportService
{
    private readonly Microsoft.Extensions.Logging.ILogger<ReportService> _logger;
    private readonly IEventRepository _eventRepository;
    private readonly IContributionRepository _contributionRepository;

    public ReportService(Microsoft.Extensions.Logging.ILogger<ReportService> logger, IEventRepository eventRepository, IContributionRepository contributionRepository)
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _contributionRepository = contributionRepository;
    }

    public async Task<ReportsSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            int? targetMonth = (month == 0 || month == null) ? null : month;
        int? targetYear = (year == 0 || year == null) ? null : year;

        var events = await _eventRepository.GetAllAsync(targetMonth, targetYear, cancellationToken);
        var pendingDues = await _contributionRepository.GetPendingAsync(targetMonth, targetYear, cancellationToken);
        var contributions = events.SelectMany(x => x.Contributions.Where(c => !c.IsDeleted)).ToList();

        return new ReportsSummaryDto
        {
            EventCollections = events.Select(x => new EventCollectionReportDto
            {
                EventId = x.EventId,
                EventName = x.EventName,
                EventTypeName = x.EventType?.EventTypeName ?? string.Empty,
                EventDate = x.EventDate,
                ExpectedAmount = x.Contributions.Where(c => !c.IsDeleted).Sum(c => c.Amount),
                PaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.PaymentStatus == PaymentStatus.Paid).Sum(c => c.Amount),
                PendingAmount = x.Contributions.Where(c => !c.IsDeleted && c.PaymentStatus != PaymentStatus.Paid).Sum(c => c.Amount)
            }).OrderByDescending(x => x.EventDate).ToList(),
            MemberContributionHistory = contributions
                .GroupBy(x => new { x.MemberId, MemberName = x.Member?.Name ?? string.Empty })
                .Select(group => new MemberContributionHistoryDto
                {
                    MemberId = group.Key.MemberId,
                    MemberName = group.Key.MemberName,
                    TotalExpectedAmount = group.Sum(x => x.Amount),
                    TotalPaidAmount = group.Where(x => x.PaymentStatus == PaymentStatus.Paid).Sum(x => x.Amount),
                    PaidEventsCount = group.Count(x => x.PaymentStatus == PaymentStatus.Paid),
                    PendingEventsCount = group.Count(x => x.PaymentStatus != PaymentStatus.Paid)
                })
                .OrderBy(x => x.MemberName)
                .ToList(),
            PendingDues = pendingDues.Select(x => new PendingDueDto
            {
                ContributionId = x.ContributionId,
                MemberName = x.Member?.Name ?? string.Empty,
                EventName = x.Event?.EventName ?? string.Empty,
                EventDate = x.Event?.EventDate ?? DateTime.MinValue,
                Amount = x.Amount
            }).OrderBy(x => x.EventDate).ToList()
        };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetSummaryAsync");
            throw;
        }
    }
}
