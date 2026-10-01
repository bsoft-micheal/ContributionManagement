using Hangfire;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

/// <summary>
/// Background job service for automated and manual contribution reminder emails.
/// </summary>
public interface IContributionReminderJobService
{
    /// <summary>
    /// Daily recurring job: Finds active Planned/Ongoing events and pending contributions,
    /// and sends personalized reminder emails to members.
    /// </summary>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    Task ProcessDailyRemindersAsync(string? triggeredBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manual trigger: Enqueues reminders for all pending contributions in a specific event.
    /// </summary>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    Task SendRemindersForEventAsync(Guid eventId, string? triggeredBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manual trigger: Enqueues a reminder for a single contribution.
    /// </summary>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    Task SendReminderForContributionAsync(Guid contributionId, string? triggeredBy = null, CancellationToken cancellationToken = default);
}
