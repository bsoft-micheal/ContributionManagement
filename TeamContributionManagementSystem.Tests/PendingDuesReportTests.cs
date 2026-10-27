using Microsoft.Extensions.Logging;
using Moq;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Services;
using TeamContributionManagementSystem.Domain.Enums;
using Xunit;

namespace TeamContributionManagementSystem.Tests;

public class PendingDuesReportTests
{
    private readonly Mock<ILogger<ReportService>> _loggerMock = new();
    private readonly Mock<IEventRepository> _eventRepoMock = new();
    private readonly Mock<IContributionRepository> _contribRepoMock = new();
    private readonly Mock<IExpenseRepository> _expenseRepoMock = new();
    private readonly Mock<IPaymentTransactionRepository> _paymentTxRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private readonly ReportService _reportService;

    public PendingDuesReportTests()
    {
        _reportService = new ReportService(
            _loggerMock.Object,
            _eventRepoMock.Object,
            _contribRepoMock.Object,
            _expenseRepoMock.Object,
            _paymentTxRepoMock.Object,
            _userRepoMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetPendingDuesReportAsync_CalculatesPriorPreviousCurrentAndTotalCorrectly()
    {
        // Arrange: Selected month = September 2026 (Month 9)
        // Previous month = August 2026 (Month 8)
        // Prior month = May 2026 (Month 5)
        int selectedMonth = 9;
        int selectedYear = 2026;

        var mayEventId = Guid.NewGuid();
        var augEventId = Guid.NewGuid();
        var septEventId = Guid.NewGuid();

        var events = new List<EventSummaryDto>
        {
            new() { EventId = mayEventId, EventName = "May Birthday", EventDate = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc) },
            new() { EventId = augEventId, EventName = "August Birthday", EventDate = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc) },
            new() { EventId = septEventId, EventName = "Sept Birthday", EventDate = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc) },
        };

        var felixId = Guid.NewGuid();
        var leoId = Guid.NewGuid();
        var motcharakkiniId = Guid.NewGuid();

        var users = new List<UserDto>
        {
            new() { UserId = felixId, FullName = "Felix R", Email = "felix@example.com" },
            new() { UserId = leoId, FullName = "Leo Thomas S", Email = "leo@example.com" },
            new() { UserId = motcharakkiniId, FullName = "Motcharakkini A", Email = "motcharakkini@example.com" },
        };

        var pendingContributions = new List<ContributionDto>
        {
            // Felix: Only Sept = 130
            new() { ContributionId = Guid.NewGuid(), MemberId = felixId, EventId = septEventId, Amount = 130m, PaymentStatus = PaymentStatus.Pending },

            // Leo: August = 100, Sept = 130
            new() { ContributionId = Guid.NewGuid(), MemberId = leoId, EventId = augEventId, Amount = 100m, PaymentStatus = PaymentStatus.Pending },
            new() { ContributionId = Guid.NewGuid(), MemberId = leoId, EventId = septEventId, Amount = 130m, PaymentStatus = PaymentStatus.Pending },

            // Motcharakkini: May = 660, August = 100, Sept = 130
            new() { ContributionId = Guid.NewGuid(), MemberId = motcharakkiniId, EventId = mayEventId, Amount = 660m, PaymentStatus = PaymentStatus.Pending },
            new() { ContributionId = Guid.NewGuid(), MemberId = motcharakkiniId, EventId = augEventId, Amount = 100m, PaymentStatus = PaymentStatus.Pending },
            new() { ContributionId = Guid.NewGuid(), MemberId = motcharakkiniId, EventId = septEventId, Amount = 130m, PaymentStatus = PaymentStatus.Pending },
        };

