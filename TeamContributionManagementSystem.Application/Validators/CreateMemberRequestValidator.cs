using FluentValidation;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Members;

namespace TeamContributionManagementSystem.Application.Validators;

public class CreateMemberRequestValidator : AbstractValidator<CreateMemberRequestDto>
{
    public CreateMemberRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(CommonValidationMessages.NameRequired)
            .MaximumLength(150).WithMessage(CommonValidationMessages.NameMaxLength);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(CommonValidationMessages.EmailRequired)
            .EmailAddress().WithMessage(CommonValidationMessages.EmailValid)
            .MaximumLength(150).WithMessage(CommonValidationMessages.EmailMaxLength);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage(CommonValidationMessages.PhoneRequired)
            .MaximumLength(20).WithMessage(CommonValidationMessages.PhoneMaxLength);

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage(CommonValidationMessages.RoleRequired);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage(CommonValidationMessages.DateOfBirthRequired)
            .LessThan(DateTime.UtcNow).WithMessage(CommonValidationMessages.DateOfBirthPast);

        RuleFor(x => x.JoiningDate)
            .NotEmpty().WithMessage(CommonValidationMessages.JoiningDateRequired);
    }
}
