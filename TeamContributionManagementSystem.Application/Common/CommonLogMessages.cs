namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Centralized structured logging message templates across all application services.
/// </summary>
public static class CommonLogMessages
{
    public static class General
    {
        public const string UnhandledException = "An unhandled exception occurred during request execution.";
        public const string OperationFailed = "Operation failed: {ErrorMessage}";
    }

    public static class Auth
    {
        public const string LoginAttempt = "User {Email} attempting login.";
        public const string LoginSuccess = "User {Email} logged in successfully.";
        public const string LoginFailed = "Login failed for email: {Email}. Reason: {Reason}";
        public const string OtpSent = "Password reset OTP sent to email: {Email}.";
        public const string OtpVerifyFailed = "Password reset OTP verification failed for email: {Email}.";
        public const string PasswordResetSuccess = "Password successfully reset for email: {Email}.";
    }

    public static class Events
    {
        public const string EventCreated = "Event {EventName} (ID: {EventId}) created by user {UserId}.";
        public const string EventUpdated = "Event {EventId} updated by user {UserId}.";
        public const string EventDeleted = "Event {EventId} deleted by user {UserId}.";
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
        public const string PaymentVerified = "Payment transaction {TxnNumber} verified by user {User}.";
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
    }

    public static class Settings
    {
        public const string SettingsUpdated = "System settings updated by user {User}.";
        public const string SettingsReset = "System settings reset to defaults by user {User}.";
    }
}
