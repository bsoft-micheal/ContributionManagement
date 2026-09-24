using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Common
{
    public abstract class BaseAuditableEntity : IAuditableEntity
    {
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("created_by")]
        public string? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("modified_by")]
        public string? ModifiedBy { get; set; }

        [Column("modified_on")]
        public DateTimeOffset? ModifiedOn { get; set; }
    }
}
