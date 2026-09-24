namespace TeamContributionManagementSystem.Domain.Enums;

public enum EventStatus
{
    Draft = 1,
    Planned = 2,
    Active = 3,
    Completed = 4,
    Cancelled = 5
}

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Overdue = 3
}

public enum PaymentMode
{
    None = 0,
    Cash = 1,
    Upi = 2,
    BankTransfer = 3,
    Card = 4,
    Split = 5
}

public enum UserRole
{
    Admin = 1,
    Manager = 2,
    User = 3,
    Member = 4
}
