using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
    public DbSet<Contribution> Contributions => Set<Contribution>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RoleRight> RoleRights => Set<RoleRight>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<GalleryPhoto> GalleryPhotos => Set<GalleryPhoto>();
    public DbSet<DeviceDetail> DeviceDetails => Set<DeviceDetail>();
    public DbSet<DeviceLoginHistory> DeviceLoginHistories => Set<DeviceLoginHistory>();
    public DbSet<UserMfaDevice> UserMfaDevices => Set<UserMfaDevice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.RoleId);
            entity.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DefaultContributionAmount).HasPrecision(12, 2);
            entity.HasIndex(x => x.RoleName).IsUnique();
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(x => x.MemberId);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => new { x.RoleId, x.IsActive });
            entity.Property(x => x.MemberType).HasMaxLength(20).HasDefaultValue("Office");
            entity.HasOne(x => x.Role)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventType>(entity =>
        {
            entity.HasKey(x => x.EventTypeId);
            entity.Property(x => x.EventTypeName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.BaseAmount).HasPrecision(12, 2).IsRequired().HasDefaultValue(0);
            entity.HasIndex(x => x.EventTypeName).IsUnique();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.ProfileImage).HasMaxLength(500);
            entity.Property(x => x.CreatedOn);
            entity.Property(x => x.PasswordResetOtp).HasMaxLength(10);
            entity.Property(x => x.PasswordResetOtpExpiry);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasMany(x => x.MfaDevices).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleRight>(entity =>
        {
            entity.HasKey(x => x.RoleRightId);
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Module).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SubModule).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Page).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Access).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => new { x.Role, x.Module, x.SubModule, x.Page }).IsUnique();
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventName).HasMaxLength(200).IsRequired();
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
            entity.HasIndex(x => new { x.EventId, x.MemberId }).IsUnique();
            entity.HasOne(x => x.Event)
                .WithMany(x => x.Participants)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Member)
                .WithMany(x => x.EventParticipants)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Contribution>(entity =>
        {
            entity.HasKey(x => x.ContributionId);
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.Property(x => x.CashAmount).HasPrecision(12, 2);
            entity.Property(x => x.UpiAmount).HasPrecision(12, 2);
            entity.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.PaymentMode).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.EventId, x.MemberId }).IsUnique();
            entity.HasIndex(x => new { x.EventId, x.PaymentStatus });
            entity.HasOne(x => x.Event)
                .WithMany(x => x.Contributions)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Member)
                .WithMany(x => x.Contributions)
                .HasForeignKey(x => x.MemberId)
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
            entity.Property(x => x.CreatedBy).HasMaxLength(150);
            entity.Property(x => x.ModifiedBy).HasMaxLength(150);
            entity.HasIndex(x => new { x.ExpenseDate, x.Status });
        });

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(x => x.TicketId);
            entity.Property(x => x.TicketNo).HasMaxLength(50).IsRequired();
            entity.Property(x => x.MemberName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.MemberId).HasMaxLength(100);
            entity.Property(x => x.RelatedEvent).HasMaxLength(200);
            entity.Property(x => x.TicketType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Priority).HasMaxLength(50).IsRequired();
            entity.Property(x => x.AssignedTo).HasMaxLength(150);
            entity.Property(x => x.RefNo).HasMaxLength(100);
            entity.Property(x => x.Utr).HasMaxLength(100);
            entity.Property(x => x.Attachment).HasMaxLength(500);
            entity.Property(x => x.ResolutionNotes).HasMaxLength(2000);
            entity.Property(x => x.CreatedBy).HasMaxLength(150);
            entity.Property(x => x.ModifiedBy).HasMaxLength(150);
            entity.HasIndex(x => x.TicketNo).IsUnique();
            entity.HasIndex(x => new { x.Status, x.Priority });
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(x => x.SettingId);
            entity.Property(x => x.SettingKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SettingValue).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.CreatedBy).HasMaxLength(150);
            entity.Property(x => x.ModifiedBy).HasMaxLength(150);
            entity.HasIndex(x => x.SettingKey).IsUnique();
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(x => x.TransactionId);
            entity.Property(x => x.TxnNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.MemberName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.EventName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.Property(x => x.PaymentMode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Utr).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.VerifiedBy).HasMaxLength(150);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.Screenshot).HasMaxLength(500);
            entity.Property(x => x.CreatedBy).HasMaxLength(150);
            entity.Property(x => x.ModifiedBy).HasMaxLength(150);
            entity.HasIndex(x => x.TxnNumber).IsUnique();
            entity.HasIndex(x => new { x.PaymentDate, x.Status });
        });

        modelBuilder.Entity<GalleryPhoto>(entity =>
        {
            entity.HasKey(x => x.PhotoId);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EventName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ImageUrl).HasColumnType("text").IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.CreatedBy).HasMaxLength(150);
            entity.Property(x => x.ModifiedBy).HasMaxLength(150);
            entity.HasIndex(x => new { x.EventName, x.Category });
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
                .OnDelete(DeleteBehavior.Cascade);
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
}
