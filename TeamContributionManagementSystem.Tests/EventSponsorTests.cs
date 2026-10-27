using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;
using TeamContributionManagementSystem.Infrastructure.Repositories;
using Xunit;

namespace TeamContributionManagementSystem.Tests;

public class EventSponsorTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EventRepository _eventRepository;

    public EventSponsorTests()
    {
        var dbName = "EventSponsorTests_" + Guid.NewGuid().ToString("N");
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
    public async Task EventRepository_ProjectsActiveSponsorCorrectly()
    {
        // Arrange
        var sponsorUser = new AppUser
        {
            UserId = Guid.NewGuid(),
            FullName = "Daniel Sponsor",
            Username = "danielsponsor",
            Email = "daniel@sponsor.com",
            IsActive = true,
            IsDeleted = false
        };
        _context.Users.Add(sponsorUser);

        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Christmass",
            IsActive = true
        };
        _context.EventTypes.Add(eventType);

        var eventItem = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = "Christmas Celebration 2026",
            EventTypeId = eventType.EventTypeId,
            EventType = eventType,
            EventDate = DateTime.UtcNow.AddDays(10),
            Status = EventStatus.Planned,
            BaseAmount = 10000m,
            RequireSponsorContribution = true,
            EnableCelebrantSelection = false,
            CreatedBy = sponsorUser.UserId,
            CreatedByUser = sponsorUser,
            IsDeleted = false
        };

        var sponsor = new EventSponsor
        {
            SponsorId = Guid.NewGuid(),
            EventId = eventItem.EventId,
            Event = eventItem,
            SponsorUserId = sponsorUser.UserId,
            SponsorUser = sponsorUser,
            ContributionAmount = 5000m,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        var history = new EventSponsorHistory
        {
            HistoryId = Guid.NewGuid(),
            SponsorId = sponsor.SponsorId,
            Sponsor = sponsor,
            EventId = eventItem.EventId,
            Event = eventItem,
            SponsorUserId = sponsorUser.UserId,
            SponsorUser = sponsorUser,
            ContributionAmount = 5000m,
            ChangeType = "CREATED",
            ActionDate = DateTime.UtcNow
        };
        sponsor.Histories.Add(history);
        eventItem.Sponsors.Add(sponsor);

        _context.Events.Add(eventItem);
        _context.EventSponsors.Add(sponsor);
        await _context.SaveChangesAsync();

        // Act
        var result = await _eventRepository.GetByIdWithDetailsAsync(eventItem.EventId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.RequireSponsorContribution);
        Assert.Equal(sponsorUser.UserId, result.SponsorUserId);
        Assert.Equal("Daniel Sponsor", result.SponsorName);
        Assert.Equal(5000m, result.SponsorContributionAmount);
    }

    [Fact]
    public async Task EventRepository_DoesNotProjectInactiveOrDeletedSponsor()
    {
        // Arrange
        var user = new AppUser
        {
            UserId = Guid.NewGuid(),
            FullName = "Creator User",
            Username = "creator",
            Email = "creator@example.com",
            IsActive = true,
            IsDeleted = false
        };
        _context.Users.Add(user);

        var inactiveSponsorUser = new AppUser
        {
            UserId = Guid.NewGuid(),
            FullName = "Old Sponsor",
            Username = "oldsponsor",
            Email = "old@sponsor.com",
            IsActive = true,
            IsDeleted = false
        };
        _context.Users.Add(inactiveSponsorUser);

        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Office Gathering",
            IsActive = true
        };
        _context.EventTypes.Add(eventType);

        var eventItem = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = "Gathering Without Sponsor",
            EventTypeId = eventType.EventTypeId,
            EventType = eventType,
            EventDate = DateTime.UtcNow.AddDays(5),
            Status = EventStatus.Planned,
            BaseAmount = 5000m,
            RequireSponsorContribution = false,
            CreatedBy = user.UserId,
            CreatedByUser = user,
            IsDeleted = false
        };

        var deactivatedSponsor = new EventSponsor
        {
            SponsorId = Guid.NewGuid(),
            EventId = eventItem.EventId,
            Event = eventItem,
            SponsorUserId = inactiveSponsorUser.UserId,
            SponsorUser = inactiveSponsorUser,
            ContributionAmount = 2000m,
            IsActive = false, // Inactive!
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        eventItem.Sponsors.Add(deactivatedSponsor);

        _context.Events.Add(eventItem);
        _context.EventSponsors.Add(deactivatedSponsor);
        await _context.SaveChangesAsync();

        // Act
        var result = await _eventRepository.GetByIdWithDetailsAsync(eventItem.EventId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.RequireSponsorContribution);
        Assert.Null(result.SponsorUserId);
        Assert.Null(result.SponsorName);
        Assert.Null(result.SponsorContributionAmount);
    }

    [Fact]
    public async Task EventRepository_ProjectsMultipleActiveSponsorsCorrectly()
    {
        // Arrange
        var sponsor1 = new AppUser
        {
            UserId = Guid.NewGuid(),
            FullName = "Sponsor One",
            Username = "sponsor1",
            Email = "sponsor1@example.com",
            IsActive = true,
            IsDeleted = false
        };
        var sponsor2 = new AppUser
        {
            UserId = Guid.NewGuid(),
            FullName = "Sponsor Two",
            Username = "sponsor2",
            Email = "sponsor2@example.com",
            IsActive = true,
            IsDeleted = false
        };
        _context.Users.AddRange(sponsor1, sponsor2);

        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Festival",
            IsActive = true
        };
        _context.EventTypes.Add(eventType);

        var eventItem = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = "Grand Festival",
            EventTypeId = eventType.EventTypeId,
            EventType = eventType,
            EventDate = DateTime.UtcNow.AddDays(5),
            Status = EventStatus.Planned,
            BaseAmount = 20000m,
            RequireSponsorContribution = true,
            EnableCelebrantSelection = false,
            CreatedBy = sponsor1.UserId,
            CreatedByUser = sponsor1,
            IsDeleted = false
        };

        var sp1 = new EventSponsor
        {
            SponsorId = Guid.NewGuid(),
            EventId = eventItem.EventId,
            Event = eventItem,
            UserId = sponsor1.UserId,
            SponsorUser = sponsor1,
            ContributionAmount = 6000m,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        var sp2 = new EventSponsor
        {
            SponsorId = Guid.NewGuid(),
            EventId = eventItem.EventId,
            Event = eventItem,
            UserId = sponsor2.UserId,
            SponsorUser = sponsor2,
            ContributionAmount = 4000m,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        eventItem.Sponsors.Add(sp1);
        eventItem.Sponsors.Add(sp2);

        _context.Events.Add(eventItem);
        _context.EventSponsors.AddRange(sp1, sp2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _eventRepository.GetByIdWithDetailsAsync(eventItem.EventId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.RequireSponsorContribution);
        Assert.Equal(10000m, result.SponsorContributionAmount);
        Assert.Equal(2, result.Sponsors.Count);
        Assert.Equal(20000m, result.BaseAmount);
        Assert.Equal(10000m, result.RemainingFundingRequirement);
    }

    [Fact]
    public async Task EventRepository_CalculatesRemainingFundingRequirement_WithSponsorDeduction()
    {
        // Scenario: Budget ₹10,000, sponsor ₹5,000
        // Expected: Remaining funding requirement is ₹5,000, BaseAmount remains ₹10,000
        var sponsorUser = new AppUser
        {
            UserId = Guid.NewGuid(),
            FullName = "Generous Sponsor",
            Username = "generous",
            Email = "generous@sponsor.com",
            IsActive = true,
            IsDeleted = false
        };
        _context.Users.Add(sponsorUser);

        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Team Outing",
            IsActive = true
        };
        _context.EventTypes.Add(eventType);

        var eventItem = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = "Annual Outing 2026",
            EventTypeId = eventType.EventTypeId,
            EventType = eventType,
            EventDate = DateTime.UtcNow.AddDays(15),
            Status = EventStatus.Planned,
            BaseAmount = 10000m,
            RequireSponsorContribution = true,
            EnableCelebrantSelection = false,
            CreatedBy = sponsorUser.UserId,
            CreatedByUser = sponsorUser,
            IsDeleted = false
        };

        var sponsor = new EventSponsor
        {
            SponsorId = Guid.NewGuid(),
            EventId = eventItem.EventId,
            Event = eventItem,
            UserId = sponsorUser.UserId,
            SponsorUser = sponsorUser,
            ContributionAmount = 5000m,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        eventItem.Sponsors.Add(sponsor);

        _context.Events.Add(eventItem);
        _context.EventSponsors.Add(sponsor);
        await _context.SaveChangesAsync();

        // Act
        var result = await _eventRepository.GetByIdWithDetailsAsync(eventItem.EventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10000m, result.BaseAmount); // Planned budget remains ₹10,000
        Assert.Equal(5000m, result.SponsorContributionAmount); // Sponsor contribution is ₹5,000
        Assert.Equal(5000m, result.RemainingFundingRequirement); // Remaining budget after sponsor is ₹5,000
    }

    [Fact]
    public async Task EventRepository_ExcludesInactiveSponsor_FromRemainingFundingRequirement()
    {
        // Scenario: Budget ₹10,000, inactive sponsor ₹5,000
        // Expected: Remaining funding requirement remains ₹10,000
        var sponsorUser = new AppUser
        {
            UserId = Guid.NewGuid(),
            FullName = "Former Sponsor",
            Username = "former",
            Email = "former@sponsor.com",
            IsActive = true,
            IsDeleted = false
        };
        _context.Users.Add(sponsorUser);

        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Lunch",
            IsActive = true
        };
        _context.EventTypes.Add(eventType);

        var eventItem = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = "Team Lunch",
            EventTypeId = eventType.EventTypeId,
            EventType = eventType,
            EventDate = DateTime.UtcNow.AddDays(7),
            Status = EventStatus.Planned,
            BaseAmount = 10000m,
            RequireSponsorContribution = false, // disabled
            EnableCelebrantSelection = false,
            CreatedBy = sponsorUser.UserId,
            CreatedByUser = sponsorUser,
            IsDeleted = false
        };

        var inactiveSponsor = new EventSponsor
        {
            SponsorId = Guid.NewGuid(),
            EventId = eventItem.EventId,
            Event = eventItem,
            UserId = sponsorUser.UserId,
            SponsorUser = sponsorUser,
            ContributionAmount = 5000m,
            IsActive = false, // inactive!
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        eventItem.Sponsors.Add(inactiveSponsor);

        _context.Events.Add(eventItem);
        _context.EventSponsors.Add(inactiveSponsor);
        await _context.SaveChangesAsync();

        // Act
        var result = await _eventRepository.GetByIdWithDetailsAsync(eventItem.EventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10000m, result.BaseAmount);
        Assert.Null(result.SponsorContributionAmount);
        Assert.Equal(10000m, result.RemainingFundingRequirement); // Not deducted because sponsor is inactive
    }
}