        _eventRepoMock.Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);
        _contribRepoMock.Setup(r => r.GetPendingAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingContributions);
        _userRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        _currentUserServiceMock.Setup(u => u.IsOrganizerRole).Returns(false);

        // Act
        var result = await _reportService.GetPendingDuesReportAsync(selectedMonth, selectedYear, null, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(9, result.SelectedMonth);
        Assert.Equal(2026, result.SelectedYear);
        Assert.Equal("Sep", result.CurrentMonthLabel);
        Assert.Equal("Aug", result.PreviousMonthLabel);
        Assert.Equal("Previous Arrear", result.PriorMonthsLabel);

        // Felix check
        var felix = Assert.Single(result.Members, m => m.MemberId == felixId);
        Assert.Equal(0m, felix.PriorMonthsPendingAmount);
        Assert.Equal(0m, felix.PreviousMonthPendingAmount);
        Assert.Equal(130m, felix.CurrentMonthPendingAmount);
        Assert.Equal(130m, felix.TotalPendingAmount);

        // Leo check
        var leo = Assert.Single(result.Members, m => m.MemberId == leoId);
        Assert.Equal(0m, leo.PriorMonthsPendingAmount);
        Assert.Equal(100m, leo.PreviousMonthPendingAmount);
        Assert.Equal(130m, leo.CurrentMonthPendingAmount);
        Assert.Equal(230m, leo.TotalPendingAmount);

        // Motcharakkini check
        var motch = Assert.Single(result.Members, m => m.MemberId == motcharakkiniId);
        Assert.Equal(660m, motch.PriorMonthsPendingAmount);
        Assert.Equal(100m, motch.PreviousMonthPendingAmount);
        Assert.Equal(130m, motch.CurrentMonthPendingAmount);
        Assert.Equal(890m, motch.TotalPendingAmount);

        // Overall totals check
        Assert.Equal(660m, result.OverallPriorPending);
        Assert.Equal(200m, result.OverallPreviousPending);
        Assert.Equal(390m, result.OverallCurrentPending);
        Assert.Equal(1250m, result.OverallTotalPending);
        Assert.Equal(3, result.TotalDefaultersCount);
    }

    [Fact]
    public async Task GetPendingDuesReportAsync_WhenFilteringByJanuary_DoesNotIncludeFutureMonths()
    {
        // Arrange: Events in October 2026, user filters by January 2026
        int selectedMonth = 1; // January
        int selectedYear = 2026;

        var octEventId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var events = new List<EventSummaryDto>
        {
            new() { EventId = octEventId, EventName = "October Event", EventDate = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc) }
        };

        var contributions = new List<ContributionDto>
        {
            new() { ContributionId = Guid.NewGuid(), EventId = octEventId, MemberId = memberId, MemberName = "Test Member", Amount = 760m, PaymentStatus = PaymentStatus.Pending }
        };

        var users = new List<UserDto>
        {
            new() { UserId = memberId, FullName = "Test Member", IsDeleted = false }
        };

        _eventRepoMock.Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>())).ReturnsAsync(events);
        _contribRepoMock.Setup(r => r.GetPendingAsync(null, null, It.IsAny<CancellationToken>())).ReturnsAsync(contributions);
        _userRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        // Act
        var result = await _reportService.GetPendingDuesReportAsync(selectedMonth, selectedYear, null, null, CancellationToken.None);

        // Assert: In January, PreviousMonthLabel is empty, and future months (October 2026) are not included
        Assert.Equal("Jan", result.CurrentMonthLabel);
        Assert.Equal(string.Empty, result.PreviousMonthLabel);
        Assert.Equal("Previous Arrear", result.PriorMonthsLabel);

        var member = Assert.Single(result.Members, m => m.MemberId == memberId);
        Assert.Equal(0m, member.CurrentMonthPendingAmount);
        Assert.Equal(0m, member.PreviousMonthPendingAmount);
        Assert.Equal(0m, member.PriorMonthsPendingAmount);
        Assert.Equal(0m, member.TotalPendingAmount);

        // And overall total for January 2026 report must be 0
        Assert.Equal(0m, result.OverallTotalPending);
        Assert.Equal(0, result.TotalDefaultersCount);
    }

    [Fact]
    public async Task GetPendingDuesReportAsync_WhenFilteredByEventId_OnlyIncludesThatEvent()
    {
        // Arrange
        var event1Id = Guid.NewGuid();
        var event2Id = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var events = new List<EventSummaryDto>
        {
            new() { EventId = event1Id, EventName = "Event 1", EventDate = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc) },
            new() { EventId = event2Id, EventName = "Event 2", EventDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc) }
        };

        var contributions = new List<ContributionDto>
        {
            new() { ContributionId = Guid.NewGuid(), EventId = event1Id, MemberId = memberId, MemberName = "Test Member", Amount = 500m, PaymentStatus = PaymentStatus.Pending },
            new() { ContributionId = Guid.NewGuid(), EventId = event2Id, MemberId = memberId, MemberName = "Test Member", Amount = 300m, PaymentStatus = PaymentStatus.Pending }
        };

        var users = new List<UserDto>
        {
            new() { UserId = memberId, FullName = "Test Member", IsDeleted = false }
        };

        _eventRepoMock.Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>())).ReturnsAsync(events);
        _contribRepoMock.Setup(r => r.GetPendingAsync(null, null, It.IsAny<CancellationToken>())).ReturnsAsync(contributions);
        _userRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        // Act: Filter specifically for event1Id
        var result = await _reportService.GetPendingDuesReportAsync(10, 2026, null, event1Id, CancellationToken.None);

        // Assert: Only 500m from Event 1 should be included
        var member = Assert.Single(result.Members, m => m.MemberId == memberId);
        Assert.Equal(500m, member.CurrentMonthPendingAmount);
        Assert.Equal(500m, member.TotalPendingAmount);
        Assert.Equal(500m, result.OverallTotalPending);
    }

    [Fact]
    public async Task GetPendingDuesReportAsync_WhenFilteringByJanuary2027_RollsAll2026MonthsIntoPreviousArrear()
    {
        // Arrange: October 2026 (498), December 2026 (125), January 2027 (125)
        var octEventId = Guid.NewGuid();
        var decEventId = Guid.NewGuid();
        var janEventId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var events = new List<EventSummaryDto>
        {
            new() { EventId = octEventId, EventName = "Oct Event", EventDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc) },
            new() { EventId = decEventId, EventName = "Dec Event", EventDate = new DateTime(2026, 12, 20, 0, 0, 0, DateTimeKind.Utc) },
            new() { EventId = janEventId, EventName = "Jan Event", EventDate = new DateTime(2027, 1, 10, 0, 0, 0, DateTimeKind.Utc) }
        };

        var contributions = new List<ContributionDto>
        {
            new() { ContributionId = Guid.NewGuid(), EventId = octEventId, MemberId = memberId, MemberName = "Anthony Infant Raj", Amount = 498m, PaymentStatus = PaymentStatus.Pending },
            new() { ContributionId = Guid.NewGuid(), EventId = decEventId, MemberId = memberId, MemberName = "Anthony Infant Raj", Amount = 125m, PaymentStatus = PaymentStatus.Pending },
            new() { ContributionId = Guid.NewGuid(), EventId = janEventId, MemberId = memberId, MemberName = "Anthony Infant Raj", Amount = 125m, PaymentStatus = PaymentStatus.Pending }
        };

        var users = new List<UserDto>
        {
            new() { UserId = memberId, FullName = "Anthony Infant Raj", IsDeleted = false }
        };

        _eventRepoMock.Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>())).ReturnsAsync(events);
        _contribRepoMock.Setup(r => r.GetPendingAsync(null, null, It.IsAny<CancellationToken>())).ReturnsAsync(contributions);
        _userRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        // Act: Filter by Year 2027, Month 1 (January)
        var result = await _reportService.GetPendingDuesReportAsync(1, 2027, null, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Previous Arrear", result.PriorMonthsLabel);
        Assert.Equal(string.Empty, result.PreviousMonthLabel);
        Assert.Equal("Jan", result.CurrentMonthLabel);

        var member = Assert.Single(result.Members, m => m.MemberId == memberId);
        // Oct 2026 (498) + Dec 2026 (125) = 623
        Assert.Equal(623m, member.PriorMonthsPendingAmount);
        Assert.Equal(0m, member.PreviousMonthPendingAmount);
        Assert.Equal(125m, member.CurrentMonthPendingAmount);
        Assert.Equal(748m, member.TotalPendingAmount);
    }
}
