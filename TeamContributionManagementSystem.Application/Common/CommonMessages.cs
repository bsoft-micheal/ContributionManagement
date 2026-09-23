namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Centralized response messages to ensure no hardcoded strings exist in controllers or services.
/// </summary>
public static class CommonMessages
{
    public static class General
    {
        public const string Success = "Operation completed successfully.";
        public const string Failure = "An unexpected error occurred.";
        public const string NotFound = "The requested record was not found.";
    }

    public static class Auth
    {
        public const string LoginSuccess = "User authenticated successfully.";
        public const string VerifyTwoFactorSuccess = "Two-factor authentication verified successfully.";
        public const string ForgotPasswordOtpSentSuccess = "If the email is registered, a password reset OTP has been sent.";
        public const string ForgotPasswordOtpVerifiedSuccess = "OTP verified successfully. You may proceed to reset your password.";
        public const string PasswordResetSuccess = "Your password has been successfully reset. Please log in with your new credentials.";
    }

    public static class Events
    {
        public const string GetAllSuccess = "Events retrieved successfully.";
        public const string GetByIdSuccess = "Event details retrieved successfully.";
        public const string SaveSuccess = "Event created successfully.";
        public const string UpdateSuccess = "Event updated successfully.";
        public const string DeleteSuccess = "Event deleted successfully.";
    }

    public static class Members
    {
        public const string GetAllSuccess = "Members retrieved successfully.";
        public const string GetByIdSuccess = "Member details retrieved successfully.";
        public const string SaveSuccess = "Member created successfully.";
        public const string SaveBulkSuccess = "Bulk members imported successfully.";
        public const string UpdateSuccess = "Member updated successfully.";
        public const string DeleteSuccess = "Member deleted successfully.";
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
    }

    public static class Roles
    {
        public const string GetAllSuccess = "Roles retrieved successfully.";
        public const string GetByIdSuccess = "Role details retrieved successfully.";
        public const string SaveSuccess = "Role created successfully.";
        public const string UpdateSuccess = "Role updated successfully.";
        public const string DeleteSuccess = "Role deleted successfully.";
    }

    public static class UserRights
    {
        public const string GetAllSuccess = "User rights retrieved successfully.";
        public const string GetByRoleSuccess = "User rights for role retrieved successfully.";
        public const string SaveSuccess = "User rights saved successfully.";
    }

    public static class EventTypes
    {
        public const string GetAllSuccess = "Event types retrieved successfully.";
        public const string GetByIdSuccess = "Event type retrieved successfully.";
        public const string SaveSuccess = "Event type created successfully.";
        public const string UpdateSuccess = "Event type updated successfully.";
        public const string DeleteSuccess = "Event type deleted successfully.";
    }

    public static class Contributions
    {
        public const string GetAllSuccess = "Contributions retrieved successfully.";
        public const string GetByEventSuccess = "Event contributions retrieved successfully.";
        public const string GetMySummarySuccess = "Personal contribution summary retrieved successfully.";
        public const string PaySuccess = "Contribution payment processed successfully.";
    }

    public static class Expenses
    {
        public const string GetAllSuccess = "Expenses retrieved successfully.";
        public const string GetByIdSuccess = "Expense retrieved successfully.";
        public const string SaveSuccess = "Expense created successfully.";
        public const string UpdateSuccess = "Expense updated successfully.";
        public const string DeleteSuccess = "Expense deleted successfully.";
    }

    public static class Payments
    {
        public const string GetAllSuccess = "Payment transactions retrieved successfully.";
        public const string GetByIdSuccess = "Payment transaction retrieved successfully.";
        public const string SaveSuccess = "Payment transaction created successfully.";
        public const string VerifySuccess = "Payment transaction verified successfully.";
        public const string DeleteSuccess = "Payment transaction deleted successfully.";
    }

    public static class Gallery
    {
        public const string GetAllSuccess = "Gallery items retrieved successfully.";
        public const string SaveSuccess = "Gallery item uploaded successfully.";
        public const string DeleteSuccess = "Gallery item deleted successfully.";
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
    }

    public static class Mfa
    {
        public const string SetupSuccess = "MFA setup initiated successfully.";
        public const string VerifySetupSuccess = "MFA verification completed successfully.";
        public const string GetDevicesSuccess = "Registered MFA devices retrieved successfully.";
        public const string RemoveDeviceSuccess = "MFA device removed successfully.";
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
    }
}
