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
            entity.HasOne(x => x.Role)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventType>(entity =>
        {
            entity.HasKey(x => x.EventTypeId);
            entity.Property(x => x.EventTypeName).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.EventTypeName).IsUnique();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
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
