namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Centralized response and error messages to ensure no hardcoded strings exist in controllers or services.
/// </summary>
public static class CommonMessages
{
    public static class General
    {
        public const string Success = "Operation completed successfully.";
        public const string Failure = "An unexpected error occurred.";
        public const string NotFound = "The requested record was not found.";
        public const string InvalidRequestBody = "The request body is invalid or empty.";
        public const string DeserializationFailed = "JSON deserialization failed.";
        public const string NullOrEmptyRequestList = "The request body deserialized to null or empty list.";
        public const string NullRequestItem = "One of the request items is null.";
        public const string Unauthorized = "Unauthorized";
        public const string UserIdentityNotAvailable = "User identity is not available.";
    }

    public static class Validation
    {
        public const string ValidationFailed = "Validation failed.";
        public const string PasswordMinLength = CommonValidationMessages.PasswordMinLength;
        public const string BaseAmountGreaterThanZero = CommonValidationMessages.BaseAmountGreaterThanZero;
        public const string WorkTypeNameRequired = CommonValidationMessages.WorkTypeNameRequired;
        public const string WorkTypeNameMaxLength = CommonValidationMessages.WorkTypeNameMaxLength;
        public const string TicketTypeNameRequired = CommonValidationMessages.TicketTypeNameRequired;
        public const string TicketTypeNameMaxLength = CommonValidationMessages.TicketTypeNameMaxLength;
        public const string StatusNameRequired = CommonValidationMessages.StatusNameRequired;
        public const string StatusNameMaxLength = CommonValidationMessages.StatusNameMaxLength;
        public const string ExpenseItemRequired = CommonValidationMessages.ExpenseItemRequired;
        public const string RateRange = CommonValidationMessages.RateRange;
        public const string DigitsGreaterThanZero = "The number of digits must be greater than 0.";
        public const string InvalidMobileFormat = "Invalid mobile phone number format.";
    }

    public static class Auth
    {
        public const string LoginSuccess = "User authenticated successfully.";
        public const string VerifyTwoFactorSuccess = "Two-factor authentication verified successfully.";
        public const string ForgotPasswordOtpSentSuccess = "If the email is registered, a password reset OTP has been sent.";
        public const string ForgotPasswordOtpVerifiedSuccess = "OTP verified successfully. You may proceed to reset your password.";
        public const string PasswordResetSuccess = "Your password has been successfully reset. Please log in with your new credentials.";
        public const string InvalidOrExpiredOtp = "Invalid or expired password reset OTP.";
        public const string InvalidCredentials = "Invalid email or password.";
        public const string UserNotFound = "User not found.";
        public const string AccountDeactivated = "This user account is deactivated. Please contact an administrator.";
        public const string MaxOtpAttemptsExceeded = "Maximum OTP attempts exceeded. Please request a new OTP.";
        public const string OtpExpired = "The password reset OTP has expired. Please request a new one.";
        public const string InvalidOtpRemainingFormat = "Invalid OTP code. {0} attempt{1} remaining.";
        public const string JwtSecretNotConfigured = "JWT secret is not configured.";
    }

    public static class Events
    {
        public const string GetAllSuccess = "Events retrieved successfully.";
        public const string GetByIdSuccess = "Event details retrieved successfully.";
        public const string SaveSuccess = "Event created successfully.";
        public const string UpdateSuccess = "Event updated successfully.";
        public const string DeleteSuccess = "Event deleted successfully.";
        public const string NotFound = "Event not found.";
        public const string InactiveEventType = "Inactive event types cannot be used.";
        public const string AtLeastOneParticipantRequired = "At least one participant is required.";
        public const string ParticipantsNotFound = "One or more participants could not be found.";
    }

    public static class Members
    {
        public const string GetAllSuccess = "Members retrieved successfully.";
        public const string GetByIdSuccess = "Member details retrieved successfully.";
        public const string SaveSuccess = "Member created successfully.";
        public const string SaveBulkSuccess = "Bulk members imported successfully.";
        public const string UpdateSuccess = "Member updated successfully.";
        public const string DeleteSuccess = "Member deleted successfully.";
        public const string NotFound = "Member not found.";
        public const string EmailExists = "A member with the same email already exists.";
    }

