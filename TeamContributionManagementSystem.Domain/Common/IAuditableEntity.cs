using System;

namespace TeamContributionManagementSystem.Domain.Common
{
    public interface IAuditableEntity
    {
        bool IsActive { get; set; }
        bool IsDeleted { get; set; }
        string? CreatedBy { get; set; }
        DateTimeOffset CreatedAt { get; set; }
        string? ModifiedBy { get; set; }
        DateTimeOffset? ModifiedOn { get; set; }
    }
}
