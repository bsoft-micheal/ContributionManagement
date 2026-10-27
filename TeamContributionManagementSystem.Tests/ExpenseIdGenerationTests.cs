using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TeamContributionManagementSystem.Application.DTOs.Expenses;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Mappings;
using TeamContributionManagementSystem.Application.Services;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;
using TeamContributionManagementSystem.Infrastructure.Repositories;
using Xunit;

namespace TeamContributionManagementSystem.Tests;

public class ExpenseIdGenerationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ExpenseRepository _expenseRepository;
    private readonly ExpenseService _expenseService;
    private readonly IMapper _mapper;

    public ExpenseIdGenerationTests()
    {
        var dbName = "TestDb_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _context = new ApplicationDbContext(options);

        var loggerRepoMock = new Mock<ILogger<ExpenseRepository>>();
        _expenseRepository = new ExpenseRepository(_context, loggerRepoMock.Object);

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile));
        var sp = services.BuildServiceProvider();
        _mapper = sp.GetRequiredService<IMapper>();

        var loggerServiceMock = new Mock<ILogger<ExpenseService>>();
        var eventRepoMock = new Mock<IEventRepository>();
        // Mock event repo to allow budget validation to pass
        eventRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);
        eventRepoMock.Setup(x => x.GetByNameAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        _expenseService = new ExpenseService(
            loggerServiceMock.Object,
            _expenseRepository,
            eventRepoMock.Object,
            _context,
            _mapper
        );
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task Creation_ShouldGenerateExpenseNumber_InExpectedFormatAndIncrementWithinSameMonth()
    {
        // Arrange: October 2026
        var testDate = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);

        var request1 = new CreateExpenseRequestDto
        {
            EventName = "Annual Gala",
            Category = "Catering",
            Amount = 500,
            BudgetAmount = 500,
            ExpenseDate = testDate,
            SubmittedBy = "Alice",
            Description = "Catering advance"
        };

        var request2 = new CreateExpenseRequestDto
        {
            EventName = "Annual Gala",
            Category = "Decorations",
            Amount = 300,
            BudgetAmount = 300,
            ExpenseDate = testDate,
            SubmittedBy = "Bob",
            Description = "Decorations balance"
        };

        // Act
        var result1 = await _expenseService.CreateAsync(request1);
        var result2 = await _expenseService.CreateAsync(request2);

        // Assert
        Assert.Equal("EXP2026100001", result1.ExpenseNumber);
        Assert.Equal("EXP2026100002", result2.ExpenseNumber);

        // Verify database records
        var dbExpense1 = await _context.Expenses.FindAsync(result1.ExpenseId);
        var dbExpense2 = await _context.Expenses.FindAsync(result2.ExpenseId);
        Assert.NotNull(dbExpense1);
        Assert.NotNull(dbExpense2);
        Assert.Equal("EXP2026100001", dbExpense1.ExpenseNumber);
        Assert.Equal("EXP2026100002", dbExpense2.ExpenseNumber);
    }

    [Fact]
    public async Task Deletion_ShouldNeverResetOrReuseSequenceNumber()
    {
        // Arrange: October 2026
        var testDate = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

        var request1 = new CreateExpenseRequestDto
        {
            EventName = "Event A",
            Category = "Logistics",
            Amount = 100,
            BudgetAmount = 100,
            ExpenseDate = testDate,
            SubmittedBy = "User 1",
            Description = "Initial expense"
        };

        var request2 = new CreateExpenseRequestDto
        {
            EventName = "Event B",
            Category = "Logistics",
            Amount = 200,
            BudgetAmount = 200,
            ExpenseDate = testDate,
            SubmittedBy = "User 2",
            Description = "Second expense"
        };

        var created1 = await _expenseService.CreateAsync(request1);
        Assert.Equal("EXP2026100001", created1.ExpenseNumber);

        var created2 = await _expenseService.CreateAsync(request2);
        Assert.Equal("EXP2026100002", created2.ExpenseNumber);

        // Act: Delete both existing expenses
        await _expenseService.DeleteAsync(created1.ExpenseId);
        await _expenseService.DeleteAsync(created2.ExpenseId);

        // Also hard delete from DB to simulate permanent deletion and verify counter does not count records
        var allDbExpenses = await _context.Expenses.ToListAsync();
        _context.Expenses.RemoveRange(allDbExpenses);
        await _context.SaveChangesAsync();
        Assert.Empty(await _context.Expenses.ToListAsync());

        // Create new expense after deletion
        var request3 = new CreateExpenseRequestDto
        {
            EventName = "Event C",
            Category = "Logistics",
            Amount = 400,
            BudgetAmount = 400,
            ExpenseDate = testDate,
            SubmittedBy = "User 3",
            Description = "Expense after previous deletions"
        };

        var created3 = await _expenseService.CreateAsync(request3);

        // Assert: Sequence must NOT reset to 0001 or 0002. It must be 0003!
        Assert.Equal("EXP2026100003", created3.ExpenseNumber);
    }

    [Fact]
    public async Task MonthChanges_ShouldStartAt0001ForEachNewMonth()
    {
        // Arrange
        var dateOct = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);
        var dateNov = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
        var dateDec = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act: October
        var expOct1 = await _expenseService.CreateAsync(new CreateExpenseRequestDto
        {
            EventName = "Oct Event",
            Category = "General",
            Amount = 150,
            ExpenseDate = dateOct,
            SubmittedBy = "Alice",
            Description = "October Expense 1"
        });

        var expOct2 = await _expenseService.CreateAsync(new CreateExpenseRequestDto
        {
            EventName = "Oct Event",
            Category = "General",
            Amount = 250,
            ExpenseDate = dateOct,
            SubmittedBy = "Alice",
            Description = "October Expense 2"
        });

        // Act: November (new month -> starts at 0001)
        var expNov1 = await _expenseService.CreateAsync(new CreateExpenseRequestDto
        {
            EventName = "Nov Event",
            Category = "General",
            Amount = 300,
            ExpenseDate = dateNov,
            SubmittedBy = "Bob",
            Description = "November Expense 1"
        });

        var expNov2 = await _expenseService.CreateAsync(new CreateExpenseRequestDto
        {
            EventName = "Nov Event",
            Category = "General",
            Amount = 350,
            ExpenseDate = dateNov,
            SubmittedBy = "Bob",
            Description = "November Expense 2"
        });

        // Act: December (new month -> starts at 0001)
        var expDec1 = await _expenseService.CreateAsync(new CreateExpenseRequestDto
        {
            EventName = "Dec Event",
            Category = "General",
            Amount = 400,
            ExpenseDate = dateDec,
            SubmittedBy = "Charlie",
            Description = "December Expense 1"
        });

        // Act: Back to October (should continue from Oct sequence -> 0003)
        var expOct3 = await _expenseService.CreateAsync(new CreateExpenseRequestDto
        {
            EventName = "Oct Event",
            Category = "General",
            Amount = 500,
            ExpenseDate = dateOct,
            SubmittedBy = "Alice",
            Description = "October Expense 3"
        });

        // Assert
        Assert.Equal("EXP2026100001", expOct1.ExpenseNumber);
        Assert.Equal("EXP2026100002", expOct2.ExpenseNumber);
        Assert.Equal("EXP2026110001", expNov1.ExpenseNumber);
        Assert.Equal("EXP2026110002", expNov2.ExpenseNumber);
        Assert.Equal("EXP2026120001", expDec1.ExpenseNumber);
        Assert.Equal("EXP2026100003", expOct3.ExpenseNumber);
    }

    [Fact]
    public async Task Concurrency_ShouldGenerateUniqueMonotonicExpenseNumbersWithoutCollisions()
    {
        // Arrange
        var testDate = new DateTime(2026, 10, 26, 8, 30, 0, DateTimeKind.Utc);
        int concurrentCount = 20;

        // Act: Concurrently request sequence generation
        var tasks = Enumerable.Range(1, concurrentCount).Select(async i =>
        {
            return await _expenseRepository.GetNextExpenseNumberAsync(testDate);
        }).ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrentCount, results.Length);

        // All IDs must start with EXP202610
        Assert.All(results, id => Assert.StartsWith("EXP202610", id));

        // All IDs must be distinct (no duplicates)
        var distinctIds = results.Distinct().ToList();
        Assert.Equal(concurrentCount, distinctIds.Count);

        // Sequence numbers from 1 to 20 should all be present
        var expectedIds = Enumerable.Range(1, concurrentCount)
            .Select(seq => $"EXP202610{seq:D4}")
            .ToHashSet();

        Assert.Equal(expectedIds, distinctIds.ToHashSet());

        // Verify sequence entity in DB has last sequence recorded as 20
        var sequenceRecord = await _context.ExpenseSequences
            .FirstOrDefaultAsync(s => s.Year == 2026 && s.Month == 10);
        Assert.NotNull(sequenceRecord);
        Assert.Equal(concurrentCount, sequenceRecord.LastSequence);
    }
}
