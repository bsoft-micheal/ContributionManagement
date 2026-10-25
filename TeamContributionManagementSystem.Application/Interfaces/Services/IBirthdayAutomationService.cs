namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IBirthdayAutomationService
{
    Task<int> CreateMonthlyBirthdayEventsAsync(CancellationToken cancellationToken = default);
    Task ProcessDailyBirthdayRemindersAsync(string? triggeredBy = null, CancellationToken cancellationToken = default);
    Task ProcessCatchUpBirthdayReminderForUserAsync(Guid newUserId, CancellationToken cancellationToken = default);
}

