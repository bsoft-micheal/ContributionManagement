using FluentValidation;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Auth;
using TeamContributionManagementSystem.Application.DTOs.Users;

namespace TeamContributionManagementSystem.Application.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequestDto>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(CommonValidationMessages.EmailRequired)
            .ValidEmailAddress()
            .MaximumLength(EmailValidationHelper.MaxEmailLength).WithMessage(CommonValidationMessages.EmailMaxLength);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequestDto>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(CommonValidationMessages.EmailRequired)
            .ValidEmailAddress()
            .MaximumLength(EmailValidationHelper.MaxEmailLength).WithMessage(CommonValidationMessages.EmailMaxLength);
    }
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequestDto>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(CommonValidationMessages.EmailRequired)
            .ValidEmailAddress()
            .MaximumLength(EmailValidationHelper.MaxEmailLength).WithMessage(CommonValidationMessages.EmailMaxLength);
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(CommonValidationMessages.EmailRequired)
            .ValidEmailAddress()
            .MaximumLength(EmailValidationHelper.MaxEmailLength).WithMessage(CommonValidationMessages.EmailMaxLength);
    }
}

public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequestDto>
{
    public VerifyOtpRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(CommonValidationMessages.EmailRequired)
            .ValidEmailAddress()
            .MaximumLength(EmailValidationHelper.MaxEmailLength).WithMessage(CommonValidationMessages.EmailMaxLength);
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(CommonValidationMessages.EmailRequired)
            .ValidEmailAddress()
            .MaximumLength(EmailValidationHelper.MaxEmailLength).WithMessage(CommonValidationMessages.EmailMaxLength);
    }
}
