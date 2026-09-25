using System.ComponentModel.DataAnnotations;

namespace TeamContributionManagementSystem.Application.DTOs.Statuses;

public class StatusDto
{
    public Guid StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

public class CreateStatusRequestDto
{
    [Required(ErrorMessage = "Status name is required.")]
    [MaxLength(100, ErrorMessage = "Status name cannot exceed 100 characters.")]
    public string StatusName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateStatusRequestDto : CreateStatusRequestDto
{
}
