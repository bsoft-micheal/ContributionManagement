using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Application.Common;

namespace TeamContributionManagementSystem.Application.DTOs.WorkTypes;

public class WorkTypeDto
{
    public Guid WorkTypeId { get; set; }
    public string WorkTypeName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

public class CreateWorkTypeRequestDto
{
    [Required(ErrorMessage = CommonValidationMessages.WorkTypeNameRequired)]
    [MaxLength(100, ErrorMessage = CommonValidationMessages.WorkTypeNameMaxLength)]
    public string WorkTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateWorkTypeRequestDto : CreateWorkTypeRequestDto
{
}
