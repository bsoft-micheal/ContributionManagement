using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;
using TeamContributionManagementSystem.Infrastructure.Repositories;
using Xunit;

namespace TeamContributionManagementSystem.Tests;

public class ContributionRepositoryCancellationAndQueryOptimizationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ContributionRepository _contributionRepository;

    public ContributionRepositoryCancellationAndQueryOptimizationTests()
    {
        var dbName = "ContribRepoTests_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _context = new ApplicationDbContext(options);
        var loggerMock = new Mock<ILogger<ContributionRepository>>();
        _contributionRepository = new ContributionRepository(_context, loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetPendingAsync_PopulatesCreatedByName_ForPendingContributions()
    {
        // Arrange: seed an admin user who created the contribution, and an unrelated user
        var adminId = Guid.NewGuid();
        var adminUser = new AppUser
        {
            UserId = adminId,
            Username = "adminUser",
            FullName = "Admin Superuser",
            Email = "admin@example.com",
            IsActive = true,
            IsDeleted = false
        };

        var memberId = Guid.NewGuid();
        var memberUser = new AppUser
        {
            UserId = memberId,
            Username = "memberUser",
            FullName = "Regular Member",
            Email = "member@example.com",
            IsActive = true,
            IsDeleted = false
        };

        _context.Users.AddRange(adminUser, memberUser);

        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Celebration",
            IsActive = true
        };
        _context.EventTypes.Add(eventType);

        var ev = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = "Annual Dinner",
            EventTypeId = eventType.EventTypeId,
            EventDate = DateTime.UtcNow.Date,
            CreatedBy = adminId,
            IsDeleted = false
        };
        _context.Events.Add(ev);

        var pendingStatus = new Status
        {
            StatusId = Guid.NewGuid(),
            StatusName = "Pending",
            IsActive = true,
            IsDeleted = false
        };
        _context.Statuses.Add(pendingStatus);

        var contribution = new Contribution
        {
            ContributionId = Guid.NewGuid(),
            EventId = ev.EventId,
            UserId = memberId,
            Amount = 500,
            StatusId = pendingStatus.StatusId,
            StatusItem = pendingStatus,
            PaymentStatus = PaymentStatus.Pending,
            PaymentMode = PaymentMode.None,
            PaymentDate = null,
            CashAmount = null,
            UpiAmount = null,
            IsDeleted = false,
            IsActive = true,
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Contributions.Add(contribution);
        await _context.SaveChangesAsync();

        // Act
        var results = await _contributionRepository.GetPendingAsync(month: null, year: null, CancellationToken.None);

        // Assert
        Assert.Single(results);
        var item = results.First();
        Assert.Equal(contribution.ContributionId, item.ContributionId);
        // CreatedBy must be resolved to the user's name (not left as raw GUID)
        Assert.Equal("Admin Superuser", item.CreatedBy);
    }

    [Fact]
    public async Task GetPendingAsync_WhenCancelled_ThrowsOperationCanceledExceptionWithoutSuppression()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled token

        // Act & Assert: Legitimate cancellation must NOT be suppressed
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _contributionRepository.GetPendingAsync(month: null, year: null, cts.Token);
        });
    }

    [Fact]
    public async Task GetPendingAsync_WhenEmpty_ReturnsEmptyWithoutError()
    {
        // Act on empty database
        var results = await _contributionRepository.GetPendingAsync(month: 10, year: 2026, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }
}
