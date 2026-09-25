using System;

namespace TeamContributionManagementSystem.Domain.Common
{
    public interface IAuditableEntity
    {
        bool IsActive { get; set; }
        bool IsDeleted { get; set; }
        string? CreatedBy { get; set; }
        DateTime? CreatedAt { get; set; }
        DateTime? CreatedOn { get; set; }
        string? ModifiedBy { get; set; }
        DateTime? ModifiedOn { get; set; }
    }
}
