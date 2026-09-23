namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        IEnumerable<InlineEmailImage>? inlineImages = null,
        CancellationToken cancellationToken = default);
}

public sealed record InlineEmailImage(string ContentId, string FilePath, string? MediaType = null);
