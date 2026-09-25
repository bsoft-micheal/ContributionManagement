using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Application.Common;

namespace TeamContributionManagementSystem.Application.DTOs.TicketTypes;

public class TicketTypeDto
{
    public Guid TicketTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

public class CreateTicketTypeRequestDto
{
    [Required(ErrorMessage = CommonValidationMessages.TicketTypeNameRequired)]
    [MaxLength(150, ErrorMessage = CommonValidationMessages.TicketTypeNameMaxLength)]
    public string TypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateTicketTypeRequestDto : CreateTicketTypeRequestDto
{
}
