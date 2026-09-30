using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IUnitOfWork
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
    public DbSet<Contribution> Contributions => Set<Contribution>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppUserRole> UserRoles => Set<AppUserRole>();
    public DbSet<RoleRight> RoleRights => Set<RoleRight>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<GalleryPhoto> GalleryPhotos => Set<GalleryPhoto>();
    public DbSet<DeviceDetail> DeviceDetails => Set<DeviceDetail>();
    public DbSet<DeviceLoginHistory> DeviceLoginHistories => Set<DeviceLoginHistory>();
    public DbSet<UserMfaDevice> UserMfaDevices => Set<UserMfaDevice>();
    public DbSet<BudgetCalculation> BudgetCalculations => Set<BudgetCalculation>();
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<WorkType> WorkTypes => Set<WorkType>();
    public DbSet<Priority> Priorities => Set<Priority>();
    public DbSet<PaymentModeItem> PaymentModes => Set<PaymentModeItem>();
    public DbSet<NavigationMenu> NavigationMenus => Set<NavigationMenu>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<Member>();

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.RoleId);
            entity.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
            entity.Ignore(x => x.DefaultContributionAmount);
            entity.HasIndex(x => x.RoleName).IsUnique();
        });

        modelBuilder.Entity<EventType>(entity =>
        {
            entity.HasKey(x => x.EventTypeId);
            entity.Property(x => x.EventTypeName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.BaseAmount).HasPrecision(12, 2).IsRequired().HasDefaultValue(0);
            entity.Property(x => x.HasTenureRule).HasDefaultValue(false);
            entity.Property(x => x.TenureThresholdYears).HasPrecision(4, 2).HasDefaultValue(1.0m);
            entity.Property(x => x.NewEntrantSharePercentage).HasPrecision(5, 2).HasDefaultValue(50.0m);
            entity.Property(x => x.StandardSharePercentage).HasPrecision(5, 2).HasDefaultValue(100.0m);
            entity.Property(x => x.RuleDescription).HasMaxLength(200);
            entity.HasIndex(x => x.EventTypeName).IsUnique();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Ignore(x => x.Role);
            entity.Property(x => x.ProfileImage).HasMaxLength(500);
            entity.Property(x => x.Phone).HasMaxLength(20);
            entity.Ignore(x => x.WorkType);
            entity.Property(x => x.WorkTypeId).HasColumnName("work_type_id");
            entity.HasOne(x => x.WorkTypeNavigation)
                .WithMany()
                .HasForeignKey(x => x.WorkTypeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.CreatedOn);
            entity.Property(x => x.PasswordResetOtp).HasMaxLength(10);
            entity.Property(x => x.PasswordResetOtpExpiry);
            entity.Property(x => x.IsFirstLogin).HasDefaultValue(true);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasMany(x => x.MfaDevices).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.UserRoles).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Contributions).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.EventParticipants).WithOne(x => x.Member).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppUserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.RoleId).HasColumnName("role_id").IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Ignore(x => x.UserRoleId);
            entity.Ignore(x => x.IsActive);
            entity.Ignore(x => x.IsDeleted);
            entity.Ignore(x => x.CreatedBy);
            entity.Ignore(x => x.ModifiedBy);
            entity.Ignore(x => x.ModifiedOn);
            entity.HasOne(x => x.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleRight>(entity =>
        {
            entity.HasKey(x => x.RoleRightId);
            entity.Property(x => x.RoleId).IsRequired();
            entity.Property(x => x.FeatureID).IsRequired();
            entity.Property(x => x.Access).HasMaxLength(20).IsRequired();
            entity.Property(x => x.AccessType).IsRequired();
            entity.Ignore(x => x.Module);
            entity.Ignore(x => x.SubModule);
            entity.Ignore(x => x.Page);
            entity.Ignore(x => x.NavigationMenu);
            entity.HasIndex(x => new { x.RoleId, x.FeatureID }).IsUnique();
            entity.HasOne(x => x.Role)
                .WithMany(r => r.RoleRights)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NavigationMenu>(entity =>
        {
            entity.HasKey(x => x.FeatureID);
            entity.Property(x => x.Module).HasMaxLength(100);

            entity.Property(x => x.SubModule).HasMaxLength(100);
            entity.Property(x => x.Activity).HasMaxLength(100);
            entity.Property(x => x.RoutingUrl).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ItemDescription).HasMaxLength(250);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EventDates).HasColumnName("event_dates").HasMaxLength(500);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.BaseAmount).HasPrecision(12, 2).IsRequired().HasDefaultValue(0);
            entity.HasIndex(x => new { x.EventDate, x.EventTypeId });
            entity.HasOne(x => x.EventType)
                .WithMany(x => x.Events)
                .HasForeignKey(x => x.EventTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedEvents)
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventParticipant>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Ignore(x => x.MemberId);
            entity.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
            entity.HasOne(x => x.Event)
                .WithMany(x => x.Participants)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Member)
                .WithMany(x => x.EventParticipants)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Contribution>(entity =>
        {
            entity.HasKey(x => x.ContributionId);
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.Property(x => x.CashAmount).HasPrecision(12, 2);
            entity.Property(x => x.UpiAmount).HasPrecision(12, 2);
            entity.Ignore(x => x.MemberId);
            entity.Ignore(x => x.Member);
            entity.Ignore(x => x.PaymentStatus);
            entity.Ignore(x => x.PaymentMode);
            entity.Property(x => x.StatusId).HasColumnName("status_id");
            entity.Property(x => x.PaymentModeId).HasColumnName("payment_mode_id");
            entity.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
            entity.HasOne(x => x.Event)
                .WithMany(x => x.Contributions)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany(u => u.Contributions)
                .HasForeignKey(x => x.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.StatusItem)
                .WithMany()
                .HasForeignKey(x => x.StatusId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PaymentModeItem)
                .WithMany()
                .HasForeignKey(x => x.PaymentModeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(x => x.ExpenseId);
            entity.Property(x => x.EventName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.SubmittedBy).HasMaxLength(150).IsRequired();
            entity.Property(x => x.ApprovedBy).HasMaxLength(150);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.FileName).HasColumnType("text");
            entity.HasIndex(x => new { x.ExpenseDate, x.Status });
        });

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(x => x.TicketId);
            entity.Property(x => x.TicketNo).HasMaxLength(50).IsRequired();
            entity.Ignore(x => x.MemberName);
            entity.Ignore(x => x.MemberId);
            entity.Ignore(x => x.RelatedEvent);
            entity.Ignore(x => x.TicketType);
            entity.Ignore(x => x.Priority);
            entity.Ignore(x => x.Status);
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.EventId).HasColumnName("event_id");
            entity.Property(x => x.TicketTypeId).HasColumnName("ticket_type_id");
            entity.Property(x => x.PriorityId).HasColumnName("priority_id");
            entity.Property(x => x.StatusId).HasColumnName("status_id");
            entity.Property(x => x.Subject).HasMaxLength(300).IsRequired(false);
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.AssignedTo).HasMaxLength(150);
            entity.Property(x => x.RefNo).HasMaxLength(100);
            entity.Property(x => x.Utr).HasMaxLength(100);
            entity.Property(x => x.Attachment).HasColumnType("text");
            entity.Property(x => x.ResolutionNotes).HasMaxLength(2000);
            entity.HasIndex(x => x.TicketNo).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Event)
                .WithMany()
                .HasForeignKey(x => x.EventId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TicketTypeItem)
                .WithMany()
                .HasForeignKey(x => x.TicketTypeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PriorityItem)
                .WithMany()
                .HasForeignKey(x => x.PriorityId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.StatusItem)
                .WithMany()
                .HasForeignKey(x => x.StatusId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(x => x.SettingId);
            entity.Property(x => x.SettingKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SettingValue).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => x.SettingKey).IsUnique();
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(x => x.TransactionId);
            entity.Property(x => x.TxnNumber).HasMaxLength(50).IsRequired();
            entity.Ignore(x => x.MemberName);
            entity.Ignore(x => x.EventName);
            entity.Ignore(x => x.PaymentMode);
            entity.Ignore(x => x.Status);
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.EventId).HasColumnName("event_id");
            entity.Property(x => x.PaymentModeId).HasColumnName("payment_mode_id");
            entity.Property(x => x.StatusId).HasColumnName("status_id");
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.Property(x => x.Utr).HasMaxLength(100);
            entity.Property(x => x.VerifiedBy).HasMaxLength(150);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.Screenshot).HasColumnType("text");
            entity.HasIndex(x => x.TxnNumber).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Event)
                .WithMany()
                .HasForeignKey(x => x.EventId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PaymentModeItem)
                .WithMany()
                .HasForeignKey(x => x.PaymentModeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.StatusItem)
                .WithMany()
                .HasForeignKey(x => x.StatusId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GalleryPhoto>(entity =>
        {
            entity.HasKey(x => x.PhotoId);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Ignore(x => x.EventName);
            entity.Ignore(x => x.Category);
            entity.Property(x => x.EventId).HasColumnName("event_id");
            entity.HasOne(x => x.Event)
                .WithMany()
                .HasForeignKey(x => x.EventId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.ImageUrl).HasColumnName("image_url").HasColumnType("text").IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasIndex(x => x.EventId);
        });

        modelBuilder.Entity<DeviceDetail>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.DeviceId);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeviceLoginHistory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.DeviceDetailId);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.DeviceDetail)
                .WithMany(x => x.LoginHistories)
                .HasForeignKey(x => x.DeviceDetailId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BudgetCalculation>(entity =>
        {
            entity.HasKey(x => x.BudgetCalculationId);
            entity.Property(x => x.ExpenseItem).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Rate).HasPrecision(12, 2).IsRequired().HasDefaultValue(0);
            entity.Ignore(x => x.Category);
            entity.Property(x => x.EventTypeId).HasColumnName("event_type_id");
            entity.HasOne(x => x.EventType)
                .WithMany()
                .HasForeignKey(x => x.EventTypeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.ExpenseItem);
        });

        modelBuilder.Entity<TicketType>(entity =>
        {
            entity.HasKey(x => x.TicketTypeId);
            entity.Property(x => x.TypeName).HasMaxLength(150).IsRequired();
            entity.HasIndex(x => x.TypeName).IsUnique();
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(x => x.StatusId);
            entity.Property(x => x.StatusName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Module).HasMaxLength(100);
            entity.HasIndex(x => new { x.StatusName, x.Module }).IsUnique();
        });

        modelBuilder.Entity<WorkType>(entity =>
        {
            entity.HasKey(x => x.WorkTypeId);
            entity.Property(x => x.WorkTypeName).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.WorkTypeName).IsUnique();
        });

        modelBuilder.Entity<Priority>(entity =>
        {
            entity.HasKey(x => x.PriorityId);
            entity.Property(x => x.PriorityName).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.PriorityName).IsUnique();
        });

        modelBuilder.Entity<PaymentModeItem>(entity =>
        {
            entity.ToTable("payment_modes");
            entity.HasKey(x => x.PaymentModeId);
            entity.Property(x => x.PaymentModeName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.IsCash).HasDefaultValue(false);
            entity.Property(x => x.SupportsQr).HasDefaultValue(true);
            entity.Property(x => x.PaymentType).HasMaxLength(50).HasDefaultValue("Digital");
            entity.HasIndex(x => x.PaymentModeName).IsUnique();
        });

        modelBuilder.ApplySnakeCaseNames();

        // Global DateTime UTC conversion for Npgsql 6.0+
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                        v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()) : v,
                        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v));
                }
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInformation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    private void ApplyAuditInformation()
    {
        var currentUserName = _currentUserService?.UserName ?? _currentUserService?.UserId;
        var currentUserId = _currentUserService?.UserId;
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                var createdByProp = entry.Metadata.FindProperty("CreatedBy");
                if (createdByProp != null)
                {
                    if (createdByProp.ClrType == typeof(string))
                    {
                        var existingValue = entry.Property("CreatedBy").CurrentValue as string;
                        if (string.IsNullOrWhiteSpace(existingValue) && !string.IsNullOrWhiteSpace(currentUserName))
                        {
                            entry.Property("CreatedBy").CurrentValue = currentUserName;
                        }
                    }
                    else if (createdByProp.ClrType == typeof(Guid) || createdByProp.ClrType == typeof(Guid?))
                    {
                        var existingValue = entry.Property("CreatedBy").CurrentValue;
                        if ((existingValue == null || (Guid)existingValue == Guid.Empty) && !string.IsNullOrWhiteSpace(currentUserId) && Guid.TryParse(currentUserId, out var parsedGuid))
                        {
                            entry.Property("CreatedBy").CurrentValue = parsedGuid;
                        }
                    }
                }

                var createdAtProp = entry.Metadata.FindProperty("CreatedAt");
                if (createdAtProp != null)
                {
                    if (createdAtProp.ClrType == typeof(DateTime) || createdAtProp.ClrType == typeof(DateTime?))
                    {
                        entry.Property("CreatedAt").CurrentValue = now;
                    }
                    else if (createdAtProp.ClrType == typeof(DateTimeOffset) || createdAtProp.ClrType == typeof(DateTimeOffset?))
                    {
                        entry.Property("CreatedAt").CurrentValue = DateTimeOffset.UtcNow;
                    }
                }

                var createdOnProp = entry.Metadata.FindProperty("CreatedOn");
                if (createdOnProp != null)
                {
                    if (createdOnProp.ClrType == typeof(DateTime) || createdOnProp.ClrType == typeof(DateTime?))
                    {
                        entry.Property("CreatedOn").CurrentValue = now;
                    }
                    else if (createdOnProp.ClrType == typeof(DateTimeOffset) || createdOnProp.ClrType == typeof(DateTimeOffset?))
                    {
                        entry.Property("CreatedOn").CurrentValue = DateTimeOffset.UtcNow;
                    }
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                var modifiedByProp = entry.Metadata.FindProperty("ModifiedBy");
                if (modifiedByProp != null)
                {
                    if (modifiedByProp.ClrType == typeof(string) && !string.IsNullOrWhiteSpace(currentUserName))
                    {
                        entry.Property("ModifiedBy").CurrentValue = currentUserName;
                    }
                    else if ((modifiedByProp.ClrType == typeof(Guid) || modifiedByProp.ClrType == typeof(Guid?)) && !string.IsNullOrWhiteSpace(currentUserId) && Guid.TryParse(currentUserId, out var parsedGuid))
                    {
                        entry.Property("ModifiedBy").CurrentValue = parsedGuid;
                    }
                }

                var modifiedOnProp = entry.Metadata.FindProperty("ModifiedOn");
                if (modifiedOnProp != null)
                {
                    if (modifiedOnProp.ClrType == typeof(DateTime) || modifiedOnProp.ClrType == typeof(DateTime?))
                    {
                        entry.Property("ModifiedOn").CurrentValue = now;
                    }
                    else if (modifiedOnProp.ClrType == typeof(DateTimeOffset) || modifiedOnProp.ClrType == typeof(DateTimeOffset?))
                    {
                        entry.Property("ModifiedOn").CurrentValue = DateTimeOffset.UtcNow;
                    }
                }

                var createdByProp = entry.Metadata.FindProperty("CreatedBy");
                if (createdByProp != null)
                {
                    entry.Property("CreatedBy").IsModified = false;
                }

                var createdAtProp = entry.Metadata.FindProperty("CreatedAt");
                if (createdAtProp != null)
                {
                    entry.Property("CreatedAt").IsModified = false;
                }

                var createdOnProp = entry.Metadata.FindProperty("CreatedOn");
                if (createdOnProp != null)
                {
                    entry.Property("CreatedOn").IsModified = false;
                }
            }
        }
    }
}
