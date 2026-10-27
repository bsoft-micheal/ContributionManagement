using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;
using TeamContributionManagementSystem.Infrastructure.Repositories;
using Xunit;

namespace TeamContributionManagementSystem.Tests;

public class EventMultiRoleIsolationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EventRepository _eventRepository;

    public EventMultiRoleIsolationTests()
    {
        var dbName = "EventMultiRoleIsolationTests_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _context = new ApplicationDbContext(options);
        var loggerMock = new Mock<ILogger<EventRepository>>();
        _eventRepository = new EventRepository(_context, loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetAllForOrganizerAsync_ExecutesWithoutEFCoreTranslationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminRoleId = Guid.NewGuid();

        var user = new AppUser
        {
            UserId = userId,
            Username = "organizerUser",
            Email = "organizer@example.com",
            FullName = "Organizer User",
            IsActive = true,
            IsDeleted = false
        };
        _context.Users.Add(user);

        var adminRole = new Role
        {
            RoleId = adminRoleId,
            RoleName = CommonRoles.Admin,
            IsDeleted = false
        };
        _context.Roles.Add(adminRole);

        var userRole = new AppUserRole
        {
            UserId = userId,
            RoleId = adminRoleId,
            User = user,
            Role = adminRole
        };
        _context.UserRoles.Add(userRole);

        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Celebration",
            IsActive = true
        };
        _context.EventTypes.Add(eventType);

        // Admin-created event (should be hidden in Organizer view)
        var adminCreatedEvent = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = "Admin Gala Event",
            EventTypeId = eventType.EventTypeId,
            EventDate = DateTime.UtcNow.Date,
            CreatedBy = userId,
            Description = "Gala details\n<!-- CREATOR_ROLE: Admin -->",
            Status = EventStatus.Planned,
            BaseAmount = 5000,
            IsDeleted = false
        };

        // Organizer-created event (should be visible in Organizer view)
        var organizerCreatedEvent = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = "Organizer Team Lunch",
            EventTypeId = eventType.EventTypeId,
            EventDate = DateTime.UtcNow.Date,
            CreatedBy = userId,
            Description = "Lunch details\n<!-- CREATOR_ROLE: Organizer -->",
            Status = EventStatus.Planned,
            BaseAmount = 3000,
            IsDeleted = false
        };

        _context.Events.AddRange(adminCreatedEvent, organizerCreatedEvent);
        await _context.SaveChangesAsync();

        // Act - this previously threw System.InvalidOperationException: Translation of member 'Role' on entity type 'AppUser' failed
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _eventRepository.GetAllForOrganizerAsync(userId);
        });

        // Assert - query must succeed without LINQ translation error
        Assert.Null(exception);

        var results = await _eventRepository.GetAllForOrganizerAsync(userId);
        Assert.NotNull(results);
        // Multi-role user operating in Organizer role should only see the Organizer-created event
        Assert.Single(results);
        Assert.Equal("Organizer Team Lunch", results.First().EventName);
    }

    [Fact]
    public async Task GetAllForOrganizerAsync_PureOrganizer_ReturnsEventsWithoutStrictRoleTagFilter()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizerRoleId = Guid.NewGuid();

        var user = new AppUser
        {
            UserId = userId,
            Username = "pureOrganizer",
            Email = "pureorg@example.com",
            FullName = "Pure Organizer",
            IsActive = true,
            IsDeleted = false
        };
        _context.Users.Add(user);

        var organizerRole = new Role
        {
            RoleId = organizerRoleId,
            RoleName = CommonRoles.Organizer,
            IsDeleted = false
        };
        _context.Roles.Add(organizerRole);

        var userRole = new AppUserRole
        {
            UserId = userId,
            RoleId = organizerRoleId,
            User = user,
            Role = organizerRole
        };
        _context.UserRoles.Add(userRole);

        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Team Outing",
            IsActive = true
        };
        _context.EventTypes.Add(eventType);

        var ev = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = "Quarterly Outing",
            EventTypeId = eventType.EventTypeId,
            EventDate = DateTime.UtcNow.Date,
            CreatedBy = userId,
            Description = "Legacy event without metadata tag",
            Status = EventStatus.Planned,
            BaseAmount = 8000,
            IsDeleted = false
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        // Act
        var results = await _eventRepository.GetAllForOrganizerAsync(userId);

        // Assert - pure organizer sees their event even without CREATOR_ROLE tag
        Assert.Single(results);
        Assert.Equal("Quarterly Outing", results.First().EventName);
    }
}
