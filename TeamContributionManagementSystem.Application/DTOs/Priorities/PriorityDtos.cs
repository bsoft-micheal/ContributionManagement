using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Application.Common;

namespace TeamContributionManagementSystem.Application.DTOs.Priorities;

public class PriorityDto
{
    public Guid PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

public class CreatePriorityRequestDto
{
    [Required(ErrorMessage = CommonValidationMessages.PriorityNameRequired)]
    [MaxLength(100, ErrorMessage = CommonValidationMessages.PriorityNameMaxLength)]
    public string PriorityName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdatePriorityRequestDto : CreatePriorityRequestDto
{
}
