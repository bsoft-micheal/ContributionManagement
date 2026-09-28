namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Strongly typed configuration model for SMTP email delivery settings.
/// Sensitive credentials (Username, Password) are populated via User Secrets or environment variables.
/// </summary>
public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