    public static class Users
    {
        public const string GetAllSuccess = "Users retrieved successfully.";
        public const string GetByIdSuccess = "User details retrieved successfully.";
        public const string SaveSuccess = "User created successfully.";
        public const string SaveBulkSuccess = "Bulk users imported successfully.";
        public const string UpdateSuccess = "User updated successfully.";
        public const string UpdateProfileSuccess = "User profile updated successfully.";
        public const string GetProfileSuccess = "User profile retrieved successfully.";
        public const string DeleteSuccess = "User deleted successfully.";
        public const string NotFound = "User not found.";
        public const string AdminNotFound = "No admin user available for scheduled event creation.";
        public const string EmailExists = "A user with this email already exists.";
        public const string UsernameExists = "A user with this username already exists.";
    }

    public static class Roles
    {
        public const string GetAllSuccess = "Roles retrieved successfully.";
        public const string GetByIdSuccess = "Role details retrieved successfully.";
        public const string SaveSuccess = "Role created successfully.";
        public const string UpdateSuccess = "Role updated successfully.";
        public const string DeleteSuccess = "Role deleted successfully.";
        public const string NotFound = "Role not found.";
        public const string AlreadyExists = "Role already exists.";
        public const string CannotDeleteWithMembers = "Cannot delete role because members are assigned to this role.";
        public const string InvalidRoleFormat = "Invalid role: '{0}'. Valid values: Admin, Manager, User.";
    }

    public static class UserRights
    {
        public const string GetAllSuccess = "User rights retrieved successfully.";
        public const string GetByRoleSuccess = "User rights for role retrieved successfully.";
        public const string SaveSuccess = "User rights saved successfully.";
        public const string NotFound = "User rights not found.";
    }

    public static class EventTypes
    {
        public const string GetAllSuccess = "Event types retrieved successfully.";
        public const string GetByIdSuccess = "Event type retrieved successfully.";
        public const string SaveSuccess = "Event type created successfully.";
        public const string UpdateSuccess = "Event type updated successfully.";
        public const string DeleteSuccess = "Event type deleted successfully.";
        public const string NotFound = "Event type not found.";
        public const string AlreadyExists = "Event type already exists.";
        public const string CannotDeleteWithEvents = "Cannot delete this event type because it is associated with existing events.";
        public const string NoActiveConfigured = "No active event type is configured in the database.";
    }

    public static class Contributions
    {
        public const string GetAllSuccess = "Contributions retrieved successfully.";
        public const string GetByEventSuccess = "Event contributions retrieved successfully.";
        public const string GetMySummarySuccess = "Personal contribution summary retrieved successfully.";
        public const string PaySuccess = "Contribution payment processed successfully.";
        public const string NotFound = "Contribution record not found.";
        public const string SplitPaymentAmountsRequired = "Both Cash Amount and UPI Amount must be provided for Split Payment.";
        public const string SplitPaymentNegative = "Split payment amounts cannot be negative.";
        public const string SplitPaymentSumMismatchFormat = "Cash (₹{0}) + UPI (₹{1}) = ₹{2} must equal Total Amount (₹{3}).";
    }

    public static class Expenses
    {
        public const string GetAllSuccess = "Expenses retrieved successfully.";
        public const string GetByIdSuccess = "Expense retrieved successfully.";
        public const string SaveSuccess = "Expense created successfully.";
        public const string UpdateSuccess = "Expense updated successfully.";
        public const string DeleteSuccess = "Expense deleted successfully.";
        public const string NotFound = "Expense not found.";
    }

    public static class Payments
    {
        public const string GetAllSuccess = "Payment transactions retrieved successfully.";
        public const string GetByIdSuccess = "Payment transaction retrieved successfully.";
        public const string SaveSuccess = "Payment transaction created successfully.";
        public const string SubmitProofSuccess = "Payment proof submitted successfully. An administrator will verify it shortly.";
        public const string GetContextSuccess = "Payment context loaded successfully.";
        public const string VerifySuccess = "Payment transaction verified successfully.";
        public const string DeleteSuccess = "Payment transaction deleted successfully.";
        public const string NotFound = "Payment transaction not found.";
    }

    public static class Gallery
    {
        public const string GetAllSuccess = "Gallery items retrieved successfully.";
        public const string SaveSuccess = "Gallery item uploaded successfully.";
        public const string DeleteSuccess = "Gallery item deleted successfully.";
        public const string NotFound = "Gallery photo not found.";
    }

    public static class Reports
    {
        public const string GetSummarySuccess = "Financial report summary retrieved successfully.";
    }

    public static class Dashboard
    {
        public const string GetSummarySuccess = "Dashboard analytics summary retrieved successfully.";
    }

