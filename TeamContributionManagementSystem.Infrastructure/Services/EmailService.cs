using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;
    private static readonly object _lock = new();

    public EmailService(IOptions<SmtpSettings> smtpOptions, ILogger<EmailService> logger)
    {
        _smtpSettings = smtpOptions?.Value ?? new SmtpSettings();
        _logger = logger;
    }

    private void CheckEmailLimit()
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
                    _logger.LogError(ex, CommonLogMessages.Emails.TrackerReadFailed);
                }
            }

            if (count >= 500)
            {
                _logger.LogError(CommonLogMessages.Emails.DailyLimitReached);
                throw new InvalidOperationException(CommonMessages.Emails.DailyLimitReached);
            }
        }
    }

    private void IncrementEmailCount()
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
                catch { }
            }

            count++;

            try
            {
                var newContent = $"{{\"Date\":\"{todayStr}\",\"Count\":{count}}}";
                File.WriteAllText(trackerPath, newContent);
                _logger.LogInformation(CommonLogMessages.Emails.DailyCountUpdated, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CommonLogMessages.Emails.TrackerWriteFailed);
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
        CheckEmailLimit();
        var inlineImageList = inlineImages?.Where(image => !string.IsNullOrWhiteSpace(image.ContentId) && !string.IsNullOrWhiteSpace(image.FilePath)).ToList()
            ?? new List<InlineEmailImage>();

        var host = _smtpSettings.Host;
        var port = _smtpSettings.Port > 0 ? _smtpSettings.Port : 587;
        var username = _smtpSettings.Username;
        var password = _smtpSettings.Password;
        var enableSsl = _smtpSettings.EnableSsl;
        var fromAddress = !string.IsNullOrWhiteSpace(_smtpSettings.FromAddress)
            ? _smtpSettings.FromAddress
            : (!string.IsNullOrWhiteSpace(username) ? username : CommonConstants.Defaults.DefaultFromAddress);
        var fromName = !string.IsNullOrWhiteSpace(_smtpSettings.FromName)
            ? _smtpSettings.FromName
            : CommonConstants.Defaults.DefaultFromName;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(CommonLogMessages.Emails.SmtpNotConfigured);

            try
            {
                var directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "SentEmails");
                Directory.CreateDirectory(directoryPath);
                var filePath = Path.Combine(directoryPath, $"{DateTime.UtcNow:yyyyMMddHHmmss}_{toEmail}.html");

                string localBody = body;
                foreach (var img in inlineImageList)
                {
                    if (File.Exists(img.FilePath))
                    {
                        var mType = string.IsNullOrWhiteSpace(img.MediaType) ? CommonConstants.Defaults.ImagePng : img.MediaType;
                        var b64 = Convert.ToBase64String(File.ReadAllBytes(img.FilePath));
                        localBody = localBody.Replace($"cid:{img.ContentId}", $"{CommonConstants.Defaults.DataUriPrefix}{mType};base64,{b64}");
                    }
                }

                await File.WriteAllTextAsync(filePath, localBody, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CommonLogMessages.Emails.MockEmailWriteFailed);
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
                        _logger.LogWarning(CommonLogMessages.Emails.InlineImageNotFound, image.FilePath);
                        continue;
                    }

                    var mediaType = string.IsNullOrWhiteSpace(image.MediaType) ? CommonConstants.Defaults.ImagePng : image.MediaType;
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
            IncrementEmailCount();
            _logger.LogInformation(CommonLogMessages.Emails.EmailSentSuccess, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.Emails.SmtpSendFailed, toEmail);
            throw new InvalidOperationException(string.Format(CommonMessages.Emails.SendFailedFormat, ex.Message), ex);
        }
    }
}
