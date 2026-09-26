namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Centralized API route templates across all controllers.
/// </summary>
public static class CommonRoutes
{
    public const string ApiBase = "api/v{version:apiVersion}";
    public const string Health = "/health";

    public static class Auth
    {
        public const string Base = $"{ApiBase}/auth";
        public const string Login = "loginAsync";
        public const string Verify2Fa = "verify-2faAsync";
        public const string ForgotPasswordRequest = "forgot-password/requestAsync";
        public const string ForgotPasswordVerify = "forgot-password/verifyAsync";
        public const string ForgotPasswordReset = "forgot-password/resetAsync";
    }

    public static class Events
    {
        public const string Base = $"{ApiBase}/events";
        public const string GetAll = "getAllEventAsync";
        public const string GetById = "getEventAsyncById/{id:guid}";
        public const string Create = "saveEventAsync";
        public const string Update = "updateEventAsyncById/{id:guid}";
        public const string Delete = "deleteEventAsyncById/{id:guid}";
    }

    public static class Members
    {
        public const string Base = $"{ApiBase}/members";
        public const string GetAll = "getAllMemberAsync";
        public const string Create = "saveMemberAsync";
        public const string SaveBulk = "saveBulkMemberAsync";
        public const string Update = "updateMemberAsyncById/{id:guid}";
        public const string Delete = "deleteMemberAsyncById/{id:guid}";
    }

    public static class Users
    {
        public const string Base = $"{ApiBase}/users";
        public const string GetAll = "getAllUserAsync";
        public const string Create = "saveUserAsync";
        public const string SaveBulk = "saveBulkUserAsync";
        public const string Update = "updateUserAsyncById/{id:guid}";
        public const string GetProfile = "getProfileAsync";
        public const string UpdateProfile = "updateProfileAsync";
        public const string Delete = "deleteUserAsyncById/{id:guid}";
    }

    public static class Roles
    {
        public const string Base = $"{ApiBase}/roles";
        public const string GetAll = "getAllRoleAsync";
        public const string Create = "saveRoleAsync";
        public const string Update = "updateRoleAsyncById/{id:guid}";
        public const string Delete = "deleteRoleAsyncById/{id:guid}";
    }

    public static class UserRights
    {
        public const string Base = $"{ApiBase}/user-rights";
        public const string GetAll = "getAllUserRightAsync";
        public const string GetByRole = "getUserRightAsyncByRole/{roleName}";
        public const string Save = "saveUserRightAsync";
    }

    public static class EventTypes
    {
        public const string Base = $"{ApiBase}/event-types";
        public const string GetAll = "getAllEventTypeAsync";
        public const string Create = "saveEventTypeAsync";
        public const string Update = "updateEventTypeAsyncById/{id:guid}";
        public const string Delete = "deleteEventTypeAsyncById/{id:guid}";
    }

    public static class Contributions
    {
        public const string Base = $"{ApiBase}/contributions";
        public const string GetAll = "getAllContributionAsync";
        public const string GetByEvent = "getContributionAsyncByEvent/{eventId:guid}";
        public const string GetMySummary = "getMySummaryAsync";
        public const string Pay = "savePayContributionAsync";
    }

    public static class Expenses
    {
        public const string Base = $"{ApiBase}/expenses";
        public const string GetAll = "getAllExpenseAsync";
        public const string GetById = "getExpenseAsyncById/{id:guid}";
        public const string Create = "saveExpenseAsync";
        public const string Update = "updateExpenseAsyncById/{id:guid}";
        public const string Delete = "deleteExpenseAsyncById/{id:guid}";
    }

    public static class Payments
    {
        public const string Base = $"{ApiBase}/payments";
        public const string GetAll = "getAllPaymentAsync";
        public const string GetById = "getPaymentAsyncById/{id:guid}";
        public const string Create = "savePaymentAsync";
        public const string SubmitProof = "submitProofAsync";
        public const string GetPaymentContext = "getPaymentContextAsync";
        public const string Verify = "verifyPaymentAsync/{id:guid}";
        public const string Delete = "deletePaymentAsyncById/{id:guid}";
    }

