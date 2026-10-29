using TeamContributionManagementSystem.Application.Services;
using TeamContributionManagementSystem.Application.Mappings;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.DTOs.Expenses;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.DTOs.Events;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
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

    [Fact]
    public async Task EventRepository_EnrichEventAdjustmentMetadata_CalculatesTotalExpectedWithSponsorAndAllocated()
    {
        // Scenario: Budget ₹10,000, Sponsor ₹5,000, Allocated from hand ₹2,000
        // Expected: TotalExpectedAmount = ₹3,000, RemainingFundingRequirement = ₹5,000, OriginalPlannedBudget = ₹10,000
        var sponsorUser = new AppUser
        {
            UserId = Guid.NewGuid(),
            FullName = "Sponsor Hand",
            Username = "sponsorhand",
            Email = "sponsorhand@example.com",
            IsActive = true,
            IsDeleted = false
        };
        _context.Users.Add(sponsorUser);

        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Sports Day",
            IsActive = true
        };
        _context.EventTypes.Add(eventType);

        var eventItem = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = "Annual Sports 2026",
            EventTypeId = eventType.EventTypeId,
            EventType = eventType,
            EventDate = DateTime.UtcNow.AddDays(20),
            Status = EventStatus.Planned,
            BaseAmount = 10000m,
            RequireSponsorContribution = true,
            EnableCelebrantSelection = false,
            UseAvailableBalance = true,
            AllocatedAmount = 2000m,
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
        Assert.Equal(10000m, result.BaseAmount);
        Assert.Equal(10000m, result.OriginalPlannedBudget);
        Assert.Equal(5000m, result.SponsorContributionAmount);
        Assert.Equal(5000m, result.RemainingFundingRequirement);
        Assert.Equal(3000m, result.TotalExpectedAmount); // 5000 remaining - 2000 allocated = 3000
        Assert.Equal(3000m, result.PendingAmount);
    }

    [Fact]
    public async Task EventRepository_ReplacedSponsor_OnlyActiveSponsorAffectsRemainingFundingRequirement()
    {
        // Scenario: Budget ₹10,000, replaced sponsor: sp1 (₹4,000 inactive), sp2 (₹3,000 active)
        // Expected: Remaining funding requirement is ₹7,000 (10000 - 3000)
        var sp1User = new AppUser
        {
            UserId = Guid.NewGuid(),
            FullName = "Old Sponsor User",
            Username = "oldsp",
            Email = "oldsp@example.com",
            IsActive = true,
            IsDeleted = false
        };
        var sp2User = new AppUser
        {
            UserId = Guid.NewGuid(),
            FullName = "New Active Sponsor User",
            Username = "newsp",
            Email = "newsp@example.com",
            IsActive = true,
            IsDeleted = false
        };
        _context.Users.AddRange(sp1User, sp2User);

        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Farewell",
            IsActive = true
        };
        _context.EventTypes.Add(eventType);

        var eventItem = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = "Farewell 2026",
            EventTypeId = eventType.EventTypeId,
            EventType = eventType,
            EventDate = DateTime.UtcNow.AddDays(12),
            Status = EventStatus.Planned,
            BaseAmount = 10000m,
            RequireSponsorContribution = true,
            EnableCelebrantSelection = false,
            CreatedBy = sp2User.UserId,
            CreatedByUser = sp2User,
            IsDeleted = false
        };

        var sp1 = new EventSponsor
        {
            SponsorId = Guid.NewGuid(),
            EventId = eventItem.EventId,
            Event = eventItem,
            UserId = sp1User.UserId,
            SponsorUser = sp1User,
            ContributionAmount = 4000m,
            IsActive = false, // Deactivated / replaced
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var sp2 = new EventSponsor
        {
            SponsorId = Guid.NewGuid(),
            EventId = eventItem.EventId,
            Event = eventItem,
            UserId = sp2User.UserId,
            SponsorUser = sp2User,
            ContributionAmount = 3000m,
            IsActive = true, // Active
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
        Assert.Equal(10000m, result.BaseAmount);
        Assert.Equal(3000m, result.SponsorContributionAmount); // Only active sponsor is reported
        Assert.Equal(7000m, result.RemainingFundingRequirement); // 10000 - 3000 = 7000
        Assert.Equal(7000m, result.TotalExpectedAmount);
    }

    private EventService CreateEventService(EventType eventType, List<Member> members)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile));
        var sp = services.BuildServiceProvider();
        var mapper = sp.GetRequiredService<IMapper>();

        var eventTypeRepoMock = new Mock<IEventTypeRepository>();
        eventTypeRepoMock.Setup(x => x.GetByIdAsync(eventType.EventTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(eventType);

        var memberRepoMock = new Mock<IMemberRepository>();
        memberRepoMock.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        var userRepoMock = new Mock<IUserRepository>();
        var contribRepoMock = new Mock<IContributionRepository>();
        contribRepoMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContributionDto>());

        var budgetCalcRepoMock = new Mock<IBudgetCalculationRepository>();
        var expenseRepoMock = new Mock<IExpenseRepository>();
        expenseRepoMock.Setup(x => x.GetAllAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExpenseDto>());

        var galleryRepoMock = new Mock<IGalleryRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var emailServiceMock = new Mock<IEmailService>();
        var loggerMock = new Mock<ILogger<EventService>>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        return new EventService(
            _eventRepository,
            eventTypeRepoMock.Object,
            memberRepoMock.Object,
            userRepoMock.Object,
            contribRepoMock.Object,
            budgetCalcRepoMock.Object,
            expenseRepoMock.Object,
            galleryRepoMock.Object,
            unitOfWorkMock.Object,
            mapper,
            emailServiceMock.Object,
            loggerMock.Object,
            scopeFactoryMock.Object
        );
    }

    [Fact]
    public async Task CalculateContributionPreview_DeductsSponsorFromFundingRequirement_KeepsPlannedBudget()
    {
        // Scenario: Planned Budget ₹10,000, Sponsor ₹5,000, 2 eligible members
        // Expected: PlannedBudget = ₹10,000, RemainingFundingRequirement = ₹5,000, ExpectedCollection = ₹5,000, Member share = ₹2,500 each
        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Team Party",
            IsActive = true
        };

        var m1Id = Guid.NewGuid();
        var m2Id = Guid.NewGuid();
        var members = new List<Member>
        {
            new Member { MemberId = m1Id, Name = "Alice", IsActive = true, WorkType = "Office" },
            new Member { MemberId = m2Id, Name = "Bob", IsActive = true, WorkType = "Office" }
        };

        var eventService = CreateEventService(eventType, members);

        var request = new EventContributionPreviewRequestDto
        {
            EventTypeId = eventType.EventTypeId,
            BaseAmount = 10000m,
            RequireSponsorContribution = true,
            SponsorContributionAmount = 5000m,
            SponsorUserId = Guid.NewGuid(),
            ParticipantIds = new List<Guid> { m1Id, m2Id },
            EventDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var preview = await eventService.CalculateContributionPreviewAsync(request);

        // Assert
        Assert.NotNull(preview);
        Assert.Equal(10000m, preview.PlannedBudget); // Total planned budget unchanged
        Assert.Equal(5000m, preview.TotalSponsorContributionAmount); // Sponsor contribution is 5000
        Assert.Equal(5000m, preview.RemainingFundingRequirement); // Remaining requirement is 5000
        Assert.Equal(5000m, preview.ExpectedCollection); // Collection from members targets remaining requirement
        Assert.Equal(2, preview.EligibleContributors);
        Assert.All(preview.Members, m => Assert.Equal(2500m, m.ContributionAmount)); // Each pays 2500
    }

    [Fact]
    public async Task CalculateContributionPreview_ThrowsWhenSponsorExceedsPlannedBudget()
    {
        // Scenario: Planned Budget ₹10,000, Sponsor ₹15,000
        // Expected: Throws InvalidOperationException
        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Team Dinner",
            IsActive = true
        };

        var m1Id = Guid.NewGuid();
        var members = new List<Member>
        {
            new Member { MemberId = m1Id, Name = "Alice", IsActive = true, WorkType = "Office" }
        };

        var eventService = CreateEventService(eventType, members);

        var request = new EventContributionPreviewRequestDto
        {
            EventTypeId = eventType.EventTypeId,
            BaseAmount = 10000m,
            RequireSponsorContribution = true,
            SponsorContributionAmount = 15000m, // Exceeds budget!
            SponsorUserId = Guid.NewGuid(),
            ParticipantIds = new List<Guid> { m1Id },
            EventDate = DateTime.UtcNow.AddDays(7)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            eventService.CalculateContributionPreviewAsync(request));

        Assert.Contains("cannot exceed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CalculateContributionPreview_SafeWhenZeroEligibleContributors()
    {
        // Scenario: Zero eligible contributors
        // Expected: Safe return with 0 collection without division by zero
        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Team Event",
            IsActive = true
        };

        var eventService = CreateEventService(eventType, new List<Member>());

        var request = new EventContributionPreviewRequestDto
        {
            EventTypeId = eventType.EventTypeId,
            BaseAmount = 10000m,
            RequireSponsorContribution = true,
            SponsorContributionAmount = 5000m,
            ParticipantIds = new List<Guid>(), // Zero contributors
            EventDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var preview = await eventService.CalculateContributionPreviewAsync(request);

        // Assert
        Assert.NotNull(preview);
        Assert.Equal(0, preview.EligibleContributors);
        Assert.Equal(0m, preview.ExpectedCollection);
        Assert.Equal(10000m, preview.PlannedBudget);
    }

    [Fact]
    public async Task CalculateContributionPreview_SponsorWithAvailableBalance_NoDoubleCounting()
    {
        // Scenario: Planned Budget ₹10,000, Sponsor ₹5,000, Available balance deduction ₹2,000, 2 members
        // Expected: Remaining funding = ₹5,000, Expected collection from members = ₹3,000 (₹1,500 each)
        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = "Tech Meetup",
            IsActive = true
        };

        var m1Id = Guid.NewGuid();
        var m2Id = Guid.NewGuid();
        var members = new List<Member>
        {
            new Member { MemberId = m1Id, Name = "Alice", IsActive = true, WorkType = "Office" },
            new Member { MemberId = m2Id, Name = "Bob", IsActive = true, WorkType = "Office" }
        };

        var eventService = CreateEventService(eventType, members);

        var request = new EventContributionPreviewRequestDto
        {
            EventTypeId = eventType.EventTypeId,
            BaseAmount = 10000m,
            RequireSponsorContribution = true,
            SponsorContributionAmount = 5000m,
            SponsorUserId = Guid.NewGuid(),
            UseAvailableBalance = true,
            AllocatedAmount = 2000m,
            ParticipantIds = new List<Guid> { m1Id, m2Id },
            EventDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var preview = await eventService.CalculateContributionPreviewAsync(request);

        // Assert
        Assert.NotNull(preview);
        Assert.Equal(10000m, preview.PlannedBudget);
        Assert.Equal(5000m, preview.TotalSponsorContributionAmount);
        Assert.Equal(5000m, preview.RemainingFundingRequirement);
        Assert.Equal(3000m, preview.ExpectedCollection); // 5000 - 2000 allocated = 3000
        Assert.Equal(2, preview.EligibleContributors);
        Assert.All(preview.Members, m => Assert.Equal(1500m, m.ContributionAmount)); // Each member pays 1500
    }
}
