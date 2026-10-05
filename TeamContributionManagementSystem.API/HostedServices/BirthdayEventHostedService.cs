using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.HostedServices;

public class BirthdayEventHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BirthdayEventHostedService> _logger;

    public BirthdayEventHostedService(IServiceProvider serviceProvider, ILogger<BirthdayEventHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunBirthdayAutomationAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(12));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunBirthdayAutomationAsync(stoppingToken);
        }
    }

    private async Task RunBirthdayAutomationAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBirthdayAutomationService>();

        try
        {
            var createdEvents = await service.CreateMonthlyBirthdayEventsAsync(cancellationToken);
            _logger.LogInformation(CommonLogMessages.Events.BirthdayAutomationCompleted, createdEvents);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, CommonLogMessages.Events.BirthdayAutomationFailed);
        }
    }
}
