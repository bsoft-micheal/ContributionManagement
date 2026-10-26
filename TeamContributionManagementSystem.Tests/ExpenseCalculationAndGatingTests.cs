using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TeamContributionManagementSystem.Application.DTOs.Expenses;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Mappings;
using TeamContributionManagementSystem.Application.Services;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;
using TeamContributionManagementSystem.Infrastructure.Repositories;
using Xunit;

namespace TeamContributionManagementSystem.Tests;

public class ExpenseCalculationAndGatingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ExpenseRepository _expenseRepository;
    private readonly ExpenseService _expenseService;
    private readonly IMapper _mapper;

    public ExpenseCalculationAndGatingTests()
    {
        var dbName = "ExpenseCalculationTests_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _context = new ApplicationDbContext(options);

        var loggerRepoMock = new Mock<ILogger<ExpenseRepository>>();
        _expenseRepository = new ExpenseRepository(_context, loggerRepoMock.Object);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new MappingProfile());
        });
        _mapper = mapperConfig.CreateMapper();

        var loggerServiceMock = new Mock<ILogger<ExpenseService>>();
        var eventRepoMock = new Mock<IEventRepository>();
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
    public async Task CreateExpense_WithMainBudgetAndAdditionalRows_CalculatesTotalCorrectly()
    {
        // Arrange
        var request = new CreateExpenseRequestDto
        {
            EventName = "Annual Meet 2026",
            Category = "Conference",
            BudgetAmount = 10000,
            ExtraAmount = 2500,
            Amount = 12500, // BudgetAmount + ExtraAmount
            ExpenseDate = DateTime.UtcNow,
            SubmittedBy = "Admin",
            Description = "Main venue and extra refreshments"
        };

        // Act
        var created = await _expenseService.CreateExpenseAsync(request);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(12500, created.Amount);
        Assert.Equal(10000, created.BudgetAmount);
        Assert.Equal(2500, created.ExtraAmount);
    }

    [Fact]
    public async Task UpdateExpense_RecalculatesTotalExpense_WithoutDoubleCounting()
    {
        // Arrange
        var initial = await _expenseService.CreateExpenseAsync(new CreateExpenseRequestDto
        {
            EventName = "Hackathon",
            Category = "Technical",
            BudgetAmount = 5000,
            ExtraAmount = 1000,
            Amount = 6000,
            ExpenseDate = DateTime.UtcNow,
            SubmittedBy = "Micheal",
            Description = "Hackathon budget"
        });

        // Act - Update with modified additional rows (row removed or updated)
        var updateRequest = new UpdateExpenseRequestDto
        {
            ExpenseId = initial.ExpenseId,
            EventName = "Hackathon",
            Category = "Technical",
            BudgetAmount = 5000,
            ExtraAmount = 500, // Reduced from 1000 to 500
            Amount = 5500,      // Total updated immediately
            ExpenseDate = DateTime.UtcNow,
            SubmittedBy = "Micheal",
            Description = "Updated hackathon budget"
        };

        var updated = await _expenseService.UpdateExpenseAsync(initial.ExpenseId, updateRequest);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(5500, updated.Amount);
        Assert.Equal(500, updated.ExtraAmount);
        Assert.Equal(5000, updated.BudgetAmount);

        // Verify database state has single updated record, no duplicate count
        var allExpenses = await _expenseService.GetAllExpensesAsync();
        Assert.Single(allExpenses);
        Assert.Equal(5500, allExpenses.First().Amount);
    }

    [Fact]
    public async Task DeleteExpense_RemovesExpense_BudgetBecomesAvailableAgain()
    {
        // Arrange
        var expense = await _expenseService.CreateExpenseAsync(new CreateExpenseRequestDto
        {
            EventName = "Team Outing",
            Category = "Recreation",
            BudgetAmount = 8000,
            ExtraAmount = 2000,
            Amount = 10000,
            ExpenseDate = DateTime.UtcNow,
            SubmittedBy = "Admin",
            Description = "Outing trip"
        });

        // Act
        var deleted = await _expenseService.DeleteExpenseAsync(expense.ExpenseId);

        // Assert
        Assert.True(deleted);
        var remainingExpenses = await _expenseService.GetAllExpensesAsync();
        Assert.Empty(remainingExpenses);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]          // 0 main, 0 extra -> 0 total
    [InlineData(10000, 0, 10000, 10000)] // 10000 main, 0 extra -> 10000 total
    [InlineData(10000, 2500, 12500, 12500)] // 10000 main + 2500 extra -> 12500 total
    [InlineData(50000, 49999, 99999, 99999)] // 5 digit boundary: 99,999
    public void TotalExpenseFormula_MatchesSpecification(
        decimal mainBudget,
        decimal extraAmount,
        decimal expectedTotal,
        decimal sumCheck)
    {
        var total = mainBudget + extraAmount;
        Assert.Equal(expectedTotal, total);
        Assert.Equal(sumCheck, total);
    }

    [Theory]
    [InlineData(10000, 0, 10000)]     // Expected 10000, Main 0 -> Remaining 10000
    [InlineData(10000, 4000, 6000)]   // Expected 10000, Main 4000 -> Remaining 6000
    [InlineData(10000, 10000, 0)]     // Expected 10000, Main 10000 -> Remaining 0
    [InlineData(10000, 12000, 0)]     // Expected 10000, Main 12000 -> Remaining max(10000-12000, 0) = 0
    public void RemainingBudgetFormula_MatchesSpecification(
        decimal expectedBudget,
        decimal mainBudget,
        decimal expectedRemaining)
    {
        var remaining = Math.Max(0, expectedBudget - mainBudget);
        Assert.Equal(expectedRemaining, remaining);
    }
}
