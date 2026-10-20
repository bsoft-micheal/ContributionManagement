using System.Text.RegularExpressions;
using FluentValidation;

namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Provides centralized, strict RFC 5321/5322 compliant email validation rules for both
/// standalone verification and FluentValidation pipeline integration.
/// </summary>
public static class EmailValidationHelper
{
    public const int MaxEmailLength = 254;

    /// <summary>
    /// Strict regex pattern matching allowed email characters and domain structure.
    /// Local part allows: a-z, A-Z, 0-9, _, +, - with optional dot-separated segments (no leading/trailing or consecutive dots).
    /// Domain part allows: alphanumeric and hyphen labels separated by dots with at least one domain extension.
    /// </summary>
    public const string EmailRegexPattern = @"^[a-zA-Z0-9_+-]+(?:\.[a-zA-Z0-9_+-]+)*@[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?)+$";

    private static readonly Regex StrictEmailRegex = new(EmailRegexPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Validates an email address against strict constraints:
    /// - Max length 254
    /// - No spaces anywhere (leading, internal, trailing)
    /// - Matching the standard email regex
    /// </summary>
    public static bool IsValidEmail(string? email, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrEmpty(email))
        {
            errorMessage = CommonValidationMessages.EmailRequired;
            return false;
        }

        // 1. Reject spaces before, inside, or after
        if (email.Any(char.IsWhiteSpace))
        {
            errorMessage = CommonValidationMessages.EmailValid;
            return false;
        }

        // 2. Reject email exceeding 254 characters
        if (email.Length > MaxEmailLength)
        {
            errorMessage = CommonValidationMessages.EmailValid;
            return false;
        }

        // 3. Strict regex verification
        if (!StrictEmailRegex.IsMatch(email))
        {
            errorMessage = CommonValidationMessages.EmailValid;
            return false;
        }

        return true;
    }

    /// <summary>
    /// FluentValidation custom extension method for validating email fields.
    /// </summary>
    public static IRuleBuilderOptionsConditions<T, string> ValidEmailAddress<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Custom((email, context) =>
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return; // Defer to NotEmpty() for null/empty checks
            }

            if (!IsValidEmail(email, out var err))
            {
                context.AddFailure(context.PropertyPath, err);
            }
        });
    }
}