    public static class Gallery
    {
        public const string Base = $"{ApiBase}/gallery";
        public const string GetAll = "getAllGalleryAsync";
        public const string Save = "saveGalleryAsync";
        public const string Delete = "deleteGalleryAsyncById/{id:guid}";
    }

    public static class Reports
    {
        public const string Base = $"{ApiBase}/reports";
        public const string GetSummary = "getSummaryReportAsync";
    }

    public static class Dashboard
    {
        public const string Base = $"{ApiBase}/dashboard";
        public const string GetSummary = "getSummaryDashboardAsync";
    }

    public static class DeviceInfo
    {
        public const string Base = "api/v1/device-info";
        public const string GetActiveSessions = "getActiveSessionAsync";
        public const string GetSessionHistory = "getSessionHistoryAsync";
        public const string LogoutSession = "logoutSessionAsync/{historyId:guid}";
        public const string LogoutCurrentSession = "logoutCurrentSessionAsync";
    }

    public static class Mfa
    {
        public const string Base = "api/v1/mfa";
        public const string Setup = "setupMfaAsync";
        public const string VerifySetup = "verifySetupMfaAsync";
        public const string GetDevices = "getDevicesMfaAsync";
        public const string RemoveDevice = "removeDeviceMfaAsync/{id:guid}";
    }

    public static class Settings
    {
        public const string Base = $"{ApiBase}/settings";
        public const string Get = "getSettingAsync";
        public const string Update = "updateSettingAsync";
        public const string Reset = "resetSettingAsync";
    }

    public static class SupportTickets
    {
        public const string Base = $"{ApiBase}/support-tickets";
        public const string GetAll = "getAllSupportTicketAsync";
        public const string GetById = "getSupportTicketAsyncById/{id:guid}";
        public const string Create = "saveSupportTicketAsync";
        public const string Update = "updateSupportTicketAsyncById/{id:guid}";
        public const string Reply = "replySupportTicketAsync/{id:guid}";
        public const string Delete = "deleteSupportTicketAsyncById/{id:guid}";
    }

    public static class BudgetCalculations
    {
        public const string Base = $"{ApiBase}/budget-calculations";
        public const string GetAll = "getAllBudgetCalculationAsync";
        public const string GetById = "getBudgetCalculationAsyncById/{id:guid}";
        public const string Create = "saveBudgetCalculationAsync";
        public const string Update = "updateBudgetCalculationAsyncById/{id:guid}";
        public const string Delete = "deleteBudgetCalculationAsyncById/{id:guid}";
    }

    public static class TicketTypes
    {
        public const string Base = $"{ApiBase}/ticket-types";
        public const string GetAll = "getAllTicketTypeAsync";
        public const string GetById = "getTicketTypeAsyncById/{id:guid}";
        public const string Create = "saveTicketTypeAsync";
        public const string Update = "updateTicketTypeAsyncById/{id:guid}";
        public const string Delete = "deleteTicketTypeAsyncById/{id:guid}";
    }

    public static class Statuses
    {
        public const string Base = $"{ApiBase}/statuses";
        public const string GetAll = "getAllStatusAsync";
        public const string GetById = "getStatusAsyncById/{id:guid}";
        public const string Create = "saveStatusAsync";
        public const string Update = "updateStatusAsyncById/{id:guid}";
        public const string Delete = "deleteStatusAsyncById/{id:guid}";
    }

    public static class WorkTypes
    {
        public const string Base = $"{ApiBase}/work-types";
        public const string GetAll = "getAllWorkTypeAsync";
        public const string GetById = "getWorkTypeAsyncById/{id:guid}";
        public const string Create = "saveWorkTypeAsync";
        public const string Update = "updateWorkTypeAsyncById/{id:guid}";
        public const string Delete = "deleteWorkTypeAsyncById/{id:guid}";
    }

    public static class Priorities
    {
        public const string Base = $"{ApiBase}/priorities";
        public const string GetAll = "getAllPriorityAsync";
        public const string GetById = "getPriorityAsyncById/{id:guid}";
        public const string Create = "savePriorityAsync";
        public const string Update = "updatePriorityAsyncById/{id:guid}";
        public const string Delete = "deletePriorityAsyncById/{id:guid}";
    }
}
