using FluentValidation;
using TeamContributionManagementSystem.Application.DTOs.Members;

namespace TeamContributionManagementSystem.Application.Validators;

public class CreateMemberRequestValidator : AbstractValidator<CreateMemberRequestDto>
{
    public CreateMemberRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email must not exceed 150 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role is required.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateTime.UtcNow).WithMessage("Date of birth cannot be in the future.");

        RuleFor(x => x.JoiningDate)
            .NotEmpty().WithMessage("Joining date is required.");
    }
}
