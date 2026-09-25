namespace TeamContributionManagementSystem.Domain.Common;

/// <summary>
/// Domain-level constants for entity defaults and classifications.
/// </summary>
public static class DomainConstants
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

    public static class MemberTypes
    {
        public const string Office = "Office";
        public const string Remote = "Remote";
    }

    public static class TicketTypes
    {
        public const string GeneralQuery = "General Query";
    }

    public static class Categories
    {
        public const string General = "General";
        public const string Moments = "Moments";
        public const string Birthday = "Birthday";
    }
}