    public static class DeviceInfo
    {
        public const string GetActiveSessionsSuccess = "Active device sessions retrieved successfully.";
        public const string GetSessionHistorySuccess = "Device session history retrieved successfully.";
        public const string LogoutSessionSuccess = "Session terminated successfully.";
        public const string LogoutCurrentSessionSuccess = "Current session terminated successfully.";
        public const string SessionNotFound = "Session not found or does not belong to the user.";
    }

    public static class Mfa
    {
        public const string SetupSuccess = "MFA setup initiated successfully.";
        public const string VerifySetupSuccess = "MFA verification completed successfully.";
        public const string GetDevicesSuccess = "Registered MFA devices retrieved successfully.";
        public const string RemoveDeviceSuccess = "MFA device removed successfully.";
        public const string InvalidOtpCode = "Invalid OTP code";
        public const string NotEnabled = "MFA is not enabled for this user.";
        public const string SingleDeviceLimit = "A user can only configure one MFA device. Please remove the existing device first.";
        public const string LockoutFormat = "Too many failed attempts. MFA verification is temporarily locked. Please try again in {0} minute(s).";
        public const string MaxAttemptsExceededFormat = "Invalid OTP. You have exceeded maximum attempts. MFA verification is temporarily locked for {0} minutes.";
        public const string InvalidOtpRemainingFormat = "Invalid OTP code. {0} attempt{1} remaining.";
    }

    public static class Settings
    {
        public const string GetSuccess = "System settings retrieved successfully.";
        public const string UpdateSuccess = "System settings updated successfully.";
        public const string ResetSuccess = "System settings reset to defaults successfully.";
    }

    public static class SupportTickets
    {
        public const string GetAllSuccess = "Support tickets retrieved successfully.";
        public const string GetByIdSuccess = "Support ticket details retrieved successfully.";
        public const string SaveSuccess = "Support ticket created successfully.";
        public const string UpdateSuccess = "Support ticket updated successfully.";
        public const string ReplySuccess = "Support ticket reply added successfully.";
        public const string DeleteSuccess = "Support ticket deleted successfully.";
        public const string NotFound = "Support ticket not found.";
    }

    public static class BudgetCalculations
    {
        public const string GetAllSuccess = "Budget calculation items retrieved successfully.";
        public const string GetByIdSuccess = "Budget calculation item details retrieved successfully.";
        public const string SaveSuccess = "Budget calculation item created successfully.";
        public const string UpdateSuccess = "Budget calculation item updated successfully.";
        public const string DeleteSuccess = "Budget calculation item deleted successfully.";
        public const string NotFound = "Budget calculation item not found.";
        public const string AlreadyExistsFormat = "Expense item '{0}' already exists.";
        public const string NotFoundFormat = "Budget calculation item with ID '{0}' was not found.";
    }

    public static class TicketTypes
    {
        public const string GetAllSuccess = "Ticket types retrieved successfully.";
        public const string GetByIdSuccess = "Ticket type details retrieved successfully.";
        public const string SaveSuccess = "Ticket type created successfully.";
        public const string UpdateSuccess = "Ticket type updated successfully.";
        public const string DeleteSuccess = "Ticket type deleted successfully.";
        public const string NotFound = "Ticket type not found.";
        public const string AlreadyExistsFormat = "Ticket type '{0}' already exists.";
        public const string NotFoundFormat = "Ticket type with ID '{0}' was not found.";
    }

    public static class Statuses
    {
        public const string GetAllSuccess = "Statuses retrieved successfully.";
        public const string GetByIdSuccess = "Status details retrieved successfully.";
        public const string SaveSuccess = "Status created successfully.";
        public const string UpdateSuccess = "Status updated successfully.";
        public const string DeleteSuccess = "Status deleted successfully.";
        public const string NotFound = "Status not found.";
        public const string AlreadyExistsFormat = "Status '{0}' already exists.";
        public const string NotFoundFormat = "Status with ID '{0}' was not found.";
    }

    public static class WorkTypes
    {
        public const string GetAllSuccess = "Work types retrieved successfully.";
        public const string GetByIdSuccess = "Work type details retrieved successfully.";
        public const string SaveSuccess = "Work type created successfully.";
        public const string UpdateSuccess = "Work type updated successfully.";
        public const string DeleteSuccess = "Work type deleted successfully.";
        public const string NotFound = "Work type not found.";
        public const string AlreadyExistsFormat = "Work type '{0}' already exists.";
        public const string NotFoundFormat = "Work type with ID '{0}' was not found.";
    }

    public static class Emails
    {
        public const string DailyLimitReached = "Daily email sending limit (500) has been reached. Please try again tomorrow.";
        public const string SendFailedFormat = "Failed to send email: {0}";
    }
}
