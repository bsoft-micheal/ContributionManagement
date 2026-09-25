using System.ComponentModel.DataAnnotations;

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
    [Required(ErrorMessage = "Ticket Type name is required.")]
    [MaxLength(150, ErrorMessage = "Ticket Type name cannot exceed 150 characters.")]
    public string TypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateTicketTypeRequestDto : CreateTicketTypeRequestDto
{
}
