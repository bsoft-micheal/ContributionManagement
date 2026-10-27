using Microsoft.Extensions.Logging;
using Moq;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.DTOs.Expenses;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Services;
using Xunit;

namespace TeamContributionManagementSystem.Tests;

public class DashboardOrganizerIsolationTests
{
    private readonly Mock<ILogger<DashboardService>> _loggerMock = new();
    private readonly Mock<IEventRepository> _eventRepoMock = new();
    private readonly Mock<IContributionRepository> _contribRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IMemberRepository> _memberRepoMock = new();
    private readonly Mock<IExpenseRepository> _expenseRepoMock = new();

    public DashboardOrganizerIsolationTests()
    {
        _contribRepoMock.Setup(r => r.GetPendingAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContributionDto>());
    }

    [Fact]
    public async Task GetSummaryAsync_WhenOrganizerHasNoEvents_ReturnsZeroExpensesAndZeroRemaining()
    {
        // Arrange
        var orgUserId = Guid.NewGuid();
        _currentUserServiceMock.Setup(u => u.IsOrganizerRole).Returns(true);
        _currentUserServiceMock.Setup(u => u.IsAdminRole).Returns(false);
        _currentUserServiceMock.Setup(u => u.IsMemberRole).Returns(false);
        _currentUserServiceMock.Setup(u => u.UserId).Returns(orgUserId.ToString());

        // Organizer has 0 events for this month/year and across all time
        _eventRepoMock.Setup(r => r.GetAllForOrganizerAsync(orgUserId, 10, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventSummaryDto>());
        _eventRepoMock.Setup(r => r.GetAllForOrganizerAsync(orgUserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventSummaryDto>());

        // There are expenses in the database from other users / admin (e.g. 5,100)
        _expenseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExpenseDto>
            {
                new()
                {
                    ExpenseId = Guid.NewGuid(),
                    EventName = "Admin Gala",
                    Amount = 5100,
                    Status = "Verified",
                    ExpenseDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc)
                }
            });

        var sut = new DashboardService(
            _loggerMock.Object,
            _eventRepoMock.Object,
            _contribRepoMock.Object,
            _currentUserServiceMock.Object,
            _memberRepoMock.Object,
            _expenseRepoMock.Object);

        // Act
        var result = await sut.GetSummaryAsync(10, 2026);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.MonthlyEventsCount);
        Assert.Equal(0, result.TotalExpenses);
        Assert.Equal(0, result.TotalContributions);
        Assert.Equal(0, result.TotalExpectedAmount);
        Assert.Equal(0, result.TotalPendingAmount);
        Assert.Equal(0, result.TotalRemainingAmount);
        Assert.Empty(result.UpcomingEvents);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenOrganizerHasEvents_IncludesOnlyExpensesForTheirOwnEvents()
    {
        // Arrange
        var orgUserId = Guid.NewGuid();
        _currentUserServiceMock.Setup(u => u.IsOrganizerRole).Returns(true);
        _currentUserServiceMock.Setup(u => u.IsAdminRole).Returns(false);
        _currentUserServiceMock.Setup(u => u.IsMemberRole).Returns(false);
        _currentUserServiceMock.Setup(u => u.UserId).Returns(orgUserId.ToString());

        var orgEventId = Guid.NewGuid();
        var orgEvents = new List<EventSummaryDto>
        {
            new()
            {
                EventId = orgEventId,
                EventName = "Organizer Workshop",
                EventTypeName = "Workshop",
                EventDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc),
                TotalExpectedAmount = 3000,
                TotalPaidAmount = 2500,
                PendingContributionsCount = 1,
                ParticipantCount = 5
            }
        };

        _eventRepoMock.Setup(r => r.GetAllForOrganizerAsync(orgUserId, 10, 2026, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orgEvents);
        _eventRepoMock.Setup(r => r.GetAllForOrganizerAsync(orgUserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orgEvents);

        // DB contains an expense of 800 for the organizer's event, and 5100 for an admin event
        _expenseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExpenseDto>
            {
                new()
                {
                    ExpenseId = Guid.NewGuid(),
                    EventName = "Organizer Workshop",
                    Amount = 800,
                    Status = "Verified",
                    ExpenseDate = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc)
                },
                new()
                {
                    ExpenseId = Guid.NewGuid(),
                    EventName = "Admin Gala",
                    Amount = 5100,
                    Status = "Verified",
                    ExpenseDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc)
                }
            });

        var sut = new DashboardService(
            _loggerMock.Object,
            _eventRepoMock.Object,
            _contribRepoMock.Object,
            _currentUserServiceMock.Object,
            _memberRepoMock.Object,
            _expenseRepoMock.Object);

        // Act
        var result = await sut.GetSummaryAsync(10, 2026);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.MonthlyEventsCount);
        // Only 800 should be counted, not 5100 + 800 = 5900!
        Assert.Equal(800, result.TotalExpenses);
        Assert.Equal(2500, result.TotalContributions);
        Assert.Equal(3000, result.TotalExpectedAmount);
        Assert.Equal(500, result.TotalPendingAmount);
        // Remaining = Collections (2500) - Expenses (800) = 1700
        Assert.Equal(1700, result.TotalRemainingAmount);
        Assert.Single(result.UpcomingEvents);
        Assert.Equal("Organizer Workshop", result.UpcomingEvents.First().EventName);
        Assert.Equal(800, result.UpcomingEvents.First().ExpenseAmount);
    }
}
