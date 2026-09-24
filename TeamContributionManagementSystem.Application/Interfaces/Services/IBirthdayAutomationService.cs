namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IBirthdayAutomationService
{
    Task<int> CreateMonthlyBirthdayEventsAsync(CancellationToken cancellationToken = default);
}
