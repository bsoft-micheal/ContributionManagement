using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TeamContributionManagementSystem.Application.DTOs.ExitClearance;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Services;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;
using TeamContributionManagementSystem.Infrastructure.Repositories;
using Xunit;

namespace TeamContributionManagementSystem.Tests;

public class ExitClearanceApprovalTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ExitClearanceRepository _clearanceRepo;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<ExitClearanceService>> _loggerMock;
    private readonly ExitClearanceService _clearanceService;

    public ExitClearanceApprovalTests()
    {
        var dbName = "ExitClearanceApprovalTests_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _context = new ApplicationDbContext(options);
        var repoLoggerMock = new Mock<ILogger<ExitClearanceRepository>>();
        _clearanceRepo = new ExitClearanceRepository(_context, repoLoggerMock.Object);

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(c => c.IsAdminRole).Returns(true);
        _currentUserServiceMock.Setup(c => c.UserId).Returns(Guid.NewGuid().ToString());
        _currentUserServiceMock.Setup(c => c.UserName).Returns("Admin Tester");

        _loggerMock = new Mock<ILogger<ExitClearanceService>>();

        _clearanceService = new ExitClearanceService(
            _clearanceRepo,
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task ApproveExitAsync_WhenApproved_MarksUserAsInactiveAndExited()
    {
        // Arrange
        var member = new AppUser
        {
            UserId = Guid.NewGuid(),
            FullName = "Atlin Joan J",
            Username = "atlinjoan",
            Email = "atlinjoan@nomail.com",
            IsActive = true,
            IsExited = false,
            IsDeleted = false
        };
        _context.Users.Add(member);

        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Farewell",
            IsActive = true
        };
        _context.EventTypes.Add(eventType);

        var exitEvent = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = "Atlin Send-Off",
            EventDate = DateTime.UtcNow,
            EventTypeId = eventType.EventTypeId,
            EventType = eventType,
            Status = EventStatus.Published,
            IsDeleted = false
        };
        _context.Events.Add(exitEvent);

        var clearance = new ExitClearance
        {
            ClearanceId = Guid.NewGuid(),
            MemberId = member.UserId,
            Member = member,
            EventId = exitEvent.EventId,
            Event = exitEvent,
            Status = "PendingClearance",
            IsDeleted = false
        };
        _context.ExitClearances.Add(clearance);
        await _context.SaveChangesAsync();

        var request = new ApproveExitRequestDto
        {
            ApprovalRemarks = "All clearance requirements met.",
            MarkAsInactive = true
        };

        // Act
        var result = await _clearanceService.ApproveExitAsync(clearance.ClearanceId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Approved", clearance.Status);
        Assert.False(member.IsActive, "User must be marked as Inactive upon exit clearance approval.");
        Assert.True(member.IsExited, "User must be marked as Exited upon exit clearance approval.");
    }

    [Fact]
    public void ExitClearanceRepository_UpdateUser_PreservesInactiveStatus()
    {
        // Arrange
        var user = new AppUser
        {
            UserId = Guid.NewGuid(),
            FullName = "Test Member",
            IsActive = false,
            IsExited = true
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        // Act
        _clearanceRepo.UpdateUser(user);
        _context.SaveChanges();

        // Assert
        var updated = _context.Users.Find(user.UserId);
        Assert.NotNull(updated);
        Assert.False(updated.IsActive, "UpdateUser should not override IsActive to true.");
        Assert.True(updated.IsExited, "UpdateUser should not override IsExited to false.");
    }
}

