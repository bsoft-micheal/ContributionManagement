using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private static readonly object _lock = new();

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private void CheckAndIncrementEmailCount()
    {
        var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);
        var trackerPath = Path.Combine(logDirectory, "email_limit_tracker.json");

        lock (_lock)
        {
            var todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            int count = 0;

            if (File.Exists(trackerPath))
            {
                try
                {
                    var content = File.ReadAllText(trackerPath);
                    if (content.Contains(todayStr))
                    {
                        var parts = content.Split(new[] { "\"Count\":" }, StringSplitOptions.None);
                        if (parts.Length > 1)
                        {
                            var countPart = parts[1].Split('}')[0].Trim();
                            int.TryParse(countPart, out count);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read email limit tracker file.");
                }
            }

            if (count >= 500)
            {
                _logger.LogError("Email send blocked. Daily email limit of 500 reached to protect Gmail SMTP threshold.");
                throw new InvalidOperationException("Daily email sending limit (500) has been reached. Please try again tomorrow.");
            }

            count++;

            try
            {
                var newContent = $"{{\"Date\":\"{todayStr}\",\"Count\":{count}}}";
                File.WriteAllText(trackerPath, newContent);
                _logger.LogInformation("Daily email count updated: {Count}/500", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write email limit tracker file.");
            }
        }
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        IEnumerable<InlineEmailImage>? inlineImages = null,
        CancellationToken cancellationToken = default)
    {
        // Enforce the 500 emails/day restriction
        CheckAndIncrementEmailCount();
        var inlineImageList = inlineImages?.Where(image => !string.IsNullOrWhiteSpace(image.ContentId) && !string.IsNullOrWhiteSpace(image.FilePath)).ToList()
            ?? new List<InlineEmailImage>();

        var smtpSection = _configuration.GetSection("Smtp");
        var host = smtpSection["Host"];
        
        int.TryParse(smtpSection["Port"], out var port);
        if (port == 0) port = 587;
        
        var username = smtpSection["Username"];
        var password = smtpSection["Password"];
        
        if (!bool.TryParse(smtpSection["EnableSsl"], out var enableSsl))
        {
            enableSsl = true;
        }
        
        var fromAddress = smtpSection["FromAddress"];
        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            fromAddress = username ?? "noreply@teamcontribution.local";
        }
        
        var fromName = smtpSection["FromName"];
        if (string.IsNullOrWhiteSpace(fromName))
        {
            fromName = "Team Contribution System";
        }

        // Fallback local logging and file saving for local development if SMTP is not fully configured
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("SMTP is not fully configured in appsettings.json. Logging email instead.");
            _logger.LogInformation("========================================\n" +
                                   "EMAIL TO: {ToEmail}\n" +
                                   "SUBJECT: {Subject}\n" +
                                   "BODY:\n{Body}\n" +
                                   "========================================", toEmail, subject, body);

            try
            {
                var directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "SentEmails");
                Directory.CreateDirectory(directoryPath);
                var filePath = Path.Combine(directoryPath, $"{DateTime.UtcNow:yyyyMMddHHmmss}_{toEmail}.html");

                // Replace cid: references with base64 data URIs for local browser preview
                string localBody = body;
                foreach (var img in inlineImageList)
                {
                    if (File.Exists(img.FilePath))
                    {
                        var mType = string.IsNullOrWhiteSpace(img.MediaType) ? "image/png" : img.MediaType;
                        var b64 = Convert.ToBase64String(File.ReadAllBytes(img.FilePath));
                        localBody = localBody.Replace($"cid:{img.ContentId}", $"data:{mType};base64,{b64}");
                    }
                }

                await File.WriteAllTextAsync(filePath, localBody, cancellationToken);
                _logger.LogInformation("Email mock saved to local file: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write mock email to local file.");
            }

            return;
        }

        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject
            };
            mailMessage.To.Add(toEmail);

            if (inlineImageList.Count > 0)
            {
                var alternateView = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);

                foreach (var image in inlineImageList)
                {
                    if (!File.Exists(image.FilePath))
                    {
                        _logger.LogWarning("Inline email image not found at {FilePath}.", image.FilePath);
                        continue;
                    }

                    var mediaType = string.IsNullOrWhiteSpace(image.MediaType) ? "image/png" : image.MediaType;
                    var linkedResource = new LinkedResource(image.FilePath, mediaType)
                    {
                        ContentId = image.ContentId,
                        TransferEncoding = TransferEncoding.Base64
                    };
                    linkedResource.ContentType.Name = Path.GetFileName(image.FilePath);
                    linkedResource.ContentType.MediaType = mediaType;
                    alternateView.LinkedResources.Add(linkedResource);
                }

                mailMessage.AlternateViews.Add(alternateView);
            }
            else
            {
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;
            }

            using var smtpClient = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {ToEmail} via SMTP.", toEmail);
            throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
        }
    }
}
