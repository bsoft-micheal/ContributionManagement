namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Centralized structured logging message templates across all application services and controllers.
/// </summary>
public static class CommonLogMessages
{
    public static class General
    {
        public const string UnhandledException = "An unhandled exception occurred during request execution.";
        public const string UnhandledExceptionPath = "Unhandled exception for request {Path}";
        public const string ValidationFailed = "Validation failed.";
        public const string OperationFailed = "Operation failed: {ErrorMessage}";
        public const string ErrorInMethod = "Error executing {MethodName} in service.";
    }

    public static class Auth
    {
        public const string LoginAttempt = "User {Email} attempting login.";
        public const string LoginSuccess = "User {Email} logged in successfully.";
        public const string LoginFailed = "Login failed for email: {Email}. Reason: {Reason}";
        public const string OtpSent = "Password reset OTP sent to email: {Email}.";
        public const string OtpVerifyFailed = "Password reset OTP verification failed for email: {Email}.";
        public const string PasswordResetSuccess = "Password successfully reset for email: {Email}.";
        public const string MfaSetupInitiated = "MFA setup initiated for user {Email}.";
        public const string MfaVerified = "MFA setup verified for user {Email}.";
    }

    public static class Events
    {
        public const string EventCreated = "Event {EventName} (ID: {EventId}) created by user {UserId}.";
        public const string EventUpdated = "Event {EventId} updated by user {UserId}.";
        public const string EventDeleted = "Event {EventId} deleted by user {UserId}.";
        public const string EmailSentSuccess = "Successfully sent event creation email to contributor: {Email} for Event: {EventName}";
        public const string EmailSendFailed = "Failed to send event creation email to contributor: {Email}";
        public const string EmailTasksFailed = "Failed to run background email tasks for new event: {EventId}";
        public const string SettingsLoadWarning = "Could not load payment settings for email notification; using default scanner details.";
        public const string BirthdayAutomationCompleted = "Birthday event automation completed. Created {CreatedEvents} event(s).";
        public const string BirthdayAutomationFailed = "Birthday event automation failed.";
    }

    public static class Members
    {
        public const string MemberCreated = "Member {MemberName} (ID: {MemberId}) created.";
        public const string MemberUpdated = "Member {MemberId} updated.";
        public const string MemberDeleted = "Member {MemberId} marked as deleted.";
        public const string BulkImport = "Imported {Count} members successfully.";
    }

    public static class Payments
    {
        public const string PaymentCreated = "Payment transaction {TxnNumber} created for amount {Amount}.";
        public const string PaymentProofSubmitted = "New Payment proof submitted: TxnNumber {TxnNumber}, Member {MemberName}, Event {EventName}, Amount {Amount}, UTR {Utr}";
        public const string PaymentVerified = "Payment transaction {TxnNumber} verified by user {User}.";
        public const string ContributionSyncSuccess = "Synchronized Contribution {ContributionId} to {Status} for Member {Member} on Event {Event}";
        public const string ContributionSyncFailed = "Failed to auto-sync contribution status during payment verification for Txn: {TxnNumber}";
        public const string SettingsLoadWarning = "Could not fetch settings for payment context; using defaults";
    }

    public static class Expenses
    {
        public const string ExpenseCreated = "Expense (ID: {ExpenseId}) of amount {Amount} submitted.";
        public const string ExpenseUpdated = "Expense (ID: {ExpenseId}) updated.";
        public const string ExpenseDeleted = "Expense (ID: {ExpenseId}) deleted.";
    }

    public static class SupportTickets
    {
        public const string TicketCreated = "Support ticket {TicketNo} created for member {MemberName}.";
        public const string TicketUpdated = "Support ticket {TicketNo} status updated to {Status}.";
        public const string TicketDeleted = "Support ticket {TicketId} deleted.";
    }

    public static class Settings
    {
        public const string SettingsUpdated = "System settings updated by user {User}.";
        public const string SettingsReset = "System settings reset to defaults by user {User}.";
    }

    public static class Users
    {
        public const string UserCreated = "User {Username} (ID: {UserId}) created.";
        public const string UserUpdated = "User {UserId} updated.";
        public const string UserDeleted = "User {UserId} deleted.";
        public const string ProfileUpdated = "User profile updated for {UserId}.";
    }

    public static class Gallery
    {
        public const string PhotoCreated = "Gallery photo {Title} (ID: {PhotoId}) uploaded.";
        public const string PhotoDeleted = "Gallery photo {PhotoId} deleted.";
    }

    public static class Emails
    {
        public const string TrackerReadFailed = "Failed to read email limit tracker file.";
        public const string TrackerWriteFailed = "Failed to write email limit tracker file.";
        public const string MockEmailWriteFailed = "Failed to write mock email to local file.";
        public const string SmtpSendFailed = "Error sending email to {ToEmail} via SMTP.";
        public const string DailyLimitReached = "Email send blocked. Daily email limit of 500 reached to protect Gmail SMTP threshold.";
        public const string DailyCountUpdated = "Daily email count updated: {Count}/500";
        public const string SmtpNotConfigured = "SMTP is not fully configured in appsettings.json. Logging email instead.";
        public const string InlineImageNotFound = "Inline email image not found at {FilePath}.";
        public const string EmailSentSuccess = "Email sent successfully to {ToEmail}";
    }
}
