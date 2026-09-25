namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Centralized validation messages for data annotations, DTO validations, and FluentValidation rules.
/// </summary>
public static class CommonValidationMessages
{
    // Auth Validations
    public const string PasswordMinLength = "Password must be at least 6 characters long.";
    public const string EmailRequired = "Email is required.";
    public const string EmailValid = "A valid email address is required.";
    public const string EmailMaxLength = "Email must not exceed 150 characters.";
    public const string PasswordRequired = "Password is required.";
    public const string OtpRequired = "OTP is required.";
    public const string OtpExactLength = "OTP must be exactly 6 characters.";

    // Member Validations
    public const string NameRequired = "Name is required.";
    public const string NameMaxLength = "Name must not exceed 150 characters.";
    public const string PhoneRequired = "Phone number is required.";
    public const string PhoneMaxLength = "Phone number must not exceed 20 characters.";
    public const string RoleRequired = "Role is required.";
    public const string DateOfBirthRequired = "Date of birth is required.";
    public const string DateOfBirthPast = "Date of birth cannot be in the future.";
    public const string JoiningDateRequired = "Joining date is required.";

    // Event & Event Type Validations
    public const string EventNameRequired = "Event name is required.";
    public const string EventDateRequired = "Event date is required.";
    public const string EventTypeRequired = "Event type is required.";
    public const string EventTypeNameRequired = "Event type name is required.";
    public const string BaseAmountGreaterThanZero = "Base amount must be greater than 0.";

    // Support Data Validations
    public const string WorkTypeNameRequired = "Work Type name is required.";
    public const string WorkTypeNameMaxLength = "Work Type name cannot exceed 100 characters.";
    public const string TicketTypeNameRequired = "Ticket Type name is required.";
    public const string TicketTypeNameMaxLength = "Ticket Type name cannot exceed 150 characters.";
    public const string StatusNameRequired = "Status name is required.";
    public const string StatusNameMaxLength = "Status name cannot exceed 100 characters.";
    public const string ExpenseItemRequired = "Expense Item is required.";
    public const string RateRange = "Rate must be between 0 and 1,000,000.";

    // Support Ticket Validations
    public const string SubjectRequired = "Subject is required.";
    public const string DescriptionRequired = "Description is required.";
    public const string MessageRequired = "Message is required.";
}
