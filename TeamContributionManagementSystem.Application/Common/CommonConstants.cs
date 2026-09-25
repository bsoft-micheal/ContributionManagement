namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Centralized application constants to eliminate all hardcoded literals across backend services, controllers, and repositories.
/// </summary>
public static class CommonConstants
{
    public static class PaymentStatuses
    {
        public const string All = "ALL";
        public const string Pending = "Pending";
        public const string Verified = "Verified";
        public const string Failed = "Failed";
        public const string NeedsClarification = "Needs Clarification";
        public const string Paid = "Paid";
        public const string Rejected = "Rejected";
        public const string Approved = "Approved";
    }

    public static class ExpenseStatuses
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Paid = "Paid";
    }

    public static class PaymentModes
    {
        public const string All = "ALL";
        public const string Upi = "UPI";
        public const string GPay = "GPay";
        public const string PhonePe = "PhonePe";
        public const string Paytm = "Paytm";
        public const string BankTransfer = "Bank Transfer";
        public const string Cash = "Cash";
        public const string Split = "Split";
        public const string None = "None";
    }

    public static class PaymentScopes
    {
        public const string PreviousArrears = "PreviousArrears";
        public const string AllOutstanding = "AllOutstanding";
    }

    public static class EventTypeNames
    {
        public const string Birthday = "Birthday";
        public const string Farewell = "Farewell";
        public const string Festival = "Festival";
        public const string General = "General";
    }

    public static class TicketStatuses
    {
        public const string Open = "Open";
        public const string InProgress = "In Progress";
        public const string Resolved = "Resolved";
        public const string Closed = "Closed";
    }

    public static class TicketPriorities
    {
        public const string Low = "Low";
        public const string Medium = "Medium";
        public const string High = "High";
        public const string Urgent = "Urgent";
    }

    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string User = "User";
    }

    public static class FinancialStatus
    {
        public const string Surplus = "Surplus";
        public const string Deficit = "Deficit";
    }

    public static class AgingCategories
    {
        public const string Critical = "Critical (> 30d)";
        public const string Moderate = "Moderate (15-30d)";
        public const string Recent = "Recent (< 15d)";
    }

    public static class SettingKeys
    {
        public const string OrgName = "orgName";
        public const string BirthdayMembersExempt = "birthdayMembersExempt";
        public const string DefaultCurrency = "defaultCurrency";
        public const string TimeZone = "timeZone";
        public const string FromEmail = "fromEmail";
        public const string FromName = "fromName";
        public const string SmtpHost = "smtpHost";
        public const string SmtpPort = "smtpPort";
        public const string Encryption = "encryption";
        public const string OtpExpiry = "otpExpiry";
        public const string MaxRetry = "maxRetry";
        public const string EnableOtpLogin = "enableOtpLogin";
        public const string Enable2faAdmin = "enable2faAdmin";
        public const string EnableEmailNotif = "enableEmailNotif";
        public const string NotifNewMember = "notifNewMember";
        public const string NotifPaymentConfirm = "notifPaymentConfirm";
        public const string NotifEventReminder = "notifEventReminder";
        public const string NotifSupportTicket = "notifSupportTicket";
        public const string QrReceiverName = "qrReceiverName";
        public const string QrUpiId = "qrUpiId";
        public const string QrImage = "qrImage";
        public const string EnableAuditLogs = "enableAuditLogs";
        public const string LogUserLogin = "logUserLogin";
        public const string LogDataChanges = "logDataChanges";
        public const string LogConfigChanges = "logConfigChanges";
        public const string RetentionPeriod = "retentionPeriod";
    }

    public static class SettingCategories
    {
        public const string General = "General";
        public const string Email = "Email";
        public const string Security = "Security";
        public const string Notifications = "Notifications";
        public const string PaymentQr = "PaymentQr";
        public const string Audit = "Audit";
    }

    public static class CacheKeys
    {
        public const string MfaLockoutPrefix = "mfa_lockout_";
        public const string MfaAttemptsPrefix = "mfa_attempts_";
        public const string PwdResetAttemptsPrefix = "pwd_reset_attempts_";
    }

    public static class ConfigSections
    {
        public const string Smtp = "Smtp";
        public const string Jwt = "Jwt";
        public const string CorsAllowedOrigins = "Cors:AllowedOrigins";
    }

    public static class ConfigKeys
    {
        public const string JwtSecret = "Jwt:Secret";
        public const string JwtIssuer = "Jwt:Issuer";
        public const string JwtAudience = "Jwt:Audience";
        public const string JwtExpiryMinutes = "Jwt:ExpiryMinutes";
        public const string Host = "Host";
        public const string Port = "Port";
        public const string Username = "Username";
        public const string Password = "Password";
        public const string EnableSsl = "EnableSsl";
        public const string FromAddress = "FromAddress";
        public const string FromName = "FromName";
    }

    public static class CorsPolicies
    {
        public const string FrontendPolicy = "FrontendPolicy";
    }

    public static class ContentTypes
    {
        public const string ApplicationJson = "application/json";
    }

    public static class ApiConfig
    {
        public const string V1 = "v1";
        public const string ApiTitle = "Team Contribution Management API";
        public const string VersionGroupFormat = "'v'VVV";
    }

    public static class Auth
    {
        public const string Bearer = "Bearer";
        public const string Authorization = "Authorization";
        public const string Jwt = "JWT";
        public const string EmailClaim = "email";
        public const string BearerDescription = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"";
    }

    public static class Defaults
    {
        public const string Currency = "INR";
        public const string CurrencySymbol = "Rs.";
        public const string TimeZone = "Asia/Kolkata";
        public const string IndiaStandardTimeId = "India Standard Time";
        public const string TxnPrefix = "TXN";
        public const string TicketPrefix = "TKT";
        public const string BirthdayMemberPrefix = "birthday-member:";
        public const string DefaultPayeeName = "Daniel A";
        public const string DefaultUpiId = "danielrobertanto604@okicici";
        public const string DefaultFrontendUrl = "http://localhost:5173";
        public const string Localhost = "localhost";
        public const string DefaultQrSize = "260x260";
        public const string DefaultQrMargin = "8";
        public const string GpayFileName = "gpay.png";
        public const string WwwRoot = "wwwroot";
        public const string UserImagesFolder = "user_images";
        public const string ImagePng = "image/png";
        public const string ImageJpeg = "image/jpeg";
        public const string ImageJpg = "image/jpg";
        public const string ExtPng = "png";
        public const string ExtJpg = "jpg";
        public const string DataUriPrefix = "data:";
        public const string DataImagePrefix = "data:image";
        public const string GpayBannerContentId = "gpay-banner";
        public const string DefaultMfaDeviceLabel = "New Device";
        public const string MfaIssuer = "TeamContributionApp";
        public const string DefaultGalleryCategory = "Moments";
        public const string DateFormatYmd = "yyyyMMdd";
        public const string SessionIdClaim = "SessionId";
        public const string Comma = ",";
        public const string Slash = "/";
        public const string Underscore = "_";
        public const string Space = " ";
        public const string QuestionMark = "?";
        public const string VersionParamPrefix = "?v=";
        public const string UserImagesPathPrefix = "/user_images/";
        public const string Uncategorized = "Uncategorized";
        public const string UnknownEvent = "Unknown Event";
        public const string JwtIssuer = "TeamContributionManagementSystem";
        public const string JwtAudience = "TeamContributionManagementSystemClient";
        public const string DefaultFromAddress = "noreply@teamcontribution.local";
        public const string DefaultFromName = "Team Contribution System";
    }

    public static class EmailTemplates
    {
        public const string BirthdayCelebrationHeader = "Birthday Celebration";
        public const string EventDetailHeader = "Event Detail";
        public const string BirthdaySubjectPrefix = "Birthday Celebration - ";
        public const string EventSubjectPrefix = "Event Detail: ";
        public const string CelebrationDateLabel = "Celebration Date:";
        public const string EventDateLabel = "Event Date:";
        public const string UpiContributionNotePrefix = "Contribution for ";
        public const string PasswordResetSubject = "Password Reset OTP - Team Contribution Management System";
    }
}
